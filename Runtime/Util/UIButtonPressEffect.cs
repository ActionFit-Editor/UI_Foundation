#if DOTWEEN
using DG.Tweening;
#else
using System;
using System.Threading;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private const float ReturnDuration = 0.1f;

    [SerializeField] private float scaleDownRatio = .95f;
    [SerializeField] private bool isScaleOneFix = false;
    [SerializeField] private Transform targetTransform; // 스케일 변화 대상 (비어있으면 자기 자신)

    private Vector3 _defaultScale;
    private Transform _pressedTarget;
    private bool _hasDefaultScale;
    private Button _button; // Button이 있으면 interactable 상태를 자동 체크
    private bool _buttonCached;
#if DOTWEEN
    private readonly object _returnAnimKey = new(); // 복귀 애니메이션 인스턴스 고유 식별자
#else
    private CancellationTokenSource _returnAnimCts; // 복귀 애니메이션 취소 수명
#endif

    private Transform Target => targetTransform != null ? targetTransform : transform;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        Transform target = Target;
        if (!_hasDefaultScale || _pressedTarget != target)
        {
            RestoreDefaultScale();
            _pressedTarget = target;
            _defaultScale = isScaleOneFix ? Vector3.one : target.localScale;
            _hasDefaultScale = true;
        }

        CancelReturnAnimation(false);
        target.localScale = _defaultScale * scaleDownRatio;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_hasDefaultScale || _pressedTarget == null) return;

        CancelReturnAnimation(false);
#if DOTWEEN
        Transform owner = _pressedTarget;
        DOTween.To(
                () => owner != null ? owner.localScale : _defaultScale,
                value =>
                {
                    if (owner != null) owner.localScale = value;
                },
                _defaultScale,
                ReturnDuration)
            .SetEase(Ease.OutQuad)
            .SetId(_returnAnimKey);
#else
        _returnAnimCts = new CancellationTokenSource();
        _ = RestoreScaleAsync(_pressedTarget, _returnAnimCts);
#endif
    }

    private void OnDisable()
    {
        CancelReturnAnimation(true);
        ClearCapturedScale();
    }

    private void OnDestroy()
    {
        CancelReturnAnimation(true);
        ClearCapturedScale();
    }

#if !DOTWEEN
    private async Awaitable RestoreScaleAsync(Transform target, CancellationTokenSource owner)
    {
        bool completed = false;
        try
        {
            completed = await UIAnimationUtility.AnimateVector3(
                target.localScale,
                _defaultScale,
                ReturnDuration,
                UIEase.OutQuad,
                value =>
                {
                    if (target != null) target.localScale = value;
                },
                owner.Token);
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_returnAnimCts, owner)) UnityEngine.Debug.LogException(exception, this);
        }
        finally
        {
            if (ReferenceEquals(_returnAnimCts, owner))
            {
                _returnAnimCts = null;
                owner.Dispose();
                if (completed && target != null) target.localScale = _defaultScale;
            }
        }
    }
#endif

    // 이 컴포넌트가 소유한 복귀 애니메이션만 취소하고, 필요하면 원래 스케일을 복원합니다.
    private void CancelReturnAnimation(bool restoreScale)
    {
#if DOTWEEN
        DOTween.Kill(_returnAnimKey);
#else
        if (_returnAnimCts != null)
        {
            CancellationTokenSource owner = _returnAnimCts;
            _returnAnimCts = null;
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
        if (restoreScale) RestoreDefaultScale();
    }

    private void RestoreDefaultScale()
    {
        if (!_hasDefaultScale || _pressedTarget == null) return;
        _pressedTarget.localScale = _defaultScale;
    }

    private void ClearCapturedScale()
    {
        _pressedTarget = null;
        _hasDefaultScale = false;
    }

    // Button 컴포넌트가 있으면 interactable을 체크, 없으면 항상 true
    private bool IsInteractable()
    {
        if (!_buttonCached)
        {
            _button = GetComponent<Button>();
            _buttonCached = true;
        }
        return _button == null || _button.interactable;
    }
}
