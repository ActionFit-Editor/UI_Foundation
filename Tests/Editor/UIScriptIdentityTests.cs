using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public class UIScriptIdentityTests
    {
        private const string RuntimeAssembly = "com.actionfit.ui.foundation";
        private const string EditorAssembly = "com.actionfit.ui.foundation.Editor";

        private static IEnumerable<TestCaseData> RuntimeScripts()
        {
            yield return Script("7c85533b386a649b9975f118b62e39e3", "Runtime/Button/UI_Button.cs", typeof(UI_Button));
            yield return Script("be5cf40c6877451593c72ad45562c318", "Runtime/Image_Slice.cs", typeof(Image_Slice));
            yield return Script("4bf6e39dc78a648beb408d2dc8b49e3b", "Runtime/Text/UI_Text.cs", typeof(UI_Text));
            yield return Script("a92d5c362b4dc4305891a1ec378f80ae", "Runtime/UI_Image.cs", typeof(UI_Image));
            yield return Script("98097fade2804163bae3343b2996848d", "Runtime/UI_ImageSlice.cs", typeof(UI_ImageSlice));
            yield return Script("ad090970ff0bc46aaada29bf49fd7098", "Runtime/UI_Input.cs", typeof(UI_Input));
            yield return Script("f59b0c03f0f4c4ac7bd9700f6abcfb2f", "Runtime/UI_InputBtn.cs", typeof(UI_InputBtn));
            yield return Script("8dfb7c6c06f154f6fa5ea7f7a0a2a71a", "Runtime/UI_Mask.cs", typeof(UI_Mask));
            yield return Script("354ee8596a3e412e9e7f37f2c73b11e1", "Runtime/UI_Mask2D.cs", typeof(UI_Mask2D));
            yield return Script("635f89f375254a3da05e42cad3043dbd", "Runtime/UI_MaskBase.cs", typeof(UI_MaskBase));
            yield return Script("ba338dd089612434eb19b12d4f415aec", "Runtime/UI_Rect.cs", typeof(UI_Rect));
            yield return Script("455d78208a3d44c58a55ceb68f8e6aa4", "Runtime/UI_Scroll.cs", typeof(UI_Scroll));
            yield return Script("d9a9abbb32fb3a1478762995d5bc93f7", "Runtime/Util/UIButtonPressEffect.cs", typeof(UIButtonPressEffect));
        }

        private static IEnumerable<TestCaseData> EditorScripts()
        {
            yield return EditorScript("8f190b90b2d74bb1bac4b75d52e208cd", "Image_SliceEditor.cs", typeof(Image_SliceEditor));
            yield return EditorScript("1e9d6fef997fa4e749c51141a963ab05", "UI_ImageEditor.cs", typeof(UI_ImageEditor));
            yield return EditorScript("d91612d9846fd445a9bbe259b24a3aa9", "UI_ButtonEditor.cs", typeof(UI_ButtonEditor));
            yield return EditorScript("2b3067c1c04ff4ab48267d97e5c661dc", "UI_TextEditor.cs", typeof(UI_TextEditor));
            yield return EditorScript("5291e73137c0410d80f78e73f5a61c9c", "UI_TextEditorPreviewCoordinator.cs", typeof(UI_TextEditorPreviewCoordinator));
            yield return EditorScript("5081d88b4ea347d5a1ce09ab29fc9108", "UI_MaskBaseEditor.cs", typeof(UI_MaskBaseEditor));
            yield return EditorScript("b9943d1409fc24ec795cc72a1734d0d9", "UIComponentRefsMigrator.cs", typeof(UIComponentRefsMigrator));
            yield return EditorScript("5f847eaa5cad4f4f9c0f1f3d7c8bb06d", "UI_ButtonLegacyMigrator.cs", typeof(UI_ButtonLegacyMigrator));
            yield return EditorScript("c4ce8c4eea0f4730b7a7e92f7bd52d8d", "ChainedVector3Drawer.cs", typeof(ChainedVector3Drawer));
        }

        [TestCaseSource(nameof(RuntimeScripts))]
        public void RuntimeScriptPreservesPathTypeAndAssembly(string guid, string path, Type expectedType)
        {
            AssertScript(guid, path, expectedType, RuntimeAssembly);
        }

        [TestCaseSource(nameof(EditorScripts))]
        public void EditorScriptPreservesPathTypeAndAssembly(string guid, string path, Type expectedType)
        {
            AssertScript(guid, path, expectedType, EditorAssembly);
        }

        [TestCase("438438e3264549c8981bfd6869cedc6a", "Packages/com.actionfit.ui.foundation/Runtime/Util/UIEase.cs")]
        [TestCase("a0c8bcabd1ad49108fbaf54e5391d760", "Packages/com.actionfit.ui.foundation/Runtime/Util/UIAnimationUtility.cs")]
        [TestCase("2a0989b0dbb54a5d827eae46c8de4585", "Packages/com.actionfit.ui.foundation/Runtime/Button/UIButtonServices.cs")]
        [TestCase("220a820aa6484e52b7a0fd96f6909bba", "Packages/com.actionfit.ui.foundation/Runtime/Util/RuntimeSpriteAssetCache.cs")]
        public void NewRuntimeUtilityGuidIsFixed(string guid, string expectedPath)
        {
            Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(expectedPath));
        }

        private static TestCaseData Script(string guid, string relativePath, Type type)
        {
            return new TestCaseData(guid, $"Packages/com.actionfit.ui.foundation/{relativePath}", type)
                .SetName($"RuntimeIdentity_{type.Name}");
        }

        private static TestCaseData EditorScript(string guid, string fileName, Type type)
        {
            return new TestCaseData(
                    guid,
                    $"Packages/com.actionfit.ui.foundation/Editor/Scripts/{fileName}",
                    type)
                .SetName($"EditorIdentity_{type.Name}");
        }

        private static void AssertScript(string guid, string expectedPath, Type expectedType, string expectedAssembly)
        {
            Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(expectedPath));
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(expectedPath);
            Assert.That(script, Is.Not.Null);
            Assert.That(script.GetClass(), Is.EqualTo(expectedType));
            Assert.That(expectedType.Assembly.GetName().Name, Is.EqualTo(expectedAssembly));
        }
    }
}
