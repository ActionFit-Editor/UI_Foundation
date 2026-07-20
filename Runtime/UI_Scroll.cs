using System;
#if DOTWEEN
using DG.Tweening;
#else
using System.Threading;
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

    #endregion

    #region Fields

    public Refs refs;

    [SerializeField, HideInInspector] private ScrollRect _scrollRect; // 직렬화 캐시 — Reset/OnValidate가 자동 채움

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
        get => ScrollRect.content;
        set => ScrollRect.content = value;
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

    #endregion

    #region Unity Lifecycle

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

    private void OnDisable() => CancelScrollAnimation(); // 비활성화 시 진행 중 스크롤 애니메이션 정리

    #endregion

    #region Public Methods — Scroll Control

    /// <summary>유저 드래그/스크롤 입력을 잠그거나(false) 해제(true)합니다. 잠금 중에도 Snap/AnimateToBottom 등 프로그램적 위치 설정은 동작합니다.</summary>
    public void SetUserScrollEnabled(bool enabled) => ScrollRect.enabled = enabled;

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

#if DOTWEEN
        DOTween.To(() => VerticalNormalizedPosition, v => VerticalNormalizedPosition = v, 0f, duration)
            .SetEase(Ease.Linear) // 등속
            .SetId(_scrollAnimKey)
            .OnComplete(() => onComplete?.Invoke());
#else
        StartScrollAnimation(0f, duration, UIEase.Linear, onComplete);
#endif
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

#if DOTWEEN
        DOTween.To(() => VerticalNormalizedPosition, v => VerticalNormalizedPosition = v, 1f, duration)
            .SetEase(Ease.Linear) // 등속
            .SetId(_scrollAnimKey)
            .OnComplete(() => onComplete?.Invoke());
#else
        StartScrollAnimation(1f, duration, UIEase.Linear, onComplete);
#endif
    }

    /// <summary>
    /// 현재 위치에서 최상단까지 duration(초) 동안 ease 곡선으로 스크롤합니다. (속도 고정 AnimateToTop(speed)의 시간 기반 버전 — 등속 버전도 확장용으로 유지)
    /// 완료(또는 스크롤 불가/duration ≤ 0이면 즉시) 시 onComplete를 1회 호출합니다.
    /// </summary>
#if DOTWEEN
    public void AnimateToTop(float duration, Ease ease, Action onComplete = null)
#else
    public void AnimateToTop(float duration, UIEase ease, Action onComplete = null)
#endif
    {
        CancelScrollAnimation();

        float scrollable = ScrollableHeight;
        if (scrollable <= 0f || duration <= 0f)
        {
            if (scrollable > 0f) VerticalNormalizedPosition = 1f;
            onComplete?.Invoke();
            return;
        }

#if DOTWEEN
        DOTween.To(() => VerticalNormalizedPosition, v => VerticalNormalizedPosition = v, 1f, duration)
            .SetEase(ease)
            .SetId(_scrollAnimKey)
            .OnComplete(() => onComplete?.Invoke());
#else
        StartScrollAnimation(1f, duration, ease, onComplete);
#endif
    }

    // 즉시 세로 위치 설정 (스크롤 불가면 무시). norm: 0=최하단, 1=최상단.
    private void SnapVertical(float norm)
    {
        CancelScrollAnimation();
        if (ScrollableHeight <= 0f) return;
        VerticalNormalizedPosition = Mathf.Clamp01(norm);
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
    private void StartScrollAnimation(float target, float duration, UIEase ease, Action onComplete)
    {
        var owner = new CancellationTokenSource();
        _scrollAnimCts = owner;
        _ = AnimateVerticalAsync(target, duration, ease, onComplete, owner);
    }

    private async Awaitable AnimateVerticalAsync(
        float target,
        float duration,
        UIEase ease,
        Action onComplete,
        CancellationTokenSource owner)
    {
        bool completed = false;
        bool ownsCompletion = false;
        try
        {
            completed = await UIAnimationUtility.AnimateFloat(
                VerticalNormalizedPosition,
                target,
                duration,
                ease,
                value => VerticalNormalizedPosition = value,
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

    // 스크롤 가능한 세로 픽셀 범위 (컨텐츠 높이 - 뷰포트 높이, 음수면 0).
    private float ScrollableHeight =>
        (Content != null && Viewport != null) ? Mathf.Max(0f, Content.rect.height - Viewport.rect.height) : 0f;

    #endregion
}
