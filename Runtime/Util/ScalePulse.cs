using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 활성 상태인 동안 자신의 Transform을 공통 위상의 스케일 펄스에 등록합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ScalePulse : MonoBehaviour
{
    #region Fields

    private Vector3 _baselineScale;
    private bool _isRegistered;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        if (_isRegistered) return;

        _baselineScale = transform.localScale;
        _isRegistered = true;
        ScalePulseScheduler.Register(this);
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    #endregion

    #region Internal Methods

    internal void ApplyScaleRatio(float ratio)
    {
        transform.localScale = _baselineScale * ratio;
    }

    internal void ResetRegistration()
    {
        if (_isRegistered && transform != null) transform.localScale = _baselineScale;
        _isRegistered = false;
    }

    #endregion

    #region Private Methods

    private void Unregister()
    {
        if (!_isRegistered) return;

        ScalePulseScheduler.Unregister(this);
        transform.localScale = _baselineScale;
        _isRegistered = false;
    }

    #endregion
}

internal static class ScalePulseScheduler
{
    #region Fields

    internal const float CycleDurationSeconds = 0.4f;
    internal const float MinimumScaleRatio = 0.8f;

    private static readonly List<ScalePulse> ActivePulses = new();
    private static float _phaseSeconds;
    private static float _currentRatio = MinimumScaleRatio;
    private static int _loopVersion;
    private static bool _isRunning;

    #endregion

    #region Properties

    internal static int ActiveCount => ActivePulses.Count;
    internal static float CurrentRatio => _currentRatio;
    internal static bool IsRunning => _isRunning;

    #endregion

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        for (int index = ActivePulses.Count - 1; index >= 0; index--)
        {
            ScalePulse pulse = ActivePulses[index];
            if (pulse != null) pulse.ResetRegistration();
        }

        ActivePulses.Clear();
        _phaseSeconds = 0f;
        _currentRatio = MinimumScaleRatio;
        _isRunning = false;
        _loopVersion++;
    }

    #endregion

    #region Internal Methods

    internal static void Register(ScalePulse pulse)
    {
        if (pulse == null)
        {
            Debug.LogError("[ScalePulseScheduler] Register: pulse is null");
            return;
        }

        if (ActivePulses.Contains(pulse)) return;

        ActivePulses.Add(pulse);
        if (_isRunning)
        {
            pulse.ApplyScaleRatio(_currentRatio);
            return;
        }

        StartLoop();
    }

    internal static void Unregister(ScalePulse pulse)
    {
        if (!ActivePulses.Remove(pulse)) return;
        if (ActivePulses.Count == 0) StopLoop();
    }

    internal static void AdvanceForTests(float deltaTime)
    {
        Advance(deltaTime);
    }

    internal static void ResetForTests()
    {
        ResetStatics();
    }

    #endregion

    #region Private Methods

    private static void StartLoop()
    {
        _phaseSeconds = 0f;
        _currentRatio = MinimumScaleRatio;
        _isRunning = true;
        int version = ++_loopVersion;
        ApplyCurrentRatio();
        _ = RunLoopAsync(version);
    }

    private static void StopLoop()
    {
        _isRunning = false;
        _phaseSeconds = 0f;
        _currentRatio = MinimumScaleRatio;
        _loopVersion++;
    }

    private static async Awaitable RunLoopAsync(int version)
    {
        try
        {
            while (_isRunning && version == _loopVersion)
            {
                await Awaitable.NextFrameAsync();
                if (!_isRunning || version != _loopVersion) return;
                Advance(Time.deltaTime);
            }
        }
        catch (OperationCanceledException)
        {
            if (version == _loopVersion) StopLoop();
        }
        catch (Exception exception)
        {
            if (version != _loopVersion) return;

            Debug.LogException(exception);
            StopLoop();
        }
    }

    private static void Advance(float deltaTime)
    {
        RemoveDestroyedPulses();
        if (ActivePulses.Count == 0)
        {
            StopLoop();
            return;
        }

        if (deltaTime > 0f)
        {
            _phaseSeconds = Mathf.Repeat(_phaseSeconds + deltaTime, CycleDurationSeconds);
            float normalizedPhase = _phaseSeconds / CycleDurationSeconds;
            float pulseProgress = 0.5f - 0.5f * Mathf.Cos(normalizedPhase * Mathf.PI * 2f);
            _currentRatio = Mathf.Lerp(MinimumScaleRatio, 1f, pulseProgress);
        }

        ApplyCurrentRatio();
    }

    private static void ApplyCurrentRatio()
    {
        for (int index = ActivePulses.Count - 1; index >= 0; index--)
        {
            ScalePulse pulse = ActivePulses[index];
            if (pulse == null)
            {
                ActivePulses.RemoveAt(index);
                continue;
            }

            pulse.ApplyScaleRatio(_currentRatio);
        }

        if (ActivePulses.Count == 0) StopLoop();
    }

    private static void RemoveDestroyedPulses()
    {
        for (int index = ActivePulses.Count - 1; index >= 0; index--)
        {
            if (ActivePulses[index] == null) ActivePulses.RemoveAt(index);
        }
    }

    #endregion
}
