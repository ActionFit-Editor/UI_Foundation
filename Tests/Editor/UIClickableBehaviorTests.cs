using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public sealed class NoTransitionClickableTestComponent : UI_Clickable
    {
        public override bool Transition => false;
    }

    public class UIClickableBehaviorTests
    {
        [Test]
        public void ClickableDoesNotAddGraphicOrNativeButtonAndKeepsSerializedContract()
        {
            var gameObject = new GameObject("ImageLessClickable", typeof(RectTransform));
            try
            {
                UI_Clickable clickable = gameObject.AddComponent<UI_Clickable>();
                var serialized = new SerializedObject(clickable);

                Assert.That(gameObject.GetComponent<Image>(), Is.Null);
                Assert.That(gameObject.GetComponent<Button>(), Is.Null);
                Assert.That(gameObject.GetComponent<EventTrigger>(), Is.Null);
                MonoScript script = MonoScript.FromMonoBehaviour(clickable);
                Assert.That(
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script)),
                    Is.EqualTo("6e7192ae2821476bbd56fd266e9700cc"));
                Assert.That(serialized.FindProperty("m_Interactable"), Is.Not.Null);
                Assert.That(serialized.FindProperty("m_OnClick"), Is.Not.Null);
                Assert.That(serialized.FindProperty("usePressEffect"), Is.Not.Null);
                Assert.That(serialized.FindProperty("scaleDownRatio"), Is.Not.Null);
                Assert.That(serialized.FindProperty("isScaleOneFix"), Is.Not.Null);
                Assert.That(serialized.FindProperty("targetTransform"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ClickRightClickAndOptInDownUpRespectCompatibilityContract()
        {
            var gameObject = new GameObject("ClickableEvents", typeof(RectTransform));
            IUIButtonClickSoundPlayer originalSound = UIButtonServices.ClickSoundPlayer;
            IUIButtonHapticPlayer originalHaptic = UIButtonServices.HapticPlayer;
            try
            {
                UI_Clickable clickable = gameObject.AddComponent<UI_Clickable>();
                var sound = new CountingSoundPlayer();
                var haptic = new CountingHapticPlayer();
                UIButtonServices.ClickSoundPlayer = sound;
                UIButtonServices.HapticPlayer = haptic;
                int clickCount = 0;
                int rightClickCount = 0;
                int downCount = 0;
                int upCount = 0;
                UnityAction clickAction = () => clickCount++;
                Action rightClick = () => rightClickCount++;

                clickable.SetEvent(clickAction).SetEvent(clickAction);
                clickable.SetRightClickEvent(rightClick).SetRightClickEvent(rightClick);
                clickable.OnButtonDown += () => downCount++;
                clickable.OnButtonUp += () => upCount++;

                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Right));
                Assert.That(clickCount, Is.EqualTo(1));
                Assert.That(rightClickCount, Is.EqualTo(1));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));

                clickable.TabEffect = false;
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));
                Assert.That(clickCount, Is.EqualTo(2));
                clickable.RemoveEvent(clickAction);
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(2));
                clickable.SetEvent(clickAction);

                clickable.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                clickable.OnPointerUp(Pointer(PointerEventData.InputButton.Left));
                Assert.That(downCount, Is.Zero);
                Assert.That(upCount, Is.Zero);

                clickable.SetDownUpButton();
                clickable.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                Assert.That(gameObject.transform.localScale, Is.EqualTo(Vector3.one * 0.95f));
                Assert.That(downCount, Is.EqualTo(1));
                clickable.OnPointerUp(Pointer(PointerEventData.InputButton.Left));
                Assert.That(upCount, Is.EqualTo(1));

                clickable.SetActive(false);
                Assert.That(gameObject.transform.localScale, Is.EqualTo(Vector3.one));
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(2));

                clickable.RemoveRightClickEvent(rightClick);
                clickable.SetActive(true, false);
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Right));
                Assert.That(rightClickCount, Is.EqualTo(1));
                clickable.TabEffect = true;
                clickable.RemoveAllEvents();
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(2));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));

                clickable.TabEffect = true;
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(sound.Count, Is.EqualTo(1));
                Assert.That(haptic.Count, Is.EqualTo(1));

                clickable.TabEffect = false;
                clickable.TabEffect = true;
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(sound.Count, Is.EqualTo(2));
                Assert.That(haptic.Count, Is.EqualTo(2));
            }
            finally
            {
                UIButtonServices.ClickSoundPlayer = originalSound;
                UIButtonServices.HapticPlayer = originalHaptic;
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ClickableHonorsCanvasGroupsAndVirtualTransition()
        {
            var outer = new GameObject("Outer", typeof(RectTransform), typeof(CanvasGroup));
            var bridge = new GameObject("Bridge", typeof(RectTransform), typeof(CanvasGroup));
            var clickableObject = new GameObject("Clickable", typeof(RectTransform));
            var noTransitionObject = new GameObject("NoTransition", typeof(RectTransform));
            try
            {
                bridge.transform.SetParent(outer.transform, false);
                clickableObject.transform.SetParent(bridge.transform, false);
                UI_Clickable clickable = clickableObject.AddComponent<UI_Clickable>();
                NoTransitionClickableTestComponent noTransition =
                    noTransitionObject.AddComponent<NoTransitionClickableTestComponent>();
                CanvasGroup outerGroup = outer.GetComponent<CanvasGroup>();
                CanvasGroup bridgeGroup = bridge.GetComponent<CanvasGroup>();
                int clickCount = 0;
                clickable.SetEvent(() => clickCount++);

                outerGroup.interactable = false;
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.Zero);

                outerGroup.interactable = true;
                outerGroup.blocksRaycasts = false;
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.Zero);

                outerGroup.interactable = false;
                outerGroup.blocksRaycasts = true;
                bridgeGroup.ignoreParentGroups = true;
                clickable.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickCount, Is.EqualTo(1));

                clickable.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                Assert.That(clickable.transform.localScale, Is.EqualTo(Vector3.one * 0.95f));
                clickable.SetActive(false);
                Assert.That(clickable.transform.localScale, Is.EqualTo(Vector3.one));

                noTransition.OnPointerDown(Pointer(PointerEventData.InputButton.Left));
                Assert.That(noTransition.Transition, Is.False);
                Assert.That(noTransition.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(outer);
                UnityEngine.Object.DestroyImmediate(noTransitionObject);
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

        private static PointerEventData Pointer(PointerEventData.InputButton button)
        {
            return new PointerEventData(null) { button = button };
        }
    }
}
