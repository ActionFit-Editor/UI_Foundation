using System;
#if DOTWEEN
using DG.Tweening;
using ScrollEase = DG.Tweening.Ease;
#else
using System.Threading;
using ScrollEase = UIEase;
#endif
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScrollRect 컴포넌트를 래핑하여 스크립트를 통해 제어합니다.
/// ScrollRect에 직접 접근하지 않고 이 스크립트를 통해 Content/Viewport/NormalizedPosition 등을 설정합니다.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class UI_Scroll : UI_Rect
{
    #region Serializable Types

    [Serializable]
    public class Refs
    {
        public UI_Image imgViewMask; // 뷰포트 마스크 이미지
        public UI_Rect rectContent; // 컨텐츠 루트 RectTransform 래퍼
    }

    private enum ScrollAxis
    {
        Horizontal,
        Vertical
    }

    #endregion

    #region Fields

    public Refs refs;

    [SerializeField, HideInInspector] protected ScrollRect _scrollRect; // 직렬화 캐시 — Reset/OnValidate가 자동 채움
    protected RectTransform _content; // 레거시 파생 타입 호환용 런타임 Content 캐시
    protected LayoutGroup _layoutGroup; // Content에 직접 연결된 LayoutGroup 캐시

    private bool _isInitialized;

#if DOTWEEN
    private readonly object _scrollAnimKey = new(); // 프로그램적 스크롤 트윈 id (인스턴스 고유 — pool stale Kill 방지)
#else
    private CancellationTokenSource _scrollAnimCts; // 프로그램적 스크롤 애니메이션 취소 수명
#endif

    #endregion

    #region Properties

    public ScrollRect ScrollRect
    {
        get
        {
            if (_scrollRect == null)
            {
                _scrollRect = GetComponent<ScrollRect>();
                UnityEngine.Debug.LogError($"[UI_Scroll] _scrollRect not serialized — runtime GetComponent fallback. Run Tools/Package/UI Foundation/Migrate Component Refs. (gameObject={name})", this);
            }
            return _scrollRect;
        }
    }

    public RectTransform Content
    {
        get
        {
            RectTransform content = ScrollRect.content;
            if (_content != content) CacheContent(content);
            return content;
        }
        set
        {
            ScrollRect.content = value;
            CacheContent(value);
        }
    }

    public RectTransform Viewport
    {
        get => ScrollRect.viewport;
        set => ScrollRect.viewport = value;
    }

    public Vector2 NormalizedPosition
    {
        get => ScrollRect.normalizedPosition;
        set => ScrollRect.normalizedPosition = value;
    }

    public float HorizontalNormalizedPosition
    {
        get => ScrollRect.horizontalNormalizedPosition;
        set => ScrollRect.horizontalNormalizedPosition = value;
    }

    public float VerticalNormalizedPosition
    {
        get => ScrollRect.verticalNormalizedPosition;
        set => ScrollRect.verticalNormalizedPosition = value;
    }

    /// <summary>사용자 입력용 ScrollRect 컴포넌트의 활성 상태입니다.</summary>
    public bool Enabled
    {
        get
        {
            Initialize();
            return ScrollRect.enabled;
        }
        set
        {
            Initialize();
            ScrollRect.enabled = value;
        }
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void OnEnable()
    {
        Initialize();
    }

    protected virtual void Reset()
    {
#if UNITY_EDITOR
        _scrollRect = GetComponent<ScrollRect>();
#endif
    }

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        if (_scrollRect == null) _scrollRect = GetComponent<ScrollRect>();
#endif
    }

    protected virtual void OnDisable()
    {
        CancelScrollAnimation();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ScrollRect와 Content/LayoutGroup 캐시를 한 번 초기화합니다.
    /// 레거시 파생 타입의 <c>if (!base.Initialize()) return false;</c> 패턴을 유지합니다.
    /// </summary>
    public virtual bool Initialize()
    {
        if (_isInitialized) return false;

        ScrollRect scrollRect = ScrollRect;
        CacheContent(scrollRect != null ? scrollRect.content : null);
        _isInitialized = true;
        return true;
    }

    #endregion

    #region Public Methods — Legacy-Compatible Layout

    /// <summary>Content에 직접 연결된 LayoutGroup을 즉시 다시 계산합니다.</summary>
    public UI_Scroll RefreshLayout()
    {
        Initialize();
        RectTransform content = Content;
        CacheContent(content);
        if (_layoutGroup != null) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        return this;
    }

    /// <summary>Canvas 갱신 후 Content에 직접 연결된 LayoutGroup을 다시 계산합니다.</summary>
    public UI_Scroll RefreshLayoutDelayed()
    {
        Initialize();
        RectTransform content = Content;
        CacheContent(content);
        if (_layoutGroup != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
        return this;
    }

    #endregion

    #region Public Methods — Legacy-Compatible Scroll Control

    /// <summary>즉시 최상단으로 이동합니다.</summary>
    public UI_Scroll ScrollToTop()
    {
        SetNormalizedPositionImmediately(ScrollAxis.Vertical, 1f);
        return this;
    }

    /// <summary>duration초 동안 최상단으로 이동합니다.</summary>
    public UI_Scroll ScrollToTop(float duration, ScrollEase ease = ScrollEase.OutQuad)
    {
        AnimateNormalizedPosition(ScrollAxis.Vertical, 1f, duration, ease);
        return this;
    }

    /// <summary>즉시 최하단으로 이동합니다.</summary>
    public UI_Scroll ScrollToBottom()
    {
        SetNormalizedPositionImmediately(ScrollAxis.Vertical, 0f);
        return this;
    }

    /// <summary>duration초 동안 최하단으로 이동합니다.</summary>
    public UI_Scroll ScrollToBottom(float duration, ScrollEase ease = ScrollEase.OutQuad)
    {
        AnimateNormalizedPosition(ScrollAxis.Vertical, 0f, duration, ease);
        return this;
    }

    /// <summary>즉시 가장 왼쪽으로 이동합니다.</summary>
    public UI_Scroll ScrollToLeft()
    {
        SetNormalizedPositionImmediately(ScrollAxis.Horizontal, 0f);
        return this;
    }

    /// <summary>duration초 동안 가장 왼쪽으로 이동합니다.</summary>
    public UI_Scroll ScrollToLeft(float duration, ScrollEase ease = ScrollEase.OutQuad)
    {
        AnimateNormalizedPosition(ScrollAxis.Horizontal, 0f, duration, ease);
        return this;
    }

    /// <summary>즉시 가장 오른쪽으로 이동합니다.</summary>
    public UI_Scroll ScrollToRight()
    {
        SetNormalizedPositionImmediately(ScrollAxis.Horizontal, 1f);
        return this;
    }

    /// <summary>duration초 동안 가장 오른쪽으로 이동합니다.</summary>
    public UI_Scroll ScrollToRight(float duration, ScrollEase ease = ScrollEase.OutQuad)
    {
        AnimateNormalizedPosition(ScrollAxis.Horizontal, 1f, duration, ease);
        return this;
    }

    /// <summary>Content 기준 자식 위치를 계산하여 세로 스크롤합니다.</summary>
    public UI_Scroll ScrollToChild(RectTransform child, float duration = 0.3f, ScrollEase ease = ScrollEase.OutBack)
    {
        Initialize();
        if (child == null) return this;

        AnimateNormalizedPosition(
            ScrollAxis.Vertical,
            GetVerticalNormalizedPosition(child),
            duration,
            ease);
        return this;
    }

    /// <summary>Content 기준 자식 위치를 계산하여 가로 스크롤합니다.</summary>
    public UI_Scroll ScrollToChildHorizontal(RectTransform child, float duration = 0.3f, ScrollEase ease = ScrollEase.OutBack)
    {
        Initialize();
        if (child == null) return this;

        AnimateNormalizedPosition(
            ScrollAxis.Horizontal,
            GetHorizontalNormalizedPosition(child),
            duration,
            ease);
        return this;
    }

    /// <summary>진행 중인 프로그램적 스크롤 애니메이션을 중지합니다.</summary>
    public void StopScroll()
    {
        CancelScrollAnimation();
    }

    #endregion

    #region Public Methods — Existing Scroll Control

    /// <summary>유저 드래그/스크롤 입력을 잠그거나(false) 해제(true)합니다. 잠금 중에도 Snap/AnimateToBottom 등 프로그램적 위치 설정은 동작합니다.</summary>
    public void SetUserScrollEnabled(bool enabled) => Enabled = enabled;

    /// <summary>즉시 최하단(컨텐츠 아래쪽 끝)으로 이동합니다.</summary>
    public void SnapToBottom() => SnapVertical(0f);

    /// <summary>즉시 최상단(컨텐츠 위쪽 끝)으로 이동합니다.</summary>
    public void SnapToTop() => SnapVertical(1f);

    /// <summary>
    /// 현재 위치에서 최하단까지 등속(speed px/초)으로 스크롤합니다. 이동 거리가 짧을수록 빨리 끝납니다(거리/속도 = 시간).
    /// 완료(또는 스크롤 불가/이미 최하단이면 즉시) 시 onComplete를 1회 호출합니다.
    /// </summary>
    public void AnimateToBottom(float speed, Action onComplete = null)
    {
        CancelScrollAnimation();

        float scrollable = ScrollableHeight;
        float start = Mathf.Clamp01(VerticalNormalizedPosition);
        float distancePx = start * scrollable; // 최하단(vertical 0)까지 남은 픽셀 거리
        float duration = (speed > 0f && distancePx > 0.5f) ? distancePx / speed : 0f;

        if (duration <= 0f)
        {
            if (scrollable > 0f) VerticalNormalizedPosition = 0f;
            onComplete?.Invoke();
            return;
        }

        StartNormalizedAnimation(ScrollAxis.Vertical, 0f, duration, ScrollEase.Linear, onComplete);
    }

    /// <summary>
    /// 현재 위치에서 최상단까지 등속(speed px/초)으로 스크롤합니다. 이동 거리가 짧을수록 빨리 끝납니다(거리/속도 = 시간).
    /// 완료(또는 스크롤 불가/이미 최상단이면 즉시) 시 onComplete를 1회 호출합니다.
    /// </summary>
    public void AnimateToTop(float speed, Action onComplete = null)
    {
        CancelScrollAnimation();

        float scrollable = ScrollableHeight;
        float start = Mathf.Clamp01(VerticalNormalizedPosition);
        float distancePx = (1f - start) * scrollable; // 최상단(vertical 1)까지 남은 픽셀 거리
        float duration = (speed > 0f && distancePx > 0.5f) ? distancePx / speed : 0f;

        if (duration <= 0f)
        {
            if (scrollable > 0f) VerticalNormalizedPosition = 1f;
            onComplete?.Invoke();
            return;
        }

        StartNormalizedAnimation(ScrollAxis.Vertical, 1f, duration, ScrollEase.Linear, onComplete);
    }

    /// <summary>
    /// 현재 위치에서 최상단까지 duration(초) 동안 ease 곡선으로 스크롤합니다. (속도 고정 AnimateToTop(speed)의 시간 기반 버전 — 등속 버전도 확장용으로 유지)
    /// 완료(또는 스크롤 불가/duration ≤ 0이면 즉시) 시 onComplete를 1회 호출합니다.
    /// </summary>
    public void AnimateToTop(float duration, ScrollEase ease, Action onComplete = null)
    {
        CancelScrollAnimation();

        float scrollable = ScrollableHeight;
        if (scrollable <= 0f || duration <= 0f)
        {
            if (scrollable > 0f) VerticalNormalizedPosition = 1f;
            onComplete?.Invoke();
            return;
        }

        StartNormalizedAnimation(ScrollAxis.Vertical, 1f, duration, ease, onComplete);
    }

    #endregion

    #region Private Methods

    private void CacheContent(RectTransform content)
    {
        _content = content;
        _layoutGroup = content != null ? content.GetComponent<LayoutGroup>() : null;
    }

    private void SetNormalizedPositionImmediately(ScrollAxis axis, float normalizedPosition)
    {
        Initialize();
        CancelScrollAnimation();
        SetNormalizedPosition(axis, Mathf.Clamp01(normalizedPosition));
    }

    private void AnimateNormalizedPosition(
        ScrollAxis axis,
        float target,
        float duration,
        ScrollEase ease,
        Action onComplete = null)
    {
        Initialize();
        CancelScrollAnimation();

        target = Mathf.Clamp01(target);
        if (duration <= 0f)
        {
            SetNormalizedPosition(axis, target);
            onComplete?.Invoke();
            return;
        }

        StartNormalizedAnimation(axis, target, duration, ease, onComplete);
    }

    private void StartNormalizedAnimation(
        ScrollAxis axis,
        float target,
        float duration,
        ScrollEase ease,
        Action onComplete)
    {
#if DOTWEEN
        DOTween.To(
                () => GetNormalizedPosition(axis),
                value => SetNormalizedPosition(axis, value),
                target,
                duration)
            .SetEase(ease)
            .SetId(_scrollAnimKey)
            .OnComplete(() => onComplete?.Invoke());
#else
        StartScrollAnimation(axis, target, duration, ease, onComplete);
#endif
    }

    private float GetVerticalNormalizedPosition(RectTransform child)
    {
        RectTransform content = Content;
        if (content == null) return VerticalNormalizedPosition;

        CacheContent(content);
        RectTransform viewport = EffectiveViewport;
        float scrollableHeight = content.rect.height - viewport.rect.height;
        if (scrollableHeight <= 0f) return 1f;

        Vector2 childLocalPosition = content.InverseTransformPoint(child.position);
        float childTop = -childLocalPosition.y - child.rect.height * (1f - child.pivot.y);
        if (_layoutGroup != null) childTop -= _layoutGroup.padding.top;

        return Mathf.Clamp01(1f - childTop / scrollableHeight);
    }

    private float GetHorizontalNormalizedPosition(RectTransform child)
    {
        RectTransform content = Content;
        if (content == null) return HorizontalNormalizedPosition;

        CacheContent(content);
        RectTransform viewport = EffectiveViewport;
        float scrollableWidth = content.rect.width - viewport.rect.width;
        if (scrollableWidth <= 0f) return 0f;

        Vector2 childLocalPosition = content.InverseTransformPoint(child.position);
        float childLeft = childLocalPosition.x - child.rect.width * child.pivot.x;
        if (_layoutGroup != null) childLeft -= _layoutGroup.padding.left;

        return Mathf.Clamp01(childLeft / scrollableWidth);
    }

    private void SnapVertical(float normalizedPosition)
    {
        CancelScrollAnimation();
        if (ScrollableHeight <= 0f) return;
        VerticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
    }

    private float GetNormalizedPosition(ScrollAxis axis)
    {
        return axis == ScrollAxis.Vertical
            ? VerticalNormalizedPosition
            : HorizontalNormalizedPosition;
    }

    private void SetNormalizedPosition(ScrollAxis axis, float value)
    {
        if (axis == ScrollAxis.Vertical) VerticalNormalizedPosition = value;
        else HorizontalNormalizedPosition = value;
    }

    private void CancelScrollAnimation()
    {
#if DOTWEEN
        DOTween.Kill(_scrollAnimKey);
#else
        CancellationTokenSource owner = _scrollAnimCts;
        _scrollAnimCts = null;
        if (owner == null) return;

        try
        {
            owner.Cancel();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception, this);
        }
        finally
        {
            owner.Dispose();
        }
#endif
    }

#if !DOTWEEN
    private void StartScrollAnimation(
        ScrollAxis axis,
        float target,
        float duration,
        ScrollEase ease,
        Action onComplete)
    {
        var owner = new CancellationTokenSource();
        _scrollAnimCts = owner;
        _ = AnimateNormalizedPositionAsync(axis, target, duration, ease, onComplete, owner);
    }

    private async Awaitable AnimateNormalizedPositionAsync(
        ScrollAxis axis,
        float target,
        float duration,
        ScrollEase ease,
        Action onComplete,
        CancellationTokenSource owner)
    {
        bool completed = false;
        bool ownsCompletion = false;
        try
        {
            completed = await UIAnimationUtility.AnimateFloat(
                GetNormalizedPosition(axis),
                target,
                duration,
                ease,
                value => SetNormalizedPosition(axis, value),
                owner.Token);
        }
        catch (OperationCanceledException)
        {
            completed = false;
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_scrollAnimCts, owner))
                UnityEngine.Debug.LogException(exception, this);
        }
        finally
        {
            ownsCompletion = ReferenceEquals(_scrollAnimCts, owner);
            if (ownsCompletion)
            {
                _scrollAnimCts = null;
                owner.Dispose();
            }
        }

        if (!completed || !ownsCompletion) return;

        try
        {
            onComplete?.Invoke();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception, this);
        }
    }
#endif

    private RectTransform EffectiveViewport => Viewport != null ? Viewport : RectTransform;

    private float ScrollableHeight
    {
        get
        {
            RectTransform content = Content;
            RectTransform viewport = EffectiveViewport;
            return content != null && viewport != null
                ? Mathf.Max(0f, content.rect.height - viewport.rect.height)
                : 0f;
        }
    }

    #endregion
}
