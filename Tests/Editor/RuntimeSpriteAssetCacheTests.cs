using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public class RuntimeSpriteAssetCacheTests
    {
        private const string TestShaderPath =
            "Packages/com.actionfit.ui.foundation/Tests/Editor/RuntimeSpriteAssetTest.shader";

        [SetUp]
        public void SetUp() => RuntimeSpriteAssetCache.ClearForTests();

        [TearDown]
        public void TearDown() => RuntimeSpriteAssetCache.ClearForTests();

        [Test]
        public void SpriteDefaultsMapRectPivotAndMetrics()
        {
            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                texture = new Texture2D(32, 24);
                sprite = Sprite.Create(
                    texture,
                    new Rect(4f, 2f, 10f, 8f),
                    new Vector2(0.25f, 0.75f));

                RuntimeSpriteGlyphSettings settings = RuntimeSpriteGlyphSettings.CreateDefault(sprite);
                Assert.That(settings.TryResolve(sprite, out RuntimeSpriteGlyphConfig config, out string error),
                    Is.True,
                    error);
                Assert.That(config.GlyphRect.x, Is.EqualTo(4));
                Assert.That(config.GlyphRect.y, Is.EqualTo(2));
                Assert.That(config.GlyphRect.width, Is.EqualTo(10));
                Assert.That(config.GlyphRect.height, Is.EqualTo(8));
                Assert.That(config.Metrics.width, Is.EqualTo(10f));
                Assert.That(config.Metrics.height, Is.EqualTo(8f));
                Assert.That(config.Metrics.horizontalBearingX, Is.EqualTo(-2.5f));
                Assert.That(config.Metrics.horizontalBearingY, Is.EqualTo(2f));
                Assert.That(config.Metrics.horizontalAdvance, Is.EqualTo(10f));
                Assert.That(config.Scale, Is.EqualTo(1f));
            }
            finally
            {
                if (sprite != null) Object.DestroyImmediate(sprite);
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void MatchingConfigurationsShareAssetUntilLastRelease()
        {
            Texture2D texture = null;
            Sprite sprite = null;
            Material material = null;
            try
            {
                texture = new Texture2D(16, 16);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
                sprite.name = "cache-icon";
                material = CreateSpriteMaterial();
                RuntimeSpriteGlyphSettings settings = RuntimeSpriteGlyphSettings.CreateDefault(sprite);
                Assert.That(RuntimeSpriteAssetCache.Config.TryCreate(
                        sprite,
                        settings,
                        material,
                        out RuntimeSpriteAssetCache.Config config,
                        out string error),
                    Is.True,
                    error);

                TMP_SpriteAsset first = RuntimeSpriteAssetCache.Acquire(config);
                TMP_SpriteAsset second = RuntimeSpriteAssetCache.Acquire(config);

                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.SameAs(first));
                Assert.That(RuntimeSpriteAssetCache.CachedAssetCount, Is.EqualTo(1));
                Assert.That(RuntimeSpriteAssetCache.GetReferenceCount(config), Is.EqualTo(2));
                Assert.That(first.spriteGlyphTable[0].sprite, Is.SameAs(sprite));
                Assert.That(first.spriteCharacterTable[0].name, Is.EqualTo(sprite.name));
                Assert.That(first.GetSpriteIndexFromName(sprite.name), Is.EqualTo(0));
                Assert.That(first.material.mainTexture, Is.SameAs(texture));
                Assert.That((first.hideFlags & HideFlags.DontSave) != 0, Is.True);
                Assert.That((first.material.hideFlags & HideFlags.DontSave) != 0, Is.True);

                RuntimeSpriteAssetCache.Release(config);
                Assert.That(first, Is.Not.Null);
                Assert.That(RuntimeSpriteAssetCache.GetReferenceCount(config), Is.EqualTo(1));

                RuntimeSpriteAssetCache.Release(config);
                Assert.That(first == null, Is.True);
                Assert.That(RuntimeSpriteAssetCache.CachedAssetCount, Is.Zero);
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                if (sprite != null) Object.DestroyImmediate(sprite);
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void TextAcquireSharesGeneratedAssetAndRestoresOriginalAssignments()
        {
            Texture2D texture = null;
            Sprite sprite = null;
            TMP_SpriteAsset originalAsset = null;
            Material material = null;
            GameObject firstObject = null;
            GameObject secondObject = null;
            try
            {
                texture = new Texture2D(12, 10);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 12f, 10f), new Vector2(0.5f, 0.5f));
                sprite.name = "shared-icon";
                originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                material = CreateSpriteMaterial();
                originalAsset.material = material;
                firstObject = CreateTextObject("First", originalAsset, sprite, out UI_Text firstText);
                secondObject = CreateTextObject("Second", originalAsset, sprite, out UI_Text secondText);
                _ = firstText.TMP;
                _ = secondText.TMP;

                firstText.AcquireRuntimeSpriteAsset();
                secondText.AcquireRuntimeSpriteAsset();

                TMP_SpriteAsset generatedAsset = firstText.TMP.spriteAsset;
                Assert.That(generatedAsset, Is.Not.Null);
                Assert.That(generatedAsset, Is.Not.SameAs(originalAsset));
                Assert.That(secondText.TMP.spriteAsset, Is.SameAs(generatedAsset));
                Assert.That(RuntimeSpriteAssetCache.CachedAssetCount, Is.EqualTo(1));

                firstText.ReleaseRuntimeSpriteAsset();
                Assert.That(firstText.TMP.spriteAsset, Is.SameAs(originalAsset));
                Assert.That(secondText.TMP.spriteAsset, Is.SameAs(generatedAsset));

                secondText.ReleaseRuntimeSpriteAsset();
                Assert.That(secondText.TMP.spriteAsset, Is.SameAs(originalAsset));
                Assert.That(generatedAsset == null, Is.True);
            }
            finally
            {
                if (firstObject != null) Object.DestroyImmediate(firstObject);
                if (secondObject != null) Object.DestroyImmediate(secondObject);
                if (originalAsset != null) Object.DestroyImmediate(originalAsset);
                if (material != null) Object.DestroyImmediate(material);
                if (sprite != null) Object.DestroyImmediate(sprite);
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ChangedSerializedConfigurationReplacesCachedAssetAndRestoresOriginal()
        {
            Texture2D texture = null;
            Sprite sprite = null;
            TMP_SpriteAsset originalAsset = null;
            Material material = null;
            GameObject gameObject = null;
            UI_Text text = null;
            try
            {
                texture = new Texture2D(16, 16);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
                sprite.name = "changing-icon";
                originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                material = CreateSpriteMaterial();
                originalAsset.material = material;
                gameObject = CreateTextObject("Changing", originalAsset, sprite, out text);
                _ = text.TMP;

                text.AcquireRuntimeSpriteAsset();
                TMP_SpriteAsset firstAsset = text.TMP.spriteAsset;
                Assert.That(firstAsset.spriteGlyphTable[0].scale, Is.EqualTo(1f));

                var serialized = new SerializedObject(text);
                SerializedProperty settings = serialized.FindProperty("spriteGlyphSettings");
                settings.FindPropertyRelative("initialized").boolValue = true;
                settings.FindPropertyRelative("scale").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                text.AcquireRuntimeSpriteAsset();

                TMP_SpriteAsset secondAsset = text.TMP.spriteAsset;
                Assert.That(secondAsset, Is.Not.SameAs(firstAsset));
                Assert.That(firstAsset == null, Is.True);
                Assert.That(secondAsset.spriteGlyphTable[0].scale, Is.EqualTo(2f));
                Assert.That(RuntimeSpriteAssetCache.CachedAssetCount, Is.EqualTo(1));

                text.ReleaseRuntimeSpriteAsset();
                Assert.That(text.TMP.spriteAsset, Is.SameAs(originalAsset));
                Assert.That(secondAsset == null, Is.True);
                Assert.That(RuntimeSpriteAssetCache.CachedAssetCount, Is.Zero);
            }
            finally
            {
                if (text != null) text.ReleaseRuntimeSpriteAsset();
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                if (originalAsset != null) Object.DestroyImmediate(originalAsset);
                if (material != null) Object.DestroyImmediate(material);
                if (sprite != null) Object.DestroyImmediate(sprite);
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void SerializedOverridesMapToGeneratedGlyph()
        {
            Texture2D texture = null;
            Sprite sprite = null;
            TMP_SpriteAsset originalAsset = null;
            Material material = null;
            GameObject gameObject = null;
            UI_Text text = null;
            try
            {
                texture = new Texture2D(32, 32);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f));
                sprite.name = "configured-icon";
                originalAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                material = CreateSpriteMaterial();
                originalAsset.material = material;
                gameObject = CreateTextObject("Configured", originalAsset, sprite, out text);

                var serialized = new SerializedObject(text);
                SerializedProperty settings = serialized.FindProperty("spriteGlyphSettings");
                settings.FindPropertyRelative("initialized").boolValue = true;
                settings.FindPropertyRelative("overrideGlyphRect").boolValue = true;
                settings.FindPropertyRelative("glyphRectX").intValue = 2;
                settings.FindPropertyRelative("glyphRectY").intValue = 3;
                settings.FindPropertyRelative("glyphRectWidth").intValue = 11;
                settings.FindPropertyRelative("glyphRectHeight").intValue = 12;
                settings.FindPropertyRelative("glyphWidth").floatValue = 13f;
                settings.FindPropertyRelative("glyphHeight").floatValue = 14f;
                settings.FindPropertyRelative("bearingX").floatValue = -5f;
                settings.FindPropertyRelative("bearingY").floatValue = 6f;
                settings.FindPropertyRelative("advance").floatValue = 15f;
                settings.FindPropertyRelative("scale").floatValue = 1.25f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                _ = text.TMP;
                text.AcquireRuntimeSpriteAsset();

                TMP_SpriteGlyph glyph = text.TMP.spriteAsset.spriteGlyphTable[0];
                Assert.That(glyph.glyphRect.x, Is.EqualTo(2));
                Assert.That(glyph.glyphRect.y, Is.EqualTo(3));
                Assert.That(glyph.glyphRect.width, Is.EqualTo(11));
                Assert.That(glyph.glyphRect.height, Is.EqualTo(12));
                Assert.That(glyph.metrics.width, Is.EqualTo(13f));
                Assert.That(glyph.metrics.height, Is.EqualTo(14f));
                Assert.That(glyph.metrics.horizontalBearingX, Is.EqualTo(-5f));
                Assert.That(glyph.metrics.horizontalBearingY, Is.EqualTo(6f));
                Assert.That(glyph.metrics.horizontalAdvance, Is.EqualTo(15f));
                Assert.That(glyph.scale, Is.EqualTo(1.25f));
            }
            finally
            {
                if (text != null) text.ReleaseRuntimeSpriteAsset();
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                if (originalAsset != null) Object.DestroyImmediate(originalAsset);
                if (material != null) Object.DestroyImmediate(material);
                if (sprite != null) Object.DestroyImmediate(sprite);
                if (texture != null) Object.DestroyImmediate(texture);
            }
        }

        private static GameObject CreateTextObject(
            string name,
            TMP_SpriteAsset originalAsset,
            Sprite sprite,
            out UI_Text text)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = null;
            tmp.spriteAsset = originalAsset;
            text = gameObject.AddComponent<UI_Text>();

            var serialized = new SerializedObject(text);
            serialized.FindProperty("isSpriteAsset").boolValue = true;
            serialized.FindProperty("sprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            text.ResetRuntimeSpriteGlyphSettings();
            return gameObject;
        }

        private static Material CreateSpriteMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(TestShaderPath);
            Assert.That(shader, Is.Not.Null);
            return new Material(shader) { name = "Runtime Sprite Asset Test Material" };
        }
    }
}
