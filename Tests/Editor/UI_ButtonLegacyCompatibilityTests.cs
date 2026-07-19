using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public sealed class NoTransitionButtonCompatibilityTestComponent : UI_Button
    {
        public override bool Transition => false;
    }

    public class UI_ButtonLegacyCompatibilityTests
    {
        [Test]
        public void NewImageAndButtonUseBuiltinUIMaskWithoutOverwritingExistingSprite()
        {
            var imageObject = new GameObject("DefaultMaskImage", typeof(RectTransform), typeof(CanvasRenderer));
            var buttonObject = new GameObject("DefaultMaskButton", typeof(RectTransform), typeof(CanvasRenderer));
            var preservedObject = new GameObject(
                "PreservedImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var materialObject = new GameObject(
                "MaterialDrivenImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Material customMaterial = null;
            try
            {
                imageObject.SetActive(false);
                Sprite mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
                Sprite standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                preservedObject.GetComponent<Image>().sprite = standard;
                customMaterial = new Material(Shader.Find("UI/Default"));
                materialObject.GetComponent<Image>().material = customMaterial;

                UI_Image image = imageObject.AddComponent<UI_Image>();
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                UI_Image preserved = preservedObject.AddComponent<UI_Image>();
                UI_Image materialDriven = materialObject.AddComponent<UI_Image>();

                Assert.That(mask, Is.Not.Null);
                Assert.That(image.Initialize(), Is.True);
                Assert.That(image.Initialize(), Is.False);
                imageObject.SetActive(true);
                Assert.That(image.Sprite, Is.SameAs(mask));
                Assert.That(button.Sprite, Is.SameAs(mask));
                Assert.That(preserved.Sprite, Is.SameAs(standard));
                Assert.That(materialDriven.Sprite, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(imageObject);
                UnityEngine.Object.DestroyImmediate(buttonObject);
                UnityEngine.Object.DestroyImmediate(preservedObject);
                UnityEngine.Object.DestroyImmediate(materialObject);
                if (customMaterial != null) UnityEngine.Object.DestroyImmediate(customMaterial);
            }
        }

        [Test]
        public void ImageExposesLegacyAliasesAndEditorSpriteSwapContract()
        {
            var gameObject = new GameObject("LegacyImageAliases", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                UI_Image image = gameObject.AddComponent<UI_Image>();
                var color = new Color(0.2f, 0.3f, 0.4f, 0.5f);

                Assert.That(image.SetColor(color), Is.SameAs(image));
                Assert.That(image.SetFill(0.35f), Is.SameAs(image));
                Assert.That(image.Color, Is.EqualTo(color));
                Assert.That(image.Fill, Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(image.Rect, Is.SameAs(gameObject.GetComponent<RectTransform>()));
                Assert.That(image.Material, Is.SameAs(image.Image.material));

                var serialized = new SerializedObject(image);
                Assert.That(serialized.FindProperty("_isSpriteSwapTarget"), Is.Not.Null);
                Assert.That(serialized.FindProperty("_spriteSlotKey"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ButtonCompatibilityEventsFeedbackStateAndInitializationRemainAdditive()
        {
            var buttonObject = new GameObject("LegacyButtonCompatibility", typeof(RectTransform), typeof(CanvasRenderer));
            var textObject = new GameObject("ContextText", typeof(RectTransform), typeof(CanvasRenderer));
            IUIButtonClickSoundPlayer originalSound = UIButtonServices.ClickSoundPlayer;
            IUIButtonHapticPlayer originalHaptic = UIButtonServices.HapticPlayer;
            IUIButtonStateColorTheme originalStateColors = UIButtonServices.StateColorTheme;
            try
            {
                buttonObject.SetActive(false);
                textObject.transform.SetParent(buttonObject.transform, false);
                textObject.AddComponent<TextMeshProUGUI>().font = null;
                UI_Text contextText = textObject.AddComponent<UI_Text>();
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                var sound = new CountingSoundPlayer();
                var haptic = new CountingHapticPlayer();
                var stateColors = new FixedStateColorTheme(Color.green, Color.magenta);
                UIButtonServices.ClickSoundPlayer = sound;
                UIButtonServices.HapticPlayer = haptic;
                UIButtonServices.StateColorTheme = stateColors;

                Assert.That(button.Initialize(), Is.True);
                Assert.That(button.Initialize(), Is.False);
                Assert.That(button.ContextText, Is.SameAs(contextText));
                Assert.That(button.ButtonImage, Is.SameAs(button));
                Assert.That(buttonObject.GetComponent<Button>(), Is.Null);
                Assert.That(buttonObject.GetComponent<EventTrigger>(), Is.Null);

                buttonObject.SetActive(true);
                int clickCount = 0;
                int rightClickCount = 0;
                int downCount = 0;
                int upCount = 0;
                Action rightClick = () => rightClickCount++;
                UnityEngine.Events.UnityAction click = () => clickCount++;

                button.SetEvent(click).SetEvent(click);
                button.SetRightClickEvent(rightClick).SetRightClickEvent(rightClick);
                button.OnButtonDown += () => downCount++;
                button.OnButtonUp += () => upCount++;
                button.SetDownUpButton();

                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Right));
                button.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                button.OnPointerUp(Pointer(PointerEventData.InputButton.Left));

                Assert.That(clickCount, Is.EqualTo(1));
                Assert.That(rightClickCount, Is.EqualTo(1));
                Assert.That(downCount, Is.EqualTo(1));
                Assert.That(upCount, Is.EqualTo(1));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));

                button.TabEffect = false;
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(2));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));

                button.SetActive(false);
                Assert.That(button.Interactable, Is.False);
                Assert.That(button.IsDisabled, Is.True);
                Assert.That(button.Color, Is.EqualTo(Color.magenta));
                Assert.That(contextText.Color, Is.EqualTo(Color.magenta));
                var buttonState = new SerializedObject(button);
                Assert.That(buttonState.FindProperty("m_Interactable").boolValue, Is.False);
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(2));

                button.SetActive(true);
                Assert.That(button.Interactable, Is.True);
                Assert.That(button.IsDisabled, Is.False);
                Assert.That(button.Color, Is.EqualTo(Color.green));
                Assert.That(contextText.Color, Is.EqualTo(Color.green));
                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
                buttonState.Update();
                Assert.That(buttonState.FindProperty("m_Interactable").boolValue, Is.True);
            }
            finally
            {
                UIButtonServices.ClickSoundPlayer = originalSound;
                UIButtonServices.HapticPlayer = originalHaptic;
                UIButtonServices.StateColorTheme = originalStateColors;
                UnityEngine.Object.DestroyImmediate(buttonObject);
            }
        }

        [Test]
        public void SerializedNonInteractableStartsDisabledAndTransitionCanSuppressPressScale()
        {
            var disabledObject = new GameObject("InitiallyDisabled", typeof(RectTransform), typeof(CanvasRenderer));
            var noTransitionObject = new GameObject("NoTransitionButton", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                disabledObject.SetActive(false);
                UI_Button disabledButton = disabledObject.AddComponent<UI_Button>();
                var serialized = new SerializedObject(disabledButton);
                SerializedProperty interactable = serialized.FindProperty("m_Interactable");
                interactable.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                disabledButton.Initialize();

                Assert.That(disabledButton.IsDisabled, Is.True);

                NoTransitionButtonCompatibilityTestComponent noTransition =
                    noTransitionObject.AddComponent<NoTransitionButtonCompatibilityTestComponent>();
                noTransition.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                Assert.That(noTransition.Transition, Is.False);
                Assert.That(noTransition.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(disabledObject);
                UnityEngine.Object.DestroyImmediate(noTransitionObject);
            }
        }

        [Test]
        public void RemoveAllEventsAlsoRemovesRegisteredFeedbackLikeLegacyButton()
        {
            var buttonObject = new GameObject("RemoveAllLegacyEvents", typeof(RectTransform), typeof(CanvasRenderer));
            IUIButtonClickSoundPlayer originalSound = UIButtonServices.ClickSoundPlayer;
            IUIButtonHapticPlayer originalHaptic = UIButtonServices.HapticPlayer;
            try
            {
                var sound = new CountingSoundPlayer();
                var haptic = new CountingHapticPlayer();
                UIButtonServices.ClickSoundPlayer = sound;
                UIButtonServices.HapticPlayer = haptic;
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                int clickCount = 0;

                button.SetEvent(() => clickCount++);
                button.RemoveAllEvents();
                button.SetEvent(() => clickCount++);
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));

                Assert.That(clickCount, Is.EqualTo(1));
                Assert.That(button.TabEffect, Is.True);
                Assert.That(sound.Count, Is.Zero);
                Assert.That(haptic.Count, Is.Zero);

                button.TabEffect = false;
                button.TabEffect = true;
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));
            }
            finally
            {
                UIButtonServices.ClickSoundPlayer = originalSound;
                UIButtonServices.HapticPlayer = originalHaptic;
                UnityEngine.Object.DestroyImmediate(buttonObject);
            }
        }

        private sealed class CountingSoundPlayer : IUIButtonClickSoundPlayer
        {
            public int Count { get; private set; }
            public void PlayClickSound() => Count++;
        }

        private sealed class CountingHapticPlayer : IUIButtonHapticPlayer
        {
            public int Count { get; private set; }
            public void PlayHaptic() => Count++;
        }

        private sealed class FixedStateColorTheme : IUIButtonStateColorTheme
        {
            private readonly Color _normal;
            private readonly Color _disabled;

            public FixedStateColorTheme(Color normal, Color disabled)
            {
                _normal = normal;
                _disabled = disabled;
            }

            public Color GetButtonStateColor(bool interactable) => interactable ? _normal : _disabled;
        }

        private static PointerEventData Pointer(PointerEventData.InputButton button)
        {
            return new PointerEventData(null) { button = button };
        }
    }
}
