using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image 컴포넌트를 래핑하여 스크립트를 통해 제어합니다.
/// Image 컴포넌트에 직접 접근하지 않고 이 스크립트를 통해 Sprite, Color 등을 설정합니다.
/// sprite 비율 기반 크기 모드 3종(상호배타, 하나만 사용):
///   setMaxSize        — maxWidth/maxHeight 박스 안에 비율 유지하며 맞춤 (object-fit: contain)
///   updateAspectRatio — aspectMode 기준 한 축으로 반대 축 보정 (AspectRatioFitter)
///   coverFill         — 부모 영역을 비율 유지하며 꽉 채움 (object-fit: cover, 넘침은 부모 Mask로 가림)
/// </summary>
[RequireComponent(typeof(Image))]
public class UI_Image : UI_Rect
{
    public enum AspectMode
    {
        None, // 비율 맞춤 안 함
        WidthControlsHeight, // 현재 width 유지, height를 sprite 비율로 보정
        HeightControlsWidth // 현재 height 유지, width를 sprite 비율로 보정
    }

    #region Fields

    [SerializeField] private bool setMaxSize = false; // 체크 시 maxWidth/maxHeight 안쪽으로 sprite 원본 비율 유지하며 최대 크기 적용 (Fit 모드)
    [SerializeField, ShowIf(nameof(setMaxSize))] private float maxWidth = 0f; // 가로 최대 크기. 0 이하면 가로 제한 없음
    [SerializeField, ShowIf(nameof(setMaxSize))] private float maxHeight = 0f; // 세로 최대 크기. 0 이하면 세로 제한 없음

    [SerializeField] private bool updateAspectRatio = false; // 비율 맞춤 마스터 토글. true면 OnEnable·Sprite 설정·크기 변경 시 aspectMode 기준으로 자동 적용 (AspectRatioFitter처럼 지속 추적, setMaxSize와 동시 사용 금지)
    [SerializeField, ShowIf(nameof(updateAspectRatio))] private AspectMode aspectMode = AspectMode.None; // 기준 축. WidthControlsHeight면 width 유지·height 보정, HeightControlsWidth면 반대 (updateAspectRatio=true일 때만 Inspector 노출)

    [SerializeField] private bool coverFill = false; // 체크 시 부모 영역을 sprite 원본 비율로 꽉 채움 (CSS object-fit: cover). setMaxSize/updateAspectRatio와 동시 사용 금지
    [SerializeField, ShowIf(nameof(coverFill))] private Vector2 coverMaxAspect = new Vector2(9f, 16f); // cover 가로:세로 최대 비율 캡. 부모가 이보다 가로로 넓으면 그 비율까지만 채움. 0 이하면 캡 비활성 (coverFill=true일 때만 노출)
    [SerializeField, ShowIf(nameof(coverFill))] private Vector2 coverPivot = new Vector2(0.5f, 0.5f); // cover 정렬 기준점 (0~1). (0.5,0.5)=중앙, (0.5,1)=상단 정렬(넘침 하단), (0.5,0)=하단 정렬(넘침 상단) (coverFill=true일 때만 노출)

    [SerializeField, HideInInspector] private Image _image; // 직렬화 캐시 — Reset/OnValidate가 자동 채움

    private CancellationTokenSource _coverReapplyCts; // cover 1프레임 지연 재적용 라이프사이클

    #endregion

    #region Properties

