using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public sealed class UIScrollLegacyCompatibilityTests
    {
        [Test]
        public void InitializeCachesLegacyProtectedMembersAndEnabledState()
        {
            GameObject scrollObject = CreateScrollHierarchy(out RectTransform content, out _, false);
            try
            {
                UI_Scroll scroll = scrollObject.GetComponent<UI_Scroll>();
                ScrollRect nativeScroll = scrollObject.GetComponent<ScrollRect>();

                Assert.That(scroll.Initialize(), Is.True);
                Assert.That(scroll.Initialize(), Is.False);
                Assert.That(GetProtectedField<ScrollRect>(scroll, "_scrollRect"), Is.SameAs(nativeScroll));
                Assert.That(GetProtectedField<RectTransform>(scroll, "_content"), Is.SameAs(content));
                Assert.That(GetProtectedField<LayoutGroup>(scroll, "_layoutGroup"), Is.SameAs(content.GetComponent<LayoutGroup>()));

                scroll.Enabled = false;
                Assert.That(scroll.Enabled, Is.False);
                Assert.That(nativeScroll.enabled, Is.False);

                MethodInfo initialize = typeof(UI_Scroll).GetMethod(nameof(UI_Scroll.Initialize));
                MethodInfo onEnable = typeof(UI_Scroll).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo onDisable = typeof(UI_Scroll).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(initialize?.IsVirtual, Is.True);
                Assert.That(onEnable?.IsVirtual, Is.True);
                Assert.That(onDisable?.IsVirtual, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(scrollObject);
            }
        }

        [Test]
        public void LegacyScrollEndpointsAndLayoutMethodsRemainFluent()
        {
            GameObject scrollObject = CreateScrollHierarchy(out _, out _);
            try
            {
                UI_Scroll scroll = scrollObject.GetComponent<UI_Scroll>();

                Assert.That(scroll.ScrollToTop(), Is.SameAs(scroll));
                Assert.That(scroll.VerticalNormalizedPosition, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(scroll.ScrollToBottom(), Is.SameAs(scroll));
                Assert.That(scroll.VerticalNormalizedPosition, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(scroll.ScrollToLeft(), Is.SameAs(scroll));
                Assert.That(scroll.HorizontalNormalizedPosition, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(scroll.ScrollToRight(), Is.SameAs(scroll));
                Assert.That(scroll.HorizontalNormalizedPosition, Is.EqualTo(1f).Within(0.0001f));

                Assert.That(InvokeDurationScroll(scroll, nameof(UI_Scroll.ScrollToTop), 0f, "OutQuad"), Is.SameAs(scroll));
                Assert.That(scroll.VerticalNormalizedPosition, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(InvokeDurationScroll(scroll, nameof(UI_Scroll.ScrollToBottom), 0f, "OutQuad"), Is.SameAs(scroll));
                Assert.That(scroll.VerticalNormalizedPosition, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(InvokeDurationScroll(scroll, nameof(UI_Scroll.ScrollToLeft), 0f, "OutQuad"), Is.SameAs(scroll));
                Assert.That(scroll.HorizontalNormalizedPosition, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(InvokeDurationScroll(scroll, nameof(UI_Scroll.ScrollToRight), 0f, "OutQuad"), Is.SameAs(scroll));
                Assert.That(scroll.HorizontalNormalizedPosition, Is.EqualTo(1f).Within(0.0001f));

                Assert.That(scroll.RefreshLayout(), Is.SameAs(scroll));
                Assert.That(scroll.RefreshLayoutDelayed(), Is.SameAs(scroll));
                Assert.DoesNotThrow(scroll.StopScroll);
            }
            finally
            {
                Object.DestroyImmediate(scrollObject);
            }
        }

        [Test]
        public void ChildScrollUsesLegacyPaddingFormulaAndOwnRectAsViewportFallback()
        {
            GameObject scrollObject = CreateScrollHierarchy(out RectTransform content, out RectTransform child);
            try
            {
                UI_Scroll scroll = scrollObject.GetComponent<UI_Scroll>();
                Assert.That(scroll.Viewport, Is.Null);

                Assert.That(InvokeChildScroll(scroll, nameof(UI_Scroll.ScrollToChild), child, 0f, "OutBack"), Is.SameAs(scroll));
                Assert.That(scroll.VerticalNormalizedPosition, Is.EqualTo(0.6f).Within(0.0001f));

                Assert.That(InvokeChildScroll(scroll, nameof(UI_Scroll.ScrollToChildHorizontal), child, 0f, "OutBack"), Is.SameAs(scroll));
                Assert.That(scroll.HorizontalNormalizedPosition, Is.EqualTo(0.4f).Within(0.0001f));

                content.sizeDelta = new Vector2(50f, 50f);
                Assert.That(
                    InvokeChildTarget(scroll, "GetVerticalNormalizedPosition", child),
                    Is.EqualTo(1f).Within(0.0001f));
                Assert.That(
                    InvokeChildTarget(scroll, "GetHorizontalNormalizedPosition", child),
                    Is.EqualTo(0f).Within(0.0001f));

                // ScrollRect normalizes undersized content from its bounds and pivot, so its public
                // getter is not a stable echo of the requested target. Verify the legacy target
                // calculation above and keep the fluent calls covered without asserting that echo.
                Assert.That(
                    InvokeChildScroll(scroll, nameof(UI_Scroll.ScrollToChild), child, 0f, "OutBack"),
                    Is.SameAs(scroll));
                Assert.That(
                    InvokeChildScroll(scroll, nameof(UI_Scroll.ScrollToChildHorizontal), child, 0f, "OutBack"),
                    Is.SameAs(scroll));
            }
            finally
            {
                Object.DestroyImmediate(scrollObject);
            }
        }

        private static GameObject CreateScrollHierarchy(
            out RectTransform content,
            out RectTransform child,
            bool activate = true)
        {
            var scrollObject = new GameObject("LegacyScrollContract", typeof(RectTransform));
            scrollObject.SetActive(false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.sizeDelta = new Vector2(100f, 100f);

            UI_Scroll scroll = scrollObject.AddComponent<UI_Scroll>();
            ScrollRect nativeScroll = scrollObject.GetComponent<ScrollRect>();

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentObject.transform.SetParent(scrollObject.transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.sizeDelta = new Vector2(300f, 300f);
            var layoutGroup = contentObject.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 0, 10, 0);
            layoutGroup.enabled = false; // Preserve the authored child position while exercising legacy padding math.

            var childObject = new GameObject("Child", typeof(RectTransform));
            childObject.transform.SetParent(content, false);
            child = childObject.GetComponent<RectTransform>();
            child.sizeDelta = new Vector2(20f, 20f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.localPosition = new Vector3(100f, -100f, 0f);

            nativeScroll.content = content;
            nativeScroll.viewport = null;
            if (activate) scrollObject.SetActive(true);
            return scrollObject;
        }

        private static T GetProtectedField<T>(UI_Scroll owner, string fieldName) where T : Object
        {
            FieldInfo field = typeof(UI_Scroll).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            Assert.That(field.IsFamily, Is.True, fieldName);
            return field.GetValue(owner) as T;
        }

        private static UI_Scroll InvokeDurationScroll(
            UI_Scroll owner,
            string methodName,
            float duration,
            string easeName)
        {
            MethodInfo method = typeof(UI_Scroll).GetMethods()
                .Single(candidate =>
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    return candidate.Name == methodName
                           && parameters.Length == 2
                           && parameters[0].ParameterType == typeof(float)
                           && parameters[1].ParameterType.IsEnum;
                });
            object ease = Enum.Parse(method.GetParameters()[1].ParameterType, easeName);
            return (UI_Scroll)method.Invoke(owner, new[] { (object)duration, ease });
        }

        private static UI_Scroll InvokeChildScroll(
            UI_Scroll owner,
            string methodName,
            RectTransform child,
            float duration,
            string easeName)
        {
            MethodInfo method = typeof(UI_Scroll).GetMethods()
                .Single(candidate =>
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    return candidate.Name == methodName
                           && parameters.Length == 3
                           && parameters[0].ParameterType == typeof(RectTransform)
                           && parameters[1].ParameterType == typeof(float)
                           && parameters[2].ParameterType.IsEnum;
                });
            object ease = Enum.Parse(method.GetParameters()[2].ParameterType, easeName);
            return (UI_Scroll)method.Invoke(owner, new object[] { child, duration, ease });
        }

        private static float InvokeChildTarget(UI_Scroll owner, string methodName, RectTransform child)
        {
            MethodInfo method = typeof(UI_Scroll).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return (float)method.Invoke(owner, new object[] { child });
        }
    }
}
