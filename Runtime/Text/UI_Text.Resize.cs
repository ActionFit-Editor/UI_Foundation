using UnityEngine;

/// <summary>UI_Text — Auto Resize: isResizeText 활성 시 텍스트 길이에 맞춰 부모 RectTransform 크기 자동 조절.</summary>
public partial class UI_Text
{
    [SerializeField] private bool isResizeText = false; // 텍스트 길이에 따라 부모 RectTransform 크기 자동 조절
    [SerializeField] private float resizePadding = 0f; // 부모 sizeDelta에 더할 여백 (isResizeText 활성 시에만 인스펙터 표시)

    private RectTransform _setParent; // isResizeText 시 크기 조절될 부모 RectTransform

    // isResizeText 활성 시 부모 RectTransform 캐싱. EnsureInit(Core)에서 호출.
    private void InitResize()
    {
        if (!isResizeText) return;

        if (transform.parent == null || !transform.parent.TryGetComponent(out _setParent))
        {
            UnityEngine.Debug.LogError($"[UI_Text] '{gameObject.name}' isResizeText 활성화됐지만 parent RectTransform 없음 — 부모에 RectTransform 필요");
        }
    }

    // 텍스트의 preferred size 계산 후 본 RectTransform + 부모 RectTransform 크기 조절
    private void ResizeText(string text)
    {
        if (text == _txt.text) return;

        Vector2 size = _txt.GetPreferredValues(text, Mathf.Infinity, Mathf.Infinity);
        RectTransform rect = _txt.rectTransform;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        Vector2 sizeDelta = rect.sizeDelta;
        if (sizeDelta.x < sizeDelta.y) sizeDelta.x = sizeDelta.y;
        _txt.text = text;

        if (_setParent == null) return;
        _setParent.sizeDelta = sizeDelta + (Vector2.one * resizePadding);
    }
}
