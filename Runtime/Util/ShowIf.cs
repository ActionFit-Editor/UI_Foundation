using System;
using System.Diagnostics;

/// <summary>
/// 지정한 bool 직렬화 필드가 true일 때만 인스펙터에 노출되는 마커 attribute.
///
/// 처리 주체는 UI_ImageEditor(공통 에디터)로, 리플렉션으로 이 attribute를 읽어 조건부로 PropertyField를 그린다.
/// PropertyDrawer가 아니므로 (1) [ChainedVector3] 등 기존 드로어와 충돌하지 않고,
/// (2) [CustomEditor(typeof(UI_Image), true)]의 상속 적용을 통해 UI_Button 등 파생 클래스에도 자동 적용된다
///     — 파생 에디터에서 조건부 처리를 누락해 필드가 항상 보이던 문제(coverFill 등)를 원천 차단.
///
/// [Conditional("UNITY_EDITOR")]라 플레이어 빌드에서는 이 attribute 적용이 컴파일에서 제거되어 런타임 영향이 없다.
/// </summary>
[Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ShowIfAttribute : Attribute
{
    public readonly string Condition; // 조건이 되는 bool 직렬화 필드명 (nameof 권장)

    public ShowIfAttribute(string condition) => Condition = condition;
}
