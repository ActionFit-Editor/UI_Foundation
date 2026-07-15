using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public class UI_TextEditorPreviewTests
    {
        private const string OutlineShaderName = "TextMeshPro/Mobile/Distance Field Shadow Outline";
        private const string TempFolder = "Assets/__UIFoundationPreviewTests";
        private const string TempShaderPath = TempFolder + "/Preview.shader";
        private const string TempMaterialPath = TempFolder + "/PreviewBase.mat";
        private const string TempPrefabPath = TempFolder + "/PreviewText.prefab";
        private const string TestShaderSource = @"Shader ""TextMeshPro/Mobile/Distance Field Shadow Outline""
{
    Properties
    {
        _FaceColor (""Face Color"", Color) = (1,1,1,1)
        _FaceDilate (""Face Dilate"", Float) = 0
        _OutlineColor (""Outline Color"", Color) = (0,0,0,1)
        _OutlineWidth (""Outline Width"", Float) = 0
        _UnderlayColor (""Underlay Color"", Color) = (0,0,0,1)
        _UnderlayOffsetX (""Underlay Offset X"", Float) = 0
        _UnderlayOffsetY (""Underlay Offset Y"", Float) = 0
        _UnderlayDilate (""Underlay Dilate"", Float) = 0
        _UnderlaySoftness (""Underlay Softness"", Float) = 0
        _GlowColor (""Glow Color"", Color) = (0,0,0,0)
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local __ OUTLINE_ON
            #pragma shader_feature_local __ UNDERLAY_ON
            #include ""UnityCG.cginc""
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag() : SV_Target { return fixed4(1, 1, 1, 1); }
            ENDCG
        }
    }
}";

        [SetUp]
        public void SetUp()
        {
            StageUtility.GoToMainStage();
            UI_TextEditorPreviewCoordinator.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            StageUtility.GoToMainStage();
            UI_TextEditorPreviewCoordinator.ResetForTests();
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void RequestsCoalesceAndApplyToActiveAndInactiveTargetsWithoutDirtyingScene()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            Material baseMaterial = CreateBaseMaterial();
            GameObject root = null;
            try
            {
                root = new GameObject("PreviewRoot");
                SceneManager.MoveGameObjectToScene(root, scene);
                UI_Text activeText = CreateText("ActiveText", root.transform, baseMaterial, true);
                UI_Text inactiveText = CreateText("InactiveText", root.transform, baseMaterial, true);
                inactiveText.gameObject.SetActive(false);
                bool dirtyBeforePreview = scene.isDirty;

                UI_TextEditorPreviewCoordinator.RequestRefresh(activeText);
                UI_TextEditorPreviewCoordinator.RequestRefresh(activeText);
                UI_TextEditorPreviewCoordinator.RequestRefresh(inactiveText);

                Assert.That(UI_TextEditorPreviewCoordinator.PendingTargetCountForTests, Is.EqualTo(2));
                Assert.That(activeText.HasOutlineEditorPreview, Is.False);
                Assert.That(inactiveText.HasOutlineEditorPreview, Is.False);

                UI_TextEditorPreviewCoordinator.ProcessPendingForTests();

                AssertPreview(activeText, baseMaterial);
                AssertPreview(inactiveText, baseMaterial);
                Assert.That(scene.isDirty, Is.EqualTo(dirtyBeforePreview));

                Material activePreview = activeText.OutlineEditorPreviewMaterial;
                UI_TextEditorPreviewCoordinator.ResetForTests();
                Assert.That(activeText.TMP.fontSharedMaterial, Is.SameAs(baseMaterial));
                Assert.That(inactiveText.TMP.fontSharedMaterial, Is.SameAs(baseMaterial));
                Assert.That(activePreview == null, Is.True);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (baseMaterial != null) Object.DestroyImmediate(baseMaterial);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void PrefabStageOpenAndReopenApplyPreviewWithoutSerializingOrDirtying()
        {
            EnsureTempFolder();
            Material materialAsset = CreateBaseMaterial();
            AssetDatabase.CreateAsset(materialAsset, TempMaterialPath);

            var source = new GameObject("PreviewTextRoot");
            CreateText("PreviewText", source.transform, materialAsset, true);
            PrefabUtility.SaveAsPrefabAsset(source, TempPrefabPath);
            Object.DestroyImmediate(source);
            AssetDatabase.SaveAssets();

            string yamlBefore = File.ReadAllText(TempPrefabPath);
            VerifyOpenedPrefabStage(materialAsset);
            StageUtility.GoToMainStage();
            VerifyOpenedPrefabStage(materialAsset);

            Assert.That(File.ReadAllText(TempPrefabPath), Is.EqualTo(yamlBefore));
        }

        [Test]
        public void UndoRedoNotificationRefreshesTheExistingPreviewMaterial()
        {
            EnsureTempFolder();
            Material materialAsset = CreateBaseMaterial();
            AssetDatabase.CreateAsset(materialAsset, TempMaterialPath);

            var source = new GameObject("UndoRoot");
            CreateText("UndoText", source.transform, materialAsset, true);
            PrefabUtility.SaveAsPrefabAsset(source, TempPrefabPath);
            Object.DestroyImmediate(source);
            AssetDatabase.SaveAssets();

            PrefabStage stage = PrefabStageUtility.OpenPrefab(TempPrefabPath);
            Assert.That(stage, Is.Not.Null);
            UI_Text text = stage.prefabContentsRoot.GetComponentInChildren<UI_Text>(true);
            Assert.That(text, Is.Not.Null);
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();

            SetFloatWithoutUndo(text, "outlineWidth", 0.45f);
            UI_TextEditorPreviewCoordinator.RequestRefresh(text);
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
            Assert.That(text.OutlineEditorPreviewMaterial.GetFloat("_OutlineWidth"), Is.EqualTo(0.45f).Within(0.0001f));

            SetFloatWithoutUndo(text, "outlineWidth", 0.2f);
            UI_TextEditorPreviewCoordinator.NotifyUndoRedoForTests();
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
            Assert.That(text.OutlineEditorPreviewMaterial.GetFloat("_OutlineWidth"), Is.EqualTo(0.2f).Within(0.0001f));
        }

        private static void VerifyOpenedPrefabStage(Material expectedBaseMaterial)
        {
            PrefabStage stage = PrefabStageUtility.OpenPrefab(TempPrefabPath);
            Assert.That(stage, Is.Not.Null);
            UI_Text text = stage.prefabContentsRoot.GetComponentInChildren<UI_Text>(true);
            Assert.That(text, Is.Not.Null);

            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();

            AssertPreview(text, expectedBaseMaterial);
            Assert.That(stage.scene.isDirty, Is.False);
        }

        private static UI_Text CreateText(string name, Transform parent, Material baseMaterial, bool includeUnderlay)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.transform.SetParent(parent, false);
            var tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSharedMaterial = baseMaterial;
            var text = gameObject.AddComponent<UI_Text>();

            var serialized = new SerializedObject(text);
            serialized.FindProperty("isSettingOutline").boolValue = true;
            serialized.FindProperty("outlineWidth").floatValue = 0.2f;
            serialized.FindProperty("isSettingUnderlay").boolValue = includeUnderlay;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return text;
        }

        private static void SetFloatWithoutUndo(UI_Text text, string propertyName, float value)
        {
            var serialized = new SerializedObject(text);
            serialized.FindProperty(propertyName).floatValue = value;
            Assert.That(serialized.ApplyModifiedPropertiesWithoutUndo(), Is.True);
        }

        private static Material CreateBaseMaterial()
        {
            Shader shader = Shader.Find(OutlineShaderName);
            if (shader == null)
            {
                EnsureTempFolder();
                File.WriteAllText(TempShaderPath, TestShaderSource);
                AssetDatabase.ImportAsset(TempShaderPath, ImportAssetOptions.ForceSynchronousImport);
                shader = AssetDatabase.LoadAssetAtPath<Shader>(TempShaderPath);
            }
            Assert.That(shader, Is.Not.Null, $"Unable to prepare test shader: {OutlineShaderName}");
            return new Material(shader) { name = "UI Foundation Preview Test Base" };
        }

        private static void AssertPreview(UI_Text text, Material expectedBaseMaterial)
        {
            Assert.That(text.HasOutlineEditorPreview, Is.True);
            Material preview = text.OutlineEditorPreviewMaterial;
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview, Is.Not.SameAs(expectedBaseMaterial));
            Assert.That(text.TMP.fontSharedMaterial, Is.SameAs(preview));
            Assert.That((preview.hideFlags & HideFlags.DontSave) != 0, Is.True);
            Assert.That(preview.IsKeywordEnabled("OUTLINE_ON"), Is.True);
            Assert.That(preview.IsKeywordEnabled("UNDERLAY_ON"), Is.True);
        }

        private static void EnsureTempFolder()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.CreateFolder("Assets", "__UIFoundationPreviewTests");
        }
    }
}
