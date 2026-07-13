using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public class ImageSliceMeshTests
    {
        private static readonly MethodInfo PopulateMeshMethod = typeof(Image_Slice).GetMethod(
            "OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(VertexHelper) },
            null);

        [TestCase(Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Left, -50f, 0f, -50f, 50f)]
        [TestCase(Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Right, 0f, 50f, -50f, 50f)]
        [TestCase(Image.FillMethod.Vertical, (int)Image.OriginVertical.Bottom, -50f, 50f, -50f, 0f)]
        [TestCase(Image.FillMethod.Vertical, (int)Image.OriginVertical.Top, -50f, 50f, 0f, 50f)]
        public void HalfFillClipsFromConfiguredOrigin(
            Image.FillMethod method,
            int origin,
            float minX,
            float maxX,
            float minY,
            float maxY)
        {
            WithImage(new Vector2(100f, 100f), image =>
            {
                image.fillMethod = method;
                image.fillOrigin = origin;
                image.fillAmount = 0.5f;
                using var helper = Populate(image);
                Bounds2D bounds = ReadBounds(helper);
                Assert.That(bounds.MinX, Is.EqualTo(minX).Within(0.001f));
                Assert.That(bounds.MaxX, Is.EqualTo(maxX).Within(0.001f));
                Assert.That(bounds.MinY, Is.EqualTo(minY).Within(0.001f));
                Assert.That(bounds.MaxY, Is.EqualTo(maxY).Within(0.001f));
            });
        }

        [TestCase(true, 36, 54)]
        [TestCase(false, 32, 48)]
        public void FullFillHonorsFillCenter(bool fillCenter, int vertices, int indices)
        {
            WithImage(new Vector2(100f, 100f), image =>
            {
                image.fillAmount = 1f;
                image.fillCenter = fillCenter;
                using var helper = Populate(image);
                Assert.That(helper.currentVertCount, Is.EqualTo(vertices));
                Assert.That(helper.currentIndexCount, Is.EqualTo(indices));
            });
        }

        [Test]
        public void TinyFillAndZeroRectProduceNoGeometry()
        {
            WithImage(new Vector2(100f, 100f), image =>
            {
                image.fillAmount = 0.0009f;
                using var helper = Populate(image);
                Assert.That(helper.currentVertCount, Is.Zero);
            });
            WithImage(Vector2.zero, image =>
            {
                using var helper = Populate(image);
                Assert.That(helper.currentVertCount, Is.Zero);
            });
        }

        [Test]
        public void BorderLargerThanRectStillProducesFiniteNonDegenerateTriangles()
        {
            WithImage(new Vector2(5f, 5f), image =>
            {
                image.fillAmount = 1f;
                using var helper = Populate(image);
                var stream = new List<UIVertex>();
                helper.GetUIVertexStream(stream);
                Assert.That(stream.Count, Is.GreaterThan(0));
                Assert.That(stream.Count % 3, Is.Zero);
                for (int index = 0; index < stream.Count; index += 3)
                {
                    Vector3 a = stream[index].position;
                    Vector3 b = stream[index + 1].position;
                    Vector3 c = stream[index + 2].position;
                    AssertFinite(a);
                    AssertFinite(b);
                    AssertFinite(c);
                    Assert.That(Vector3.Cross(b - a, c - a).sqrMagnitude, Is.GreaterThan(0.0000001f));
                }
            }, 40f);
        }

        private static void WithImage(Vector2 size, System.Action<Image_Slice> test, float border = 10f)
        {
            var texture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 100f, 100f),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            var gameObject = new GameObject("ImageSliceMeshTest", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var rect = (RectTransform)gameObject.transform;
                rect.sizeDelta = size;
                var image = gameObject.AddComponent<Image_Slice>();
                image.sprite = sprite;
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.fillCenter = true;
                image.fillAmount = 1f;
                image.IsSliceImage = true;
                test(image);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static VertexHelper Populate(Image_Slice image)
        {
            Assert.That(PopulateMeshMethod, Is.Not.Null);
            var helper = new VertexHelper();
            PopulateMeshMethod.Invoke(image, new object[] { helper });
            return helper;
        }

        private static Bounds2D ReadBounds(VertexHelper helper)
        {
            Assert.That(helper.currentVertCount, Is.GreaterThan(0));
            var vertex = new UIVertex();
            helper.PopulateUIVertex(ref vertex, 0);
            var result = new Bounds2D(vertex.position.x, vertex.position.x, vertex.position.y, vertex.position.y);
            for (int index = 1; index < helper.currentVertCount; index++)
            {
                helper.PopulateUIVertex(ref vertex, index);
                result.MinX = Mathf.Min(result.MinX, vertex.position.x);
                result.MaxX = Mathf.Max(result.MaxX, vertex.position.x);
                result.MinY = Mathf.Min(result.MinY, vertex.position.y);
                result.MaxY = Mathf.Max(result.MaxY, vertex.position.y);
            }
            return result;
        }

        private static void AssertFinite(Vector3 value)
        {
            Assert.That(float.IsNaN(value.x) || float.IsInfinity(value.x), Is.False);
            Assert.That(float.IsNaN(value.y) || float.IsInfinity(value.y), Is.False);
            Assert.That(float.IsNaN(value.z) || float.IsInfinity(value.z), Is.False);
        }

        private struct Bounds2D
        {
            public Bounds2D(float minX, float maxX, float minY, float maxY)
            {
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
            }

            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;
        }
    }
}
