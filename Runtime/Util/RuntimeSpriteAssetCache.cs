using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

[Serializable]
internal struct RuntimeSpriteGlyphSettings
{
    [SerializeField] private bool initialized; // Sprite 기준 기본값 초기화 여부.
    [SerializeField] private bool overrideGlyphRect; // 자동 Sprite 영역 대신 수동 영역 사용 여부.
    [SerializeField] private int glyphRectX; // Glyph 영역 X.
    [SerializeField] private int glyphRectY; // Glyph 영역 Y.
    [SerializeField] private int glyphRectWidth; // Glyph 영역 너비.
    [SerializeField] private int glyphRectHeight; // Glyph 영역 높이.
    [SerializeField] private float glyphWidth; // Glyph metric 너비.
    [SerializeField] private float glyphHeight; // Glyph metric 높이.
    [SerializeField] private float bearingX; // Glyph metric bearing X.
    [SerializeField] private float bearingY; // Glyph metric bearing Y.
    [SerializeField] private float advance; // Glyph metric advance.
    [SerializeField] private float scale; // Glyph 배율.

    internal bool Initialized => initialized;
    internal bool OverrideGlyphRect => overrideGlyphRect;
    internal int GlyphRectX => glyphRectX;
    internal int GlyphRectY => glyphRectY;
    internal int GlyphRectWidth => glyphRectWidth;
    internal int GlyphRectHeight => glyphRectHeight;
    internal float GlyphWidth => glyphWidth;
    internal float GlyphHeight => glyphHeight;
    internal float BearingX => bearingX;
    internal float BearingY => bearingY;
    internal float Advance => advance;
    internal float Scale => scale;

    internal static RuntimeSpriteGlyphSettings CreateDefault(Sprite sprite)
    {
        var settings = new RuntimeSpriteGlyphSettings { scale = 1f };
        if (sprite == null) return settings;

        Rect spriteRect = sprite.rect;
        Rect glyphRect = spriteRect;
        if (TryGetAutomaticGlyphRect(sprite, out Rect automaticRect, out _))
            glyphRect = automaticRect;

        settings.initialized = true;
        settings.glyphRectX = Mathf.RoundToInt(glyphRect.x);
        settings.glyphRectY = Mathf.RoundToInt(glyphRect.y);
        settings.glyphRectWidth = Mathf.RoundToInt(glyphRect.width);
        settings.glyphRectHeight = Mathf.RoundToInt(glyphRect.height);
        settings.glyphWidth = spriteRect.width;
        settings.glyphHeight = spriteRect.height;
        settings.bearingX = -sprite.pivot.x;
        settings.bearingY = spriteRect.height - sprite.pivot.y;
        settings.advance = spriteRect.width;
        return settings;
    }

    internal bool TryResolve(Sprite sprite, out RuntimeSpriteGlyphConfig config, out string error)
    {
        config = default;
        error = string.Empty;
        if (sprite == null)
        {
            error = "Sprite is missing";
            return false;
        }

        RuntimeSpriteGlyphSettings resolved = initialized ? this : CreateDefault(sprite);
        if (!TryGetAutomaticGlyphRect(sprite, out Rect automaticRect, out error)) return false;

        var glyphRect = resolved.overrideGlyphRect
            ? new GlyphRect(resolved.glyphRectX, resolved.glyphRectY, resolved.glyphRectWidth, resolved.glyphRectHeight)
            : new GlyphRect(automaticRect);

        Texture2D texture = sprite.texture;
        if (glyphRect.x < 0 || glyphRect.y < 0 || glyphRect.width <= 0 || glyphRect.height <= 0 ||
            glyphRect.x + glyphRect.width > texture.width || glyphRect.y + glyphRect.height > texture.height)
        {
            error = $"Glyph Rect is outside the Sprite texture: rect={glyphRect}, texture={texture.width}x{texture.height}";
            return false;
        }

        if (resolved.glyphWidth <= 0f || resolved.glyphHeight <= 0f || resolved.advance < 0f || resolved.scale <= 0f)
        {
            error = "Glyph W, H, and Scale must be greater than zero, and AD must not be negative";
            return false;
        }

        var metrics = new GlyphMetrics(
            resolved.glyphWidth,
            resolved.glyphHeight,
            resolved.bearingX,
            resolved.bearingY,
            resolved.advance);
        config = new RuntimeSpriteGlyphConfig(glyphRect, metrics, resolved.scale);
        return true;
    }

