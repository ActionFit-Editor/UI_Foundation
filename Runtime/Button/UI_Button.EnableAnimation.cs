#if DOTWEEN
using DG.Tweening;
#else
using System;
using System.Threading;
#endif
using UnityEngine;

/// <summary>UI_Button — UseEnableAnimation: Disable→Enable 전환 시 스케일 팝 애니메이션 (peak에서 비주얼 복원).</summary>
public partial class UI_Button
{
    [SerializeField] private bool useEnableAnimation = true; // Disable→Enable 전환 시 스케일 애니메이션 재생 여부
    [SerializeField, ChainedVector3, ShowIf(nameof(useEnableAnimation))] private Vector3 animScaleXYZ = new Vector3(1.05f, 1.05f, 1.05f); // 피크 스케일 배율
    [SerializeField, ShowIf(nameof(useEnableAnimation))] private float animDuration = 0.3f; // 전체 애니메이션 시간 (초)
#if DOTWEEN
    [SerializeField, ShowIf(nameof(useEnableAnimation))] private Ease animEaseUp = Ease.OutSine; // peak으로 확대할 때 이징
    [SerializeField, ShowIf(nameof(useEnableAnimation))] private Ease animEaseDown = Ease.InSine; // base로 축소할 때 이징
#else
    [SerializeField, ShowIf(nameof(useEnableAnimation))] private UIEase animEaseUp = UIEase.OutSine; // peak으로 확대할 때 이징
    [SerializeField, ShowIf(nameof(useEnableAnimation))] private UIEase animEaseDown = UIEase.InSine; // base로 축소할 때 이징
#endif

    private Vector3 _baseScale; // 애니메이션 기준 스케일
#if DOTWEEN
    private readonly object _enableAnimKey = new(); // Enable 애니메이션 식별자 (Kill용)
#else
    private CancellationTokenSource _enableAnimCts; // Enable 애니메이션 취소 수명
#endif

    private void InitEnableAnimation() => _baseScale = transform.localScale; // Awake

    private void KillEnableAnimation()
    {
#if DOTWEEN
        DOTween.Kill(_enableAnimKey);
#else
        CancelEnableAnimation();
#endif
    }

    // Enable 전환 스케일 애니메이션 재생 (통합 press effect와 별도 ID/취소 수명을 사용).
    // peak 스케일 도달 순간 RestoreNormalVisuals로 sprite/color를 normal로 교체.
    private void PlayEnableAnimation()
    {
#if DOTWEEN
        DOTween.Kill(_enableAnimKey);

        var peakScale = Vector3.Scale(_baseScale, animScaleXYZ);
        var half = Mathf.Max(animDuration * 0.5f, 0.01f);
        DOTween.Sequence()
            .Append(DOTween.To(() => transform.localScale, value => transform.localScale = value, peakScale, half).SetEase(animEaseUp))
            .AppendCallback(RestoreNormalVisuals)
            .Append(DOTween.To(() => transform.localScale, value => transform.localScale = value, _baseScale, half).SetEase(animEaseDown))
            .OnKill(RestoreNormalVisuals)
            .SetId(_enableAnimKey);
#else
        CancelEnableAnimation();
        _enableAnimCts = new CancellationTokenSource();
        _ = PlayEnableAnimationAsync(_enableAnimCts);
#endif
    }

#if !DOTWEEN
    private async Awaitable PlayEnableAnimationAsync(CancellationTokenSource owner)
    {
        try
        {
            Vector3 peakScale = Vector3.Scale(_baseScale, animScaleXYZ);
            float half = Mathf.Max(animDuration * 0.5f, 0.01f);
            bool completed = await UIAnimationUtility.AnimateVector3(
                transform.localScale,
                peakScale,
                half,
                animEaseUp,
                value => transform.localScale = value,
                owner.Token);

            if (completed)
            {
                RestoreNormalVisuals();
                await UIAnimationUtility.AnimateVector3(
                    transform.localScale,
                    _baseScale,
                    half,
                    animEaseDown,
                    value => transform.localScale = value,
                    owner.Token);
            }
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_enableAnimCts, owner)) UnityEngine.Debug.LogException(exception, this);
        }
        finally
        {
            if (ReferenceEquals(_enableAnimCts, owner))
            {
                _enableAnimCts = null;
                owner.Dispose();
                transform.localScale = _baseScale;
                RestoreNormalVisuals();
            }
        }
    }

    private void CancelEnableAnimation()
    {
        CancellationTokenSource owner = _enableAnimCts;
        _enableAnimCts = null;
        if (owner == null) return;

        try
        {
            owner.Cancel();
        }
        finally
        {
            owner.Dispose();
        }

        transform.localScale = _baseScale;
        RestoreNormalVisuals();
    }
#endif
}
