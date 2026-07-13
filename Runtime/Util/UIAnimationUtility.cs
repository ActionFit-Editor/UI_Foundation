using System;
using System.Threading;
using UnityEngine;

internal static class UIAnimationUtility
{
    public static async Awaitable<bool> AnimateFloat(
        float from,
        float to,
        float duration,
        UIEase ease,
        Action<float> apply,
        CancellationToken cancellationToken)
    {
        return await Animate(
            duration,
            ease,
            progress => apply(Mathf.LerpUnclamped(from, to, progress)),
            cancellationToken);
    }

    public static async Awaitable<bool> AnimateVector2(
        Vector2 from,
        Vector2 to,
        float duration,
        UIEase ease,
        Action<Vector2> apply,
        CancellationToken cancellationToken)
    {
        return await Animate(
            duration,
            ease,
            progress => apply(Vector2.LerpUnclamped(from, to, progress)),
            cancellationToken);
    }

    public static async Awaitable<bool> AnimateVector3(
        Vector3 from,
        Vector3 to,
        float duration,
        UIEase ease,
        Action<Vector3> apply,
        CancellationToken cancellationToken)
    {
        return await Animate(
            duration,
            ease,
            progress => apply(Vector3.LerpUnclamped(from, to, progress)),
            cancellationToken);
    }

    private static async Awaitable<bool> Animate(
        float duration,
        UIEase ease,
        Action<float> apply,
        CancellationToken cancellationToken)
    {
        if (apply == null) throw new ArgumentNullException(nameof(apply));
        if (duration <= 0f)
        {
            apply(1f);
            return true;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            try
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested) return false;
            elapsed += Time.deltaTime;
            float progress = UIEaseUtility.Evaluate(ease, elapsed / duration);
            apply(progress);
        }

        if (cancellationToken.IsCancellationRequested) return false;
        apply(1f);
        return true;
    }
}
