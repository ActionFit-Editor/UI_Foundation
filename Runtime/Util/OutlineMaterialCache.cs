using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 텍스트 외곽선/그림자 머티리얼 관리.
/// - Get/GetDarkened: (base, color)/(base, factor) 조합 캐싱(영구, Clear까지). ← UI_Button.DisableTextColor용 (기존 동작 보존)
/// - Acquire/Release: ShadowOutline 셰이더 기반 Config 풀(refcount + 재활용, per-release destroy 안 함). ← UI_Text Face/Outline/Underlay용
///   같은 Config는 공유(배칭), count 0이면 free 풀로 보관 후 새 Config에 재구성해 재사용 → 살아있는 머티리얼 수가 peak에서 plateau.
/// 생성된 복제 인스턴스는 Clear() 전까지 누적되므로 씬 전환 등 적절한 시점에 해제하세요.
/// </summary>
public static class OutlineMaterialCache
{
    #region Fields

    private static readonly Dictionary<(Material, Color), Material> _cache = new();
    private static readonly Dictionary<(Material, float), Material> _darkenCache = new(); // (base, factor) → 색 일괄 어둡게 인스턴스
    private static readonly List<Material> _createdInstances = new(); // Clear 시 파괴 대상 (base Material은 제외)

    private static readonly int _faceColorId = Shader.PropertyToID("_FaceColor");
    private static readonly int _faceDilateId = Shader.PropertyToID("_FaceDilate");
    private static readonly int _outlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int _outlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int _underlayColorId = Shader.PropertyToID("_UnderlayColor");
    private static readonly int _underlayOffsetXId = Shader.PropertyToID("_UnderlayOffsetX");
    private static readonly int _underlayOffsetYId = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int _underlayDilateId = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int _underlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");
    private static readonly int _glowColorId = Shader.PropertyToID("_GlowColor");

    private const string ShadowOutlineShaderName = "TextMeshPro/Mobile/Distance Field Shadow Outline";
    private const string OutlineKeyword = "OUTLINE_ON";
    private const string UnderlayKeyword = "UNDERLAY_ON";
    private static Shader _shadowOutlineShader;

    // ── Config 풀 (refcount + 재활용, per-release destroy 안 함) — UI_Text 용 ──
    private sealed class PoolEntry { public Material mat; public int count; }
    private static readonly Dictionary<Config, PoolEntry> _poolActive = new(); // 동일 Config 공유 + refcount
    private static readonly Dictionary<Material, Stack<Material>> _poolFree = new(); // base별 유휴 인스턴스 (재활용 대기)

    #endregion

    #region Config

