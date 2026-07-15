using TMPro;
using UnityEngine;

/// <summary>UI_Text — 기존 TMP Sprite Asset과 Sprite 기반 런타임 인라인 이미지를 지원합니다.</summary>
public partial class UI_Text
{
    #region Fields

    [SerializeField] private bool isSpriteAsset = false; // Sprite 기반 임시 TMP Sprite Asset 사용 여부.
    [SerializeField] private Sprite sprite; // 임시 TMP Sprite Asset의 원본 Sprite.
    [SerializeField] private RuntimeSpriteGlyphSettings spriteGlyphSettings = default; // Glyph 영역과 metric 설정.

    private TMP_SpriteAsset _originalSpriteAsset; // 런타임 생성 asset 적용 전 원본 참조.
    private TMP_SpriteAsset _runtimeSpriteAsset; // 현재 cache에서 획득한 런타임 asset.
    private RuntimeSpriteAssetCache.Config _runtimeSpriteConfig; // 현재 cache 획득 키.
    private bool _hasRuntimeSpriteAsset; // cache 획득 여부.

#if UNITY_EDITOR
    internal static event System.Action<UI_Text> SpriteEditorPreviewChanged;

    private TMP_SpriteAsset _originalSpriteEditorPreviewAsset; // Editor preview 적용 전 원본 참조.
    private TMP_SpriteAsset _spriteEditorPreviewAsset; // Editor 전용 임시 Sprite Asset.
    private RuntimeSpriteAssetCache.Config _spriteEditorPreviewConfig; // Editor preview 설정.
#endif

    #endregion

    #region Properties

    public TMP_SpriteAsset SpriteAsset
    {
        get
        {
            EnsureInit();
            return _txt.spriteAsset;
        }
        set
        {
            EnsureInit();
#if UNITY_EDITOR
            if (!Application.isPlaying) RestoreSpriteEditorPreview();
#endif
            ReleaseRuntimeSpriteAsset();
            _txt.spriteAsset = value;
        }
    }

    public bool IsSpriteAsset => isSpriteAsset;
    public Sprite RuntimeSprite => sprite;

    #endregion

    #region Public Methods

    /// <summary>TMP가 인라인 sprite 태그를 조회할 Sprite Asset을 설정합니다.</summary>
    public UI_Text SetSpriteAsset(TMP_SpriteAsset spriteAsset)
    {
        SpriteAsset = spriteAsset;
        return this;
    }

    /// <summary>직렬화된 Sprite와 glyph 설정으로 임시 Sprite Asset을 다시 적용합니다.</summary>
    public UI_Text ApplyRuntimeSpriteAsset()
    {
        EnsureInit();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            TryApplySpriteEditorPreview();
            return this;
        }
#endif
        AcquireRuntimeSpriteAsset();
        return this;
    }

    /// <summary>앞뒤 텍스트 사이에 이름 기반 sprite 태그를 넣고 Rich Text를 활성화합니다.</summary>
    public UI_Text SetTextWithSprite(string prefix, string spriteName, string suffix = "", bool tint = false)
    {
        string spriteTag = BuildSpriteTag(spriteName, tint);
        if (string.IsNullOrEmpty(spriteTag)) return this;

        EnsureInit();
        _txt.richText = true;
        Text = string.Concat(prefix, spriteTag, suffix);
        return this;
    }

    /// <summary>현재 TMP Sprite Asset에서 이름으로 조회할 인라인 sprite 태그를 만듭니다.</summary>
    public static string BuildSpriteTag(string spriteName, bool tint = false)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            UnityEngine.Debug.LogError("[UI_Text] Sprite name is empty");
            return string.Empty;
        }

        return tint
            ? $"<sprite name=\"{spriteName}\" tint=1>"
            : $"<sprite name=\"{spriteName}\">";
    }

    /// <summary>현재 TMP Sprite Asset에서 인덱스로 조회할 인라인 sprite 태그를 만듭니다.</summary>
    public static string BuildSpriteTag(int spriteIndex, bool tint = false)
    {
        if (spriteIndex < 0)
        {
            UnityEngine.Debug.LogError($"[UI_Text] Sprite index is invalid: {spriteIndex}");
            return string.Empty;
        }

        return tint
            ? $"<sprite={spriteIndex} tint=1>"
            : $"<sprite={spriteIndex}>";
    }

    #endregion

    #region Runtime Sprite Asset

    internal void ResetRuntimeSpriteGlyphSettings() =>
        spriteGlyphSettings = RuntimeSpriteGlyphSettings.CreateDefault(sprite);

    internal void AcquireRuntimeSpriteAsset()
    {
        if (_txt == null) return;
        if (!isSpriteAsset)
        {
            ReleaseRuntimeSpriteAsset();
            return;
        }
        if (sprite == null)
        {
            ReleaseRuntimeSpriteAsset();
            UnityEngine.Debug.LogError($"[UI_Text] Runtime Sprite is missing: object={name}");
            return;
        }

        TMP_SpriteAsset originalAsset = _hasRuntimeSpriteAsset ? _originalSpriteAsset : _txt.spriteAsset;
        Material preferredMaterial = originalAsset != null ? originalAsset.material : null;
        if (!RuntimeSpriteAssetCache.Config.TryCreate(
                sprite,
                spriteGlyphSettings,
                preferredMaterial,
                out RuntimeSpriteAssetCache.Config config,
                out string error))
        {
            ReleaseRuntimeSpriteAsset();
            UnityEngine.Debug.LogError($"[UI_Text] Runtime Sprite configuration is invalid: object={name}, detail={error}");
            return;
        }

        if (_hasRuntimeSpriteAsset && _runtimeSpriteConfig.Equals(config))
        {
            if (_txt.spriteAsset != _runtimeSpriteAsset) _txt.spriteAsset = _runtimeSpriteAsset;
            return;
        }

        ReleaseRuntimeSpriteAsset();
        TMP_SpriteAsset generatedAsset = RuntimeSpriteAssetCache.Acquire(config);
        if (generatedAsset == null)
        {
            UnityEngine.Debug.LogError($"[UI_Text] Failed to create Runtime Sprite Asset: object={name}, sprite={sprite.name}");
            return;
        }

        _originalSpriteAsset = originalAsset;
        _runtimeSpriteConfig = config;
        _runtimeSpriteAsset = generatedAsset;
        _hasRuntimeSpriteAsset = true;
        _txt.spriteAsset = generatedAsset;
    }

    internal void ReleaseRuntimeSpriteAsset()
    {
        if (!_hasRuntimeSpriteAsset) return;
        if (_txt != null && _txt.spriteAsset == _runtimeSpriteAsset)
            _txt.spriteAsset = _originalSpriteAsset;

        RuntimeSpriteAssetCache.Release(_runtimeSpriteConfig);
        _originalSpriteAsset = null;
        _runtimeSpriteAsset = null;
        _runtimeSpriteConfig = default;
        _hasRuntimeSpriteAsset = false;
    }

