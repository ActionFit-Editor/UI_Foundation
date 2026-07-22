using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ActionFit.UIFoundation.Runtime.Tests
{
    public sealed class ScalePulseTests
    {
        private readonly List<GameObject> _objects = new();

        [SetUp]
        public void SetUp()
        {
            ScalePulseScheduler.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject target in _objects)
            {
                if (target != null) Object.DestroyImmediate(target);
            }

            _objects.Clear();
            ScalePulseScheduler.ResetForTests();
        }

        [Test]
        public void EmptyRegistryIsDormant()
        {
            Assert.That(ScalePulseScheduler.ActiveCount, Is.Zero);
            Assert.That(ScalePulseScheduler.IsRunning, Is.False);
        }

        [Test]
        public void FirstRegistrationStartsAtMinimumAndLastDisableRestoresBaseline()
        {
            Vector3 baseline = new(2f, 3f, 4f);
            ScalePulse pulse = CreatePulse("First", baseline);

            pulse.gameObject.SetActive(true);

            Assert.That(ScalePulseScheduler.ActiveCount, Is.EqualTo(1));
            Assert.That(ScalePulseScheduler.IsRunning, Is.True);
            AssertVector(pulse.transform.localScale, baseline * ScalePulseScheduler.MinimumScaleRatio);

            pulse.gameObject.SetActive(false);

            Assert.That(ScalePulseScheduler.ActiveCount, Is.Zero);
            Assert.That(ScalePulseScheduler.IsRunning, Is.False);
            AssertVector(pulse.transform.localScale, baseline);
        }

        [Test]
        public void LaterRegistrationJoinsCurrentPhaseWithoutRestartingExistingPulse()
        {
            Vector3 firstBaseline = new(2f, 2f, 2f);
            ScalePulse first = CreatePulse("First", firstBaseline);
            first.gameObject.SetActive(true);
            ScalePulseScheduler.AdvanceForTests(0.1f);
            float joinedRatio = ScalePulseScheduler.CurrentRatio;
            Vector3 firstBeforeJoin = first.transform.localScale;

            Vector3 secondBaseline = new(1f, 2f, 3f);
            ScalePulse second = CreatePulse("Second", secondBaseline);
            second.gameObject.SetActive(true);

            Assert.That(ScalePulseScheduler.ActiveCount, Is.EqualTo(2));
            Assert.That(ScalePulseScheduler.CurrentRatio, Is.EqualTo(joinedRatio));
            AssertVector(first.transform.localScale, firstBeforeJoin);
            AssertVector(second.transform.localScale, secondBaseline * joinedRatio);
        }

        [Test]
        public void ZeroDeltaTimeKeepsCurrentPhase()
        {
            ScalePulse pulse = CreatePulse("Paused", Vector3.one);
            pulse.gameObject.SetActive(true);
            ScalePulseScheduler.AdvanceForTests(0.1f);
            float ratio = ScalePulseScheduler.CurrentRatio;

            ScalePulseScheduler.AdvanceForTests(0f);

            Assert.That(ScalePulseScheduler.CurrentRatio, Is.EqualTo(ratio));
            AssertVector(pulse.transform.localScale, Vector3.one * ratio);
        }

        [Test]
        public void RepeatedEnableDisableDoesNotDuplicateRegistration()
        {
            ScalePulse pulse = CreatePulse("Repeated", Vector3.one);

            pulse.gameObject.SetActive(true);
            pulse.gameObject.SetActive(true);
            Assert.That(ScalePulseScheduler.ActiveCount, Is.EqualTo(1));

            pulse.gameObject.SetActive(false);
            pulse.gameObject.SetActive(false);
            Assert.That(ScalePulseScheduler.ActiveCount, Is.Zero);
        }

        private ScalePulse CreatePulse(string name, Vector3 baseline)
        {
            var target = new GameObject(name);
            target.SetActive(false);
            target.transform.localScale = baseline;
            ScalePulse pulse = target.AddComponent<ScalePulse>();
            _objects.Add(target);
            return pulse;
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }
}
