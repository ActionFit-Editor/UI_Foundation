using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public sealed class UI_ButtonLegacyMigratorTests
    {
        private const string TestRoot = "Assets/UIFoundationMigrationTests";
        private const string BasePrefabPath = TestRoot + "/Base.prefab";
        private const string VariantPrefabPath = TestRoot + "/Variant.prefab";
        private const string ScenePath = TestRoot + "/Override.unity";

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void MigrateHierarchyCopiesLegacyStateAndIsIdempotent()
        {
            var root = new GameObject("LegacyButton", typeof(RectTransform), typeof(CanvasRenderer));
            var targetObject = new GameObject("ScaleTarget", typeof(RectTransform));
            targetObject.transform.SetParent(root.transform, false);
            try
            {
                UI_Button target = root.AddComponent<UI_Button>();
                Button legacyButton = root.AddComponent<Button>();
                UIButtonPressEffect legacyPressEffect = root.AddComponent<UIButtonPressEffect>();
                legacyButton.interactable = false;
                UnityEventTools.AddPersistentListener(legacyButton.onClick, target.SetDisable);
                legacyPressEffect.enabled = false;

                var pressObject = new SerializedObject(legacyPressEffect);
                pressObject.FindProperty("scaleDownRatio").floatValue = 0.9f;
                pressObject.FindProperty("isScaleOneFix").boolValue = true;
                pressObject.FindProperty("targetTransform").objectReferenceValue = targetObject.transform;
                pressObject.ApplyModifiedPropertiesWithoutUndo();

                var report = new UI_ButtonMigrationReport();
                Assert.That(UI_ButtonLegacyMigrator.MigrateHierarchy(root, report, "memory"), Is.True);
                Assert.That(root.GetComponent<Button>(), Is.Null);
                Assert.That(root.GetComponent<UIButtonPressEffect>(), Is.Null);
                Assert.That(report.TargetsMigrated, Is.EqualTo(1));
                Assert.That(report.ButtonsRemoved, Is.EqualTo(1));
                Assert.That(report.PressEffectsRemoved, Is.EqualTo(1));

                var migratedObject = new SerializedObject(target);
                Assert.That(migratedObject.FindProperty("m_Interactable").boolValue, Is.False);
                Assert.That(migratedObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls").arraySize, Is.EqualTo(1));
                Assert.That(migratedObject.FindProperty("usePressEffect").boolValue, Is.False);
                Assert.That(migratedObject.FindProperty("scaleDownRatio").floatValue, Is.EqualTo(0.9f).Within(0.0001f));
                Assert.That(migratedObject.FindProperty("isScaleOneFix").boolValue, Is.True);
                Assert.That(
                    migratedObject.FindProperty("targetTransform").objectReferenceValue,
                    Is.SameAs(targetObject.transform));

                var secondReport = new UI_ButtonMigrationReport();
                Assert.That(UI_ButtonLegacyMigrator.MigrateHierarchy(root, secondReport, "memory"), Is.False);
                Assert.That(secondReport.TargetsMigrated, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyAssetsPreservesVariantAndSceneOverrides()
        {
            AssetDatabase.CreateFolder("Assets", "UIFoundationMigrationTests");
            CreateBasePrefab();
            CreateVariantPrefab();
            CreateOverrideScene();

            UI_ButtonMigrationReport report = UI_ButtonLegacyMigrator.ApplyAssets(
                new[] { BasePrefabPath, VariantPrefabPath },
                new[] { ScenePath });

            Assert.That(report.Failures, Is.Empty);
            Assert.That(report.AssetsChanged, Is.EqualTo(3));

            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPrefabPath);
            UI_Button variantButton = variant.GetComponent<UI_Button>();
            Assert.That(variant.GetComponent<Button>(), Is.Null);
            Assert.That(variant.GetComponent<UIButtonPressEffect>(), Is.Null);
            Assert.That(new SerializedObject(variantButton).FindProperty("m_Interactable").boolValue, Is.False);

            GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);
            try
            {
                UI_Button baseButton = baseInstance.GetComponent<UI_Button>();
                baseButton.SetEnable();
                baseButton.OnPointerClick(LeftPointer());
                Assert.That(baseButton.IsDisabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(baseInstance);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = scene.GetRootGameObjects()[0];
            UI_Button sceneButton = sceneRoot.GetComponent<UI_Button>();
            var sceneButtonObject = new SerializedObject(sceneButton);
            Assert.That(sceneRoot.GetComponent<Button>(), Is.Null);
            Assert.That(sceneRoot.GetComponent<UIButtonPressEffect>(), Is.Null);
            Assert.That(sceneButtonObject.FindProperty("m_Interactable").boolValue, Is.True);
            Assert.That(sceneButtonObject.FindProperty("scaleDownRatio").floatValue, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(sceneRoot), Is.Zero);

            UI_ButtonMigrationReport remaining = UI_ButtonLegacyMigrator.PreviewAssets(
                new[] { BasePrefabPath, VariantPrefabPath },
                new[] { ScenePath });
            Assert.That(remaining.Failures, Is.Empty);
            Assert.That(remaining.HasCandidates, Is.False);
        }

        [Test]
        public void ApplyAssetsRefusesPackagePaths()
        {
            UI_ButtonMigrationReport report = UI_ButtonLegacyMigrator.ApplyAssets(
                new[] { "Packages/com.example.readonly/Test.prefab" },
                new[] { "Packages/com.example.readonly/Test.unity" });

            Assert.That(report.AssetsChanged, Is.Zero);
            Assert.That(report.Failures, Has.Count.EqualTo(2));
            Assert.That(report.Failures, Has.Some.Contains("outside Assets/"));
        }

        private static void CreateBasePrefab()
        {
            var root = new GameObject("Base", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                UI_Button target = root.AddComponent<UI_Button>();
                Button button = root.AddComponent<Button>();
                button.interactable = true;
                UnityEventTools.AddPersistentListener(button.onClick, target.SetDisable);
                button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
                root.AddComponent<UIButtonPressEffect>();
                PrefabUtility.SaveAsPrefabAsset(root, BasePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateVariantPrefab()
        {
            GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);
            try
            {
                Button button = instance.GetComponent<Button>();
                button.interactable = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                PrefabUtility.SaveAsPrefabAsset(instance, VariantPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void CreateOverrideScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(variantAsset, scene);
            Button button = instance.GetComponent<Button>();
            button.interactable = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);

            UIButtonPressEffect pressEffect = instance.GetComponent<UIButtonPressEffect>();
            var pressObject = new SerializedObject(pressEffect);
            pressObject.FindProperty("scaleDownRatio").floatValue = 0.8f;
            pressObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(pressEffect);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static PointerEventData LeftPointer()
        {
            return new PointerEventData(null) { button = PointerEventData.InputButton.Left };
        }
    }
}
