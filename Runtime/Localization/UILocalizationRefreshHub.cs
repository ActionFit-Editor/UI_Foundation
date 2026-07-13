using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// 스크립트에서 StringTable.Get 등으로 설정한 텍스트를 언어 변경 시 자동 갱신하는 인터페이스
public interface ILocaleRefreshable
{
    void RefreshLocalization();
}

// 언어 변경 시 등록된 콜백을 호출하는 Hub 클래스
public static class UILocalizationRefreshHub
{
    private static readonly HashSet<Action> _subscribers = new();
    private static readonly List<ILocaleRefreshable> _registered = new();
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (_initialized) LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        _subscribers.Clear();
        _registered.Clear();
        _initialized = false;
    }

    private static void Initialize()
    {
        if (_initialized) return;

        _initialized = true;

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    /// <summary>
    /// ILocaleRefreshable 구현체를 등록합니다.
    /// 오브젝트 파괴 시 자동으로 해제되므로 OnDisable에서 해제할 필요가 없습니다.
    /// 동일 오브젝트 중복 등록을 방지합니다.
    /// </summary>
    public static void Register(ILocaleRefreshable target)
    {
        if (target == null) return;

        Initialize();
        if (!_registered.Contains(target))
            _registered.Add(target);
    }

    public static void OnRegister(Action action)
    {
        if (action == null) return;

        Initialize();
        _subscribers.Add(action);
    }

    public static void OnUnregister(Action action)
    {
        if (action == null) return;

        _subscribers.Remove(action);
    }

    private static void OnLocaleChanged(Locale _) => RefreshAll();

    /// <summary>
    /// 등록된 모든 구독 콜백과 ILocaleRefreshable 대상을 강제로 다시 실행합니다.
    /// 언어 변경뿐 아니라, 폴백 폰트가 런타임에 동적 주입된 직후 이미 그려진 텍스트를 재적용할 때도 호출합니다.
    /// (TMP는 TMP_Settings.fallbackFontAssets에 폰트를 나중에 Add해도 기존 텍스트 메시를 자동 rebuild하지 않음)
    /// </summary>
    public static void RefreshAll()
    {
        var subscriberSnapshot = new List<Action>(_subscribers);
        foreach (Action subscriber in subscriberSnapshot)
        {
            subscriber?.Invoke();
        }

        // 파괴된 오브젝트 자동 정리 후 콜백 호출
        _registered.RemoveAll(target =>
            target == null || target is UnityEngine.Object unityObject && unityObject == null);
        foreach (var target in _registered)
        {
            target.RefreshLocalization();
        }
    }
}
