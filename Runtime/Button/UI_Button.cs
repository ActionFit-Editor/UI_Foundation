using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem 포인터 입력을 직접 처리하는 UI 버튼. 기능별로 partial 분리되어 있다:
///   Event / PressEffect / Sound / Sprite / Disable / DisableSprite / DisableColor / DisableTextColor / EnableAnimation.
/// 이 파일(Core)은 Unity 생명주기를 담당하며, 각 기능의 init/reset/cleanup을 호출만 한다.
/// 새 기능 추가 시: UI_Button.&lt;Feature&gt;.cs 파일을 만들고, 필요하면 Awake/Reset/OnDestroy에 호출 한 줄만 추가한다.
/// </summary>
public partial class UI_Button : UI_Image,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    private void Awake()
    {
        InitializeInteractableState();

        InitSprite();           // Sprite              — _normalSprite 기록
        InitEnableAnimation();  // EnableAnimation     — _baseScale 기록
        InitDisableSprite();    // DisableSprite       — 기본 gray 스프라이트 로드
        CacheImageColors();     // DisableColor        — 원본 color 기록
        CacheTextMaterials();   // DisableTextColor — 원본 fontSharedMaterial 기록
        RegisterClickSound();   // Sound               — 공용 클릭음 리스너 등록
    }

    private void OnDestroy()
    {
        CancelPointerInteraction(); // Event / PressEffect
        KillEnableAnimation();  // EnableAnimation
    }

    // 에디터에서 컴포넌트 추가/리셋 시 기본 설정
    protected override void Reset()
    {
        base.Reset();
        ResetInteractableState();
        ResetPressEffect();

#if UNITY_EDITOR
        ResetDisableSprite();   // DisableSprite — 기본 gray 스프라이트 GUID 검색
#endif
    }

    protected override void OnValidate()
    {
        base.OnValidate();
#if UNITY_EDITOR
        if (!Application.isPlaying) ResetRuntimeInteractableState();
#endif
    }
}
