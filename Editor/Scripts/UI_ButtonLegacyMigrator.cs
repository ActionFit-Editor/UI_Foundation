#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 기존 UI_Button과 같은 GameObject에 붙은 Button/UIButtonPressEffect 데이터를
/// UI_Button 소유 필드로 옮긴 뒤 해당 레거시 컴포넌트만 제거합니다.
/// </summary>
public static class UI_ButtonLegacyMigrator
{
    private const string MenuRoot = "Tools/Package/UI Foundation/";
    private const string BatchApplyFlag = "-uiButtonMigrationApply";

    [MenuItem(MenuRoot + "Preview Legacy UI_Button Migration", false, 30)]
    private static void PreviewMenu()
    {
        UI_ButtonMigrationReport report = PreviewProject();
        UnityEngine.Debug.Log(report.Format("Preview"));
        EditorUtility.DisplayDialog("Legacy UI_Button Migration Preview", report.Format("Preview"), "OK");
    }

    [MenuItem(MenuRoot + "Apply Legacy UI_Button Migration...", false, 31)]
    private static void ApplyMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        UI_ButtonMigrationReport preview = PreviewProject();
        UnityEngine.Debug.Log(preview.Format("Preview"));
        if (preview.Failures.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Legacy UI_Button Migration",
                preview.Format("Preview") + "\n\n실패 원인을 먼저 해결하세요.",
                "OK");
            return;
        }

        if (!preview.HasCandidates)
        {
            EditorUtility.DisplayDialog("Legacy UI_Button Migration", "마이그레이션 대상이 없습니다.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply Legacy UI_Button Migration",
                preview.Format("Preview")
                + "\n\nButton/UIButtonPressEffect를 제거하고 prefab/scene을 저장합니다."
                + "\n깨끗한 작업 브랜치인지 확인한 뒤 진행하세요.",
                "Apply and Save",
                "Cancel"))
            return;

        UI_ButtonMigrationReport applied = ApplyProject();
        UnityEngine.Debug.Log(applied.Format("Applied"));
        UI_ButtonMigrationReport remaining = PreviewProject();
        UnityEngine.Debug.Log(remaining.Format("Verification"));
        EditorUtility.DisplayDialog(
            "Legacy UI_Button Migration",
            applied.Format("Applied") + "\n\n" + remaining.Format("Verification"),
            "OK");
    }

    /// <summary>배치 모드 read-only preview 진입점.</summary>
    public static void PreviewBatch()
    {
        UI_ButtonMigrationReport report = PreviewProject();
        UnityEngine.Debug.Log(report.Format("Batch Preview"));
        if (report.Failures.Count > 0)
            throw new InvalidOperationException("[UI_ButtonLegacyMigrator] PreviewBatch: preview failed");
    }

    /// <summary>배치 모드 적용 진입점. 명령줄에 -uiButtonMigrationApply가 있어야 저장합니다.</summary>
    public static void ApplyBatch()
    {
        if (!Environment.GetCommandLineArgs().Contains(BatchApplyFlag, StringComparer.Ordinal))
            throw new InvalidOperationException($"[UI_ButtonLegacyMigrator] ApplyBatch: missing {BatchApplyFlag}");

        UI_ButtonMigrationReport preview = PreviewProject();
        UnityEngine.Debug.Log(preview.Format("Batch Preview"));
        if (preview.Failures.Count > 0)
            throw new InvalidOperationException("[UI_ButtonLegacyMigrator] ApplyBatch: preview failed");

        UI_ButtonMigrationReport applied = ApplyProject();
        UnityEngine.Debug.Log(applied.Format("Batch Applied"));
        if (applied.Failures.Count > 0)
            throw new InvalidOperationException("[UI_ButtonLegacyMigrator] ApplyBatch: migration failed");

        UI_ButtonMigrationReport remaining = PreviewProject();
        UnityEngine.Debug.Log(remaining.Format("Batch Verification"));
        if (remaining.HasCandidates || remaining.Failures.Count > 0)
            throw new InvalidOperationException("[UI_ButtonLegacyMigrator] ApplyBatch: migration is incomplete");
    }

    internal static UI_ButtonMigrationReport PreviewProject()
    {
        return PreviewAssets(FindPrefabPaths(), FindScenePaths());
    }

    internal static UI_ButtonMigrationReport ApplyProject()
    {
        return ApplyAssets(FindPrefabPaths(), FindScenePaths());
    }

    internal static UI_ButtonMigrationReport PreviewAssets(
        IReadOnlyCollection<string> prefabPaths,
        IReadOnlyCollection<string> scenePaths)
    {
        var report = new UI_ButtonMigrationReport();
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            InspectScenes(scenePaths, report);
            InspectPrefabs(prefabPaths, report);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RestoreSceneSetup(originalSetup);
        }

        return report;
    }

    internal static UI_ButtonMigrationReport ApplyAssets(
        IReadOnlyCollection<string> prefabPaths,
        IReadOnlyCollection<string> scenePaths)
    {
        var report = new UI_ButtonMigrationReport();
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            // Scene/prefab instance의 Button override를 먼저 UI_Button override로 옮깁니다.
            ApplyScenes(scenePaths, report);
            ApplyPrefabs(prefabPaths, report);
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RestoreSceneSetup(originalSetup);
        }

        return report;
    }

    internal static bool MigrateHierarchy(GameObject root, UI_ButtonMigrationReport report, string assetPath)
    {
        UI_Button[] targets = root.GetComponentsInChildren<UI_Button>(true);
        foreach (UI_Button target in targets)
        {
            Button[] buttons = target.GetComponents<Button>();
            UIButtonPressEffect[] pressEffects = target.GetComponents<UIButtonPressEffect>();
            if (buttons.Length > 1 || pressEffects.Length > 1)
            {
                report.Failures.Add(
                    $"{assetPath} :: {GetHierarchyPath(target.transform)} has "
                    + $"Button={buttons.Length}, UIButtonPressEffect={pressEffects.Length}");
                return false;
            }
        }

        bool changed = false;
        foreach (UI_Button target in targets)
        {
            Button button = target.GetComponent<Button>();
            UIButtonPressEffect pressEffect = target.GetComponent<UIButtonPressEffect>();
            bool migrateButton = ShouldMigrateButton(button);
            bool migratePressEffect = ShouldMigratePressEffect(pressEffect);
            if (!migrateButton && !migratePressEffect) continue;

            var targetObject = new SerializedObject(target);
            var pendingOverrides = new List<PendingPrefabOverride>();
            if (migrateButton)
            {
                var buttonObject = new SerializedObject(button);
                CopyRequiredProperty(
                    buttonObject,
                    targetObject,
                    "m_Interactable",
                    assetPath,
                    target,
                    pendingOverrides);
                CopyUnityEvent(buttonObject, targetObject, assetPath, target, pendingOverrides);
            }

            SerializedProperty usePressEffect = targetObject.FindProperty("usePressEffect");
            if (usePressEffect == null)
                throw new InvalidOperationException($"[UI_ButtonLegacyMigrator] Missing usePressEffect: {assetPath}");

            if (pressEffect == null)
            {
                usePressEffect.boolValue = false;
            }
            else if (migratePressEffect)
            {
                usePressEffect.boolValue = pressEffect.enabled;
                var pressObject = new SerializedObject(pressEffect);
                CapturePrefabOverride(pressObject.FindProperty("m_Enabled"), usePressEffect, pendingOverrides);
                CopyRequiredProperty(
                    pressObject,
                    targetObject,
                    "scaleDownRatio",
                    assetPath,
                    target,
                    pendingOverrides);
                CopyRequiredProperty(
                    pressObject,
                    targetObject,
                    "isScaleOneFix",
                    assetPath,
                    target,
                    pendingOverrides);
                CopyRequiredProperty(
                    pressObject,
                    targetObject,
                    "targetTransform",
                    assetPath,
                    target,
                    pendingOverrides);
            }

            targetObject.ApplyModifiedPropertiesWithoutUndo();
            ApplyPrefabOverrides(target, pendingOverrides);
            EditorUtility.SetDirty(target);

            if (migratePressEffect)
            {
                UnityEngine.Object.DestroyImmediate(pressEffect);
                report.PressEffectsRemoved++;
            }

            if (migrateButton)
            {
                UnityEngine.Object.DestroyImmediate(button);
                report.ButtonsRemoved++;
            }

            report.TargetsMigrated++;
            changed = true;
        }

        return changed;
    }

    private static void InspectScenes(IReadOnlyCollection<string> scenePaths, UI_ButtonMigrationReport report)
    {
        string[] paths = scenePaths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        for (int index = 0; index < paths.Length; index++)
        {
            string path = paths[index];
            EditorUtility.DisplayProgressBar(
                "Legacy UI_Button Migration Preview",
                $"Scene {index + 1}/{paths.Length}: {path}",
                paths.Length == 0 ? 1f : (float)index / paths.Length);

            try
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                InspectRoots(scene.GetRootGameObjects(), report, path);
                report.AssetsInspected++;
            }
            catch (Exception exception)
            {
                report.Failures.Add($"{path} :: {exception.Message}");
            }
        }
    }

    private static void InspectPrefabs(IReadOnlyCollection<string> prefabPaths, UI_ButtonMigrationReport report)
    {
        string[] paths = prefabPaths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        for (int index = 0; index < paths.Length; index++)
        {
            string path = paths[index];
            EditorUtility.DisplayProgressBar(
                "Legacy UI_Button Migration Preview",
                $"Prefab {index + 1}/{paths.Length}: {path}",
                paths.Length == 0 ? 1f : (float)index / paths.Length);

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                InspectRoots(new[] { root }, report, path);
                report.AssetsInspected++;
            }
            catch (Exception exception)
            {
                report.Failures.Add($"{path} :: {exception.Message}");
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ApplyScenes(IReadOnlyCollection<string> scenePaths, UI_ButtonMigrationReport report)
    {
        string[] paths = scenePaths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        for (int index = 0; index < paths.Length; index++)
        {
            string path = paths[index];
            if (!IsProjectAssetPath(path))
            {
                report.Failures.Add($"{path} :: refusing to save a scene outside Assets/");
                continue;
            }
            EditorUtility.DisplayProgressBar(
                "Legacy UI_Button Migration",
                $"Scene {index + 1}/{paths.Length}: {path}",
                paths.Length == 0 ? 1f : (float)index / paths.Length);

            try
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (!ValidateRoots(scene.GetRootGameObjects(), report, path)) continue;

                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    changed |= MigrateHierarchy(root, report, path);

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("SaveScene returned false");
                    report.AssetsChanged++;
                }

                report.AssetsInspected++;
            }
            catch (Exception exception)
            {
                report.Failures.Add($"{path} :: {exception.Message}");
            }
        }
    }

    private static void ApplyPrefabs(IReadOnlyCollection<string> prefabPaths, UI_ButtonMigrationReport report)
    {
        string[] paths = prefabPaths
            .OrderByDescending(GetPrefabInheritanceDepth)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        for (int index = 0; index < paths.Length; index++)
        {
            string path = paths[index];
            if (!IsProjectAssetPath(path))
            {
                report.Failures.Add($"{path} :: refusing to save a prefab outside Assets/");
                continue;
            }
            EditorUtility.DisplayProgressBar(
                "Legacy UI_Button Migration",
                $"Prefab {index + 1}/{paths.Length}: {path}",
                paths.Length == 0 ? 1f : (float)index / paths.Length);

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                if (!ValidateRoots(new[] { root }, report, path)) continue;
                if (MigrateHierarchy(root, report, path))
                {
                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                    if (saved == null) throw new InvalidOperationException("SaveAsPrefabAsset returned null");
                    report.AssetsChanged++;
                }

                report.AssetsInspected++;
            }
            catch (Exception exception)
            {
                report.Failures.Add($"{path} :: {exception.Message}");
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void InspectRoots(IEnumerable<GameObject> roots, UI_ButtonMigrationReport report, string assetPath)
    {
        foreach (GameObject root in roots)
        {
            foreach (UI_Button target in root.GetComponentsInChildren<UI_Button>(true))
            {
                Button[] buttons = target.GetComponents<Button>();
                UIButtonPressEffect[] pressEffects = target.GetComponents<UIButtonPressEffect>();
                if (buttons.Length > 1 || pressEffects.Length > 1)
                {
                    report.Failures.Add(
                        $"{assetPath} :: {GetHierarchyPath(target.transform)} has "
                        + $"Button={buttons.Length}, UIButtonPressEffect={pressEffects.Length}");
                    continue;
                }

                if (buttons.Length == 0 && pressEffects.Length == 0) continue;

                bool migrateButton = buttons.Length == 1 && ShouldMigrateButton(buttons[0]);
                bool migratePressEffect = pressEffects.Length == 1 && ShouldMigratePressEffect(pressEffects[0]);
                if (!migrateButton && !migratePressEffect) continue;

                report.CandidateTargets++;
                report.CandidateButtons += migrateButton ? 1 : 0;
                report.CandidatePressEffects += migratePressEffect ? 1 : 0;
                if (migrateButton)
                {
                    Button button = buttons[0];
                    if (button.transition != Selectable.Transition.None)
                    {
                        report.Warnings.Add(
                            $"{assetPath} :: {GetHierarchyPath(target.transform)} uses Button transition={button.transition}");
                    }

                    if (button.navigation.mode != Navigation.Mode.Automatic)
                    {
                        report.Warnings.Add(
                            $"{assetPath} :: {GetHierarchyPath(target.transform)} uses Button navigation={button.navigation.mode}");
                    }
                }
            }
        }
    }

    private static bool ValidateRoots(IEnumerable<GameObject> roots, UI_ButtonMigrationReport report, string assetPath)
    {
        int failureCount = report.Failures.Count;
        foreach (GameObject root in roots)
        {
            foreach (UI_Button target in root.GetComponentsInChildren<UI_Button>(true))
            {
                int buttonCount = target.GetComponents<Button>().Length;
                int pressEffectCount = target.GetComponents<UIButtonPressEffect>().Length;
                if (buttonCount > 1 || pressEffectCount > 1)
                {
                    report.Failures.Add(
                        $"{assetPath} :: {GetHierarchyPath(target.transform)} has "
                        + $"Button={buttonCount}, UIButtonPressEffect={pressEffectCount}");
                }
            }
        }

        return report.Failures.Count == failureCount;
    }

    private static void CopyRequiredProperty(
        SerializedObject sourceObject,
        SerializedObject targetObject,
        string propertyName,
        string assetPath,
        UI_Button owner,
        ICollection<PendingPrefabOverride> pendingOverrides)
    {
        SerializedProperty source = sourceObject.FindProperty(propertyName);
        SerializedProperty target = targetObject.FindProperty(propertyName);
        if (source == null || target == null)
        {
            throw new InvalidOperationException(
                $"[UI_ButtonLegacyMigrator] Missing property {propertyName}: "
                + $"{assetPath} :: {GetHierarchyPath(owner.transform)}");
        }

        CopyValue(source, target);
        CapturePrefabOverride(source, target, pendingOverrides);
    }

    private static bool ShouldMigrateButton(Button button)
    {
        return ShouldMigrateComponent(button, new[]
        {
            "m_Interactable",
            "m_OnClick",
        });
    }

    private static bool ShouldMigratePressEffect(UIButtonPressEffect pressEffect)
    {
        return ShouldMigrateComponent(pressEffect, new[]
        {
            "m_Enabled",
            "scaleDownRatio",
            "isScaleOneFix",
            "targetTransform",
        });
    }

    private static bool ShouldMigrateComponent(Component component, IReadOnlyCollection<string> propertyRoots)
    {
        if (component == null) return false;
        if (!PrefabUtility.IsPartOfPrefabInstance(component)) return true;
        if (PrefabUtility.IsAddedComponentOverride(component)) return true;

        var serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.GetIterator();
        while (property.Next(true))
        {
            if (!property.prefabOverride) continue;
            foreach (string root in propertyRoots)
            {
                if (string.Equals(property.propertyPath, root, StringComparison.Ordinal)
                    || property.propertyPath.StartsWith(root + ".", StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static void CopyUnityEvent(
        SerializedObject sourceObject,
        SerializedObject targetObject,
        string assetPath,
        UI_Button owner,
        ICollection<PendingPrefabOverride> pendingOverrides)
    {
        SerializedProperty sourceCalls = sourceObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        SerializedProperty targetCalls = targetObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (sourceCalls == null || targetCalls == null)
        {
            throw new InvalidOperationException(
                $"[UI_ButtonLegacyMigrator] Missing m_OnClick calls: "
                + $"{assetPath} :: {GetHierarchyPath(owner.transform)}");
        }

        string[] relativePaths =
        {
            "m_Target",
            "m_TargetAssemblyTypeName",
            "m_MethodName",
            "m_Mode",
            "m_Arguments.m_ObjectArgument",
            "m_Arguments.m_ObjectArgumentAssemblyTypeName",
            "m_Arguments.m_IntArgument",
            "m_Arguments.m_FloatArgument",
            "m_Arguments.m_StringArgument",
            "m_Arguments.m_BoolArgument",
            "m_CallState",
        };

        targetCalls.arraySize = sourceCalls.arraySize;
        if (sourceCalls.prefabOverride)
        {
            pendingOverrides.Add(PendingPrefabOverride.FromArraySize(
                targetCalls.propertyPath + ".Array.size",
                targetCalls.arraySize));
        }

        for (int index = 0; index < sourceCalls.arraySize; index++)
        {
            SerializedProperty sourceCall = sourceCalls.GetArrayElementAtIndex(index);
            SerializedProperty targetCall = targetCalls.GetArrayElementAtIndex(index);
            foreach (string relativePath in relativePaths)
            {
                SerializedProperty source = sourceCall.FindPropertyRelative(relativePath);
                SerializedProperty target = targetCall.FindPropertyRelative(relativePath);
                if (source == null || target == null)
                {
                    throw new InvalidOperationException(
                        $"[UI_ButtonLegacyMigrator] Missing UnityEvent property {relativePath}: "
                        + $"{assetPath} :: {GetHierarchyPath(owner.transform)}");
                }

                CopyValue(source, target);
                CapturePrefabOverride(source, target, pendingOverrides);
            }
        }
    }

    private static void CapturePrefabOverride(
        SerializedProperty source,
        SerializedProperty target,
        ICollection<PendingPrefabOverride> pendingOverrides)
    {
        if (source == null || target == null || !source.prefabOverride) return;
        pendingOverrides.Add(PendingPrefabOverride.FromProperty(target));
    }

    private static void ApplyPrefabOverrides(
        UI_Button target,
        IReadOnlyCollection<PendingPrefabOverride> pendingOverrides)
    {
        if (pendingOverrides.Count == 0 || !PrefabUtility.IsPartOfPrefabInstance(target)) return;

        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
        UnityEngine.Object sourceTarget = PrefabUtility.GetCorrespondingObjectFromSource(target);
        if (instanceRoot == null || sourceTarget == null) return;

        var modifications = new List<PropertyModification>(
            PrefabUtility.GetPropertyModifications(instanceRoot) ?? Array.Empty<PropertyModification>());
        foreach (PendingPrefabOverride pending in pendingOverrides)
        {
            modifications.RemoveAll(modification =>
                modification.target == sourceTarget
                && string.Equals(modification.propertyPath, pending.PropertyPath, StringComparison.Ordinal));
            modifications.Add(new PropertyModification
            {
                target = sourceTarget,
                propertyPath = pending.PropertyPath,
                value = pending.Value,
                objectReference = pending.ObjectReference,
            });
        }

        PrefabUtility.SetPropertyModifications(instanceRoot, modifications.ToArray());
    }

    private static void CopyValue(SerializedProperty source, SerializedProperty target)
    {
        switch (source.propertyType)
        {
            case SerializedPropertyType.Boolean:
                target.boolValue = source.boolValue;
                break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
                target.intValue = source.intValue;
                break;
            case SerializedPropertyType.Float:
                target.floatValue = source.floatValue;
                break;
            case SerializedPropertyType.String:
                target.stringValue = source.stringValue;
                break;
            case SerializedPropertyType.ObjectReference:
                target.objectReferenceValue = source.objectReferenceValue;
                break;
            default:
                throw new NotSupportedException(
                    $"[UI_ButtonLegacyMigrator] Unsupported property type: {source.propertyPath}={source.propertyType}");
        }
    }

    private static int GetPrefabInheritanceDepth(string path)
    {
        GameObject current = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        int depth = 0;
        while (current != null && PrefabUtility.GetPrefabAssetType(current) == PrefabAssetType.Variant)
        {
            current = PrefabUtility.GetCorrespondingObjectFromSource(current);
            depth++;
        }

        return depth;
    }

    private static void RestoreSceneSetup(SceneSetup[] setup)
    {
        if (setup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            return;
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static string[] FindPrefabPaths()
    {
        return FindAssetPaths("t:Prefab", new[] { "Assets" });
    }

    private static string[] FindScenePaths()
    {
        return FindAssetPaths("t:Scene", new[] { "Assets" });
    }

    private static string[] FindAssetPaths(string filter, string[] folders)
    {
        return AssetDatabase.FindAssets(filter, folders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsProjectAssetPath(string path)
    {
        return !string.IsNullOrEmpty(path)
               && (path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal));
    }

    private static string GetHierarchyPath(Transform target)
    {
        var names = new Stack<string>();
        for (Transform current = target; current != null; current = current.parent)
            names.Push(current.name);
        return string.Join("/", names);
    }
}

internal readonly struct PendingPrefabOverride
{
    internal readonly string PropertyPath;
    internal readonly string Value;
    internal readonly UnityEngine.Object ObjectReference;

    private PendingPrefabOverride(string propertyPath, string value, UnityEngine.Object objectReference)
    {
        PropertyPath = propertyPath;
        Value = value;
        ObjectReference = objectReference;
    }

    internal static PendingPrefabOverride FromArraySize(string propertyPath, int arraySize)
    {
        return new PendingPrefabOverride(
            propertyPath,
            arraySize.ToString(CultureInfo.InvariantCulture),
            null);
    }

    internal static PendingPrefabOverride FromProperty(SerializedProperty property)
    {
        return property.propertyType switch
        {
            SerializedPropertyType.Boolean => new PendingPrefabOverride(
                property.propertyPath,
                property.boolValue ? "1" : "0",
                null),
            SerializedPropertyType.Integer or SerializedPropertyType.Enum => new PendingPrefabOverride(
                property.propertyPath,
                property.intValue.ToString(CultureInfo.InvariantCulture),
                null),
            SerializedPropertyType.Float => new PendingPrefabOverride(
                property.propertyPath,
                property.floatValue.ToString("R", CultureInfo.InvariantCulture),
                null),
            SerializedPropertyType.String => new PendingPrefabOverride(
                property.propertyPath,
                property.stringValue,
                null),
            SerializedPropertyType.ObjectReference => new PendingPrefabOverride(
                property.propertyPath,
                null,
                property.objectReferenceValue),
            _ => throw new NotSupportedException(
                $"[UI_ButtonLegacyMigrator] Unsupported prefab override type: "
                + $"{property.propertyPath}={property.propertyType}"),
        };
    }
}

internal sealed class UI_ButtonMigrationReport
{
    internal int AssetsInspected;
    internal int AssetsChanged;
    internal int CandidateTargets;
    internal int CandidateButtons;
    internal int CandidatePressEffects;
    internal int TargetsMigrated;
    internal int ButtonsRemoved;
    internal int PressEffectsRemoved;
    internal readonly List<string> Warnings = new();
    internal readonly List<string> Failures = new();

    internal bool HasCandidates => CandidateTargets > 0 || TargetsMigrated > 0;

    internal string Format(string phase)
    {
        var lines = new List<string>
        {
            $"{phase}",
            $"Assets inspected: {AssetsInspected}",
            $"Assets changed: {AssetsChanged}",
            $"Candidates: targets={CandidateTargets}, Button={CandidateButtons}, UIButtonPressEffect={CandidatePressEffects}",
            $"Migrated: targets={TargetsMigrated}, Button removed={ButtonsRemoved}, UIButtonPressEffect removed={PressEffectsRemoved}",
            $"Warnings: {Warnings.Count}",
            $"Failures: {Failures.Count}",
        };

        foreach (string warning in Warnings.Take(20)) lines.Add("WARN: " + warning);
        if (Warnings.Count > 20) lines.Add($"WARN: ... {Warnings.Count - 20} more");
        foreach (string failure in Failures.Take(20)) lines.Add("FAIL: " + failure);
        if (Failures.Count > 20) lines.Add($"FAIL: ... {Failures.Count - 20} more");
        return string.Join("\n", lines);
    }
}
#endif
