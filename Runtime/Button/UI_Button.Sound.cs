using UnityEngine;

/// <summary>UI_Button — 클릭 시 공용 클릭음(clipShare.buttonClick, Overlay) 재생.</summary>
public partial class UI_Button
{
    [SerializeField] private bool playClickSound = true; // 클릭음 재생 여부 — 토글/탭 등은 false로 끌 수 있음

    // 공용 클릭 피드백 리스너를 onClick에 등록 (중복 방지: 먼저 제거 후 추가). Awake + RemoveAllListeners 후 호출.
    private void RegisterClickSound()
    {
        ClickEvent.RemoveListener(OnButtonEffect);
        if (playClickSound) ClickEvent.AddListener(OnButtonEffect);
    }
}
