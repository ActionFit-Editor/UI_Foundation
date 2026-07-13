using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RectMask2D 기반 마스크 컴포넌트 — RectTransform의 rect 영역으로 자식 UI를 사각 클리핑합니다.
/// reveal 애니메이션(Expand/Collapse) + targetRect 등 공통 로직은 베이스 UI_MaskBase에 있습니다.
/// UI_Mask(Mask + Image 스텐실)와 달리 그래픽 Image가 필요 없어, 크기(sizeDelta) 애니메이션 기반 reveal에 더 가볍게 맞습니다.
/// </summary>
[RequireComponent(typeof(RectMask2D))]
public class UI_Mask2D : UI_MaskBase
{
}
