using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>UI_Button — 클릭 이벤트 등록/해제.</summary>
public partial class UI_Button
{
    [SerializeField, InspectorName("On Click ()")] private UnityEvent m_OnClick = new();

    private readonly List<CanvasGroup> _canvasGroupCache = new();
    private bool _pointerDown;

    private UnityEvent ClickEvent => m_OnClick ??= new UnityEvent();

    /// <summary>
    /// 클릭 이벤트를 등록합니다.
    /// 등록한 동일 메서드 참조로 RemoveListener를 호출하여 해제할 수 있습니다.
    /// </summary>
    public void AddListener(UnityAction callback)
    {
        ClickEvent.AddListener(callback);
    }

    /// <summary>
    /// 클릭 이벤트를 해제합니다.
    /// AddListener에 전달한 동일 메서드 참조를 전달해야 합니다.
    /// </summary>
    public void RemoveListener(UnityAction callback)
    {
        ClickEvent.RemoveListener(callback);
    }

    /// <summary>
    /// 등록된 모든 클릭 이벤트를 해제합니다. (공용 클릭음 리스너는 자동 재등록)
    /// </summary>
    public void RemoveAllListeners()
    {
        ClickEvent.RemoveAllListeners();
        RegisterClickSound(); // RemoveAllListeners가 클릭음 리스너도 함께 제거하므로 재등록
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_pointerDown && IsInteractionAllowed()) ApplyPressEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pointerDown) ReleasePressEffect();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsLeftButton(eventData) || !IsInteractionAllowed()) return;

        _pointerDown = true;
        ApplyPressEffect();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsLeftButton(eventData) || !_pointerDown) return;

        _pointerDown = false;
        ReleasePressEffect();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsLeftButton(eventData) || !IsInteractionAllowed()) return;
        ClickEvent.Invoke();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelPointerInteraction();
    }

    private bool IsInteractionAllowed()
    {
        if (!isActiveAndEnabled || !IsInteractableState) return false;

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

    private void CancelPointerInteraction()
    {
        _pointerDown = false;
        CancelPressReturnAnimation(true);
        ClearCapturedPressScale();
    }

    private static bool IsLeftButton(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }
}
