using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Image나 UnityEngine.UI.Button을 요구하지 않는 포인터 클릭 컴포넌트입니다.
/// 레이캐스트 대상은 같은 오브젝트 또는 자식에 배치한 프로젝트 소유 Graphic이 담당합니다.
/// </summary>
public class UI_Clickable : UI_Rect,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    private const float PressReturnDuration = 0.1f;

    [SerializeField, InspectorName("Interactable")] private bool m_Interactable = true;
    [SerializeField, InspectorName("On Click ()")] private UnityEvent m_OnClick = new();
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private bool usePressEffect = true;
    [SerializeField, ShowIf(nameof(usePressEffect))] private float scaleDownRatio = 0.95f;
    [SerializeField, ShowIf(nameof(usePressEffect))] private bool isScaleOneFix;
    [SerializeField, ShowIf(nameof(usePressEffect))] private Transform targetTransform;

    private readonly List<CanvasGroup> _canvasGroupCache = new();
    private CancellationTokenSource _holdCts;
    private CancellationTokenSource _pressReturnCts;
    private Action _holdAction;
    private float _holdDelay;
    private float _holdInterval;
    private Vector3 _defaultPressScale;
    private Transform _pressedTarget;
    private UI_Text _contextText;
    private bool _compatibilityInitialized;
    private bool _feedbackRegistered;
    private bool _hasDefaultPressScale;
    private bool _invokeDownUpEvents;
    private bool _pointerDown;

    public virtual bool Transition => true;

    public bool TabEffect
    {
        get => playClickSound;
        set
        {
            if (playClickSound == value) return;
            playClickSound = value;
            _feedbackRegistered = value;
        }
    }

    public bool Interactable
    {
        get => m_Interactable;
        set => SetActive(value, false);
    }

    public UI_Text ContextText
    {
        get
        {
            Initialize();
            if (_contextText == null) _contextText = GetComponentInChildren<UI_Text>();
            return _contextText;
        }
    }

    public event Action OnButtonDown;
    public event Action OnButtonUp;
    public event Action OnRightClick;

    private UnityEvent ClickEvent => m_OnClick ??= new UnityEvent();
    private Transform PressTarget => targetTransform != null ? targetTransform : transform;

    /// <summary>레거시 UI.Initialize와 같은 1회 초기화 계약을 제공합니다.</summary>
    public virtual bool Initialize()
    {
        if (_compatibilityInitialized) return false;

        _compatibilityInitialized = true;
        _feedbackRegistered = playClickSound;
        _contextText = GetComponentInChildren<UI_Text>();
        return true;
    }

    public void AddListener(UnityAction action)
    {
        if (action != null) ClickEvent.AddListener(action);
    }

    public void RemoveListener(UnityAction action)
    {
        if (action != null) ClickEvent.RemoveListener(action);
    }

    public void RemoveAllListeners()
    {
        ClickEvent.RemoveAllListeners();
    }

    public UI_Clickable SetEvent(UnityAction action)
    {
        Initialize();
        if (action == null) return this;

        ClickEvent.RemoveListener(action);
        ClickEvent.AddListener(action);
        return this;
    }

    public UI_Clickable RemoveEvent(UnityAction action)
    {
        Initialize();
        if (action != null) ClickEvent.RemoveListener(action);
        return this;
    }

    public UI_Clickable SetRightClickEvent(Action action)
    {
        Initialize();
        if (action == null) return this;

        OnRightClick -= action;
        OnRightClick += action;
        return this;
    }

    public UI_Clickable RemoveRightClickEvent(Action action)
    {
        Initialize();
        if (action != null) OnRightClick -= action;
        return this;
    }

    public UI_Clickable RemoveAllEvents()
    {
        Initialize();
        RemoveAllListeners();
        _feedbackRegistered = false;
        OnRightClick = null;
        return this;
    }

    public void SetDownUpButton()
    {
        Initialize();
        _invokeDownUpEvents = true;
    }

    /// <summary>포인터를 누르고 있는 동안 지정한 지연 뒤 콜백을 반복합니다.</summary>
    public UI_Clickable SetHoldEvent(Action action, float holdDelay = 0.5f, float holdInterval = 0.1f)
    {
        Initialize();
        CancelHold();
        _holdAction = action;
        _holdDelay = Mathf.Max(0f, holdDelay);
        _holdInterval = Mathf.Max(0f, holdInterval);
        return this;
    }

    /// <summary>
    /// 레거시 버튼과 같은 호출 형태로 상호작용 가능 상태를 설정합니다.
    /// UI_Clickable은 Graphic을 소유하지 않으므로 setColor는 호환성 인자로만 유지합니다.
    /// </summary>
    public UI_Clickable SetActive(bool active, bool setColor = true)
    {
        Initialize();
        m_Interactable = active;
        if (!active) CancelPointerInteraction();
        if (setColor) ContextText?.SetColor(UIButtonServices.GetButtonStateColor(active));
        return this;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_pointerDown && IsInteractionAllowed()) ApplyPressEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_pointerDown) return;

        ReleasePressEffect();
        CancelHold();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsLeftButton(eventData) || !IsInteractionAllowed()) return;

        _pointerDown = true;
        ApplyPressEffect();
        if (_invokeDownUpEvents) OnButtonDown?.Invoke();
        StartHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsLeftButton(eventData) || !_pointerDown) return;

        _pointerDown = false;
        ReleasePressEffect();
        CancelHold();
        if (_invokeDownUpEvents) OnButtonUp?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractionAllowed()) return;

        if (IsRightButton(eventData))
        {
            OnRightClick?.Invoke();
            return;
        }

        if (!IsLeftButton(eventData)) return;
        if (_feedbackRegistered) OnButtonEffect();
        ClickEvent.Invoke();
    }

    protected virtual void OnEnable()
    {
        Initialize();
    }

    protected virtual void OnDisable()
    {
        CancelPointerInteraction();
    }

    protected virtual void OnDestroy()
    {
        CancelPointerInteraction();
    }

    /// <summary>프로젝트가 등록한 클릭음과 햅틱 피드백을 실행합니다.</summary>
    protected virtual void OnButtonEffect()
    {
        UIButtonServices.PlayClickSound();
        UIButtonServices.PlayHaptic();
    }

    private bool IsInteractionAllowed()
    {
        if (!isActiveAndEnabled || !m_Interactable) return false;

        Transform current = transform;
        while (current != null)
        {
            bool ignoreParentGroups = false;
            current.GetComponents(_canvasGroupCache);
            foreach (CanvasGroup group in _canvasGroupCache)
            {
                if (!group.interactable || !group.blocksRaycasts) return false;
                if (group.ignoreParentGroups) ignoreParentGroups = true;
            }

            if (ignoreParentGroups) break;
            current = current.parent;
        }

        return true;
    }

    private void StartHold()
    {
        CancelHold();
        if (_holdAction == null) return;

        _holdCts = new CancellationTokenSource();
        _ = HoldLoopAsync(_holdCts);
    }

    private async Awaitable HoldLoopAsync(CancellationTokenSource owner)
    {
        CancellationToken cancellationToken = owner.Token;
        try
        {
            if (!await WaitForHoldDelayAsync(_holdDelay, cancellationToken))
            {
                CancelCapturedPointerForBlockedInteraction();
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!IsInteractionAllowed())
                {
                    CancelCapturedPointerForBlockedInteraction();
                    return;
                }

                _holdAction?.Invoke();
                if (cancellationToken.IsCancellationRequested) return;
                if (!await WaitForHoldDelayAsync(_holdInterval, cancellationToken))
                {
                    CancelCapturedPointerForBlockedInteraction();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Pointer up/exit/disable cancellation is an expected control path.
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_holdCts, owner)) Debug.LogException(exception, this);
        }
        finally
        {
            if (ReferenceEquals(_holdCts, owner))
            {
                _holdCts = null;
                owner.Dispose();
            }
        }
    }

    private async Awaitable<bool> WaitForHoldDelayAsync(float duration, CancellationToken cancellationToken)
    {
        float elapsed = 0f;
        do
        {
            if (cancellationToken.IsCancellationRequested || !IsInteractionAllowed()) return false;
            await Awaitable.NextFrameAsync(cancellationToken);
            elapsed += Time.deltaTime;
        }
        while (elapsed < duration);

        return !cancellationToken.IsCancellationRequested && IsInteractionAllowed();
    }

    private void CancelHold()
    {
        if (_holdCts == null) return;

        CancellationTokenSource owner = _holdCts;
        _holdCts = null;
        try
        {
            owner.Cancel();
        }
        finally
        {
            owner.Dispose();
        }
    }

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
        _pressReturnCts = new CancellationTokenSource();
        _ = RestorePressScaleAsync(_pressedTarget, _pressReturnCts);
    }

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
            if (ReferenceEquals(_pressReturnCts, owner)) Debug.LogException(exception, this);
        }
        finally
        {
            if (ReferenceEquals(_pressReturnCts, owner))
            {
                _pressReturnCts = null;
                owner.Dispose();
                if (completed && target != null) target.localScale = _defaultPressScale;
            }
        }
    }

    private void CancelPointerInteraction()
    {
        _pointerDown = false;
        CancelHold();
        CancelPressReturnAnimation(true);
        ClearCapturedPressScale();
    }

    private void CancelCapturedPointerForBlockedInteraction()
    {
        _pointerDown = false;
        CancelPressReturnAnimation(true);
        ClearCapturedPressScale();
    }

    private void CancelPressReturnAnimation(bool restoreScale)
    {
        if (_pressReturnCts != null)
        {
            CancellationTokenSource owner = _pressReturnCts;
            _pressReturnCts = null;
            try
            {
                owner.Cancel();
            }
            finally
            {
                owner.Dispose();
            }
        }

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

    private static bool IsLeftButton(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }

    private static bool IsRightButton(PointerEventData eventData)
    {
        return eventData != null && eventData.button == PointerEventData.InputButton.Right;
    }
}
