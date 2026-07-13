using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Button — Disable/Enable 상태 오케스트레이터.
/// SetDisable/SetEnable이 각 비주얼 기능(DisableSprite/DisableColor/DisableTextColor/EnableAnimation)을 호출만 하고,
/// 각 기능의 적용 여부(useX 토글) 판정은 해당 기능 partial 내부가 책임진다.
/// </summary>
public partial class UI_Button
{
#if UNITY_EDITOR
    [SerializeField, InspectorName("Editor_Disable")] private bool editorDisablePreview = false; // 에디터 전용 Disable 비주얼 프리뷰
    private bool _editorPreviewActive;
    private bool _editorPreviewOriginalInteractable;
#endif

    private bool _disabled;
    public bool IsDisabled => _disabled;

    /// <summary>
    /// 버튼을 비활성화합니다.
    /// Interactable 비활성 + 각 Disable 비주얼(스프라이트/컬러/텍스트 아웃라인) 적용.
    /// </summary>
    public void SetDisable()
    {
        _disabled = true;
        Button.interactable = false;
        ApplyDisableSprite();    // DisableSprite    (useDisableSprite 체크 내장)
        ApplyDisableColor();     // DisableColor     (useDisableColor 체크 내장)
        ApplyDisableTextColor(); // DisableTextColor (useDisableTextColor 체크 내장)
    }

    /// <summary>
    /// 버튼을 활성화 상태로 복원합니다.
    /// Interactable 즉시 활성 + (애니메이션 사용 시) Enable 애니메이션, 아니면 즉시 비주얼 복원.
    /// </summary>
    public void SetEnable()
    {
        bool wasDisabled = _disabled;
        _disabled = false;
        Button.interactable = true;
        if (useEnableAnimation && wasDisabled)
        {
            // Sprite/Color는 애니메이션 peak에서 교체
            PlayEnableAnimation(); // EnableAnimation
        }
        else
        {
            RestoreNormalVisuals();
        }
    }

    /// <summary>비활성/활성 상태를 설정합니다.</summary>
    public void SetInteractable(bool interactable)
    {
        if (interactable) SetEnable();
        else SetDisable();
    }

    // 활성(normal) 비주얼로 복원 — 각 기능의 복원을 호출 (idempotent). Enable 애니 peak/완료 시점에 호출됨.
    private void RestoreNormalVisuals()
    {
        RestoreSprite();     // Sprite
        RestoreColor();      // DisableColor
        RestoreTextColor();  // DisableTextColor
    }

    // 색의 RGB만 factor로 곱해 어둡게 (알파 보존) — DisableColor/DisableTextColor 공용 유틸
    private static Color Darken(Color c, float f)
    {
        return new Color(c.r * f, c.g * f, c.b * f, c.a);
    }

#if UNITY_EDITOR
    public void ApplyEditorDisablePreview()
    {
        if (Application.isPlaying) return;

        if (_button == null) _button = GetComponent<Button>();

        bool wasEditorPreviewActive = _editorPreviewActive;
        if (_editorPreviewActive || _disabled)
        {
            _disabled = false;
            Button.interactable = _editorPreviewActive ? _editorPreviewOriginalInteractable : true;
            RestoreNormalVisuals();
            _editorPreviewActive = false;
        }

        InitSprite();
        InitDisableSprite();
        CacheImageColors();
        CacheTextMaterials();

        if (editorDisablePreview)
        {
            _editorPreviewOriginalInteractable = Button.interactable;
            _editorPreviewActive = true;
            SetDisable();
        }
        else
        {
            _disabled = false;
            if (wasEditorPreviewActive) Button.interactable = _editorPreviewOriginalInteractable;
            RestoreNormalVisuals();
        }
    }
#endif
}
