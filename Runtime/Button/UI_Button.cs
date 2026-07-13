using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button 컴포넌트를 래핑하는 UI 버튼. 기능별로 partial 분리되어 있다:
///   Event / Sound / Sprite / Disable / DisableSprite / DisableColor / DisableTextColor / EnableAnimation.
/// 이 파일(Core)은 컴포넌트 캐시와 Unity 생명주기를 담당하며, 각 기능의 init/reset/cleanup을 호출만 한다.
/// 새 기능 추가 시: UI_Button.&lt;Feature&gt;.cs 파일을 만들고, 필요하면 Awake/Reset/OnDestroy에 호출 한 줄만 추가한다.
/// </summary>
[RequireComponent(typeof(Button))]
public partial class UI_Button : UI_Image
{
    [SerializeField, HideInInspector] private Button _button; // 직렬화 캐시 — Reset/OnValidate가 자동 채움

    public Button Button
    {
        get
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
                UnityEngine.Debug.LogError($"[UI_Button] _button not serialized — runtime GetComponent fallback. Run Tools/Package/UI Foundation/Migrate Component Refs. (gameObject={name})", this);
            }
            return _button;
        }
    }

    private void Awake()
    {
        // _button / _image 직렬화 캐시는 Reset/OnValidate가 자동 채움 — 미채워진 경우 Button/Image 프로퍼티가 fallback + LogError
        _ = Button;

        InitSprite();           // Sprite              — _normalSprite 기록
        InitEnableAnimation();  // EnableAnimation     — _baseScale 기록
        InitDisableSprite();    // DisableSprite       — 기본 gray 스프라이트 로드
        CacheImageColors();     // DisableColor        — 원본 color 기록
        CacheTextMaterials();   // DisableTextColor — 원본 fontSharedMaterial 기록
        RegisterClickSound();   // Sound               — 공용 클릭음 리스너 등록
    }

    private void OnDestroy()
    {
        KillEnableAnimation();  // EnableAnimation
    }

    // 에디터에서 컴포넌트 추가/리셋 시 기본 설정
    protected override void Reset()
    {
        base.Reset();

        if (GetComponent<UIButtonPressEffect>() == null)
            gameObject.AddComponent<UIButtonPressEffect>();

#if UNITY_EDITOR
        _button = GetComponent<Button>();
        ResetDisableSprite();   // DisableSprite — 기본 gray 스프라이트 GUID 검색
#endif
    }

    protected override void OnValidate()
    {
        base.OnValidate();
#if UNITY_EDITOR
        if (_button == null) _button = GetComponent<Button>();
#endif
    }
}
