using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sliced + Filled를 동시에 지원하는 Image. (Unity 기본 Image는 Type이 상호배타라 9-slice 테두리 보존 + 채움을 동시에 못 함)
/// Type=Filled로 두고 isSliceImage를 켜면 9-slice 테두리를 유지한 채 fillAmount만큼 채운다.
/// Image의 fillAmount/fillCenter/fillMethod/fillOrigin/sprite/color를 그대로 사용 → UI_Image도 무수정 호환(Image_Slice가 곧 Image).
/// 기능별 partial 분리: Image_Slice.cs(Core: 토글 + OnPopulateMesh 분기) / Image_Slice.Mesh.cs(선형 fill 영역과 9-slice 셀의 교차 메시 생성).
/// ⚠️ 방사형(Radial) fill은 미지원 — 그 경우 기본 Image 동작으로 폴백.
/// </summary>
[AddComponentMenu("UI/Image Slice", 12)]
public partial class Image_Slice : Image
{
    [SerializeField] private bool isSliceImage = false; // 체크 시 9-slice + fill 결합 (Type=Filled · 가로/세로 fill에서만)

    public bool IsSliceImage
    {
        get => isSliceImage;
        set
        {
            if (isSliceImage == value) return;
            isSliceImage = value;
            SetVerticesDirty();
        }
    }

    // 9-slice 채움 방향 (Image의 fillMethod/fillOrigin에서 매핑)
    private enum FillDir { Right, Left, Up, Down }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        Sprite spr = overrideSprite;

        // isSliceImage + Type=Filled + 9-slice 보더 + 가로/세로 fill 일 때만 커스텀 sliced-fill. 그 외엔 기본 Image.
        if (!isSliceImage || spr == null || type != Type.Filled || !HasBorder(spr) || !IsLinearFill())
        {
            base.OnPopulateMesh(vh);
            return;
        }

        GenerateSlicedFilledSprite(vh, spr, ResolveFillDir());
    }

    private static bool HasBorder(Sprite spr) => spr.border.sqrMagnitude > 0f;

    private bool IsLinearFill() => fillMethod == FillMethod.Horizontal || fillMethod == FillMethod.Vertical;

    // Image의 fillMethod/fillOrigin → 선형 채움 방향. (채움은 origin 반대쪽으로 자람)
    private FillDir ResolveFillDir()
    {
        if (fillMethod == FillMethod.Horizontal)
            return fillOrigin == (int)OriginHorizontal.Left ? FillDir.Right : FillDir.Left;
        // Vertical
        return fillOrigin == (int)OriginVertical.Bottom ? FillDir.Up : FillDir.Down;
    }
}
