#if DOTWEEN
using DG.Tweening;
#else
using System;
using System.Threading;
#endif
using UnityEngine;

/// <summary>UI_Button — 포인터 press/exit/re-enter/release 스케일 연출.</summary>
public partial class UI_Button
{
    private const float PressReturnDuration = 0.1f;

    [SerializeField] private bool usePressEffect = true;
    [SerializeField, ShowIf(nameof(usePressEffect))] private float scaleDownRatio = 0.95f;
    [SerializeField, ShowIf(nameof(usePressEffect))] private bool isScaleOneFix = false;
    [SerializeField, ShowIf(nameof(usePressEffect))] private Transform targetTransform;

    private Vector3 _defaultPressScale;
    private Transform _pressedTarget;
    private bool _hasDefaultPressScale;
#if DOTWEEN
    private readonly object _pressReturnAnimKey = new();
#else
    private CancellationTokenSource _pressReturnAnimCts;
#endif

    private Transform PressTarget => targetTransform != null ? targetTransform : transform;

    private void ApplyPressEffect()
    {
        if (!Transition || !usePressEffect) return;

        Transform target = PressTarget;
        if (!_hasDefaultPressScale || _pressedTarget != target)
        {
            RestoreDefaultPressScale();
            _pressedTarget = target;
            _defaultPressScale = isScaleOneFix ? Vector3.one : target.localScale;
            _hasDefaultPressScale = true;
        }

        CancelPressReturnAnimation(false);
        target.localScale = _defaultPressScale * scaleDownRatio;
    }

    private void ReleasePressEffect()
    {
        if (!_hasDefaultPressScale || _pressedTarget == null) return;

        CancelPressReturnAnimation(false);
#if DOTWEEN
        Transform owner = _pressedTarget;
        DOTween.To(
                () => owner != null ? owner.localScale : _defaultPressScale,
                value =>
                {
                    if (owner != null) owner.localScale = value;
                },
                _defaultPressScale,
                PressReturnDuration)
            .SetEase(Ease.OutQuad)
            .SetId(_pressReturnAnimKey);
#else
        _pressReturnAnimCts = new CancellationTokenSource();
        _ = RestorePressScaleAsync(_pressedTarget, _pressReturnAnimCts);
#endif
    }

#if !DOTWEEN
    private async Awaitable RestorePressScaleAsync(Transform target, CancellationTokenSource owner)
    {
        bool completed = false;
        try
        {
            completed = await UIAnimationUtility.AnimateVector3(
                target.localScale,
                _defaultPressScale,
                PressReturnDuration,
                UIEase.OutQuad,
                value =>
                {
                    if (target != null) target.localScale = value;
                },
                owner.Token);
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_pressReturnAnimCts, owner)) UnityEngine.Debug.LogException(exception, this);
        }
        finally
        {
            if (ReferenceEquals(_pressReturnAnimCts, owner))
            {
                _pressReturnAnimCts = null;
                owner.Dispose();
                if (completed && target != null) target.localScale = _defaultPressScale;
            }
        }
    }
#endif

    private void CancelPressReturnAnimation(bool restoreScale)
    {
#if DOTWEEN
        DOTween.Kill(_pressReturnAnimKey);
#else
        if (_pressReturnAnimCts != null)
        {
            CancellationTokenSource owner = _pressReturnAnimCts;
            _pressReturnAnimCts = null;
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
        if (restoreScale) RestoreDefaultPressScale();
    }

    private void RestoreDefaultPressScale()
    {
        if (!_hasDefaultPressScale || _pressedTarget == null) return;
        _pressedTarget.localScale = _defaultPressScale;
    }

    private void ClearCapturedPressScale()
    {
        _pressedTarget = null;
        _hasDefaultPressScale = false;
    }

    private void ResetPressEffect()
    {
        usePressEffect = true;
        scaleDownRatio = 0.95f;
        isScaleOneFix = false;
        targetTransform = null;
    }
}