    private static bool TryGetAutomaticGlyphRect(Sprite sprite, out Rect rect, out string error)
    {
        rect = default;
        error = string.Empty;
        if (sprite.texture == null)
        {
            error = "Sprite texture is missing";
            return false;
        }

        if (sprite.packed && sprite.packingRotation != SpritePackingRotation.None)
        {
            error = $"Rotated packed Sprites are not supported: {sprite.name}";
            return false;
        }

        try
        {
            rect = sprite.textureRect;
        }
        catch (UnityException)
        {
            error = $"Tightly packed Sprites are not supported: {sprite.name}";
            return false;
        }

        if (rect.width <= 0f || rect.height <= 0f)
        {
            error = $"Sprite rect is empty: {sprite.name}";
            return false;
        }

        return true;
    }
}

internal readonly struct RuntimeSpriteGlyphConfig : IEquatable<RuntimeSpriteGlyphConfig>
{
    internal readonly GlyphRect GlyphRect;
    internal readonly GlyphMetrics Metrics;
    internal readonly float Scale;

    internal RuntimeSpriteGlyphConfig(GlyphRect glyphRect, GlyphMetrics metrics, float scale)
    {
        GlyphRect = glyphRect;
        Metrics = metrics;
        Scale = scale;
    }

    public bool Equals(RuntimeSpriteGlyphConfig other)
    {
        return GlyphRect.x == other.GlyphRect.x &&
               GlyphRect.y == other.GlyphRect.y &&
               GlyphRect.width == other.GlyphRect.width &&
               GlyphRect.height == other.GlyphRect.height &&
               Metrics.width.Equals(other.Metrics.width) &&
               Metrics.height.Equals(other.Metrics.height) &&
               Metrics.horizontalBearingX.Equals(other.Metrics.horizontalBearingX) &&
               Metrics.horizontalBearingY.Equals(other.Metrics.horizontalBearingY) &&
               Metrics.horizontalAdvance.Equals(other.Metrics.horizontalAdvance) &&
               Scale.Equals(other.Scale);
    }

    public override bool Equals(object obj) => obj is RuntimeSpriteGlyphConfig other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = GlyphRect.x;
            hash = (hash * 397) ^ GlyphRect.y;
            hash = (hash * 397) ^ GlyphRect.width;
            hash = (hash * 397) ^ GlyphRect.height;
            hash = (hash * 397) ^ Metrics.width.GetHashCode();
            hash = (hash * 397) ^ Metrics.height.GetHashCode();
            hash = (hash * 397) ^ Metrics.horizontalBearingX.GetHashCode();
            hash = (hash * 397) ^ Metrics.horizontalBearingY.GetHashCode();
            hash = (hash * 397) ^ Metrics.horizontalAdvance.GetHashCode();
            return (hash * 397) ^ Scale.GetHashCode();
        }
    }
}

internal static class RuntimeSpriteAssetCache
{
    private const string SpriteShaderName = "TextMeshPro/Sprite";

    private sealed class Entry
    {
        internal TMP_SpriteAsset Asset;
        internal int ReferenceCount;
    }