    public Image Image
    {
        get
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
                UnityEngine.Debug.LogError($"[UI_Image] _image not serialized — runtime GetComponent fallback. Run Tools/Package/UI Foundation/Migrate Component Refs. (gameObject={name})", this);
            }
            return _image;
        }
    }
    public Sprite Sprite
    {
        get => Image.sprite;
        set
        {
            Image.sprite = value;
            ApplyMaxSize();
            if (updateAspectRatio) ApplyAspectRatio();
            if (coverFill) ApplyCoverFill();
        }
    }
    public Color Color
    {
        get => Image.color;
        set => Image.color = value;
    }
    public float Alpha
    {
        get => Image.color.a;
        set
        {
            var c = Image.color;
            c.a = value;
            Image.color = c;
        }
    }
    public bool RaycastTarget
    {
        get => Image.raycastTarget;
        set => Image.raycastTarget = value;
    }
    public float FillAmount
    {
        get => Image.fillAmount;
        set => Image.fillAmount = value;
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void OnEnable()
    {
        ApplyMaxSize();
        if (updateAspectRatio) ApplyAspectRatio();
        if (coverFill)
        {
            ApplyCoverFill();
            _ = ReapplyCoverFillNextFrameAsync(); // OnEnable 시점 부모(캔버스)가 reference resolution일 수 있어 레이아웃 확정 후 1회 재적용
        }
    }

    protected virtual void OnDisable()
    {
        CancelCoverReapply();
    }

    // 캔버스 첫 레이아웃(CanvasScaler 실제 해상도 확정) 이후 cover를 1회 재적용
    private async Awaitable ReapplyCoverFillNextFrameAsync()
    {
        CancelCoverReapply();
        var owner = new CancellationTokenSource();
        _coverReapplyCts = owner;

        try
        {
            await Awaitable.NextFrameAsync(owner.Token);
            if (!owner.IsCancellationRequested) ApplyCoverFill();
        }
        catch (OperationCanceledException)
        {
            // OnDisable or a newer request owns cleanup.
        }
        finally
        {
            if (ReferenceEquals(_coverReapplyCts, owner))
            {
                _coverReapplyCts = null;
                owner.Dispose();
            }
        }
    }

    // cover 지연 재적용 취소 + 정리
    private void CancelCoverReapply()
    {
        CancellationTokenSource owner = _coverReapplyCts;
        _coverReapplyCts = null;
        if (owner == null) return;

        try
        {
            owner.Cancel();
        }
        finally
        {
            owner.Dispose();
        }
    }

    protected virtual void Reset()
    {
#if UNITY_EDITOR
        _image = GetComponent<Image>();
#endif
    }

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        if (_image == null) _image = GetComponent<Image>();
        ApplyMaxSize();
        if (updateAspectRatio) ApplyAspectRatio();
        if (coverFill) ApplyCoverFill();
#endif
    }

    // updateAspectRatio/coverFill=true면 RectTransform(또는 부모) 크기가 바뀔 때마다 보정 (매 프레임 Update보다 효율적 — 변경 시에만 호출)
    protected virtual void OnRectTransformDimensionsChange()
    {
        if (updateAspectRatio) ApplyAspectRatio();
        if (coverFill) ApplyCoverFill();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Resources 경로로 Sprite를 로드하여 설정합니다. setMaxSize=true면 적용 후 즉시 ApplyMaxSize.
    /// </summary>
    public void SetSprite(string resourcePath)
    {
        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            Image.sprite = sprite;
            ApplyMaxSize();
            if (updateAspectRatio) ApplyAspectRatio();
            if (coverFill) ApplyCoverFill();
        }
        else UnityEngine.Debug.LogError($"[UI_Image] Sprite not found: {resourcePath}");
    }

    /// <summary>
    /// Image의 SetNativeSize를 호출합니다.
    /// </summary>
    public void SetNativeSize() => Image.SetNativeSize();

    /// <summary>
    /// maxWidth/maxHeight 안쪽으로 현재 sprite 원본 비율을 유지하며 최대 크기로 RectTransform.sizeDelta를 적용합니다.
    /// setMaxSize=false면 무시. maxWidth/maxHeight 둘 다 0 이하면 SetNativeSize() 호출.
    /// 외부에서 Image.sprite를 .Sprite 프로퍼티가 아닌 Image 컴포넌트로 직접 교체한 경우 수동 호출하여 반영하세요.
    /// </summary>
    public void ApplyMaxSize()
    {
        if (!setMaxSize) return;
        if (Image == null || Image.sprite == null) return;

        var sRect = Image.sprite.rect;
        if (sRect.width <= 0f || sRect.height <= 0f) return;

        float scaleW = maxWidth > 0f ? maxWidth / sRect.width : float.PositiveInfinity;
        float scaleH = maxHeight > 0f ? maxHeight / sRect.height : float.PositiveInfinity;

        if (float.IsPositiveInfinity(scaleW) && float.IsPositiveInfinity(scaleH))
        {
            Image.SetNativeSize();
            return;
        }

        float scale = Mathf.Min(scaleW, scaleH);
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(sRect.width * scale, sRect.height * scale);
    }

    /// <summary>
    /// aspectMode 기준으로 한 축을 유지한 채 다른 축을 현재 sprite 원본 비율에 맞춰 RectTransform 크기를 보정합니다.
    /// WidthControlsHeight면 현재 width 유지·height 보정, HeightControlsWidth면 그 반대. aspectMode=None이면 무시.
    /// 기준 축 크기는 sizeDelta가 아닌 rect(앵커 반영 실제 크기)에서 읽고, SetSizeWithCurrentAnchors로 적용하므로 stretch 앵커여도 올바르게 동작합니다.
    /// updateAspectRatio=true면 OnEnable·Sprite 설정·크기 변경 시 자동 호출됩니다. 외부에서 1회성 보정이 필요하면 이 메서드를 직접 호출하세요.
    /// </summary>
    public void ApplyAspectRatio()
    {
        if (aspectMode == AspectMode.None) return;
        if (Image == null || Image.sprite == null) return;

        var sRect = Image.sprite.rect;
        if (sRect.width <= 0f || sRect.height <= 0f) return;

        float aspectRatio = sRect.width / sRect.height;

        if (aspectMode == AspectMode.WidthControlsHeight)
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RectTransform.rect.width / aspectRatio);
        else
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RectTransform.rect.height * aspectRatio);
    }

    /// <summary>
    /// 부모 RectTransform 영역을 sprite 원본 비율로 꽉 채웁니다 (CSS object-fit: cover). 넘치는 영역은 부모의 RectMask2D/Mask로 가립니다.
    /// 부모 비율보다 sprite가 더 가로로 길면 높이를 맞추고 가로로 넘치며, 더 세로로 길면 너비를 맞추고 세로로 넘칩니다.
    /// coverMaxAspect로 가로 비율 캡, coverPivot으로 정렬 기준점을 지정합니다. coverFill=false면 무시.
    /// 앵커/피벗/anchoredPosition/sizeDelta를 모두 덮어쓰므로 setMaxSize·updateAspectRatio와 동시 사용하지 마세요.
    /// coverFill=true면 OnEnable·Sprite 설정·(부모) 크기 변경 시 자동 호출됩니다.
    /// </summary>
    public void ApplyCoverFill()
    {
        if (!coverFill) return;
        if (Image == null || Image.sprite == null) return;
        if (!(RectTransform.parent is RectTransform parent))
        {
            UnityEngine.Debug.LogError("[UI_Image] ApplyCoverFill: parent is not a RectTransform");
            return;
        }

        var parentRect = parent.rect;
        if (parentRect.width <= 0f || parentRect.height <= 0f) return;

        var sRect = Image.sprite.rect;
        if (sRect.width <= 0f || sRect.height <= 0f) return;

        // 가로 비율 캡: 부모가 coverMaxAspect(가로:세로)보다 가로로 넓으면 effective width를 캡 값까지 줄여 그 안에서 채움
        float targetW = parentRect.width;
        float targetH = parentRect.height;
        if (coverMaxAspect.x > 0f && coverMaxAspect.y > 0f)
        {
            float cap = coverMaxAspect.x / coverMaxAspect.y;
            if (targetW / targetH > cap) targetW = targetH * cap;
        }

        float spriteAspect = sRect.width / sRect.height;
        float targetAspect = targetW / targetH;

        Vector2 size = spriteAspect > targetAspect
            ? new Vector2(targetH * spriteAspect, targetH) // 가로 넘침
            : new Vector2(targetW, targetW / spriteAspect); // 세로 넘침

        // 앵커를 부모 중앙에 고정하고 pivot점을 부모 영역 내 coverPivot 비율 위치에 배치
        var rt = RectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = coverPivot;
        rt.anchoredPosition = new Vector2((coverPivot.x - 0.5f) * parentRect.width, (coverPivot.y - 0.5f) * parentRect.height);
        rt.sizeDelta = size;
    }

    #endregion

    #region Editor Resizer

