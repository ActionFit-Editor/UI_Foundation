#if DOTWEEN
using DG.Tweening;
#else
using System;
using System.Threading;
using Ease = UIEase;
using Tween = UnityEngine.Awaitable<bool>;
#endif
using UnityEngine;

/// <summary>
/// UI_MaskBase 애니메이션 파트 — Expand/Collapse 등 마스크 reveal 트윈.
/// AnimationPivot(Up/Down/Left/Right)이 피벗(가장자리 고정)과 트윈 축을 정한다: Up·Down=Height, Left·Right=Width.
/// targetRect가 지정되면 펼침 목표 치수를 그 Rect의 현재 크기에서 자동으로 따라간다(expandedHeight/Width 무시). 미지정 시 수동값 사용.
/// 피벗은 isAnimationMask가 true일 때만 자동 적용(미적용 시 기존 피벗 유지 — 후방호환). 인스펙터 노출은 UI_MaskBaseEditor가 isAnimationMask로 게이트.
/// DOTween 트윈은 id(_sizeKey/_posKey), 기본 애니메이션은 CTS 기반으로 관리되어 재호출·비활성화 시 안전하게 취소됩니다.
/// </summary>
public abstract partial class UI_MaskBase
{
    #region Types

    public enum AnimationPivot { Up, Down, Left, Right }

    #endregion

    #region Fields

    [SerializeField] private AnimationPivot animPivot = AnimationPivot.Down; // 펼침 방향 — 피벗(가장자리)과 트윈 축(H/W)을 결정
    [SerializeField] private UI_Rect targetRect; // 지정 시 펼침 목표 치수를 이 Rect의 현재 크기에서 자동 추종(expandedHeight/Width 무시)
    [SerializeField] private float expandedHeight = 400f; // Up/Down 펼침 목표 높이 (targetRect 미지정 시)
    [SerializeField] private float expandedWidth = 400f;  // Left/Right 펼침 목표 너비 (targetRect 미지정 시)
    [SerializeField] private float animDuration = 0.3f;   // 기준 시간(초). full range(0↔목표치수) 이동 기준이며 실제 거리에 비례해 줄어듦
    [SerializeField] private Ease animEase = Ease.OutQuad; // 기본 이징

#if DOTWEEN
    private readonly object _sizeKey = new(); // sizeDelta(Width/Height) 트윈 식별자
    private readonly object _posKey = new();  // anchoredPosition 트윈 식별자
#else
    private CancellationTokenSource _sizeCts; // sizeDelta 애니메이션 취소 수명
    private CancellationTokenSource _posCts; // anchoredPosition 애니메이션 취소 수명
#endif

    private bool IsVertical => animPivot is AnimationPivot.Up or AnimationPivot.Down;

    // 펼침 목표 치수: targetRect 지정 시 그 Rect의 현재 rect 크기, 아니면 수동값
    private float ResolvedExpandedHeight => targetRect != null ? targetRect.RectTransform.rect.height : expandedHeight;
    private float ResolvedExpandedWidth  => targetRect != null ? targetRect.RectTransform.rect.width  : expandedWidth;

    #endregion

    #region Unity Lifecycle

    // UI_Rect는 lifecycle 훅이 없으므로 override/base 호출 없이 직접 정의 (Unity 매직 메서드 — private도 파생 인스턴스에서 호출됨)
    private void OnEnable()
    {
        if (isAnimationMask) ApplyPivot();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isAnimationMask) ApplyPivot();
    }

    // 에디트 모드 전용: targetRect 크기를 마스크에 라이브로 반영 (인스펙터에서 targetRect를 바꾸거나 targetRect 자체 크기가 변하면 즉시 따라옴).
    // 런타임은 Expand/Collapse가 sizeDelta를 제어하므로 제외 — 여기서 매 프레임 덮어쓰면 reveal 애니가 깨진다.
    private void Update()
    {
        if (Application.isPlaying || !isAnimationMask || targetRect == null) return;

        var size = new Vector2(ResolvedExpandedWidth, ResolvedExpandedHeight); // targetRect의 현재 rect W/H
        if (RectTransform.sizeDelta != size) RectTransform.sizeDelta = size;
    }
