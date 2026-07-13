using UnityEngine;

/// <summary>UI Foundation 애니메이션에서 사용하는 이징 종류입니다.</summary>
public enum UIEase
{
    Unset = 0,
    Linear = 1,
    InSine = 2,
    OutSine = 3,
    InOutSine = 4,
    InQuad = 5,
    OutQuad = 6,
    InOutQuad = 7,
    InCubic = 8,
    OutCubic = 9,
    InOutCubic = 10,
    InQuart = 11,
    OutQuart = 12,
    InOutQuart = 13,
    InQuint = 14,
    OutQuint = 15,
    InOutQuint = 16,
    InExpo = 17,
    OutExpo = 18,
    InOutExpo = 19,
    InCirc = 20,
    OutCirc = 21,
    InOutCirc = 22,
    InElastic = 23,
    OutElastic = 24,
    InOutElastic = 25,
    InBack = 26,
    OutBack = 27,
    InOutBack = 28,
    InBounce = 29,
    OutBounce = 30,
    InOutBounce = 31,
    // DOTween 직렬화 숫자 호환을 위한 예약값. 기본 애니메이터에서는 Linear로 폴백합니다.
    Flash = 32,
    InFlash = 33,
    OutFlash = 34,
    InOutFlash = 35
}

/// <summary>정규화된 진행률에 UI 이징을 적용합니다.</summary>
public static class UIEaseUtility
{
    private const float BackOvershoot = 1.70158f;

    /// <summary>0~1 진행률에 지정한 이징을 적용한 값을 반환합니다.</summary>
    public static float Evaluate(UIEase ease, float progress)
    {
        float x = Mathf.Clamp01(progress);
        return ease switch
        {
            UIEase.InSine => 1f - Mathf.Cos(x * Mathf.PI * 0.5f),
            UIEase.OutSine => Mathf.Sin(x * Mathf.PI * 0.5f),
            UIEase.InOutSine => -(Mathf.Cos(Mathf.PI * x) - 1f) * 0.5f,
            UIEase.InQuad => x * x,
            UIEase.OutQuad => 1f - (1f - x) * (1f - x),
            UIEase.InOutQuad => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) * 0.5f,
            UIEase.InCubic => x * x * x,
            UIEase.OutCubic => 1f - Mathf.Pow(1f - x, 3f),
            UIEase.InOutCubic => x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f,
            UIEase.InQuart => x * x * x * x,
            UIEase.OutQuart => 1f - Mathf.Pow(1f - x, 4f),
            UIEase.InOutQuart => x < 0.5f ? 8f * x * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 4f) * 0.5f,
            UIEase.InQuint => x * x * x * x * x,
            UIEase.OutQuint => 1f - Mathf.Pow(1f - x, 5f),
            UIEase.InOutQuint => x < 0.5f ? 16f * x * x * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 5f) * 0.5f,
            UIEase.InExpo => x <= 0f ? 0f : Mathf.Pow(2f, 10f * x - 10f),
            UIEase.OutExpo => x >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * x),
            UIEase.InOutExpo => EvaluateInOutExpo(x),
            UIEase.InCirc => 1f - Mathf.Sqrt(1f - x * x),
            UIEase.OutCirc => Mathf.Sqrt(1f - Mathf.Pow(x - 1f, 2f)),
            UIEase.InOutCirc => x < 0.5f
                ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * x, 2f))) * 0.5f
                : (Mathf.Sqrt(1f - Mathf.Pow(-2f * x + 2f, 2f)) + 1f) * 0.5f,
            UIEase.InElastic => EvaluateInElastic(x),
            UIEase.OutElastic => EvaluateOutElastic(x),
            UIEase.InOutElastic => EvaluateInOutElastic(x),
            UIEase.InBack => (BackOvershoot + 1f) * x * x * x - BackOvershoot * x * x,
            UIEase.OutBack => EvaluateOutBack(x),
            UIEase.InOutBack => EvaluateInOutBack(x),
            UIEase.InBounce => 1f - EvaluateOutBounce(1f - x),
            UIEase.OutBounce => EvaluateOutBounce(x),
            UIEase.InOutBounce => x < 0.5f
                ? (1f - EvaluateOutBounce(1f - 2f * x)) * 0.5f
                : (1f + EvaluateOutBounce(2f * x - 1f)) * 0.5f,
            _ => x
        };
    }

    private static float EvaluateInOutExpo(float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        return x < 0.5f
            ? Mathf.Pow(2f, 20f * x - 10f) * 0.5f
            : (2f - Mathf.Pow(2f, -20f * x + 10f)) * 0.5f;
    }

    private static float EvaluateInElastic(float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        return -Mathf.Pow(2f, 10f * x - 10f) * Mathf.Sin((x * 10f - 10.75f) * c4);
    }

    private static float EvaluateOutElastic(float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
    }

    private static float EvaluateInOutElastic(float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        const float c5 = 2f * Mathf.PI / 4.5f;
        return x < 0.5f
            ? -(Mathf.Pow(2f, 20f * x - 10f) * Mathf.Sin((20f * x - 11.125f) * c5)) * 0.5f
            : Mathf.Pow(2f, -20f * x + 10f) * Mathf.Sin((20f * x - 11.125f) * c5) * 0.5f + 1f;
    }

    private static float EvaluateOutBack(float x)
    {
        float c3 = BackOvershoot + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + BackOvershoot * Mathf.Pow(x - 1f, 2f);
    }

    private static float EvaluateInOutBack(float x)
    {
        float c2 = BackOvershoot * 1.525f;
        return x < 0.5f
            ? Mathf.Pow(2f * x, 2f) * ((c2 + 1f) * 2f * x - c2) * 0.5f
            : (Mathf.Pow(2f * x - 2f, 2f) * ((c2 + 1f) * (2f * x - 2f) + c2) + 2f) * 0.5f;
    }

    private static float EvaluateOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (x < 1f / d1) return n1 * x * x;
        if (x < 2f / d1)
        {
            x -= 1.5f / d1;
            return n1 * x * x + 0.75f;
        }
        if (x < 2.5f / d1)
        {
            x -= 2.25f / d1;
            return n1 * x * x + 0.9375f;
        }

        x -= 2.625f / d1;
        return n1 * x * x + 0.984375f;
    }
}
