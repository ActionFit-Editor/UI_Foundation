using UnityEngine;

/// <summary>
/// 마스크 reveal 컴포넌트의 공통 추상 베이스. RectTransform 영역(크기/위치/피벗) 기반 Expand/Collapse 애니메이션을 제공합니다.
/// 실제 마스킹 방식은 파생 클래스가 정합니다: UI_Mask(Mask + Image 스텐실), UI_Mask2D(RectMask2D 사각 클리핑).
/// 애니메이션 파트는 UI_MaskBase.Animation.cs(파티셜)에 분리되어 있습니다.
/// isAnimationMask를 켜야 인스펙터에 애니 설정(AnimationPivot/targetRect/치수/Duration/Ease)이 노출되고 피벗이 자동 적용됩니다(UI_MaskBaseEditor가 게이트).
/// [ExecuteAlways]: 에디트 모드에서 targetRect 크기를 마스크에 라이브로 반영하기 위함(UI_MaskBase.Animation.cs의 Update 참고). 런타임 동작에는 영향 없음.
/// </summary>
[ExecuteAlways]
public abstract partial class UI_MaskBase : UI_Rect
{
    [SerializeField] private bool isAnimationMask; // true면 마스크 reveal 애니메이션 사용 + 인스펙터에 애니 설정 노출 + 피벗 자동 적용
}