    /// <summary>
    /// ShadowOutline 머티리얼을 결정하는 설정값. 풀 키로 쓰이므로 "해석된 최종 값"을 담아야 한다
    /// (Face OFF=흰색+0, Outline/Underlay OFF=off 고정). 같은 Config면 머티리얼을 공유한다.
    /// </summary>
    public readonly struct Config : System.IEquatable<Config>
    {
        public readonly Material BaseMat;
        public readonly Color FaceColor;
        public readonly float FaceDilate;
        public readonly bool OutlineOn;
        public readonly Color OutlineColor;
        public readonly float OutlineWidth;
        public readonly bool UnderlayOn;
        public readonly Color UnderlayColor;
        public readonly Vector2 UnderlayOffset;
        public readonly float UnderlayDilate;
        public readonly float UnderlaySoftness;

        public Config(Material baseMat, Color faceColor, float faceDilate,
            bool outlineOn, Color outlineColor, float outlineWidth,
            bool underlayOn, Color underlayColor, Vector2 underlayOffset,
            float underlayDilate, float underlaySoftness)
        {
            BaseMat = baseMat;
            FaceColor = faceColor; FaceDilate = faceDilate;
            OutlineOn = outlineOn; OutlineColor = outlineColor; OutlineWidth = outlineWidth;
            UnderlayOn = underlayOn; UnderlayColor = underlayColor; UnderlayOffset = underlayOffset;
            UnderlayDilate = underlayDilate; UnderlaySoftness = underlaySoftness;
        }

        public bool Equals(Config o) =>
            ReferenceEquals(BaseMat, o.BaseMat) &&
            FaceColor.Equals(o.FaceColor) && FaceDilate.Equals(o.FaceDilate) &&
            OutlineOn == o.OutlineOn && OutlineColor.Equals(o.OutlineColor) && OutlineWidth.Equals(o.OutlineWidth) &&
            UnderlayOn == o.UnderlayOn && UnderlayColor.Equals(o.UnderlayColor) && UnderlayOffset.Equals(o.UnderlayOffset) &&
            UnderlayDilate.Equals(o.UnderlayDilate) && UnderlaySoftness.Equals(o.UnderlaySoftness);

        public override bool Equals(object obj) => obj is Config c && Equals(c);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = BaseMat != null ? BaseMat.GetHashCode() : 0;
                h = h * 397 ^ FaceColor.GetHashCode();
                h = h * 397 ^ FaceDilate.GetHashCode();
                h = h * 397 ^ OutlineOn.GetHashCode();
                h = h * 397 ^ OutlineColor.GetHashCode();
                h = h * 397 ^ OutlineWidth.GetHashCode();
                h = h * 397 ^ UnderlayOn.GetHashCode();
                h = h * 397 ^ UnderlayColor.GetHashCode();
                h = h * 397 ^ UnderlayOffset.GetHashCode();
                h = h * 397 ^ UnderlayDilate.GetHashCode();
                h = h * 397 ^ UnderlaySoftness.GetHashCode();
                return h;
            }
        }
    }

    #endregion

    #region Config Pool (UI_Text)

    private static Shader ShadowOutlineShader
    {
        get
        {
            if (_shadowOutlineShader == null) _shadowOutlineShader = Shader.Find(ShadowOutlineShaderName);
            return _shadowOutlineShader;
        }
    }

    /// <summary>
    /// Config에 해당하는 ShadowOutline 머티리얼을 빌려옵니다(refcount +1). 같은 Config가 쓰이는 중이면 공유,
    /// 없으면 free 풀의 유휴 인스턴스를 재구성해 재사용, 그것도 없으면 base를 복사해 셰이더를 교체합니다.
    /// 사용 후 같은 Config로 <see cref="Release"/> 필수.
    /// </summary>
    public static Material Acquire(in Config cfg)
    {
        if (cfg.BaseMat == null) { UnityEngine.Debug.LogError("[OutlineMaterialCache] Acquire BaseMat is null"); return null; }

        if (_poolActive.TryGetValue(cfg, out var entry) && entry.mat != null)
        {
            entry.count++; // 같은 Config 공유 — 새로 안 만듦
            return entry.mat;
        }

        Material mat = null;
        if (_poolFree.TryGetValue(cfg.BaseMat, out var stack))
        {
            while (stack.Count > 0)
            {
                var m = stack.Pop();
                if (m != null) { mat = m; break; } // 파괴 잔재 건너뜀
            }
        }
        if (mat == null)
        {
            mat = new Material(cfg.BaseMat) { hideFlags = HideFlags.DontSave }; // 재활용할 게 없을 때만 생성
            _createdInstances.Add(mat); // Clear() 시 파괴 대상
        }

        Configure(mat, cfg);
        _poolActive[cfg] = new PoolEntry { mat = mat, count = 1 };
        return mat;
    }

    /// <summary>Acquire한 Config 반납(refcount -1). 0이면 파괴 없이 free 풀로 보관(재활용 대기) → peak 이상으로 안 쌓임.</summary>
    public static void Release(in Config cfg)
    {
        if (cfg.BaseMat == null) return;
        if (!_poolActive.TryGetValue(cfg, out var entry)) return;

        entry.count--;
        if (entry.count > 0) return; // 다른 텍스트가 아직 사용 중

        _poolActive.Remove(cfg);
        if (entry.mat == null) return;

        if (!_poolFree.TryGetValue(cfg.BaseMat, out var stack))
        {
            stack = new Stack<Material>();
            _poolFree[cfg.BaseMat] = stack;
        }
        stack.Push(entry.mat); // 유휴 보관 (destroy 안 함)
    }

    /// <summary>
    /// 머티리얼을 ShadowOutline 셰이더로 교체하고 Config대로 키워드/색/값을 세팅합니다.
    /// (런타임 풀 + 에디터 프리뷰 공용. 셰이더를 못 찾으면 셰이더 교체만 건너뜀)
    /// </summary>
    public static void Configure(Material mat, in Config cfg)
    {
        if (mat == null) return;

        var shader = ShadowOutlineShader;
        if (shader != null && mat.shader != shader) mat.shader = shader;
        else if (shader == null) UnityEngine.Debug.LogWarning($"[OutlineMaterialCache] shader '{ShadowOutlineShaderName}' not found (Always Included Shaders 확인)");

        // Face (항상 적용 — OFF면 호출 측에서 흰색+0을 넘김)
        mat.SetColor(_faceColorId, cfg.FaceColor);
        mat.SetFloat(_faceDilateId, cfg.FaceDilate);

        // Outline
        if (cfg.OutlineOn)
        {
            mat.EnableKeyword(OutlineKeyword);
            mat.SetColor(_outlineColorId, cfg.OutlineColor);
            mat.SetFloat(_outlineWidthId, cfg.OutlineWidth);
        }
        else mat.DisableKeyword(OutlineKeyword);

        // Underlay (그림자)
        if (cfg.UnderlayOn)
        {
            mat.EnableKeyword(UnderlayKeyword);
            mat.SetColor(_underlayColorId, cfg.UnderlayColor);
            mat.SetFloat(_underlayOffsetXId, cfg.UnderlayOffset.x);
            mat.SetFloat(_underlayOffsetYId, cfg.UnderlayOffset.y);
            mat.SetFloat(_underlayDilateId, cfg.UnderlayDilate);
            mat.SetFloat(_underlaySoftnessId, cfg.UnderlaySoftness);
        }
        else mat.DisableKeyword(UnderlayKeyword);
    }

    #endregion

    #region Legacy (UI_Button DisableTextColor)

    /// <summary>
    /// base Material을 복사하고 _OutlineColor만 교체한 인스턴스를 반환합니다.
    /// 동일 (baseMat, outlineColor) 조합으로 이미 생성된 인스턴스가 있으면 캐시된 것을 재사용합니다.
    /// 또한 base Material 자신의 원래 outlineColor와 일치하는 요청은 base Material을 그대로 반환합니다.
    /// </summary>
    public static Material Get(Material baseMat, Color outlineColor)
    {
        if (baseMat == null)
        {
            UnityEngine.Debug.LogError("[OutlineMaterialCache] baseMat is null");
            return null;
        }

        var baseColor = baseMat.GetColor(_outlineColorId);
        var baseKey = (baseMat, baseColor);
        if (!_cache.ContainsKey(baseKey))
            _cache[baseKey] = baseMat;

        var key = (baseMat, outlineColor);
        if (_cache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var mat = new Material(baseMat) { hideFlags = HideFlags.DontSave };
        mat.SetColor(_outlineColorId, outlineColor);
        _cache[key] = mat;
        _createdInstances.Add(mat);
        return mat;
    }

    /// <summary>
    /// base Material을 복사하고 본문(face)/외곽선/underlay/glow 색을 factor로 어둡게(각 RGB×factor, 알파 보존) 곱한 인스턴스를 반환합니다.
    /// 동일 (baseMat, factor) 조합은 캐시 재사용. factor>=1이면 base Material을 그대로 반환.
    /// </summary>
    public static Material GetDarkened(Material baseMat, float factor)
    {
        if (baseMat == null)
        {
            UnityEngine.Debug.LogError("[OutlineMaterialCache] baseMat is null");
            return null;
        }
        if (factor >= 1f) return baseMat; // 어둡게 안 함

        var key = (baseMat, factor);
        if (_darkenCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var mat = new Material(baseMat) { hideFlags = HideFlags.DontSave };
        MultiplyColor(mat, _faceColorId, factor);     // 본문(face) — 내부 색도 함께 어둡게
        MultiplyColor(mat, _outlineColorId, factor);
        MultiplyColor(mat, _underlayColorId, factor);
        MultiplyColor(mat, _glowColorId, factor);
        _darkenCache[key] = mat;
        _createdInstances.Add(mat);
        return mat;
    }

    // 머티리얼의 지정 색 프로퍼티가 있으면 RGB만 factor로 곱함 (알파 보존)
    private static void MultiplyColor(Material mat, int propId, float factor)
    {
        if (!mat.HasProperty(propId)) return;
        var c = mat.GetColor(propId);
        mat.SetColor(propId, new Color(c.r * factor, c.g * factor, c.b * factor, c.a));
    }

    #endregion

    #region Clear

    /// <summary>
    /// 캐시/풀에 생성된 복제 Material 인스턴스만 파괴하고 비웁니다. base Material(프로젝트 에셋)은 제외.
    /// 씬 전환 등 복제 Material 참조가 더 이상 필요 없는 시점에 호출하세요.
    /// </summary>
    public static void Clear()
    {
        foreach (var mat in _createdInstances)
        {
            if (mat != null) Object.Destroy(mat);
        }
        _createdInstances.Clear();
        _cache.Clear();
        _darkenCache.Clear();
        _poolActive.Clear();
        _poolFree.Clear();
    }

    #endregion
}
