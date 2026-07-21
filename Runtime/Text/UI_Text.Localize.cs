using System;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// UI_Text — 로컬라이징(Localized String).
/// isLocalizeText 토글이 켜진 인스턴스만 LocalizedString(테이블+엔트리)을 동기 조회(GetLocalizedString)해 텍스트에 적용하고
/// UILocalizationRefreshHub에 등록해 언어 변경 시 자동 갱신한다.
/// (모드 B — 비동기 StringChanged를 쓰지 않아 per-instance 핸들/구독이 없고, 프로젝트의 Hub 갱신 모델과 일관.)
/// 코드에서 키를 지정하려면 <see cref="SetLocalizeKey"/>, 포맷 인자를 갱신하려면
/// <see cref="SetLocalizeArguments"/> 사용.
/// </summary>
public partial class UI_Text : ILocaleRefreshable
{
    [SerializeField] private bool isLocalizeText = false;     // ON일 때만 Hub 등록/적용 (다른 UI_Text 토글과 동일 패턴)
    [SerializeField] private LocalizedString localizedString; // 테이블+엔트리 피커 (isLocalizeText ON일 때만 사용)

    // 토글 ON + 유효 참조일 때만 로컬라이징 동작 (둘 중 하나라도 빠지면 기존 텍스트 유지)
    public bool IsLocalized => isLocalizeText && localizedString != null && !localizedString.IsEmpty;

    // 런타임 활성화 시(Core OnEnable에서 호출): 로컬라이즈 텍스트 적용 + 언어변경 갱신 등록
    private void ApplyLocalization()
    {
        if (!IsLocalized) return;
        RefreshLocalization();
        UILocalizationRefreshHub.Register(this); // 언어 변경 시 RefreshLocalization 호출, 파괴 시 자동 해제(중복 등록 방지 내장)
    }

    // ILocaleRefreshable — 등록/언어 변경 시 LocalizedString을 동기 조회해 텍스트 적용
    public void RefreshLocalization()
    {
        if (_txt == null || !IsLocalized) return;
        _txt.SetText(localizedString.GetLocalizedString());
    }

    /// <summary>
    /// 코드에서 테이블/엔트리 키를 지정해 즉시 적용 + 언어변경 갱신 등록. (isLocalizeText 자동 ON)
    /// 예) text.SetLocalizeKey("UITable", "shop_title"); — 인스펙터 피커 없이 동적으로 키를 바꿀 때 사용.
    /// </summary>
    public UI_Text SetLocalizeKey(string table, string entry)
    {
        EnsureInit();
        isLocalizeText = true;
        localizedString ??= new LocalizedString();
        localizedString.SetReference(table, entry); // string → TableReference / TableEntryReference 암시 변환
        RefreshLocalization();
        UILocalizationRefreshHub.Register(this);
        return this;
    }

    /// <summary>
    /// 현재 LocalizedString에 포맷 인자를 지정하고, 유효한 키가 있으면 현재 locale 텍스트를 즉시 갱신합니다.
    /// 이 메서드는 localization 토글이나 키를 변경하지 않으므로, Inspector 설정 또는
    /// <see cref="SetLocalizeKey"/>와 함께 사용하세요.
    /// </summary>
    public UI_Text SetLocalizeArguments(params object[] arguments)
    {
        EnsureInit();
        localizedString ??= new LocalizedString();
        localizedString.Arguments = arguments ?? Array.Empty<object>();
        RefreshLocalization();
        return this;
    }
}
