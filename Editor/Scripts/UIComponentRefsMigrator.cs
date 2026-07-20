#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Image / UI_Input / UI_Scroll의 직렬화 캐시 필드를
/// 기존 prefab/씬에 일괄로 채워주는 1회성 마이그레이션 메뉴.
/// SerializedObject로 직접 objectReferenceValue를 set하므로 OnValidate 발화 의존 없이 확실하게 동작.
/// </summary>
public static class UIComponentRefsMigrator
{
    private const string MENU_PATH = "Tools/Package/UI Foundation/Migrate Component Refs";

    [MenuItem(MENU_PATH, false, 20)]
    private static void Run()
    {
        if (!EditorUtility.DisplayDialog(
                "Migrate UI Component Refs",
                "모든 prefab/씬의 UI_Image/UI_Input/UI_Scroll 직렬화 캐시(_image/_inputField/_scrollRect)를 채웁니다.\n장시간 소요될 수 있습니다. 진행할까요?",
                "Run", "Cancel"))
            return;

        try
        {
            int prefabTouched = MigratePrefabs();
            int sceneTouched = MigrateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[UIComponentRefsMigrator] Done. Prefabs touched: {prefabTouched}, Scenes touched: {sceneTouched}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // 모든 .prefab 자산을 순회하며 UI 컴포넌트 캐시 채움
    private static int MigratePrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int touched = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar(
                "UIComponentRefsMigrator",
                $"Prefab {i + 1}/{guids.Length}: {path}",
                guids.Length > 0 ? (float)i / guids.Length : 1f);

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                if (PopulateRefs(root))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    touched++;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[UIComponentRefsMigrator] Prefab failed: {path}\n{e}");
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }
        return touched;
    }

    // 모든 .unity 씬을 순회하며 UI 컴포넌트 캐시 채움
    private static int MigrateScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        int touched = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar(
                "UIComponentRefsMigrator",
                $"Scene {i + 1}/{guids.Length}: {path}",
                guids.Length > 0 ? (float)i / guids.Length : 1f);

            try
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool dirtied = false;
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (PopulateRefs(go)) dirtied = true;
                }
                if (dirtied)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    touched++;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[UIComponentRefsMigrator] Scene failed: {path}\n{e}");
            }
        }
        return touched;
    }

    // root 하위의 UI_Image / UI_Input / UI_Scroll 컴포넌트 직렬화 캐시를 채움.
    // UI_Button은 UI_Image를 상속하므로 _image 캐시는 GetComponentsInChildren<UI_Image>에서 함께 처리됨.
    private static bool PopulateRefs(GameObject root)
    {
        bool dirtied = false;

        foreach (var c in root.GetComponentsInChildren<UI_Image>(includeInactive: true))
        {
            var image = c.GetComponent<Image>();
            if (image == null) continue;

            var so = new SerializedObject(c);
            var prop = so.FindProperty("_image");
            if (prop != null && prop.objectReferenceValue != image)
            {
                prop.objectReferenceValue = image;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirtied = true;
            }
        }

        foreach (var c in root.GetComponentsInChildren<UI_Input>(includeInactive: true))
        {
            var inputField = c.GetComponent<TMP_InputField>();
            if (inputField == null) continue;

            var so = new SerializedObject(c);
            var prop = so.FindProperty("_inputField");
            if (prop != null && prop.objectReferenceValue != inputField)
            {
                prop.objectReferenceValue = inputField;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirtied = true;
            }
        }

        foreach (var c in root.GetComponentsInChildren<UI_Scroll>(includeInactive: true))
        {
            var scrollRect = c.GetComponent<ScrollRect>();
            if (scrollRect == null) continue;

            var so = new SerializedObject(c);
            var prop = so.FindProperty("_scrollRect");
            if (prop != null && prop.objectReferenceValue != scrollRect)
            {
                prop.objectReferenceValue = scrollRect;
                so.ApplyModifiedPropertiesWithoutUndo();
                dirtied = true;
            }
        }

        return dirtied;
    }
}
#endif
