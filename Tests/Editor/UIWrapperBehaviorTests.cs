using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public class UIWrapperBehaviorTests
    {
        [Test]
        public void ButtonListenersDisableStateSoundAndThemeAreProjectAgnostic()
        {
            var gameObject = new GameObject("ButtonContract", typeof(RectTransform), typeof(CanvasRenderer));
            Texture2D texture = null;
            Sprite sprite = null;
            IUIButtonClickSoundPlayer originalSound = UIButtonServices.ClickSoundPlayer;
            IUIButtonTheme originalTheme = UIButtonServices.Theme;
            try
            {
                var button = gameObject.AddComponent<UI_Button>();
                var sound = new CountingSoundPlayer();
                texture = new Texture2D(2, 2);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
                UIButtonServices.ClickSoundPlayer = sound;
                UIButtonServices.Theme = new FixedTheme(sprite);

                Assert.That(gameObject.GetComponent<Button>(), Is.Null);
                Assert.That(gameObject.GetComponent<UIButtonPressEffect>(), Is.Null);

                MethodInfo registerSound = typeof(UI_Button).GetMethod(
                    "RegisterClickSound",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(registerSound, Is.Not.Null);
                registerSound.Invoke(button, null);

                int callbackCount = 0;
                button.AddListener(() => callbackCount++);
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(sound.Count, Is.EqualTo(1));

                button.SetDisable();
                Assert.That(button.IsDisabled, Is.True);
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(callbackCount, Is.EqualTo(1));
                button.SetEnable();
                Assert.That(button.IsDisabled, Is.False);
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Right));
                Assert.That(callbackCount, Is.EqualTo(1));
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(callbackCount, Is.EqualTo(2));
                Assert.That(sound.Count, Is.EqualTo(2));

                button.SetButtonSprite(UI_Button.ButtonSprite.Green);
                Assert.That(button.Sprite, Is.SameAs(sprite));
            }
            finally
            {
                UIButtonServices.ClickSoundPlayer = originalSound;
                UIButtonServices.Theme = originalTheme;
                Object.DestroyImmediate(gameObject);
                if (sprite != null) Object.DestroyImmediate(sprite);
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ButtonHonorsCanvasGroupsAndRestoresPressScaleAcrossPointerTransitions()
        {
            var outer = new GameObject("Outer", typeof(RectTransform), typeof(CanvasGroup));
            var bridge = new GameObject("Bridge", typeof(RectTransform), typeof(CanvasGroup));
            var buttonObject = new GameObject("PointerButton", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                bridge.transform.SetParent(outer.transform, false);
                buttonObject.transform.SetParent(bridge.transform, false);
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                var outerGroup = outer.GetComponent<CanvasGroup>();
                var bridgeGroup = bridge.GetComponent<CanvasGroup>();
                int clickCount = 0;
                button.AddListener(() => clickCount++);

                outerGroup.interactable = false;
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.Zero);

                outerGroup.interactable = true;
                outerGroup.blocksRaycasts = false;
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.Zero);

                outerGroup.interactable = false;
                outerGroup.blocksRaycasts = true;
                bridgeGroup.ignoreParentGroups = true;
                button.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(1));

                button.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one * 0.95f));
                button.OnPointerExit(Pointer(PointerEventData.InputButton.Left));
                button.OnPointerEnter(Pointer(PointerEventData.InputButton.Left));
                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one * 0.95f));

                button.SetDisable();
                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(outer);
            }
        }

        [Test]
        public void ButtonEnableWithoutOwnedTextOverridePreservesCurrentMaterial()
        {
            var buttonObject = new GameObject("TextMaterialButton", typeof(RectTransform), typeof(CanvasRenderer));
            var textObject = new GameObject("TargetText", typeof(RectTransform), typeof(CanvasRenderer));
            Material initialMaterial = null;
            Material outlinedMaterial = null;
            try
            {
                textObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI targetText = textObject.AddComponent<TextMeshProUGUI>();
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                ConfigureDisableTextColor(button, targetText, null);

                initialMaterial = CreateTestMaterial("Initial");
                outlinedMaterial = CreateTestMaterial("Outlined");
                targetText.fontSharedMaterial = initialMaterial;
                InvokeNonPublic(button, "Awake");

                // UI_Text.OnEnable이 버튼 초기화 이후 런타임 outline 재질을 적용한 상황을 재현한다.
                targetText.fontSharedMaterial = outlinedMaterial;
                button.SetEnable();

                Assert.That(targetText.fontSharedMaterial, Is.SameAs(outlinedMaterial));
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
                if (outlinedMaterial != null) Object.DestroyImmediate(outlinedMaterial);
                if (initialMaterial != null) Object.DestroyImmediate(initialMaterial);
            }
        }

        [Test]
        public void ButtonDisableTextColorRestoresExactCurrentMaterialAndIsIdempotent()
        {
            var buttonObject = new GameObject("TextMaterialButton", typeof(RectTransform), typeof(CanvasRenderer));
            var textObject = new GameObject("TargetText", typeof(RectTransform), typeof(CanvasRenderer));
            Material outlinedMaterial = null;
            Material disabledMaterial = null;
            try
            {
                textObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI targetText = textObject.AddComponent<TextMeshProUGUI>();
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                ConfigureDisableTextColor(button, targetText, null);

                outlinedMaterial = CreateTestMaterial("Outlined");
                targetText.fontSharedMaterial = outlinedMaterial;
                targetText.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);
                Color vertexColor = targetText.color;

                Assert.DoesNotThrow(button.SetDisable);
                disabledMaterial = targetText.fontSharedMaterial;
                Assert.That(disabledMaterial, Is.Not.SameAs(outlinedMaterial));
                Assert.That(targetText.color, Is.EqualTo(vertexColor));

                Assert.DoesNotThrow(button.SetDisable);
                Assert.That(targetText.fontSharedMaterial, Is.SameAs(disabledMaterial));

                Assert.DoesNotThrow(button.SetEnable);
                Assert.That(targetText.fontSharedMaterial, Is.SameAs(outlinedMaterial));
                Assert.That(targetText.color, Is.EqualTo(vertexColor));

                Assert.DoesNotThrow(button.SetEnable);
                Assert.That(targetText.fontSharedMaterial, Is.SameAs(outlinedMaterial));
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
                if (disabledMaterial != null && !ReferenceEquals(disabledMaterial, outlinedMaterial))
                    Object.DestroyImmediate(disabledMaterial);
                if (outlinedMaterial != null) Object.DestroyImmediate(outlinedMaterial);
            }
        }

        [Test]
        public void ButtonRestoresOnlyItsOwnedTextMaterialAssignment()
        {
            var buttonObject = new GameObject("TextMaterialButton", typeof(RectTransform), typeof(CanvasRenderer));
            var textObject = new GameObject("TargetText", typeof(RectTransform), typeof(CanvasRenderer));
            Material outlinedMaterial = null;
            Material replacementMaterial = null;
            Material disabledMaterial = null;
            try
            {
                textObject.transform.SetParent(buttonObject.transform, false);
                TextMeshProUGUI targetText = textObject.AddComponent<TextMeshProUGUI>();
                UI_Button button = buttonObject.AddComponent<UI_Button>();
                ConfigureDisableTextColor(button, targetText);

                outlinedMaterial = CreateTestMaterial("Outlined");
                replacementMaterial = CreateTestMaterial("Replacement");
                targetText.fontSharedMaterial = outlinedMaterial;
                button.SetDisable();
                disabledMaterial = targetText.fontSharedMaterial;

                targetText.fontSharedMaterial = replacementMaterial;
                button.SetEnable();

                Assert.That(targetText.fontSharedMaterial, Is.SameAs(replacementMaterial));

                targetText.fontSharedMaterial = outlinedMaterial;
                button.SetDisable();
                InvokeNonPublic(button, "OnDisable");
                Assert.That(targetText.fontSharedMaterial, Is.SameAs(outlinedMaterial));

                button.SetDisable();
                Assert.That(targetText.fontSharedMaterial, Is.SameAs(disabledMaterial));
                button.SetEnable();
                Assert.That(targetText.fontSharedMaterial, Is.SameAs(outlinedMaterial));
            }
            finally
            {
                Object.DestroyImmediate(buttonObject);
                if (disabledMaterial != null && !ReferenceEquals(disabledMaterial, outlinedMaterial))
                    Object.DestroyImmediate(disabledMaterial);
                if (replacementMaterial != null) Object.DestroyImmediate(replacementMaterial);
                if (outlinedMaterial != null) Object.DestroyImmediate(outlinedMaterial);
            }
        }

        [Test]
        public void ImageTextScrollAndMaskExposeStableWrapperBehavior()
        {
            var imageObject = new GameObject("ImageContract", typeof(RectTransform), typeof(CanvasRenderer));
            var textObject = new GameObject("TextContract", typeof(RectTransform), typeof(CanvasRenderer));
            var scrollObject = new GameObject("ScrollContract", typeof(RectTransform));
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer));
            var contentObject = new GameObject("Content", typeof(RectTransform));
            var maskObject = new GameObject("MaskContract", typeof(RectTransform));
            try
            {
                UI_Image image = imageObject.AddComponent<UI_Image>();
                image.Color = new Color(0.2f, 0.4f, 0.6f, 1f);
                image.Alpha = 0.3f;
                Assert.That(image.Color.a, Is.EqualTo(0.3f).Within(0.0001f));

                textObject.AddComponent<TextMeshProUGUI>().font = null;
                UI_Text text = textObject.AddComponent<UI_Text>();
                text.Text = "Foundation";
                text.SetSize(24).SetColor(Color.cyan);
                Assert.That(text.Text, Is.EqualTo("Foundation"));
                Assert.That(text.TMP.fontSize, Is.EqualTo(24f));
                Assert.That(text.Color, Is.EqualTo(Color.cyan));

                UI_Scroll scroll = scrollObject.AddComponent<UI_Scroll>();
                viewportObject.transform.SetParent(scrollObject.transform, false);
                contentObject.transform.SetParent(viewportObject.transform, false);
                UI_Image viewport = viewportObject.AddComponent<UI_Image>();
                UI_Rect content = contentObject.AddComponent<UI_Rect>();
                viewport.RectTransform.sizeDelta = new Vector2(100f, 100f);
                content.RectTransform.sizeDelta = new Vector2(100f, 300f);
                scroll.refs = new UI_Scroll.Refs { imgViewMask = viewport, rectContent = content };
                scroll.Viewport = viewport.RectTransform;
                scroll.Content = content.RectTransform;
                scroll.VerticalNormalizedPosition = 0.75f;
                scroll.SnapToBottom();
                Assert.That(scroll.VerticalNormalizedPosition, Is.EqualTo(0f).Within(0.0001f));

                UI_Mask2D mask = maskObject.AddComponent<UI_Mask2D>();
                mask.RectTransform.sizeDelta = new Vector2(10f, 10f);
                // The project's DOTWEEN define changes AnimHeight's return type to DG.Tweening.Tween.
                // Invoke by reflection so this package test remains independent of optional DOTween.
                MethodInfo animHeight = typeof(UI_MaskBase).GetMethod(
                    "AnimHeight",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.That(animHeight, Is.Not.Null);
                animHeight.Invoke(mask, new object[] { 25f, 0f, null });
                Assert.That(mask.RectTransform.sizeDelta.y, Is.EqualTo(25f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(imageObject);
                Object.DestroyImmediate(textObject);
                Object.DestroyImmediate(scrollObject);
                Object.DestroyImmediate(maskObject);
            }
        }

        [Test]
        public void TextSupportsNamedInlineSpriteTags()
        {
            var textObject = new GameObject("TextSpriteContract", typeof(RectTransform), typeof(CanvasRenderer));
            TMP_SpriteAsset spriteAsset = null;
            try
            {
                textObject.AddComponent<TextMeshProUGUI>().font = null;
                UI_Text text = textObject.AddComponent<UI_Text>();
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

                text.SetSpriteAsset(spriteAsset)
                    .SetTextWithSprite("Reward ", "coin", " 100", true);

                Assert.That(text.SpriteAsset, Is.SameAs(spriteAsset));
                Assert.That(text.Text, Is.EqualTo("Reward <sprite name=\"coin\" tint=1> 100"));
                Assert.That(text.TMP.richText, Is.True);
                Assert.That(UI_Text.BuildSpriteTag(2), Is.EqualTo("<sprite=2>"));
            }
            finally
            {
                Object.DestroyImmediate(textObject);
                if (spriteAsset != null) Object.DestroyImmediate(spriteAsset);
            }
        }

        [Test]
        public void TextUsesExpectedOutlineAndUnderlayDefaults()
        {
            var textObject = new GameObject("TextMaterialDefaults", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                textObject.AddComponent<TextMeshProUGUI>().font = null;
                UI_Text text = textObject.AddComponent<UI_Text>();
                var serialized = new SerializedObject(text);

                Assert.That(serialized.FindProperty("outlineWidth").floatValue, Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(serialized.FindProperty("underlayOffsetY").floatValue, Is.EqualTo(-0.5f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(textObject);
            }
        }

        private sealed class CountingSoundPlayer : IUIButtonClickSoundPlayer
        {
            public int Count { get; private set; }
            public void PlayClickSound() => Count++;
        }

        private sealed class FixedTheme : IUIButtonTheme
        {
            private readonly Sprite _sprite;
            public FixedTheme(Sprite sprite) => _sprite = sprite;
            public Sprite GetButtonSprite(UI_Button.ButtonSprite preset) => _sprite;
        }

        private static void ConfigureDisableTextColor(UI_Button button, params TextMeshProUGUI[] targetTexts)
        {
            var serialized = new SerializedObject(button);
            serialized.FindProperty("useDisableTextColor").boolValue = true;
            serialized.FindProperty("useEnableAnimation").boolValue = false;

            SerializedProperty targets = serialized.FindProperty("targetTexts");
            targets.arraySize = targetTexts.Length;
            for (int i = 0; i < targetTexts.Length; i++)
                targets.GetArrayElementAtIndex(i).objectReferenceValue = targetTexts[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateTestMaterial(string materialName)
        {
            Shader shader = Shader.Find("UI/Default") ?? Shader.Find("Hidden/InternalErrorShader");
            Assert.That(shader, Is.Not.Null);
            return new Material(shader) { name = materialName };
        }

        private static void InvokeNonPublic(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static PointerEventData Pointer(PointerEventData.InputButton button)
        {
            return new PointerEventData(null) { button = button };
        }
    }
}
