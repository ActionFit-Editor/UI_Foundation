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

    private sealed class TextMaterialOverride
    {
        public readonly Material Original;
        public readonly Material Applied;

        public TextMaterialOverride(Material original, Material applied)
        {
            Original = original;
            Applied = applied;
        }
    }

    // 버튼이 실제로 교체한 재질만 추적한다. UI_Text가 이후 교체한 재질은 버튼 소유가 아니다.
    private Dictionary<TextMeshProUGUI, TextMaterialOverride> _textMaterialOverrides;

    // Disable 시: fontSharedMaterial을 본문(face)+외곽선+underlay+glow 모두 어둡게 한 인스턴스로 교체.
    // ※ tmp.color(버텍스 틴트)는 건드리지 않음 — 외부 코드(텍스트/로컬라이즈 갱신)가 tmp.color를 재설정해도 어둡기가 유지되도록 머티리얼 기반으로 처리. off면 무시.
    private void ApplyDisableTextColor()
    {
        if (!useDisableTextColor || targetTexts == null) return;

        _textMaterialOverrides ??= new Dictionary<TextMeshProUGUI, TextMaterialOverride>(targetTexts.Length);
        foreach (var tmp in targetTexts)
        {
            if (tmp == null) continue;

            if (_textMaterialOverrides.TryGetValue(tmp, out TextMaterialOverride currentOverride))
            {
                if (ReferenceEquals(tmp.fontSharedMaterial, currentOverride.Applied)) continue;
                _textMaterialOverrides.Remove(tmp);
            }

            Material originalMaterial = tmp.fontSharedMaterial;
            if (originalMaterial == null) continue;

            Material disabledMaterial = OutlineMaterialCache.GetDarkened(originalMaterial, disableTextDarkenFactor);
            if (disabledMaterial == null || ReferenceEquals(disabledMaterial, originalMaterial)) continue;

            tmp.fontSharedMaterial = disabledMaterial;
            _textMaterialOverrides[tmp] = new TextMaterialOverride(originalMaterial, disabledMaterial);
        }
    }

    // 버튼이 적용한 재질이 그대로 남아 있을 때만 원본을 복원한다.
    private void RestoreTextColor()
    {
        if (_textMaterialOverrides == null || _textMaterialOverrides.Count == 0) return;

        foreach (KeyValuePair<TextMeshProUGUI, TextMaterialOverride> pair in _textMaterialOverrides)
        {
            TextMeshProUGUI tmp = pair.Key;
            TextMaterialOverride materialOverride = pair.Value;
            if (tmp == null || !ReferenceEquals(tmp.fontSharedMaterial, materialOverride.Applied)) continue;

            tmp.fontSharedMaterial = materialOverride.Original;
        }

        _textMaterialOverrides.Clear();
    }
}