#if UNITY_EDITOR
    [Serializable]
    public class Editor_Resizer
    {
        public enum BaseAxis
        {
            Horizontal,
            Vertical
        }

        public BaseAxis baseAxis = BaseAxis.Horizontal; // 리사이즈 기준 축
        public float targetSize = 100f; // 기준 축의 목표 크기
    }

    [SerializeField] private Editor_Resizer editor_Resizer = new(); // 에디터 전용 리사이저 설정
#endif

    /// <summary>
    /// 에디터 전용. 현재 sprite 원본 비율을 유지하며 editor_Resizer 설정에 맞춰 RectTransform.sizeDelta를 재정렬합니다.
    /// baseAxis가 Horizontal이면 가로 = targetSize, 세로 = 비율 보정. Vertical이면 반대.
    /// UNITY_EDITOR 심볼이 없으면 본체 자체가 빌드에 포함되지 않습니다.
    /// </summary>
    public void ApplyResize()
    {
#if UNITY_EDITOR
        if (editor_Resizer == null)
        {
            UnityEngine.Debug.LogError("[UI_Image] ApplyResize: editor_Resizer is null");
            return;
        }

        if (Image == null || Image.sprite == null)
        {
            UnityEngine.Debug.LogError("[UI_Image] ApplyResize: Image or sprite is null");
            return;
        }

        var sRect = Image.sprite.rect;
        if (sRect.width <= 0f || sRect.height <= 0f)
        {
            UnityEngine.Debug.LogError("[UI_Image] ApplyResize: invalid sprite size");
            return;
        }

        float aspectRatio = sRect.width / sRect.height;
        var rt = (RectTransform)transform;
        var newSize = rt.sizeDelta;

        if (editor_Resizer.baseAxis == Editor_Resizer.BaseAxis.Horizontal)
        {
            newSize.x = editor_Resizer.targetSize;
            newSize.y = editor_Resizer.targetSize / aspectRatio;
        }
        else
        {
            newSize.y = editor_Resizer.targetSize;
            newSize.x = editor_Resizer.targetSize * aspectRatio;
        }

        UnityEditor.Undo.RecordObject(rt, "UI_Image ApplyResize");
        rt.sizeDelta = newSize;
        UnityEditor.EditorUtility.SetDirty(rt);
#endif
    }

    #endregion
}
