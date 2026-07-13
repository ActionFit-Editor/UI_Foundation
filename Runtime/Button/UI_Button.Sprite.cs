using UnityEngine;

/// <summary>UI_Button — 활성(normal) 스프라이트 보유 및 프리셋/직접 지정 변경.</summary>
public partial class UI_Button
{
    /// <summary>프로젝트가 <see cref="IUIButtonTheme"/>으로 등록한 버튼 스프라이트 프리셋.</summary>
    public enum ButtonSprite
    {
        None,
        Green,
        DarkGreen,
        Yellow,
        Pink,
        Red,
        Gray
    }

    private Sprite _normalSprite; // 활성 상태의 원래 스프라이트

    // Awake: 현재 sprite를 활성 기준으로 기록
    private void InitSprite() => _normalSprite = Image.sprite;

    // 활성(normal) sprite로 복원 (Disable 해제 시 RestoreNormalVisuals에서 호출).
    // _normalSprite가 null인 경우(inactive parent 아래 Instantiate되어 Awake 미호출 등) sprite는 건드리지 않음.
    private void RestoreSprite()
    {
        if (_normalSprite != null) Image.sprite = _normalSprite;
    }

    /// <summary>
    /// 프리셋 enum으로 버튼 스프라이트를 변경합니다.
    /// 변경된 스프라이트가 활성 상태의 기본 스프라이트로 저장됩니다.
    /// </summary>
    public void SetButtonSprite(ButtonSprite preset)
    {
        if (preset == ButtonSprite.None) return;

        Sprite sprite = UIButtonServices.GetButtonSprite(preset);
        if (sprite == null)
        {
            UnityEngine.Debug.LogError($"[UI_Button] Theme sprite not found: preset={preset}");
            return;
        }

        _normalSprite = sprite;
        if (!_disabled) Image.sprite = _normalSprite;
    }

    /// <summary>
    /// Sprite를 직접 지정하여 버튼 스프라이트를 변경합니다.
    /// 변경된 스프라이트가 활성 상태의 기본 스프라이트로 저장됩니다.
    /// </summary>
    public void SetButtonSprite(Sprite sprite)
    {
        if (sprite == null) return;
        _normalSprite = sprite;
        if (!_disabled) Image.sprite = _normalSprite;
    }
}