    internal readonly struct Config : IEquatable<Config>
    {
        internal readonly Sprite Sprite;
        internal readonly RuntimeSpriteGlyphConfig Glyph;
        internal readonly Material MaterialTemplate;
        private readonly int _spriteInstanceId;
        private readonly int _materialTemplateInstanceId;

        private Config(Sprite sprite, RuntimeSpriteGlyphConfig glyph, Material materialTemplate)
        {
            Sprite = sprite;
            Glyph = glyph;
            MaterialTemplate = materialTemplate;
            _spriteInstanceId = sprite.GetInstanceID();
            _materialTemplateInstanceId = materialTemplate != null ? materialTemplate.GetInstanceID() : 0;
        }

        internal static bool TryCreate(
            Sprite sprite,
            RuntimeSpriteGlyphSettings settings,
            Material preferredMaterial,
            out Config config,
            out string error)
        {
            config = default;
            if (!settings.TryResolve(sprite, out RuntimeSpriteGlyphConfig glyph, out error)) return false;
            config = new Config(sprite, glyph, ResolveMaterialTemplate(preferredMaterial));
            return true;
        }

        public bool Equals(Config other)
        {
            return _spriteInstanceId == other._spriteInstanceId &&
                   _materialTemplateInstanceId == other._materialTemplateInstanceId &&
                   Glyph.Equals(other.Glyph);
        }

        public override bool Equals(object obj) => obj is Config other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _spriteInstanceId;
                hash = (hash * 397) ^ _materialTemplateInstanceId;
                return (hash * 397) ^ Glyph.GetHashCode();
            }
        }
    }

    private static readonly Dictionary<Config, Entry> Entries = new();

    internal static int CachedAssetCount => Entries.Count;

    internal static TMP_SpriteAsset Acquire(in Config config)
    {
        if (Entries.TryGetValue(config, out Entry cached))
        {
            cached.ReferenceCount++;
            return cached.Asset;
        }

        TMP_SpriteAsset asset = CreateAsset(config, HideFlags.HideAndDontSave);
        if (asset == null) return null;

        Entries.Add(config, new Entry { Asset = asset, ReferenceCount = 1 });
        return asset;
    }

    internal static void Release(in Config config)
    {
        if (!Entries.TryGetValue(config, out Entry entry)) return;
        entry.ReferenceCount--;
        if (entry.ReferenceCount > 0) return;

        Entries.Remove(config);
        DestroyAsset(entry.Asset);
    }

    internal static TMP_SpriteAsset CreatePreview(in Config config) =>
        CreateAsset(config, HideFlags.HideAndDontSave);

    internal static void DestroyPreview(TMP_SpriteAsset asset) => DestroyAsset(asset);

    internal static int GetReferenceCount(in Config config) =>
        Entries.TryGetValue(config, out Entry entry) ? entry.ReferenceCount : 0;

    internal static void ClearForTests() => Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeCache() => Clear();

    private static TMP_SpriteAsset CreateAsset(in Config config, HideFlags hideFlags)
    {
        Material material = CreateMaterial(config, hideFlags);
        if (material == null) return null;

        var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.hideFlags = hideFlags;
        asset.name = $"{config.Sprite.name} (Runtime Sprite Asset)";
        asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
        asset.spriteSheet = config.Sprite.texture;

        var glyph = new TMP_SpriteGlyph(
            0,
            config.Glyph.Metrics,
            config.Glyph.GlyphRect,
            config.Glyph.Scale,
            0,
            config.Sprite);
        asset.spriteGlyphTable.Add(glyph);

        // TMP 1.1 lookup을 먼저 구성해 legacy upgrade나 AssetDatabase 저장 경로가 실행되지 않게 합니다.
        asset.UpdateLookupTables();
        var character = new TMP_SpriteCharacter(0xFFFE, asset, glyph)
        {
            name = string.IsNullOrEmpty(config.Sprite.name) ? "sprite" : config.Sprite.name,
            scale = 1f
        };
        asset.spriteCharacterTable.Add(character);
        asset.UpdateLookupTables();
        asset.material = material;
        return asset;
    }

    private static Material CreateMaterial(in Config config, HideFlags hideFlags)
    {
        Material material;
        if (config.MaterialTemplate != null)
        {
            material = new Material(config.MaterialTemplate);
        }
        else
        {
            Shader shader = Shader.Find(SpriteShaderName);
            if (shader == null)
            {
                UnityEngine.Debug.LogError($"[RuntimeSpriteAssetCache] Sprite shader is missing: {SpriteShaderName}");
                return null;
            }
            material = new Material(shader);
        }

        material.hideFlags = hideFlags;
        material.name = $"{config.Sprite.name} (Runtime Sprite Material)";
        ShaderUtilities.GetShaderPropertyIDs();
        material.SetTexture(ShaderUtilities.ID_MainTex, config.Sprite.texture);
        return material;
    }

    private static Material ResolveMaterialTemplate(Material preferredMaterial)
    {
        TMP_SpriteAsset defaultSpriteAsset = TMP_Settings.GetSpriteAsset();
        Material defaultMaterial = defaultSpriteAsset != null ? defaultSpriteAsset.material : null;
        if (IsValidTemplate(defaultMaterial)) return defaultMaterial;
        return IsValidTemplate(preferredMaterial) ? preferredMaterial : null;
    }

    private static bool IsValidTemplate(Material material)
    {
        if (material == null || material.shader == null) return false;
        ShaderUtilities.GetShaderPropertyIDs();
        return material.HasProperty(ShaderUtilities.ID_MainTex);
    }

    private static void Clear()
    {
        foreach (Entry entry in Entries.Values) DestroyAsset(entry.Asset);
        Entries.Clear();
    }

    private static void DestroyAsset(TMP_SpriteAsset asset)
    {
        if (asset == null) return;
        Material material = asset.material;
        asset.material = null;
        DestroyObject(material);
        DestroyObject(asset);
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(target);
        else UnityEngine.Object.DestroyImmediate(target);
    }
}
