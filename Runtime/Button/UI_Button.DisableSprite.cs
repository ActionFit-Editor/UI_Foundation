using UnityEngine;

/// <summary>UI_Button — UseDisableSprite: Disable 시 스프라이트를 disableSprite로 교체.</summary>
public partial class UI_Button
{
    [SerializeField] private bool useDisableSprite = false; // Disable 시 Sprite 교체 여부
    [SerializeField, ShowIf(nameof(useDisableSprite))] private Sprite disableSprite; // Disable 시 표시할 스프라이트 (기본: gray)

    // Awake: useDisableSprite인데 Inspector에서 미지정이면 기본 gray 스프라이트 로드
    private void InitDisableSprite()
    {
        if (useDisableSprite && disableSprite == null)
            disableSprite = UIButtonServices.GetButtonSprite(ButtonSprite.Gray);
    }

    // Disable 시 스프라이트 교체 (off거나 미지정이면 무시)
    private void ApplyDisableSprite()
    {
        if (!useDisableSprite || disableSprite == null) return;
        Image.sprite = disableSprite;
    }

#if UNITY_EDITOR
    // Reset(에디터): 프로젝트가 등록한 Theme의 기본 gray 스프라이트로 채움
    private void ResetDisableSprite()
    {
        if (!useDisableSprite || disableSprite != null) return;
        disableSprite = UIButtonServices.GetButtonSprite(ButtonSprite.Gray);
    }
#endif
}
