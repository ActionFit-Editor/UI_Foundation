using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>UI_Button — UseDisableTextColor: Disable 시 targetTexts의 본문(face)+외곽선+underlay+glow를 fontSharedMaterial 교체로 일괄 어둡게.</summary>
public partial class UI_Button
{
    // 구 useDisableTextOutlineColor에서 개명 — 의미는 "텍스트 전체(face+외곽선+underlay+glow) 어둡게". 기존 직렬화 값은 FormerlySerializedAs로 보존.
    [SerializeField, FormerlySerializedAs("useDisableTextOutlineColor")] private bool useDisableTextColor = false;
    [SerializeField, ShowIf(nameof(useDisableTextColor))] private TextMeshProUGUI[] targetTexts; // 어둡게 할 TMP 목록 (Inspector 드래그앤드롭)
    [SerializeField, Range(0f, 1f), ShowIf(nameof(useDisableTextColor))] private float disableTextDarkenFactor = 0.4f; // 색 곱 계수(0=검정 / 1=원본, 알파 보존)

    private Dictionary<TextMeshProUGUI, Material> _originalTextMaterials; // targetTexts 각 원본 fontSharedMaterial

    // Awake: targetTexts 각 TMP의 원본 fontSharedMaterial 기록
    private void CacheTextMaterials()
    {
        int n = targetTexts != null ? targetTexts.Length : 0;
        _originalTextMaterials = new Dictionary<TextMeshProUGUI, Material>(n);
        if (targetTexts == null) return;
        foreach (var tmp in targetTexts)
        {
            if (tmp == null) continue;
            _originalTextMaterials[tmp] = tmp.fontSharedMaterial;
        }
    }

    // Disable 시: fontSharedMaterial을 본문(face)+외곽선+underlay+glow 모두 어둡게 한 인스턴스로 교체.
    // ※ tmp.color(버텍스 틴트)는 건드리지 않음 — 외부 코드(텍스트/로컬라이즈 갱신)가 tmp.color를 재설정해도 어둡기가 유지되도록 머티리얼 기반으로 처리. off면 무시.
    private void ApplyDisableTextColor()
    {
        if (!useDisableTextColor || targetTexts == null) return;
        if (_originalTextMaterials == null) CacheTextMaterials();
        foreach (var tmp in targetTexts)
        {
            if (tmp == null) continue;
            if (_originalTextMaterials.TryGetValue(tmp, out var originalMat) && originalMat != null)
                tmp.fontSharedMaterial = OutlineMaterialCache.GetDarkened(originalMat, disableTextDarkenFactor);
        }
    }

    // 복원: fontSharedMaterial을 원본으로. off면 무시.
    private void RestoreTextColor()
    {
        if (targetTexts == null) return;
        if (_originalTextMaterials == null) return;
        foreach (var tmp in targetTexts)
        {
            if (tmp == null) continue;
            if (_originalTextMaterials.TryGetValue(tmp, out var originalMat) && originalMat != null)
                tmp.fontSharedMaterial = originalMat;
        }
    }
}
