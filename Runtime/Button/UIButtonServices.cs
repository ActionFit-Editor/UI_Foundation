using UnityEngine;

/// <summary>UI_Button 클릭음을 실제 게임 사운드 시스템으로 전달합니다.</summary>
public interface IUIButtonClickSoundPlayer
{
    void PlayClickSound();
}

/// <summary>UI_Button 클릭 햅틱을 실제 프로젝트 피드백 시스템으로 전달합니다.</summary>
public interface IUIButtonHapticPlayer
{
    void PlayHaptic();
}

/// <summary>UI_Button 프리셋을 프로젝트 소유 스프라이트로 변환합니다.</summary>
public interface IUIButtonTheme
{
    Sprite GetButtonSprite(UI_Button.ButtonSprite preset);
}

/// <summary>프로젝트 소유 버튼 활성/비활성 색상을 UI Foundation에 제공합니다.</summary>
public interface IUIButtonStateColorTheme
{
    Color GetButtonStateColor(bool interactable);
}

/// <summary>프로젝트별 버튼 사운드와 테마 구현을 UI Foundation에 연결합니다.</summary>
public static class UIButtonServices
{
    public static IUIButtonClickSoundPlayer ClickSoundPlayer { get; set; }
    public static IUIButtonHapticPlayer HapticPlayer { get; set; }
    public static IUIButtonTheme Theme { get; set; }
    public static IUIButtonStateColorTheme StateColorTheme { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        ClickSoundPlayer = null;
        HapticPlayer = null;
        Theme = null;
        StateColorTheme = null;
    }

    internal static void PlayClickSound() => ClickSoundPlayer?.PlayClickSound();

    internal static void PlayHaptic() => HapticPlayer?.PlayHaptic();

    internal static Sprite GetButtonSprite(UI_Button.ButtonSprite preset) => Theme?.GetButtonSprite(preset);

    internal static Color GetButtonStateColor(bool interactable)
    {
        if (StateColorTheme != null) return StateColorTheme.GetButtonStateColor(interactable);
        return interactable ? Color.white : new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);
    }
}