#endif

    // 비활성화 시 진행 중인 마스크 트윈 정리 (런타임 한정 — [ExecuteAlways]로 에디트 모드에서도 호출되므로 가드)
    private void OnDisable()
    {
        if (!Application.isPlaying) return;
#if DOTWEEN
        DOTween.Kill(_sizeKey);
        DOTween.Kill(_posKey);
#else
        CancelSizeAnimation();
        CancelPositionAnimation();
#endif
    }

    #endregion

    #region Public Methods

    /// <summary>설정 방향으로 펼칩니다. Up/Down→Height, Left/Right→Width. targetRect 지정 시 그 Rect 크기를 따라갑니다. 남은 거리에 비례해 시간이 줄어 중간 전환에도 일정 속도로 동작합니다.</summary>
    public Tween Expand(float duration = -1f, Ease? ease = null) =>
        IsVertical ? AnimHeight(ResolvedExpandedHeight, ScaledDur(ResolvedExpandedHeight, duration), ease)
                   : AnimWidth(ResolvedExpandedWidth, ScaledDur(ResolvedExpandedWidth, duration), ease);

    /// <summary>설정 방향으로 0까지 접습니다. Up/Down→Height, Left/Right→Width.</summary>
    public Tween Collapse(float duration = -1f, Ease? ease = null) =>
        IsVertical ? AnimHeight(0f, ScaledDur(0f, duration), ease)
                   : AnimWidth(0f, ScaledDur(0f, duration), ease);

    /// <summary>마스크 너비(sizeDelta.x)를 width로 트윈합니다. duration&lt;0이면 기본값, ease=null이면 기본값.</summary>
    public Tween AnimWidth(float width, float duration = -1f, Ease? ease = null)
    {
        var size = new Vector2(width, RectTransform.sizeDelta.y);
        return AnimateSize(size, Dur(duration), Ez(ease), applyImmediatelyWhenZero: true);
    }

    /// <summary>마스크 높이(sizeDelta.y)를 height로 트윈합니다. (예: AnimHeight(400) 펼침 / AnimHeight(0) 접힘)</summary>
    public Tween AnimHeight(float height, float duration = -1f, Ease? ease = null)
    {
        var size = new Vector2(RectTransform.sizeDelta.x, height);
        return AnimateSize(size, Dur(duration), Ez(ease), applyImmediatelyWhenZero: true);
    }

    /// <summary>마스크 크기(sizeDelta)를 size로 트윈합니다.</summary>
    public Tween AnimSize(Vector2 size, float duration = -1f, Ease? ease = null)
    {
        return AnimateSize(size, Dur(duration), Ez(ease), applyImmediatelyWhenZero: false);
    }

    /// <summary>마스크 위치(anchoredPosition)를 pos로 트윈합니다.</summary>
    public Tween AnimPosition(Vector2 pos, float duration = -1f, Ease? ease = null)
    {
        return AnimatePosition(pos, Dur(duration), Ez(ease));
    }

    #endregion

    #region Private Methods

    // AnimationPivot → RectTransform.pivot. 가장자리를 고정점으로 잡아 그 반대편으로 reveal된다 (Down=(0.5,0): 아래 고정·위로 펼침 / Right=(1,0.5): 오른쪽 고정·왼쪽으로 펼침).
    private void ApplyPivot()
    {
        RectTransform.pivot = animPivot switch
        {
            AnimationPivot.Up    => new Vector2(0.5f, 1f),
            AnimationPivot.Down  => new Vector2(0.5f, 0f),
            AnimationPivot.Left  => new Vector2(0f,   0.5f),
            AnimationPivot.Right => new Vector2(1f,   0.5f),
            _ => RectTransform.pivot
        };
    }

    private float Dur(float duration) => duration < 0f ? animDuration : duration;
    private Ease Ez(Ease? ease) => ease ?? animEase;

    // sizeDelta 애니메이션을 교체하고 duration이 0 이하면 즉시 적용합니다.
    private Tween AnimateSize(Vector2 size, float duration, Ease ease, bool applyImmediatelyWhenZero)
    {
#if DOTWEEN
        DOTween.Kill(_sizeKey);
        if (applyImmediatelyWhenZero && duration <= 0f)
        {
            RectTransform.sizeDelta = size;
            return null;
        }

        return DOTween.To(
                () => RectTransform.sizeDelta,
                value => RectTransform.sizeDelta = value,
                size,
                duration)
            .SetEase(ease)
            .SetId(_sizeKey);
#else
        return AnimateSizeAsync(size, duration, ease);
#endif
    }

    // anchoredPosition 애니메이션을 교체하고 duration이 0 이하면 즉시 적용합니다.
    private Tween AnimatePosition(Vector2 position, float duration, Ease ease)
    {
#if DOTWEEN
        DOTween.Kill(_posKey);
        return DOTween.To(
                () => RectTransform.anchoredPosition,
                value => RectTransform.anchoredPosition = value,
                position,
                duration)
            .SetEase(ease)
            .SetId(_posKey);
#else
        return AnimatePositionAsync(position, duration, ease);
#endif
    }

