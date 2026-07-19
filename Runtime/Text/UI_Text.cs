using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Text 래퍼 컴포넌트. 기능별로 partial 분리되어 있다: Material(Face/Outline/Underlay) / Resize / Sprite.
/// 이 파일(Core)은 TMP_Text 캐시와 Unity 생명주기를 담당하며, 각 기능의 init/apply를 호출만 한다.
/// 새 기능 추가 시: UI_Text.&lt;Feature&gt;.cs 파일을 만들고, 필요하면 EnsureInit/OnEnable 등에 호출 한 줄만 추가한다.
/// - Text/Color/TMP 프로퍼티로 직관적인 접근 + Chainable Setter(SetSize/SetColor/SetFont/SetAlignment)
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public partial class UI_Text : MonoBehaviour
{
    protected TMP_Text _txt;
    private bool _initialized;
    private bool _compatibilityInitialized;

    #region Properties

    public string Text
    {
        get
        {
            EnsureInit();
            return _txt.text;
        }
        set
        {
            EnsureInit();
            if (isResizeText) ResizeText(value); // Resize partial
            else _txt.text = value;
        }
    }

    public Color Color
    {
        get
        {
            EnsureInit();
            return _txt.color;
        }
    }

    public TMP_Text TMP
    {
        get
        {
            EnsureInit();
            return _txt;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake() => EnsureInit();

    // 런타임: 활성화 시 Sprite Asset, 머티리얼, Localization 순서로 적용.
    protected virtual void OnEnable()
    {
        Initialize();
        if (!Application.isPlaying) return; // 에디터(비플레이)는 Editor preview coordinator가 담당
        AcquireRuntimeSpriteAsset();
        AcquireOutline();
        ApplyLocalization(); // Localize partial
    }

    // 런타임: 풀 반납. 에디터: 모든 임시 프리뷰 정리 + 원본 복원.
    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            RestoreSpriteEditorPreview();
            RestoreOutlineEditorPreview();
            return;
        }
#endif
        ReleaseRuntimeSpriteAsset();
        ReleaseOutline();
    }

#if UNITY_EDITOR
    // 일반 MonoBehaviour는 Edit Mode에서 enabled 변경만으로 OnDisable이 호출되지 않을 수 있습니다.
    private void OnValidate()
    {
        if (!Application.isPlaying && !enabled) RestoreSpriteEditorPreview();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            RestoreSpriteEditorPreview();
            RestoreOutlineEditorPreview();
        }
    }
#endif

    #endregion

    #region Initialization

    /// <summary>
    /// 레거시 UI.Initialize와 같은 1회 초기화 계약을 제공합니다.
    /// TMP 캐시 초기화와 분리되어 있어 Awake 뒤 첫 호출도 true를 반환합니다.
    /// </summary>
    public virtual bool Initialize()
    {
        EnsureInit();
        if (_compatibilityInitialized) return false;

        _compatibilityInitialized = true;
        return true;
    }

    // TMP_Text 캐싱 + (isResizeText 시) 부모 RectTransform 캐싱. 1회만 실행.
    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;

        if (!TryGetComponent(out _txt))
        {
            UnityEngine.Debug.LogError($"[UI_Text] '{gameObject.name}'에 TMP_Text 컴포넌트가 없습니다");
            return;
        }

        InitResize(); // Resize partial
    }

    #endregion

    #region Chainable Setters

    /// <summary>음수 전달 시 enableAutoSizing 활성, 양수 전달 시 fontSize 직접 설정.</summary>
    public UI_Text SetSize(int size)
    {
        EnsureInit();
        if (size < 0) _txt.enableAutoSizing = true;
        else _txt.fontSize = size;
        return this;
    }

    public UI_Text SetColor(Color color)
    {
        EnsureInit();
        _txt.color = color;
        return this;
    }

    public UI_Text SetFont(TMP_FontAsset font)
    {
        EnsureInit();
        _txt.font = font;
        return this;
    }

    public UI_Text SetAlignment(TextAlignmentOptions alignment)
    {
        EnsureInit();
        _txt.alignment = alignment;
        return this;
    }

    #endregion
}
