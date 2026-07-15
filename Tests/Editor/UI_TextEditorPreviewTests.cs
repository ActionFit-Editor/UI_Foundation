using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
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
        private const string TempSpritePath = TempFolder + "/PreviewSprite.png";
        private const string TempSpriteAssetPath = TempFolder + "/OriginalSpriteAsset.asset";
        private const string TempSpriteMaterialPath = TempFolder + "/OriginalSpriteMaterial.mat";
        private const string RuntimeSpriteTestShaderPath =
            "Packages/com.actionfit.ui.foundation/Tests/Editor/RuntimeSpriteAssetTest.shader";
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

        [Test]
        public void SpritePreviewRestoresOriginalAssetOnAssemblyReloadWithoutDirtyingScene()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            Sprite sprite = null;
            TMP_SpriteAsset originalAsset = null;
            Material spriteMaterial = null;
            GameObject root = null;
            try
            {
                sprite = CreateSpriteAsset();
                originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                spriteMaterial = CreateSpriteMaterial();
                originalAsset.material = spriteMaterial;
                root = new GameObject("SpritePreviewRoot");
                SceneManager.MoveGameObjectToScene(root, scene);
                UI_Text text = CreateSpriteText("SpritePreview", root.transform, originalAsset, sprite);
                bool dirtyBeforePreview = scene.isDirty;

                UI_TextEditorPreviewCoordinator.RequestRefresh(text);
                UI_TextEditorPreviewCoordinator.ProcessPendingForTests();

                Assert.That(text.HasSpriteEditorPreview, Is.True);
                TMP_SpriteAsset previewAsset = text.SpriteEditorPreviewAsset;
                Assert.That(previewAsset, Is.Not.Null);
                Assert.That(text.TMP.spriteAsset, Is.SameAs(previewAsset));
                Assert.That(previewAsset.spriteGlyphTable[0].sprite, Is.SameAs(sprite));
                Assert.That(previewAsset.GetSpriteIndexFromName(sprite.name), Is.Zero);
                Assert.That((previewAsset.hideFlags & HideFlags.DontSave) != 0, Is.True);
                Assert.That((previewAsset.material.hideFlags & HideFlags.DontSave) != 0, Is.True);
                Assert.That(scene.isDirty, Is.EqualTo(dirtyBeforePreview));

                UI_TextEditorPreviewCoordinator.NotifyBeforeAssemblyReloadForTests();

                Assert.That(text.HasSpriteEditorPreview, Is.False);
                Assert.That(text.TMP.spriteAsset, Is.SameAs(originalAsset));
                Assert.That(previewAsset == null, Is.True);
                Assert.That(scene.isDirty, Is.EqualTo(dirtyBeforePreview));
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (originalAsset != null) Object.DestroyImmediate(originalAsset);
                if (spriteMaterial != null) Object.DestroyImmediate(spriteMaterial);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void SpritePreviewRestoresOriginalAssetBeforeEnteringPlayMode()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            Sprite sprite = null;
            TMP_SpriteAsset originalAsset = null;
            Material spriteMaterial = null;
            GameObject root = null;
            try
            {
                sprite = CreateSpriteAsset();
                originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                spriteMaterial = CreateSpriteMaterial();
                originalAsset.material = spriteMaterial;
                root = new GameObject("PlayModeCleanupRoot");
                SceneManager.MoveGameObjectToScene(root, scene);
                UI_Text text = CreateSpriteText("PlayModeCleanup", root.transform, originalAsset, sprite);
                UI_TextEditorPreviewCoordinator.RequestRefresh(text);
                UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
                TMP_SpriteAsset previewAsset = text.SpriteEditorPreviewAsset;

                UI_TextEditorPreviewCoordinator.NotifyExitingEditModeForTests();

                Assert.That(text.HasSpriteEditorPreview, Is.False);
                Assert.That(text.TMP.spriteAsset, Is.SameAs(originalAsset));
                Assert.That(previewAsset == null, Is.True);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (originalAsset != null) Object.DestroyImmediate(originalAsset);
                if (spriteMaterial != null) Object.DestroyImmediate(spriteMaterial);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [UnityTest]
        public IEnumerator SpritePreviewCleansUpWhenComponentIsDisabledOrDestroyed()
        {
            EnsureTempFolder();
            Sprite sprite = CreateSpriteAsset();
            var originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            Material spriteMaterial = CreateSpriteMaterial();
            AssetDatabase.CreateAsset(spriteMaterial, TempSpriteMaterialPath);
            originalAsset.material = spriteMaterial;
            AssetDatabase.CreateAsset(originalAsset, TempSpriteAssetPath);

            var source = new GameObject("ComponentCleanupRoot");
            CreateSpriteText("Disabled", source.transform, originalAsset, sprite);
            CreateSpriteText("Destroyed", source.transform, originalAsset, sprite);
            PrefabUtility.SaveAsPrefabAsset(source, TempPrefabPath);
            Object.DestroyImmediate(source);
            AssetDatabase.SaveAssets();

            PrefabStage stage = PrefabStageUtility.OpenPrefab(TempPrefabPath);
            Assert.That(stage, Is.Not.Null);
            UI_Text[] texts = stage.prefabContentsRoot.GetComponentsInChildren<UI_Text>(true);
            UI_Text disabledText = System.Array.Find(texts, text => text.name == "Disabled");
            UI_Text destroyedText = System.Array.Find(texts, text => text.name == "Destroyed");
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
            TMP_Text disabledTmp = disabledText.TMP;
            TMP_SpriteAsset disabledPreview = disabledText.SpriteEditorPreviewAsset;

            var disabledSerialized = new SerializedObject(disabledText);
            disabledSerialized.FindProperty("m_Enabled").boolValue = false;
            disabledSerialized.ApplyModifiedPropertiesWithoutUndo();
            yield return null;

            Assert.That(disabledTmp.spriteAsset, Is.SameAs(originalAsset));
            Assert.That(disabledPreview == null, Is.True);

            TMP_Text destroyedTmp = destroyedText.TMP;
            TMP_SpriteAsset destroyedPreview = destroyedText.SpriteEditorPreviewAsset;
            Object.DestroyImmediate(destroyedText);
            yield return null;

            Assert.That(destroyedTmp.spriteAsset, Is.SameAs(originalAsset));
            Assert.That(destroyedPreview == null, Is.True);
        }

        [Test]
        public void SpritePreviewRefreshesAfterInspectorAndUndoRedoSettingsChanges()
        {
            EnsureTempFolder();
            Sprite sprite = CreateSpriteAsset();
            var originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            Material spriteMaterial = CreateSpriteMaterial();
            AssetDatabase.CreateAsset(spriteMaterial, TempSpriteMaterialPath);
            originalAsset.material = spriteMaterial;
            AssetDatabase.CreateAsset(originalAsset, TempSpriteAssetPath);

            var source = new GameObject("SpriteRefreshRoot");
            CreateSpriteText("SpriteRefresh", source.transform, originalAsset, sprite);
            PrefabUtility.SaveAsPrefabAsset(source, TempPrefabPath);
            Object.DestroyImmediate(source);
            AssetDatabase.SaveAssets();

            PrefabStage stage = PrefabStageUtility.OpenPrefab(TempPrefabPath);
            Assert.That(stage, Is.Not.Null);
            UI_Text text = stage.prefabContentsRoot.GetComponentInChildren<UI_Text>(true);
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
            TMP_SpriteAsset firstPreview = text.SpriteEditorPreviewAsset;

            SetSpriteScaleWithoutUndo(text, 1.5f);
            UI_TextEditorPreviewCoordinator.RequestRefresh(text);
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
            TMP_SpriteAsset inspectorPreview = text.SpriteEditorPreviewAsset;
            Assert.That(firstPreview == null, Is.True);
            Assert.That(inspectorPreview.spriteGlyphTable[0].scale, Is.EqualTo(1.5f));

            SetSpriteScaleWithoutUndo(text, 0.75f);
            UI_TextEditorPreviewCoordinator.NotifyUndoRedoForTests();
            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();
            Assert.That(inspectorPreview == null, Is.True);
            Assert.That(text.SpriteEditorPreviewAsset.spriteGlyphTable[0].scale, Is.EqualTo(0.75f));
        }

        [Test]
        public void SpritePreviewDoesNotSerializeGeneratedAssetIntoPrefabYaml()
        {
            EnsureTempFolder();
            Sprite sprite = CreateSpriteAsset();
            var originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            Material spriteMaterial = CreateSpriteMaterial();
            AssetDatabase.CreateAsset(spriteMaterial, TempSpriteMaterialPath);
            originalAsset.material = spriteMaterial;
            AssetDatabase.CreateAsset(originalAsset, TempSpriteAssetPath);

            var source = new GameObject("SpritePreviewPrefabRoot");
            CreateSpriteText("SpritePreview", source.transform, originalAsset, sprite);
            PrefabUtility.SaveAsPrefabAsset(source, TempPrefabPath);
            Object.DestroyImmediate(source);
            AssetDatabase.SaveAssets();

            string yamlBefore = File.ReadAllText(TempPrefabPath);
            PrefabStage stage = PrefabStageUtility.OpenPrefab(TempPrefabPath);
            Assert.That(stage, Is.Not.Null);
            UI_Text text = stage.prefabContentsRoot.GetComponentInChildren<UI_Text>(true);
            Assert.That(text, Is.Not.Null);

            UI_TextEditorPreviewCoordinator.ProcessPendingForTests();

            Assert.That(text.HasSpriteEditorPreview, Is.True);
            Assert.That(text.TMP.spriteAsset, Is.SameAs(text.SpriteEditorPreviewAsset));
            Assert.That(stage.scene.isDirty, Is.False);
            StageUtility.GoToMainStage();
            Assert.That(File.ReadAllText(TempPrefabPath), Is.EqualTo(yamlBefore));
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

        private static UI_Text CreateSpriteText(
            string name,
            Transform parent,
            TMP_SpriteAsset originalAsset,
            Sprite sprite)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.transform.SetParent(parent, false);
            var tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = null;
            tmp.spriteAsset = originalAsset;
            var text = gameObject.AddComponent<UI_Text>();

            var serialized = new SerializedObject(text);
            serialized.FindProperty("isSpriteAsset").boolValue = true;
            serialized.FindProperty("sprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            text.ResetRuntimeSpriteGlyphSettings();
            return text;
        }

        private static Sprite CreateSpriteAsset()
        {
            EnsureTempFolder();
            var texture = new Texture2D(8, 8);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(TempSpritePath, png);
            AssetDatabase.ImportAsset(TempSpritePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TempSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TempSpritePath);
            Assert.That(sprite, Is.Not.Null);
            return sprite;
        }

        private static Material CreateSpriteMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RuntimeSpriteTestShaderPath);
            Assert.That(shader, Is.Not.Null);
            return new Material(shader) { name = "UI Foundation Sprite Preview Test Material" };
        }

        private static void SetFloatWithoutUndo(UI_Text text, string propertyName, float value)
        {
            var serialized = new SerializedObject(text);
            serialized.FindProperty(propertyName).floatValue = value;
            Assert.That(serialized.ApplyModifiedPropertiesWithoutUndo(), Is.True);
        }

        private static void SetSpriteScaleWithoutUndo(UI_Text text, float value)
        {
            var serialized = new SerializedObject(text);
            SerializedProperty settings = serialized.FindProperty("spriteGlyphSettings");
            settings.FindPropertyRelative("scale").floatValue = value;
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
