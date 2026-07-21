using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace ActionFit.UIFoundation.Runtime.Tests
{
    public class UIRuntimeContractTests
    {
        [Test]
        public void PublicRuntimeTypesComeFromFoundationAssembly()
        {
            Assert.That(typeof(UI_Rect).Assembly.GetName().Name, Is.EqualTo("com.actionfit.ui.foundation"));
            Assert.That(typeof(UI_Text).Assembly, Is.EqualTo(typeof(UI_Button).Assembly));
            Assert.That(typeof(Image_Slice).Assembly, Is.EqualTo(typeof(UI_Button).Assembly));
        }

        [Test]
        public void RectWrapperResolvesRequiredRectTransform()
        {
            var gameObject = new GameObject("UIRuntimeContract", typeof(RectTransform), typeof(UI_Rect));
            try
            {
                UI_Rect wrapper = gameObject.GetComponent<UI_Rect>();
                Assert.That(wrapper.RectTransform, Is.SameAs(gameObject.transform));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LinearEaseClampsNormalizedProgress()
        {
            Assert.That(UIEaseUtility.Evaluate(UIEase.Linear, -1f), Is.EqualTo(0f));
            Assert.That(UIEaseUtility.Evaluate(UIEase.Linear, 0.5f), Is.EqualTo(0.5f));
            Assert.That(UIEaseUtility.Evaluate(UIEase.Linear, 2f), Is.EqualTo(1f));
        }

        [Test]
        public void TextLocalizationArgumentsAreStoredAndSetterIsChainable()
        {
            var gameObject = new GameObject(
                "LocalizedText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(UI_Text));
            try
            {
                UI_Text text = gameObject.GetComponent<UI_Text>();

                Assert.That(text.SetLocalizeArguments(7, "levels"), Is.SameAs(text));

                FieldInfo field = typeof(UI_Text).GetField(
                    "localizedString",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                var localizedString = (LocalizedString)field.GetValue(text);
                Assert.That(localizedString, Is.Not.Null);
                Assert.That(localizedString.Arguments, Is.EqualTo(new object[] { 7, "levels" }));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
