using System;
using NUnit.Framework;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public class UIEaseCompatibilityTests
    {
        private static readonly string[] ExpectedNames =
        {
            "Unset", "Linear", "InSine", "OutSine", "InOutSine", "InQuad", "OutQuad", "InOutQuad",
            "InCubic", "OutCubic", "InOutCubic", "InQuart", "OutQuart", "InOutQuart", "InQuint",
            "OutQuint", "InOutQuint", "InExpo", "OutExpo", "InOutExpo", "InCirc", "OutCirc",
            "InOutCirc", "InElastic", "OutElastic", "InOutElastic", "InBack", "OutBack", "InOutBack",
            "InBounce", "OutBounce", "InOutBounce", "Flash", "InFlash", "OutFlash", "InOutFlash"
        };

        [Test]
        public void EnumNamesAndNumericValuesMatchDotweenContract()
        {
            string[] names = Enum.GetNames(typeof(UIEase));
            Array values = Enum.GetValues(typeof(UIEase));
            Assert.That(names, Is.EqualTo(ExpectedNames));
            Assert.That(values.Length, Is.EqualTo(ExpectedNames.Length));
            for (int index = 0; index < ExpectedNames.Length; index++)
                Assert.That((int)values.GetValue(index), Is.EqualTo(index), ExpectedNames[index]);
        }

        [Test]
        public void EveryEaseHasStableFiniteEndpoints()
        {
            foreach (UIEase ease in Enum.GetValues(typeof(UIEase)))
            {
                Assert.That(UIEaseUtility.Evaluate(ease, 0f), Is.EqualTo(0f).Within(0.00001f), ease.ToString());
                Assert.That(UIEaseUtility.Evaluate(ease, 1f), Is.EqualTo(1f).Within(0.00001f), ease.ToString());
                float midpoint = UIEaseUtility.Evaluate(ease, 0.5f);
                Assert.That(float.IsNaN(midpoint) || float.IsInfinity(midpoint), Is.False, ease.ToString());
            }
        }

        [TestCase(UIEase.Unset)]
        [TestCase(UIEase.Flash)]
        [TestCase(UIEase.InFlash)]
        [TestCase(UIEase.OutFlash)]
        [TestCase(UIEase.InOutFlash)]
        public void UnsupportedEaseSlotsUseLinearFallback(UIEase ease)
        {
            Assert.That(UIEaseUtility.Evaluate(ease, 0.25f), Is.EqualTo(0.25f).Within(0.00001f));
        }

        [Test]
        public void UnknownValuesUseLinearFallbackAndProgressIsClamped()
        {
            Assert.That(UIEaseUtility.Evaluate((UIEase)999, 0.4f), Is.EqualTo(0.4f).Within(0.00001f));
            Assert.That(UIEaseUtility.Evaluate(UIEase.Linear, -10f), Is.EqualTo(0f));
            Assert.That(UIEaseUtility.Evaluate(UIEase.Linear, 10f), Is.EqualTo(1f));
        }
    }
}
