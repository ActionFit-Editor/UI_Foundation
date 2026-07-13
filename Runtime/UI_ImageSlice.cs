using UnityEngine;

/// <summary>
/// Image_Slice(9-slice + fill) 전용 UI_Image. 추가 시 Image_Slice가 자동으로 붙는다(RequireComponent).
/// UI_Image의 모든 기능(Sprite/Color/Alpha/FillAmount 등)을 그대로 쓰며, 그래픽만 Image_Slice라 sliced-fill을 지원한다.
/// (UI_Image는 RequireComponent(Image)이고 Image_Slice : Image라 그대로 호환 — UI_Image 코드 수정 없음.)
/// </summary>
[RequireComponent(typeof(Image_Slice))]
public class UI_ImageSlice : UI_Image
{
}
