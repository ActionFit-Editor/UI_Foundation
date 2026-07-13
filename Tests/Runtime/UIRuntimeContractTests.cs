using NUnit.Framework;
using UnityEngine;

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
    }
}
