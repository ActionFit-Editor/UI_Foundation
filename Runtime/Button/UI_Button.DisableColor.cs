using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI_Button — UseDisableColor: Disable 시 targetImages의 색을 어둡게.</summary>
public partial class UI_Button
{
    [SerializeField] private bool useDisableColor = false; // Disable 시 targetImages 색을 disableColorDarkenFactor로 어둡게
    [SerializeField, ShowIf(nameof(useDisableColor))] private Image[] targetImages; // 어둡게 할 Image 목록 (Inspector 드래그앤드롭 / 체크 시 자동 채움)
    [SerializeField, Range(0f, 1f), ShowIf(nameof(useDisableColor))] private float disableColorDarkenFactor = 0.4f; // 이미지 색 곱 계수(0=검정 / 1=원본, 알파 보존)

    private Dictionary<Image, Color> _originalImageColors; // targetImages 각 원본 color

    // Awake: targetImages 각 Image의 원본 color를 기록
    private void CacheImageColors()
    {
        _originalImageColors = new Dictionary<Image, Color>(targetImages != null ? targetImages.Length : 0);
        if (targetImages == null) return;
        foreach (var img in targetImages)
        {
            if (img == null) continue;
            _originalImageColors[img] = img.color;
        }
    }

    // Disable 시: targetImages 각 color를 원본 기준으로 어둡게 (반복 호출에도 누적되지 않음). off면 무시.
    private void ApplyDisableColor()
    {
        if (!useDisableColor || targetImages == null) return;
        if (_originalImageColors == null) CacheImageColors();
        foreach (var img in targetImages)
        {
            if (img == null) continue;
            if (!_originalImageColors.TryGetValue(img, out var orig)) orig = img.color;
            img.color = Darken(orig, disableColorDarkenFactor);
        }
    }

    // 복원: targetImages 각 color를 원본으로. off면 무시.
    private void RestoreColor()
    {
        if (targetImages == null) return;
        if (_originalImageColors == null) return;
        foreach (var img in targetImages)
        {
            if (img == null) continue;
            if (!_originalImageColors.TryGetValue(img, out var originalColor)) continue;
            img.color = originalColor;
        }
    }
}
