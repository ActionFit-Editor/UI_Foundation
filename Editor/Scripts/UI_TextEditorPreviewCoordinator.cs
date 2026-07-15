#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class UI_TextEditorPreviewCoordinator
{
    private const int MaxInitializationRetries = 3;

    private static readonly HashSet<UI_Text> PendingTargets = new();
    private static bool _refreshCurrentStage;
    private static bool _drainScheduled;
    private static int _initializationRetryCount;

    static UI_TextEditorPreviewCoordinator()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        Undo.undoRedoPerformed += OnUndoRedo;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RequestCurrentStageRefresh();
    }

    internal static int PendingTargetCountForTests => PendingTargets.Count;

    internal static void RequestRefresh(UI_Text text)
    {
        if (Application.isPlaying || text == null) return;
        if (!_refreshCurrentStage) PendingTargets.Add(text);
        _initializationRetryCount = 0;
        ScheduleDrain();
    }

    internal static void RequestCurrentStageRefresh()
    {
        if (Application.isPlaying) return;
        _refreshCurrentStage = true;
        PendingTargets.Clear();
        _initializationRetryCount = 0;
        ScheduleDrain();
    }

    internal static void ProcessPendingForTests()
    {
        EditorApplication.delayCall -= Drain;
        _drainScheduled = false;
        Drain();
    }

    internal static void ResetForTests()
    {
        CancelPending();
        RestoreAllLoadedPreviews();
    }

    internal static void NotifyUndoRedoForTests() => OnUndoRedo();

    private static void ScheduleDrain()
    {
        if (_drainScheduled) return;
        _drainScheduled = true;
        EditorApplication.delayCall += Drain;
    }

    private static void Drain()
    {
        _drainScheduled = false;
        if (Application.isPlaying)
        {
            CancelPending();
            return;
        }

        List<UI_Text> targets = _refreshCurrentStage
            ? CollectCurrentStageTargets()
            : new List<UI_Text>(PendingTargets);

        _refreshCurrentStage = false;
        PendingTargets.Clear();

        foreach (UI_Text text in targets)
        {
            if (!IsCurrentStageTarget(text)) continue;
            if (!text.TryApplyOutlineEditorPreview()) PendingTargets.Add(text);
        }

        if (PendingTargets.Count > 0 && _initializationRetryCount < MaxInitializationRetries)
        {
            _initializationRetryCount++;
            ScheduleDrain();
        }
        else
        {
            PendingTargets.Clear();
            _initializationRetryCount = 0;
        }
    }

    private static List<UI_Text> CollectCurrentStageTargets()
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.prefabContentsRoot != null)
            return new List<UI_Text>(stage.prefabContentsRoot.GetComponentsInChildren<UI_Text>(true));

        var result = new List<UI_Text>();
        foreach (UI_Text text in Object.FindObjectsByType<UI_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (IsLoadedSceneTarget(text)) result.Add(text);
        }
        return result;
    }

    private static bool IsCurrentStageTarget(UI_Text text)
    {
        if (text == null || EditorUtility.IsPersistent(text)) return false;

        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null) return IsLoadedSceneTarget(text);
        if (stage.prefabContentsRoot == null) return false;

        Transform root = stage.prefabContentsRoot.transform;
        return text.transform == root || text.transform.IsChildOf(root);
    }

    private static bool IsLoadedSceneTarget(UI_Text text)
    {
        if (text == null || EditorUtility.IsPersistent(text)) return false;
        Scene scene = text.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static void OnPrefabStageOpened(PrefabStage _) => RequestCurrentStageRefresh();

    private static void OnPrefabStageClosing(PrefabStage stage)
    {
        if (stage?.prefabContentsRoot == null) return;
        foreach (UI_Text text in stage.prefabContentsRoot.GetComponentsInChildren<UI_Text>(true))
            text.RestoreOutlineEditorPreview();
    }

    private static void OnSceneOpened(Scene _, OpenSceneMode __) => RequestCurrentStageRefresh();

    private static void OnUndoRedo() => RequestCurrentStageRefresh();

    private static void BeforeAssemblyReload()
    {
        CancelPending();
        RestoreAllLoadedPreviews();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            CancelPending();
            RestoreAllLoadedPreviews();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            RequestCurrentStageRefresh();
        }
    }

    private static void RestoreAllLoadedPreviews()
    {
        foreach (UI_Text text in Resources.FindObjectsOfTypeAll<UI_Text>())
        {
            if (IsLoadedSceneTarget(text)) text.RestoreOutlineEditorPreview();
        }
    }

    private static void CancelPending()
    {
        EditorApplication.delayCall -= Drain;
        PendingTargets.Clear();
        _refreshCurrentStage = false;
        _drainScheduled = false;
        _initializationRetryCount = 0;
    }
}
#endif
