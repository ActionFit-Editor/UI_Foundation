using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

/// <summary>UI_Button — 레거시 호출부를 단계적으로 이전하기 위한 additive 호환 API.</summary>
public partial class UI_Button
{
    private bool _compatibilityInitialized;
    private bool _invokeDownUpEvents;
    private Action _holdAction;
    private float _holdDelay;
    private float _holdInterval;
    private CancellationTokenSource _holdCts;

    protected UI_Text _contextText;

    public virtual bool Transition => true;

    public bool TabEffect
    {
        get => playClickSound;
        set
        {
            if (playClickSound == value) return;
            playClickSound = value;
            RegisterClickSound();
        }
    }

    public UI_Image ButtonImage => this;

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

    /// <summary>
    /// 레거시 UI.Initialize와 같은 1회 초기화 계약을 제공합니다.
    /// Foundation의 필수 캐시는 Awake 전 수동 호출에도 안전하게 먼저 준비됩니다.
    /// </summary>
    public override bool Initialize()
    {
        if (_compatibilityInitialized)
        {
            EnsureFoundationInitialized();
            return false;
        }

        base.Initialize();
        EnsureFoundationInitialized();
        _compatibilityInitialized = true;
        _contextText = GetComponentInChildren<UI_Text>();
        return true;
    }

    public UI_Button SetEvent(UnityAction action)
    {
        Initialize();
        if (action == null) return this;

        RemoveListener(action);
        AddListener(action);
        return this;
    }

    public UI_Button RemoveEvent(UnityAction action)
    {
        Initialize();
        if (action != null) RemoveListener(action);
        return this;
    }

    public UI_Button RemoveAllEvents()
    {
        Initialize();
        // LegacyUI.UI_Button clears the backing Button.onClick event itself, so the
        // registered click feedback is removed together with consumer listeners.
        ClickEvent.RemoveAllListeners();
        OnRightClick = null;
        return this;
    }

    public UI_Button SetRightClickEvent(Action action)
    {
        Initialize();
        if (action == null) return this;

        OnRightClick -= action;
        OnRightClick += action;
        return this;
    }

    public UI_Button RemoveRightClickEvent(Action action)
    {
        Initialize();
        if (action != null) OnRightClick -= action;
        return this;
    }

    /// <summary>직접 포인터 경로에서 down/up 이벤트 전달을 활성화합니다.</summary>
    public void SetDownUpButton()
    {
        Initialize();
        _invokeDownUpEvents = true;
    }

    /// <summary>누른 상태를 유지하는 동안 지정한 간격으로 콜백을 반복합니다.</summary>
    public UI_Button SetHoldEvent(Action action, float holdDelay = 0.5f, float holdInterval = 0.1f)
    {
        Initialize();
        CancelHold();
        _holdAction = action;
        _holdDelay = Mathf.Max(0f, holdDelay);
        _holdInterval = Mathf.Max(0f, holdInterval);
        return this;
    }

    /// <summary>레거시 SetActive와 동일하게 GameObject가 아닌 버튼 상호작용 상태를 설정합니다.</summary>
    public UI_Button SetActive(bool active, bool setColor = true)
    {
        Initialize();
        SetLegacyActiveState(active);

        if (!setColor) return this;

        Color stateColor = UIButtonServices.GetButtonStateColor(active);
        SetColor(stateColor);
        ContextText?.SetColor(stateColor);
        return this;
    }

    private void SetLegacyActiveState(bool active)
    {
        KillEnableAnimation();
        InitializeInteractableState();
        _disabled = !active;
        SetRuntimeInteractable(active);
        if (!active) CancelPointerInteraction();
    }

    /// <summary>프로젝트가 등록한 클릭음과 햅틱 피드백을 실행합니다.</summary>
    protected virtual void OnButtonEffect()
    {
        UIButtonServices.PlayClickSound();
        UIButtonServices.PlayHaptic();
    }

    private void NotifyPointerDown()
    {
        if (_invokeDownUpEvents) OnButtonDown?.Invoke();
        StartHold();
    }

    private void NotifyPointerUp()
    {
        CancelHold();
        if (_invokeDownUpEvents) OnButtonUp?.Invoke();
    }

    private void NotifyRightClick()
    {
        OnRightClick?.Invoke();
    }

    private void StartHold()
    {
        if (_holdAction == null || !IsInteractionAllowed()) return;

        CancelHold();
        var owner = new CancellationTokenSource();
        _holdCts = owner;
        _ = HoldLoopAsync(owner);
    }

    private async Awaitable HoldLoopAsync(CancellationTokenSource owner)
    {
        try
        {
            if (!await WaitForHoldDelayAsync(_holdDelay, owner.Token)) return;
            while (!owner.IsCancellationRequested && _pointerDown && IsInteractionAllowed())
            {
                _holdAction?.Invoke();
                if (!await WaitForHoldDelayAsync(_holdInterval, owner.Token)) return;
            }
        }
        catch (OperationCanceledException)
        {
            // Pointer up/exit, disable, or state gating owns cancellation.
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_holdCts, owner)) UnityEngine.Debug.LogException(exception, this);
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

    private async Awaitable<bool> WaitForHoldDelayAsync(float seconds, CancellationToken cancellationToken)
    {
        float elapsed = 0f;
        do
        {
            if (cancellationToken.IsCancellationRequested || !_pointerDown || !IsInteractionAllowed()) return false;
            await Awaitable.NextFrameAsync(cancellationToken);
            elapsed += Time.deltaTime;
        }
        while (elapsed < seconds);

        return !cancellationToken.IsCancellationRequested && _pointerDown && IsInteractionAllowed();
    }

    private void CancelHold()
    {
        CancellationTokenSource owner = _holdCts;
        _holdCts = null;
        if (owner == null) return;

        try
        {
            owner.Cancel();
        }
        finally
        {
            owner.Dispose();
        }
    }
}
