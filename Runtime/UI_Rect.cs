using UnityEngine;

/// <summary>
/// RectTransform을 가진 UI 오브젝트의 베이스 컴포넌트.
/// Image가 붙지 않는 일반 UI 컨테이너/레이아웃 오브젝트에 부착하여 직렬화 시 타입 특정성을 부여하기 위해 사용합니다.
/// UI_Image / UI_Button 등은 이 클래스를 상속합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UI_Rect : MonoBehaviour
{
    private RectTransform _rectTransform; // RectTransform 캐시
    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;
}
