using UnityEngine.Events;

/// <summary>UI_Button — 클릭 이벤트 등록/해제.</summary>
public partial class UI_Button
{
    /// <summary>
    /// 클릭 이벤트를 등록합니다.
    /// 등록한 동일 메서드 참조로 RemoveListener를 호출하여 해제할 수 있습니다.
    /// </summary>
    public void AddListener(UnityAction callback)
    {
        Button.onClick.AddListener(callback);
    }

    /// <summary>
    /// 클릭 이벤트를 해제합니다.
    /// AddListener에 전달한 동일 메서드 참조를 전달해야 합니다.
    /// </summary>
    public void RemoveListener(UnityAction callback)
    {
        Button.onClick.RemoveListener(callback);
    }

    /// <summary>
    /// 등록된 모든 클릭 이벤트를 해제합니다. (공용 클릭음 리스너는 자동 재등록)
    /// </summary>
    public void RemoveAllListeners()
    {
        Button.onClick.RemoveAllListeners();
        RegisterClickSound(); // RemoveAllListeners가 클릭음 리스너도 함께 제거하므로 재등록
    }
}
