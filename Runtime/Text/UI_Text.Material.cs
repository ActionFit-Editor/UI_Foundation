using UnityEngine;

/// <summary>
/// UI_Text — Face/Outline/Underlay 머티리얼 적용.
/// 셋 중 하나라도 ON이면 폰트 Shared 머티리얼을 복사해 ShadowOutline 셰이더로 교체한 머티리얼을 적용한다.
/// ON 토글=직렬화 값, OFF 토글=기본값 고정(Face=흰색+0, Outline/Underlay=꺼짐).
/// 머티리얼은 OutlineMaterialCache 풀에서 Config(=해석된 값)별로 공유/재활용(런타임 OnEnable Acquire / OnDisable Release).
/// 에디터(비플레이)는 프리뷰용 로컬 인스턴스 사용(풀 미사용).
/// </summary>
public partial class UI_Text
{
    // Face (항상 렌더 — OFF면 흰색+0 기본값으로 고정)
    [SerializeField] private bool isSettingFace = false;
    [SerializeField] private Color faceColor = Color.white;
    [SerializeField] private float faceDilate = 0f;

    // Outline (OFF면 외곽선 없음) — 기존 isOutlineColor에서 이름 변경(켜진 프리팹 없음 확인)
    [SerializeField] private bool isSettingOutline = false;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineWidth = 0.2f;

    // Underlay = 그림자 (OFF면 그림자 없음)
    [SerializeField] private bool isSettingUnderlay = false;
    [SerializeField] private Color underlayColor = Color.black;
    [SerializeField] private float underlayOffsetX = 0f;
    [SerializeField] private float underlayOffsetY = -1f;
    [SerializeField] private float underlayDilate = 0f;
    [SerializeField] private float underlaySoftness = 0f;

    private Material _baseMaterial; // 토글 ON 시점에 캡처한 원본 머티리얼 (복원용 — 런타임 전용, 직렬화 안 함)
    private Material _pooledMaterial; // OutlineMaterialCache에서 Acquire한 공유 인스턴스 (런타임 전용)
    private OutlineMaterialCache.Config _acquiredConfig; // Acquire에 사용한 Config (동일 키로 Release하기 위해 보관)
    private bool _hasAcquired; // 풀에서 빌린 상태 여부

#if UNITY_EDITOR
    private Material _editPreviewMaterial; // 에디터(비플레이) 프리뷰 전용 로컬 인스턴스 (풀 미사용)
#endif

    private bool AnySetting => isSettingFace || isSettingOutline || isSettingUnderlay;

    // 토글 ON=직렬화 값, OFF=기본값(Face 흰색+0 / Outline·Underlay 꺼짐)으로 해석한 Config 생성
    private OutlineMaterialCache.Config BuildConfig(Material baseMat)
    {
        return new OutlineMaterialCache.Config(
            baseMat,
            isSettingFace ? faceColor : Color.white,
            isSettingFace ? faceDilate : 0f,
            isSettingOutline, isSettingOutline ? outlineColor : Color.black, isSettingOutline ? outlineWidth : 0f,
            isSettingUnderlay, isSettingUnderlay ? underlayColor : Color.black,
            isSettingUnderlay ? new Vector2(underlayOffsetX, underlayOffsetY) : Vector2.zero,
            isSettingUnderlay ? underlayDilate : 0f,
            isSettingUnderlay ? underlaySoftness : 0f);
    }

    /// <summary>
    /// 설정을 현재 모드에 맞게 다시 적용합니다.
    /// 런타임: 풀 Acquire/Release. 에디터(비플레이): 프리뷰. (토글/값을 런타임에 바꾼 뒤 호출하면 갱신)
    /// </summary>
    public void ApplyOutline()
    {
        EnsureInit();
        if (_txt == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { ApplyOutlineEditorPreview(); return; }
#endif
        AcquireOutline();
    }

    /// <summary>현재 아웃라인 색 (외부 authoring 조회용).</summary>
    public Color OutlineColor => outlineColor;

    /// <summary>아웃라인이 켜져 있는지 (외부 authoring 조회용).</summary>
    public bool IsOutlineOn => isSettingOutline;

    /// <summary>아웃라인 색을 지정하고 아웃라인을 켠 뒤 즉시 적용한다. (외부 authoring용)</summary>
    public void SetOutlineColor(Color color)
    {
        isSettingOutline = true;
        outlineColor = color;
        ApplyOutline();
    }

    // ── 런타임: Config를 풀에서 빌려 적용 ──
    private void AcquireOutline()
    {
        if (_txt == null) return;
        if (!AnySetting) { ReleaseOutline(); return; }

        // base 결정: 이미 풀 머티리얼을 쓰는 중이면 저장해둔 원본, 아니면 현재 머티리얼
        Material baseMat = (_hasAcquired && _baseMaterial != null) ? _baseMaterial : _txt.fontSharedMaterial;
#if UNITY_EDITOR
        if (!_hasAcquired && baseMat == _editPreviewMaterial && _baseMaterial != null) baseMat = _baseMaterial;
#endif
        if (baseMat == null) return;

        var cfg = BuildConfig(baseMat);
        if (_hasAcquired && _acquiredConfig.Equals(cfg)) return; // 동일 — no-op

        if (_hasAcquired) ReleaseOutline(); // 변경됨 → 반납(원본 복원, _baseMaterial은 유지)

        _baseMaterial = baseMat;
        _pooledMaterial = OutlineMaterialCache.Acquire(cfg);
        if (_pooledMaterial == null) return;
        _acquiredConfig = cfg;
        _hasAcquired = true;
        _txt.fontSharedMaterial = _pooledMaterial;
    }

    // ── 런타임: 빌린 머티리얼 반납 + 원본 복원 (_baseMaterial은 다음 Acquire 위해 유지) ──
    private void ReleaseOutline()
    {
        if (!_hasAcquired) return;
        if (_txt != null && _baseMaterial != null && _txt.fontSharedMaterial == _pooledMaterial)
            _txt.fontSharedMaterial = _baseMaterial;
        OutlineMaterialCache.Release(_acquiredConfig);
        _hasAcquired = false;
        _pooledMaterial = null;
    }

#if UNITY_EDITOR
    // ── 에디터(비플레이) 프리뷰: 로컬 인스턴스로 미리보기 (풀 미사용 — 에디터 머티리얼 오염 방지) ──
    private void ApplyOutlineEditorPreview()
    {
        if (!AnySetting)
        {
            if (_editPreviewMaterial != null && _baseMaterial != null && _txt.fontSharedMaterial == _editPreviewMaterial)
                _txt.fontSharedMaterial = _baseMaterial;
            return;
        }

        Material baseMat = (_txt.fontSharedMaterial == _editPreviewMaterial && _baseMaterial != null) ? _baseMaterial : _txt.fontSharedMaterial;
        if (baseMat == null) return;
        _baseMaterial = baseMat;

        if (_editPreviewMaterial == null)
            _editPreviewMaterial = new Material(_baseMaterial)
            {
                hideFlags = HideFlags.DontSave,
                name = $"{_baseMaterial.name} (Outline Preview {name})"
            };

        OutlineMaterialCache.Configure(_editPreviewMaterial, BuildConfig(_baseMaterial));
        _txt.fontSharedMaterial = _editPreviewMaterial;
    }
#endif
}
