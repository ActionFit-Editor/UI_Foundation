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
    private static readonly HashSet<UI_Text> SpritePreviewTargets = new();
    private static bool _refreshCurrentStage;
    private static bool _drainScheduled;
    private static bool _previewMonitorRegistered;
    private static int _initializationRetryCount;

    static UI_TextEditorPreviewCoordinator()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        Undo.undoRedoPerformed += OnUndoRedo;
        ObjectChangeEvents.changesPublished += OnObjectChanges;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UI_Text.SpriteEditorPreviewChanged += OnSpriteEditorPreviewChanged;
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

    internal static void NotifyBeforeAssemblyReloadForTests() => BeforeAssemblyReload();

    internal static void NotifyExitingEditModeForTests() =>
        OnPlayModeStateChanged(PlayModeStateChange.ExitingEditMode);

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
            bool outlineReady = text.TryApplyOutlineEditorPreview();
            bool spriteReady = text.TryApplySpriteEditorPreview();
            if (!outlineReady || !spriteReady) PendingTargets.Add(text);
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
        {
            text.RestoreSpriteEditorPreview();
            text.RestoreOutlineEditorPreview();
        }
    }

    private static void OnSceneOpened(Scene _, OpenSceneMode __) => RequestCurrentStageRefresh();

    private static void OnUndoRedo() => RequestCurrentStageRefresh();

    private static void OnSpriteEditorPreviewChanged(UI_Text text)
    {
        if (ReferenceEquals(text, null)) return;
        if (text.HasSpriteEditorPreview)
        {
            SpritePreviewTargets.Add(text);
            RegisterPreviewMonitor();
        }
        else
        {
            SpritePreviewTargets.Remove(text);
            UnregisterPreviewMonitorWhenIdle();
        }
    }

    private static void RegisterPreviewMonitor()
    {
        if (_previewMonitorRegistered) return;
        _previewMonitorRegistered = true;
        EditorApplication.update += MonitorSpritePreviewTargets;
    }

    private static void MonitorSpritePreviewTargets()
    {
        foreach (UI_Text text in new List<UI_Text>(SpritePreviewTargets))
        {
            if (text != null && text.enabled) continue;
            text.RestoreSpriteEditorPreview();
            if (text != null) text.RestoreOutlineEditorPreview();
            SpritePreviewTargets.Remove(text);
        }
        UnregisterPreviewMonitorWhenIdle();
    }

    private static void UnregisterPreviewMonitorWhenIdle()
    {
        if (!_previewMonitorRegistered || SpritePreviewTargets.Count > 0) return;
        EditorApplication.update -= MonitorSpritePreviewTargets;
        _previewMonitorRegistered = false;
    }

    private static void OnObjectChanges(ref ObjectChangeEventStream stream)
    {
        if (Application.isPlaying) return;
        for (int index = 0; index < stream.length; index++)
        {
            if (stream.GetEventType(index) != ObjectChangeKind.ChangeGameObjectOrComponentProperties) continue;
            stream.GetChangeGameObjectOrComponentPropertiesEvent(
                index,
                out ChangeGameObjectOrComponentPropertiesEventArgs change);
            if (EditorUtility.InstanceIDToObject(change.instanceId) is not UI_Text text) continue;

            if (!text.enabled)
            {
                PendingTargets.Remove(text);
                text.RestoreSpriteEditorPreview();
                text.RestoreOutlineEditorPreview();
            }
            else
            {
                RequestRefresh(text);
            }
        }
    }

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
        foreach (UI_Text text in new List<UI_Text>(SpritePreviewTargets))
        {
            text.RestoreSpriteEditorPreview();
            if (text != null) text.RestoreOutlineEditorPreview();
        }
        SpritePreviewTargets.Clear();
        UnregisterPreviewMonitorWhenIdle();

        foreach (UI_Text text in Resources.FindObjectsOfTypeAll<UI_Text>())
        {
            if (!IsLoadedSceneTarget(text)) continue;
            text.RestoreSpriteEditorPreview();
            text.RestoreOutlineEditorPreview();
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
