using UnityEngine;

/// <summary>UI_Button 클릭음을 실제 게임 사운드 시스템으로 전달합니다.</summary>
public interface IUIButtonClickSoundPlayer
{
    void PlayClickSound();
}

/// <summary>UI_Button 프리셋을 프로젝트 소유 스프라이트로 변환합니다.</summary>
public interface IUIButtonTheme
{
    Sprite GetButtonSprite(UI_Button.ButtonSprite preset);
}

/// <summary>프로젝트별 버튼 사운드와 테마 구현을 UI Foundation에 연결합니다.</summary>
public static class UIButtonServices
{
    public static IUIButtonClickSoundPlayer ClickSoundPlayer { get; set; }
    public static IUIButtonTheme Theme { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        ClickSoundPlayer = null;
        Theme = null;
    }

    internal static void PlayClickSound() => ClickSoundPlayer?.PlayClickSound();

    internal static Sprite GetButtonSprite(UI_Button.ButtonSprite preset) => Theme?.GetButtonSprite(preset);
}
