using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public sealed class LegacyTextInitializationTestComponent : UI_Text
    {
        public int CompatibilityInitializeCount { get; private set; }

        public override bool Initialize()
        {
            if (!base.Initialize()) return false;
            CompatibilityInitializeCount++;
            return true;
        }
    }

    public sealed class LegacyImageProtectedFieldTestComponent : UI_Image
    {
        public Image CachedImage => _image;
    }

    public class UIWrapperBehaviorTests
    {
        [Test]
        public void ImageKeepsLegacyProtectedImageCacheContract()
        {
            var imageObject = new GameObject("LegacyDerivedImage", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                imageObject.SetActive(false);
                LegacyImageProtectedFieldTestComponent image =
                    imageObject.AddComponent<LegacyImageProtectedFieldTestComponent>();

                Assert.That(image.Initialize(), Is.True);
                Assert.That(image.CachedImage, Is.SameAs(imageObject.GetComponent<Image>()));
            }
            finally
            {
                Object.DestroyImmediate(imageObject);
            }
        }

        [Test]
        public void TextCacheDoesNotConsumeLegacyDerivedInitializationGate()
        {
            var textObject = new GameObject("LegacyDerivedText", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                textObject.SetActive(false);
                textObject.AddComponent<TextMeshProUGUI>().font = null;
                LegacyTextInitializationTestComponent text =
                    textObject.AddComponent<LegacyTextInitializationTestComponent>();

                Assert.That(text.TMP, Is.SameAs(textObject.GetComponent<TextMeshProUGUI>()));
                Assert.That(text.CompatibilityInitializeCount, Is.Zero);

                MethodInfo onEnable = typeof(UI_Text).GetMethod(
                    "OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(onEnable, Is.Not.Null);
                onEnable.Invoke(text, null);

                Assert.That(text.CompatibilityInitializeCount, Is.EqualTo(1));
                Assert.That(text.Initialize(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(textObject);
            }
        }

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
                textObject.SetActive(false);
                UI_Image image = imageObject.AddComponent<UI_Image>();
                image.Color = new Color(0.2f, 0.4f, 0.6f, 1f);
                image.Alpha = 0.3f;
                Assert.That(image.Color.a, Is.EqualTo(0.3f).Within(0.0001f));

                textObject.AddComponent<TextMeshProUGUI>().font = null;
                UI_Text text = textObject.AddComponent<UI_Text>();
                Assert.That(text.Initialize(), Is.True);
                Assert.That(text.Initialize(), Is.False);
                textObject.SetActive(true);
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

        private static PointerEventData Pointer(PointerEventData.InputButton button)
        {
            return new PointerEventData(null) { button = button };
        }
    }
}
