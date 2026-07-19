using UnityEngine;

/// <summary>UI_Button — 클릭 시 공용 클릭음(clipShare.buttonClick, Overlay) 재생.</summary>
public partial class UI_Button
{
    [SerializeField] private bool playClickSound = true; // 클릭음 재생 여부 — 토글/탭 등은 false로 끌 수 있음

    // 공용 클릭음 리스너를 onClick에 등록 (중복 방지: 먼저 제거 후 추가). Awake + RemoveAllListeners 후 호출.
    private void RegisterClickSound()
    {
        ClickEvent.RemoveListener(PlayClickSound);
        ClickEvent.AddListener(PlayClickSound);
    }

    // 클릭 시 공용 클릭음 재생 (Overlay). playClickSound=false면 생략, 클립 미할당 시 PlaySE가 no-op.
    private void PlayClickSound()
    {
        if (!playClickSound) return;
        UIButtonServices.PlayClickSound();
    }
}