#if UNITY_EDITOR
    internal bool HasSpriteEditorPreview => _spriteEditorPreviewAsset != null;
    internal TMP_SpriteAsset SpriteEditorPreviewAsset => _spriteEditorPreviewAsset;

    internal bool TryApplySpriteEditorPreview()
    {
        if (Application.isPlaying) return true;
        if (_txt == null && !TryGetComponent(out _txt)) return false;
        if (!isSpriteAsset || sprite == null)
        {
            RestoreSpriteEditorPreview();
            return true;
        }

        TMP_SpriteAsset originalAsset = _txt.spriteAsset == _spriteEditorPreviewAsset
            ? _originalSpriteEditorPreviewAsset
            : _txt.spriteAsset;
        Material preferredMaterial = originalAsset != null ? originalAsset.material : null;
        if (!RuntimeSpriteAssetCache.Config.TryCreate(
                sprite,
                spriteGlyphSettings,
                preferredMaterial,
                out RuntimeSpriteAssetCache.Config config,
                out _))
        {
            RestoreSpriteEditorPreview();
            return true;
        }

        if (_spriteEditorPreviewAsset != null && _spriteEditorPreviewConfig.Equals(config))
        {
            if (_txt.spriteAsset != _spriteEditorPreviewAsset) _txt.spriteAsset = _spriteEditorPreviewAsset;
            return true;
        }

        RestoreSpriteEditorPreview();
        TMP_SpriteAsset previewAsset = RuntimeSpriteAssetCache.CreatePreview(config);
        if (previewAsset == null) return true;

        _originalSpriteEditorPreviewAsset = originalAsset;
        _spriteEditorPreviewConfig = config;
        _spriteEditorPreviewAsset = previewAsset;
        _txt.spriteAsset = previewAsset;
        SpriteEditorPreviewChanged?.Invoke(this);
        return true;
    }

    internal void RestoreSpriteEditorPreview()
    {
        if (_spriteEditorPreviewAsset == null)
        {
            _originalSpriteEditorPreviewAsset = null;
            return;
        }

        TMP_SpriteAsset previewAsset = _spriteEditorPreviewAsset;
        if (_txt != null && _txt.spriteAsset == previewAsset)
            _txt.spriteAsset = _originalSpriteEditorPreviewAsset;

        _originalSpriteEditorPreviewAsset = null;
        _spriteEditorPreviewAsset = null;
        _spriteEditorPreviewConfig = default;
        RuntimeSpriteAssetCache.DestroyPreview(previewAsset);
        SpriteEditorPreviewChanged?.Invoke(this);
    }
#endif

    #endregion
}
