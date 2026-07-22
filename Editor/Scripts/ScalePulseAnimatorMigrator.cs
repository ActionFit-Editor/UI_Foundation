#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// 지정된 Animator Controller GUID를 사용하는 YAML 컴포넌트를 동일 fileID의 ScalePulse로 전환합니다.
/// </summary>
public static class ScalePulseAnimatorMigrator
{
    #region Fields

    private const string ScalePulseScriptPath =
        "Packages/com.actionfit.ui.foundation/Runtime/Util/ScalePulse.cs";

    private static readonly Regex AnimatorBlockPattern = new(
        @"^--- !u!95 &(?<fileId>-?\d+)(?<stripped> stripped)?\r?\nAnimator:\r?\n(?<body>.*?)(?=^--- !u!|\z)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);

    private static readonly Regex ControllerPattern = new(
        @"^  m_Controller: \{fileID: 9100000, guid: (?<guid>[0-9a-fA-F]{32}), type: 2\}\r?$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CorrespondingSourcePattern = new(
        @"^  m_CorrespondingSourceObject: \{fileID: (?<fileId>-?\d+), guid: (?<guid>[0-9a-fA-F]{32}), type: 3\}\r?$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    #endregion

    #region Public Methods

    /// <summary>요청된 자산을 읽기만 하고 전환 대상과 오류를 반환합니다.</summary>
    public static ScalePulseAnimatorMigrationReport Preview(ScalePulseAnimatorMigrationRequest request)
    {
        return Inspect(request, false);
    }

    /// <summary>Preview와 같은 정확한 대상만 변경하고 강제 재임포트합니다.</summary>
    public static ScalePulseAnimatorMigrationReport Apply(ScalePulseAnimatorMigrationRequest request)
    {
        ScalePulseAnimatorMigrationReport preview = Inspect(request, false);
        if (preview.Failures.Count > 0) return preview;

        return Inspect(request, true);
    }

    #endregion

    #region Internal Methods

    internal static string RewriteYamlForTests(
        string yaml,
        IReadOnlyCollection<string> controllerGuids,
        string scalePulseScriptGuid,
        out int replacementCount)
    {
        return RewriteYaml(
            yaml,
            controllerGuids,
            Array.Empty<ScalePulsePrefabComponentReference>(),
            scalePulseScriptGuid,
            out replacementCount);
    }

    internal static string RewriteYamlForTests(
        string yaml,
        IReadOnlyCollection<string> controllerGuids,
        IReadOnlyCollection<ScalePulsePrefabComponentReference> prefabComponentReferences,
        string scalePulseScriptGuid,
        out int replacementCount)
    {
        return RewriteYaml(
            yaml,
            controllerGuids,
            prefabComponentReferences,
            scalePulseScriptGuid,
            out replacementCount);
    }

    internal static bool IsExcludedForTests(
        string assetPath,
        IReadOnlyCollection<string> excludedPathPrefixes)
    {
        return IsExcluded(assetPath, excludedPathPrefixes);
    }

    #endregion

    #region Private Methods

    private static ScalePulseAnimatorMigrationReport Inspect(
        ScalePulseAnimatorMigrationRequest request,
        bool apply)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var report = new ScalePulseAnimatorMigrationReport();
        string scriptGuid = AssetDatabase.AssetPathToGUID(ScalePulseScriptPath);
        if (string.IsNullOrEmpty(scriptGuid))
        {
            report.Failures.Add($"{ScalePulseScriptPath} :: ScalePulse script GUID is missing");
            return report;
        }

        foreach (string assetPath in request.AssetPaths.OrderBy(path => path, StringComparer.Ordinal))
        {
            if (IsExcluded(assetPath, request.ExcludedPathPrefixes))
            {
                report.ExcludedAssets.Add(assetPath);
                continue;
            }

            try
            {
                ValidateAssetPath(assetPath);
                string fullPath = Path.GetFullPath(assetPath);
                string original = File.ReadAllText(fullPath);
                string rewritten = RewriteYaml(
                    original,
                    request.ControllerGuids,
                    request.PrefabComponentReferences,
                    scriptGuid,
                    out int replacementCount);

                report.AssetsInspected++;
                if (replacementCount == 0) continue;

                report.CandidateComponents += replacementCount;
                report.CandidateAssets.Add(assetPath);
                if (!apply) continue;

                WriteAtomically(fullPath, rewritten);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                report.ComponentsChanged += replacementCount;
                report.AssetsChanged++;
            }
            catch (Exception exception)
            {
                report.Failures.Add($"{assetPath} :: {exception.Message}");
            }
        }

        return report;
    }

    private static string RewriteYaml(
        string yaml,
        IReadOnlyCollection<string> controllerGuids,
        IReadOnlyCollection<ScalePulsePrefabComponentReference> prefabComponentReferences,
        string scalePulseScriptGuid,
        out int replacementCount)
    {
        if (yaml == null) throw new ArgumentNullException(nameof(yaml));
        if (controllerGuids == null || controllerGuids.Count == 0)
            throw new ArgumentException("At least one Animator Controller GUID is required.", nameof(controllerGuids));
        if (prefabComponentReferences == null)
            throw new ArgumentNullException(nameof(prefabComponentReferences));
        if (!IsGuid(scalePulseScriptGuid))
            throw new ArgumentException("ScalePulse script GUID is invalid.", nameof(scalePulseScriptGuid));

        var targetGuids = new HashSet<string>(controllerGuids, StringComparer.OrdinalIgnoreCase);
        var targetSources = new HashSet<string>(
            prefabComponentReferences.Select(reference => reference.Key),
            StringComparer.OrdinalIgnoreCase);
        int replacements = 0;
        string rewritten = AnimatorBlockPattern.Replace(yaml, match =>
        {
            Match controller = ControllerPattern.Match(match.Groups["body"].Value);
            if (controller.Success && targetGuids.Contains(controller.Groups["guid"].Value))
            {
                replacements++;
                return BuildScalePulseBlock(match, scalePulseScriptGuid);
            }

            Match source = CorrespondingSourcePattern.Match(match.Groups["body"].Value);
            string sourceKey = source.Success
                ? ScalePulsePrefabComponentReference.BuildKey(
                    source.Groups["guid"].Value,
                    source.Groups["fileId"].Value)
                : string.Empty;
            if (!match.Groups["stripped"].Success || !targetSources.Contains(sourceKey))
                return match.Value;

            replacements++;
            return BuildStrippedScalePulseBlock(match);
        });
        replacementCount = replacements;
        return rewritten;
    }

    private static string BuildScalePulseBlock(Match animatorMatch, string scriptGuid)
    {
        string body = animatorMatch.Groups["body"].Value;
        string newline = animatorMatch.Value.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string fileId = animatorMatch.Groups["fileId"].Value;
        string objectHideFlags = ReadRequiredValue(body, "m_ObjectHideFlags");
        string correspondingSource = ReadRequiredValue(body, "m_CorrespondingSourceObject");
        string prefabInstance = ReadRequiredValue(body, "m_PrefabInstance");
        string prefabAsset = ReadRequiredValue(body, "m_PrefabAsset");
        string gameObject = ReadRequiredValue(body, "m_GameObject");
        string enabled = ReadRequiredValue(body, "m_Enabled");

        var builder = new StringBuilder();
        builder.Append("--- !u!114 &").Append(fileId).Append(newline);
        builder.Append("MonoBehaviour:").Append(newline);
        builder.Append("  m_ObjectHideFlags: ").Append(objectHideFlags).Append(newline);
        builder.Append("  m_CorrespondingSourceObject: ").Append(correspondingSource).Append(newline);
        builder.Append("  m_PrefabInstance: ").Append(prefabInstance).Append(newline);
        builder.Append("  m_PrefabAsset: ").Append(prefabAsset).Append(newline);
        builder.Append("  m_GameObject: ").Append(gameObject).Append(newline);
        builder.Append("  m_Enabled: ").Append(enabled).Append(newline);
        builder.Append("  m_EditorHideFlags: 0").Append(newline);
        builder.Append("  m_Script: {fileID: 11500000, guid: ").Append(scriptGuid).Append(", type: 3}").Append(newline);
        builder.Append("  m_Name:").Append(newline);
        builder.Append("  m_EditorClassIdentifier:").Append(newline);
        return builder.ToString();
    }

    private static string BuildStrippedScalePulseBlock(Match animatorMatch)
    {
        string newline = animatorMatch.Value.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return "--- !u!114 &" + animatorMatch.Groups["fileId"].Value + " stripped" + newline
            + "MonoBehaviour:" + newline
            + animatorMatch.Groups["body"].Value;
    }

    private static string ReadRequiredValue(string body, string propertyName)
    {
        Match match = Regex.Match(
            body,
            $@"^  {Regex.Escape(propertyName)}: (?<value>.*)\r?$",
            RegexOptions.Multiline);
        if (!match.Success)
            throw new InvalidOperationException($"Animator YAML is missing {propertyName}");

        return match.Groups["value"].Value.TrimEnd('\r');
    }

    private static void ValidateAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            throw new ArgumentException("Asset path is empty.");
        if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal)
            && !assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            throw new InvalidOperationException("Only Assets/ and Packages/ paths are supported");
        if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
            && !assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only prefab and scene YAML assets are supported");
        if (!File.Exists(Path.GetFullPath(assetPath)))
            throw new FileNotFoundException("Asset file was not found", assetPath);
    }

    private static bool IsExcluded(
        string assetPath,
        IReadOnlyCollection<string> excludedPathPrefixes)
    {
        if (excludedPathPrefixes == null) return false;
        return excludedPathPrefixes.Any(prefix =>
            !string.IsNullOrEmpty(prefix)
            && assetPath.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static void WriteAtomically(string fullPath, string contents)
    {
        string temporaryPath = fullPath + ".scale-pulse.tmp";
        try
        {
            File.WriteAllText(temporaryPath, contents, new UTF8Encoding(false));
            FileUtil.ReplaceFile(temporaryPath, fullPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static bool IsGuid(string value)
    {
        return !string.IsNullOrEmpty(value)
            && value.Length == 32
            && value.All(character => Uri.IsHexDigit(character));
    }

    #endregion
}

public sealed class ScalePulseAnimatorMigrationRequest
{
    public ScalePulseAnimatorMigrationRequest(
        IReadOnlyCollection<string> controllerGuids,
        IReadOnlyCollection<string> assetPaths,
        IReadOnlyCollection<string> excludedPathPrefixes = null,
        IReadOnlyCollection<ScalePulsePrefabComponentReference> prefabComponentReferences = null)
    {
        if (controllerGuids == null || controllerGuids.Count == 0)
            throw new ArgumentException("At least one Animator Controller GUID is required.", nameof(controllerGuids));
        if (assetPaths == null || assetPaths.Count == 0)
            throw new ArgumentException("At least one asset path is required.", nameof(assetPaths));
        if (controllerGuids.Any(guid => guid == null || guid.Length != 32 || !guid.All(Uri.IsHexDigit)))
            throw new ArgumentException("Every Animator Controller GUID must be a 32-character hex value.", nameof(controllerGuids));

        ControllerGuids = controllerGuids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        AssetPaths = assetPaths.Distinct(StringComparer.Ordinal).ToArray();
        ExcludedPathPrefixes = excludedPathPrefixes?.Distinct(StringComparer.Ordinal).ToArray()
            ?? Array.Empty<string>();
        PrefabComponentReferences = prefabComponentReferences?.ToArray()
            ?? Array.Empty<ScalePulsePrefabComponentReference>();
    }

    public IReadOnlyCollection<string> ControllerGuids { get; }
    public IReadOnlyCollection<string> AssetPaths { get; }
    public IReadOnlyCollection<string> ExcludedPathPrefixes { get; }
    public IReadOnlyCollection<ScalePulsePrefabComponentReference> PrefabComponentReferences { get; }
}

public sealed class ScalePulsePrefabComponentReference
{
    public ScalePulsePrefabComponentReference(string prefabGuid, long componentFileId)
    {
        if (prefabGuid == null || prefabGuid.Length != 32 || !prefabGuid.All(Uri.IsHexDigit))
            throw new ArgumentException("Prefab GUID must be a 32-character hex value.", nameof(prefabGuid));

        PrefabGuid = prefabGuid;
        ComponentFileId = componentFileId;
    }

    public string PrefabGuid { get; }
    public long ComponentFileId { get; }
    internal string Key => BuildKey(PrefabGuid, ComponentFileId.ToString());

    internal static string BuildKey(string prefabGuid, string componentFileId)
    {
        return prefabGuid + ":" + componentFileId;
    }
}

public sealed class ScalePulseAnimatorMigrationReport
{
    public int AssetsInspected { get; internal set; }
    public int CandidateComponents { get; internal set; }
    public int AssetsChanged { get; internal set; }
    public int ComponentsChanged { get; internal set; }
    public List<string> CandidateAssets { get; } = new();
    public List<string> ExcludedAssets { get; } = new();
    public List<string> Failures { get; } = new();
    public bool HasCandidates => CandidateComponents > 0;

    public string Format(string operation)
    {
        return $"[ScalePulseAnimatorMigrator] {operation}: "
            + $"inspected={AssetsInspected}, candidates={CandidateComponents}, "
            + $"changedAssets={AssetsChanged}, changedComponents={ComponentsChanged}, "
            + $"excluded={ExcludedAssets.Count}, failures={Failures.Count}"
            + (CandidateAssets.Count > 0 ? $"\nCandidates:\n- {string.Join("\n- ", CandidateAssets)}" : string.Empty)
            + (Failures.Count > 0 ? $"\nFailures:\n- {string.Join("\n- ", Failures)}" : string.Empty);
    }
}
#endif
