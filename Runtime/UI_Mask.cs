using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mask + Image 스텐실 기반 마스크 컴포넌트. 부착된 Image를 Mask의 그래픽으로 사용해 자식 UI를 잘라냅니다.
/// reveal 애니메이션(Expand/Collapse) + targetRect 등 공통 로직은 베이스 UI_MaskBase에 있습니다.
/// Image 기능(sprite/maxSize/cover)은 쓰지 않고 RectTransform 영역만 다루므로, Mask 그래픽용 Image는 RequireComponent로만 보장합니다(별도 raw Image).
/// 사각 클리핑(그래픽 불필요)만 필요하면 UI_Mask2D(RectMask2D 기반)를 사용하세요.
/// </summary>
[RequireComponent(typeof(Mask))]
[RequireComponent(typeof(Image))]
public class UI_Mask : UI_MaskBase
{
}