#if !DOTWEEN
    private async Awaitable<bool> AnimateSizeAsync(Vector2 size, float duration, UIEase ease)
    {
        CancelSizeAnimation();
        if (duration <= 0f)
        {
            RectTransform.sizeDelta = size;
            return true;
        }

        var owner = new CancellationTokenSource();
        _sizeCts = owner;
        try
        {
            return await UIAnimationUtility.AnimateVector2(
                RectTransform.sizeDelta,
                size,
                duration,
                ease,
                value => RectTransform.sizeDelta = value,
                owner.Token);
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_sizeCts, owner)) UnityEngine.Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            if (ReferenceEquals(_sizeCts, owner))
            {
                _sizeCts = null;
                owner.Dispose();
            }
        }
    }

    private async Awaitable<bool> AnimatePositionAsync(Vector2 position, float duration, UIEase ease)
    {
        CancelPositionAnimation();
        if (duration <= 0f)
        {
            RectTransform.anchoredPosition = position;
            return true;
        }

        var owner = new CancellationTokenSource();
        _posCts = owner;
        try
        {
            return await UIAnimationUtility.AnimateVector2(
                RectTransform.anchoredPosition,
                position,
                duration,
                ease,
                value => RectTransform.anchoredPosition = value,
                owner.Token);
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_posCts, owner)) UnityEngine.Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            if (ReferenceEquals(_posCts, owner))
            {
                _posCts = null;
                owner.Dispose();
            }
        }
    }

    private void CancelSizeAnimation()
    {
        CancellationTokenSource owner = _sizeCts;
        if (owner == null) return;
        _sizeCts = null;
        try
        {
            owner.Cancel();
        }
        finally
        {
            owner.Dispose();
        }
    }

    private void CancelPositionAnimation()
    {
        CancellationTokenSource owner = _posCts;
        if (owner == null) return;
        _posCts = null;
        try
        {
            owner.Cancel();
        }
        finally
        {
            owner.Dispose();
        }
    }
#endif

    // full range(0↔목표치수) 기준 시간을 현재→target 남은 거리에 비례해 스케일 (전환 지점과 무관하게 일정 속도). 축(Vertical=Height, Horizontal=Width)에 맞는 치수 사용.
    private float ScaledDur(float target, float duration)
    {
        float full = Dur(duration);
        float fullRange = Mathf.Max(IsVertical ? ResolvedExpandedHeight : ResolvedExpandedWidth, 1f);
        float current = IsVertical ? RectTransform.sizeDelta.y : RectTransform.sizeDelta.x;
        float dist = Mathf.Abs(target - current);
        return full * Mathf.Clamp01(dist / fullRange);
    }

    #endregion
}
