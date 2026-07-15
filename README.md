# UI Foundation

`UI_Text`, `UI_Button`, `UI_Image`를 비롯한 ActionFit 공용 UGUI wrapper와 기존 prefab/scene의 직렬화 호환 계약을 제공하는 Unity Package Manager 패키지입니다.

- Package ID: `com.actionfit.ui.foundation`
- Version: `1.0.5`
- Minimum Unity: `6000.2`
- Public repository: <https://github.com/ActionFit-Editor/UI_Foundation>

## 설치

`Packages/manifest.json`의 `dependencies`에 버전 태그가 포함된 Git URL을 추가합니다.

```json
{
  "dependencies": {
    "com.actionfit.ui.foundation": "https://github.com/ActionFit-Editor/UI_Foundation.git#1.0.5"
  }
}
```

Unity Package Manager의 **Add package from git URL**에도 아래 URL을 그대로 사용할 수 있습니다.

```text
https://github.com/ActionFit-Editor/UI_Foundation.git#1.0.5
```

재현 가능한 빌드를 위해 브랜치나 저장소 HEAD 대신 릴리스 태그를 고정하세요.

## 의존성

| 구분 | 패키지/어셈블리 | 비고 |
| --- | --- | --- |
| 필수 | `com.unity.ugui` `2.0.0` | UGUI와 TextMeshPro API를 사용합니다. |
| 필수 | `com.unity.localization` `1.5.5` | `LocalizedString` 및 locale 갱신 기능을 사용합니다. |
| 선택 | DOTween core | 패키지에 포함되지 않습니다. `DOTWEEN` 심벌을 정의한 프로젝트에서만 연동됩니다. |

UGUI와 Localization은 `package.json`의 hard dependency이므로 설치 시 함께 해석되어야 합니다. Localization을 사용하지 않는 소비 프로젝트라도 1.0.5에서는 의존성을 제거할 수 없습니다.

이 패키지는 TMP font/material/shader 자산을 번들하지 않습니다. 새 소비 프로젝트에서는 **Window > TextMeshPro > Import TMP Essential Resources**를 실행하고, 프로젝트가 소유한 TMP Font Asset을 `UI_Text`와 sample에 연결하세요.

## 포함 기능

- 기본 wrapper: `UI_Rect`, `UI_Image`, `UI_ImageSlice`, `UI_Input`, `UI_InputBtn`, `UI_Scroll`
- 텍스트: `UI_Text`, Localization 연동, 자동 크기 조절, 기존 TMP Sprite Asset 태그, Sprite 기반 런타임 Sprite Asset과 Editor preview, face/outline/underlay 재질 관리
- 버튼: `UI_Button`, 활성/비활성 비주얼, 클릭음/테마 provider, press/enable 애니메이션
- 마스크: `UI_MaskBase`, `UI_Mask`, `UI_Mask2D`와 reveal 애니메이션
- 이미지: `Image_Slice`의 9-slice + linear fill 결합
- Editor: 전용 inspector, 지연·이벤트 기반 `UI_Text` Sprite/Face/Outline/Underlay preview와 기존 prefab/scene의 component reference migration 메뉴
- Tests: script GUID/type/assembly identity, `UIEase` 숫자 계약, `Image_Slice` mesh 경계를 검증하는 Editor tests

Runtime 어셈블리는 `com.actionfit.ui.foundation`, Editor 어셈블리는 `com.actionfit.ui.foundation.Editor`입니다. Runtime 어셈블리는 `autoReferenced: true`이므로 일반적인 `Assembly-CSharp` 코드에서 별도 asmdef 참조 없이 public API를 사용할 수 있습니다. 자체 asmdef를 사용하는 소비 코드는 `com.actionfit.ui.foundation`을 명시적으로 참조해야 합니다.

## 기존 프로젝트 호환 계약

1.0.5는 기존 냥카페 prefab/scene 호환을 위해 다음을 의도적으로 유지합니다.

- 공개 MonoBehaviour 및 helper 타입은 **global namespace**에 있고 Runtime asmdef의 `rootNamespace`도 비어 있습니다.
- 원본 스크립트의 `.meta`와 GUID를 보존해 prefab/scene의 `MonoScript` 참조가 패키지 이동 후에도 이어지도록 했습니다.
- 타입명과 `[SerializeField]` 필드명은 저장 데이터 계약입니다. 승인된 필드 rename은 기존 값을 유지한 채 해당 prefab/scene YAML key만 직접 migration하고 실제 자산을 검증하세요. 새 `FormerlySerializedAs`를 추가하지 말고, 이미 존재하는 legacy attribute는 호환 이력으로 보존합니다.
- 기존 구현 파일과 패키지 구현을 동시에 두면 global 타입 중복으로 컴파일되지 않습니다. 기존 프로젝트 전환은 원본 제거와 패키지 추가를 같은 변경에서 수행하고, GUID를 재생성하지 마세요.

패키지를 적용한 뒤 prefab/scene에 `Missing Script`가 없는지 확인하고, 필요하면 **Tools > Package > UI Foundation > Migrate Component Refs**를 실행해 `_image`, `_button`, `_inputField`, `_scrollRect` 캐시를 채우세요.

## Localization 갱신 계약

`UI_Text.SetLocalizeKey(table, entry)`는 즉시 텍스트를 적용하고 locale 변경 갱신 대상으로 등록합니다. `UI_Text` 밖에서 locale-dependent text를 관리하는 객체는 `ILocaleRefreshable.RefreshLocalization()`을 구현하고 초기화 시 `UILocalizationRefreshHub.Register(this)`를 한 번 호출할 수 있습니다.

- `Register`는 같은 객체의 중복 등록을 막습니다.
- 등록된 Unity object가 파괴되면 다음 `RefreshAll()`에서 자동 제거되므로 `ILocaleRefreshable` 등록에는 별도 unregister API가 필요하지 않습니다.
- `RefreshLocalization()`은 현재 locale 기준으로 텍스트를 다시 적용해야 합니다.
- `OnRegister(Action)` 콜백 경로는 자동 정리 대상이 아니므로 같은 활성 수명에서 `OnUnregister(Action)`과 짝을 맞추세요.
- 화면 표시나 매 frame마다 항상 텍스트를 다시 적용하는 객체는 별도 등록이 필요하지 않습니다.

## `UIButtonServices` 프로젝트 연결

Foundation은 게임 전용 사운드 시스템이나 버튼 아트 리소스를 참조하지 않습니다. 소비 프로젝트가 아래 provider를 등록할 수 있습니다.

- `IUIButtonClickSoundPlayer`: 버튼 클릭음을 프로젝트 오디오 시스템으로 전달
- `IUIButtonTheme`: `UI_Button.ButtonSprite` preset을 프로젝트 Sprite로 변환
- `UIButtonServices`: 위 두 provider의 런타임 등록 지점

```csharp
using UnityEngine;

internal static class ProjectUIButtonBootstrap
{
    private sealed class ClickSoundPlayer : IUIButtonClickSoundPlayer
    {
        public void PlayClickSound()
        {
            // 프로젝트 오디오 시스템을 호출합니다.
        }
    }

    private sealed class ButtonTheme : IUIButtonTheme
    {
        public Sprite GetButtonSprite(UI_Button.ButtonSprite preset) =>
            Resources.Load<Sprite>($"UI/Buttons/{preset}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        UIButtonServices.ClickSoundPlayer = new ClickSoundPlayer();
        UIButtonServices.Theme = new ButtonTheme();
    }
}
```

provider를 등록하지 않아도 패키지는 동작합니다. 클릭음은 재생하지 않고, theme lookup은 `null`을 반환합니다. `UIButtonServices`는 `SubsystemRegistration`에서 초기화되므로 런타임 provider는 그 이후 단계(예: `BeforeSceneLoad`)에 다시 등록해야 합니다.

냥카페 전용 연결은 패키지에 포함하지 않고 소비 프로젝트의 `Assets/_Project/_Common/UI/UI_Prefab.Extensions/CatMergeCafe/CatMergeCafeUIButtonServices.cs`에 둡니다. 이 adapter만 `Main.Sound`와 냥카페 `Resources` 경로를 알아야 합니다.

## DOTween 선택 연동

DOTween 소스, DLL, 라이선스는 이 패키지에 번들되지 않습니다. 기본 구성은 `DOTWEEN` 심벌 없이 Unity `Awaitable`과 `CancellationTokenSource` 기반 애니메이션을 사용합니다.

`DOTWEEN`을 정의하려면 소비 프로젝트에 호환되는 DOTween core가 설치되어 있고, 그 precompiled assembly가 `com.actionfit.ui.foundation` asmdef에서 auto-reference 가능한 상태여야 합니다. DOTween을 별도 asmdef 소스로 구성했다면 현재 1.0.5 asmdef에는 해당 참조가 없으므로 심벌만 추가해서는 안 됩니다.

두 모드는 동작 목적은 같지만 public API 타입이 완전히 같지는 않습니다.

| API | `DOTWEEN` 정의 | 미정의 |
| --- | --- | --- |
| `UI_MaskBase.Expand/Collapse/Anim*` 반환 | `DG.Tweening.Tween` | `UnityEngine.Awaitable<bool>` |
| mask ease 인자/직렬화 필드 | `DG.Tweening.Ease` | `UIEase` |
| `UI_Scroll.AnimateToTop(duration, ease, ...)` | `DG.Tweening.Ease` | `UIEase` |

`UIEase`의 명시적 숫자값 `0`~`35`는 DOTween `Ease` 슬롯과의 직렬화 호환을 위한 것입니다. `Linear`부터 `InOutBounce`까지는 내장 evaluator가 처리하지만 다음 차이가 있습니다.

- `Unset`과 `Flash`, `InFlash`, `OutFlash`, `InOutFlash`는 linear로 fallback합니다.
- overshoot, amplitude, period 같은 DOTween의 추가 파라미터를 지원하지 않습니다.
- `UIEase` 및 `Awaitable<bool>`은 DOTween의 source/binary-compatible 대체 API가 아닙니다.

따라서 두 구성 모두 지원하는 소비 코드는 `DG.Tweening.Tween` 전용 extension이나 반환 타입에 직접 결합하지 않도록 작성하고, 심벌 정의/미정의 빌드를 각각 컴파일 검증하세요.

## `Image_Slice` 사용

`Image_Slice`는 Unity `Image`를 상속하고, Unity 기본 Image가 동시에 제공하지 않는 9-slice border와 linear fill을 한 메시에서 결합합니다.

1. Sprite Editor에서 sprite border를 설정합니다.
2. Image `Type`을 `Filled`로 설정합니다.
3. `Fill Method`를 `Horizontal` 또는 `Vertical`로 설정합니다.
4. `Is Slice Image`를 활성화하고 `Fill Amount`를 조절합니다.

방향 매핑은 다음과 같습니다.

| Fill Method | Origin | 진행 방향 |
| --- | --- | --- |
| Horizontal | Left | Right |
| Horizontal | Right | Left |
| Vertical | Bottom | Up |
| Vertical | Top | Down |

`fillCenter`가 꺼져 있으면 가운데 3x3 cell만 생략하고 border cell은 fill 구간에 맞춰 잘라 그립니다. `fillAmount`, `pixelsPerUnitMultiplier`, `overrideSprite`, color, sprite border/padding을 존중합니다.

`Is Slice Image`가 꺼져 있거나, sprite border가 없거나, `Type`이 `Filled`가 아니거나, Radial fill이면 Unity `Image.OnPopulateMesh`로 fallback합니다. 인스펙터는 `Image_SliceEditor`가 제공합니다.

## `UI_Text` 인라인 Sprite

`UI_Text.Sprite.cs` partial은 기존 TMP 컴포넌트의 `spriteAsset`을 `SpriteAsset` 프로퍼티와 `SetSpriteAsset`로 노출합니다. 소비 프로젝트가 자신의 `TMP_SpriteAsset`과 atlas texture를 소유해야 하며, Foundation은 아트 자산을 번들하거나 자동 로드하지 않습니다.

```csharp
text.SetSpriteAsset(itemSpriteAsset)
    .SetTextWithSprite("보상 ", "coin", " 100", tint: true);

string iconTag = UI_Text.BuildSpriteTag("coin");
text.Text = $"보상 {iconTag} 100";
```

`SetTextWithSprite`는 TMP Rich Text를 활성화하고 이름 기반 `<sprite name="...">` 태그를 앞뒤 텍스트 사이에 배치합니다. `BuildSpriteTag(int)`로 인덱스 태그도 만들 수 있지만 Sprite Asset 순서 변경에 안정적인 이름 방식을 권장합니다. Localization 문자열에 TMP `<sprite>` 태그를 직접 저장하는 기존 방식도 그대로 작동합니다.

### Sprite에서 런타임 Sprite Asset 생성

미리 제작한 `TMP_SpriteAsset` 없이 하나의 프로젝트 소유 Sprite를 인라인 이미지로 사용하려면 `UI_Text` Inspector에서 `Is Sprite Asset`을 켜고 `Sprite`를 연결합니다. Single 모드 Sprite도 그대로 사용할 수 있으며 texture를 Multiple로 바꿀 필요가 없습니다.

`Reset Glyph Settings From Sprite`는 Sprite 영역과 pivot을 기준으로 아래 값을 초기화합니다.

| 그룹 | Inspector | TMP 값 |
| --- | --- | --- |
| Glyph Rect | `X`, `Y`, `W`, `H` | atlas texture의 `GlyphRect` |
| Glyph Metrics | `W`, `H` | glyph width/height |
| Glyph Metrics | `BX`, `BY` | horizontal bearing X/Y |
| Glyph Metrics | `AD` | horizontal advance |
| Glyph Metrics | `Scale` | `TMP_SpriteGlyph.scale` |

`Override Glyph Rect`가 꺼져 있으면 현재 Sprite의 texture 영역을 사용하므로 일반적인 재임포트에 안전합니다. 직접 영역을 고정해야 할 때만 켜고, Sprite 또는 atlas가 바뀌면 reset 후 확인하세요. 회전 또는 tight packing된 Sprite Atlas 항목은 지원하지 않습니다.

런타임에는 같은 Sprite와 최종 glyph 설정, Material template 조합이 하나의 임시 `TMP_SpriteAsset`과 Material을 reference count로 공유합니다. 활성화 시 Localization 적용보다 먼저 획득하고 비활성화 시 반납하며, 적용 전 `TMP_Text.spriteAsset`을 복원합니다. 런타임 Inspector에서 값을 바꾼 뒤에는 `ApplyRuntimeSpriteAsset()`으로 다시 적용할 수 있습니다.

텍스트에는 `<sprite=0>` 또는 `UI_Text.BuildSpriteTag(0)`을 명시적으로 넣습니다. 이 기능은 텍스트나 Localization 값을 자동으로 변경하지 않습니다.

## `UI_Text` Editor preview 생명주기

Sprite, Face, Outline 또는 Underlay를 사용하는 `UI_Text`는 Editor에서 프로젝트 자산을 수정하지 않는 임시 asset을 사용합니다. Sprite preview의 `TMP_SpriteAsset`과 Material은 `HideFlags.HideAndDontSave`, Face/Outline/Underlay Material은 `HideFlags.DontSave`로 유지합니다. `UI_TextEditorPreviewCoordinator`가 다음 이벤트를 받아 TMP 초기화 이후의 `EditorApplication.delayCall`에서 preview를 적용합니다.

- Unity script/domain reload 이후 Editor 재초기화
- scene open
- Prefab Mode open/close
- Inspector의 Sprite/glyph 또는 Face/Outline/Underlay 값 변경
- Undo/Redo
- Play Mode 진입 전과 Edit Mode 복귀

Prefab Mode에서는 현재 `prefabContentsRoot` 아래의 활성·비활성 `UI_Text`만 갱신합니다. 반복 요청은 같은 지연 큐에서 합쳐지며, TMP가 아직 준비되지 않았으면 제한된 횟수만 재시도합니다. Sprite preview가 존재하는 동안에는 생성된 preview 보유자만 경량 Editor update로 추적합니다. 컴포넌트 비활성화·제거, Prefab Mode 종료, assembly reload 또는 Play Mode 진입 전에는 원본 `spriteAsset`과 `fontSharedMaterial`을 복원하고 임시 asset과 Material을 제거합니다.

Preview 갱신은 prefab/scene을 저장하거나 dirty 상태를 지우지 않습니다. preview로 인해 자산이 dirty 처리되거나 `m_spriteAsset`·`m_sharedMaterial`·`m_fontMaterial` YAML이 변경되면 저장하지 말고 회귀로 취급하세요. 이 동작은 Player의 `RuntimeSpriteAssetCache`와 `OutlineMaterialCache` 풀링과 별개입니다.

## TMP outline/shadow와 Player 빌드

`OutlineMaterialCache`는 `Shader.Find("TextMeshPro/Mobile/Distance Field Shadow Outline")`로 shader를 찾고 동일 설정의 Material을 공유합니다. 이 shader는 Foundation에 포함되지 않으므로 먼저 TMP Essential Resources를 import해야 합니다. `Acquire`로 얻은 Material은 같은 `Config`로 `Release`해야 하며, 더 이상 복제 Material을 사용하지 않는 수명 경계에서는 `Clear`를 호출할 수 있습니다.

Sprite 기반 런타임 asset은 유효한 TMP default/original Sprite Material을 복제하고, 없으면 `Shader.Find("TextMeshPro/Sprite")`를 사용합니다. Editor에서 보이더라도 Player stripping으로 이 shader를 찾지 못할 수 있으므로 실제 Material asset 참조나 프로젝트의 승인된 shader 유지 정책을 적용하고 대상 Player에서 확인하세요.

Editor에서는 보이더라도 Player shader stripping으로 `Shader.Find`가 실패할 수 있습니다. 대상 플랫폼 빌드에서 outline/underlay를 확인하고, 필요하면 실제 Material asset 참조, Preloaded Assets 또는 **Project Settings > Graphics > Always Included Shaders** 등 프로젝트 정책에 맞는 방법으로 shader를 보존하세요.

## Unity 메뉴

- **Tools > Package > UI Foundation > README**: 이 문서를 엽니다.
- **Tools > Package > UI Foundation > Migrate Component Refs**: 프로젝트의 모든 prefab과 scene을 순회해 wrapper의 직렬화 component cache를 채우고 변경 자산을 저장합니다.

Migration 메뉴는 다수 자산을 수정하고 scene을 차례로 열기 때문에 실행 전에 작업을 commit하거나 백업하고, 실행 후 diff를 반드시 검토하세요.

## Agent Skills

Custom Package Manager의 `Install or Refresh Agent Skills`를 실행하면 Codex와 Claude에 다음 read-only 진입점이 설치됩니다.

- `ui-foundation-help`: global wrapper, GUID/직렬화 호환, `UIButtonServices`, 선택적 DOTween, 메뉴와 테스트 경계를 설명합니다.
- `ui-foundation-audit`: Unity나 migration 메뉴를 실행하지 않고 asmdef, `.meta` GUID, serialized field, identity test와 프로젝트 service 경계를 소스 기준으로 점검합니다.

두 스킬 모두 `Migrate Component Refs`를 실행하거나 prefab/scene을 저장·재직렬화하지 않으며, GUID·serialized value·provider·scripting symbol을 변경하지 않습니다.

## 검증

1.0.5는 `Tests/Runtime`의 `com.actionfit.ui.foundation.Runtime.Tests`와 `Tests/Editor`의 `com.actionfit.ui.foundation.Editor.Tests`를 포함합니다. 두 어셈블리 모두 `autoReferenced: false`, `UNITY_INCLUDE_TESTS` 조건이며 Unity Test Framework의 `TestAssemblies` 참조를 사용합니다. **Window > General > Test Runner**에서 실행하세요.

현재 자동 테스트 범위는 다음과 같습니다.

- `UIScriptIdentityTests`: 기존 runtime/editor script GUID, package 경로, public type, assembly identity
- `UIEaseCompatibilityTests`: enum 이름/숫자값, endpoint 안정성, unsupported/unknown 값의 linear fallback
- `ImageSliceMeshTests`: 네 fill 방향, `fillCenter`, tiny fill/zero rect, border가 rect보다 큰 경우의 유효 mesh
- `RuntimeSpriteAssetCacheTests`: Sprite rect/pivot 기본값, TMP glyph mapping, cache reference count, 기존 Sprite Asset 복원
- `UI_TextEditorPreviewTests`: 지연 요청 병합, 활성·비활성 대상, Prefab Mode 재진입, Undo/Redo, 임시 Sprite Asset/Material 복원·비직렬화와 dirty 상태 불변
- `UIRuntimeContractTests`와 `UIWrapperBehaviorTests`: runtime assembly identity, Image/Text/Button/Scroll/Mask의 기본 wrapper 계약과 `UI_Text` 인라인 sprite 태그

자동 테스트만으로 Player와 소비 프로젝트 통합을 모두 보장하지는 않습니다. 릴리스 시 다음 항목도 검증하세요.

- `DOTWEEN` 미정의 상태에서 Runtime/Editor 전체 컴파일 및 Awaitable 애니메이션 취소/완료
- DOTween core를 공급하고 `DOTWEEN`을 정의한 상태에서 전체 컴파일 및 tween 취소/완료
- 기존 prefab/scene의 `Missing Script`와 wrapper 직렬화 값 손실 여부
- 실제 Sprite의 padding/PPU/`overrideSprite`를 사용한 `Image_Slice` 시각 결과
- locale 변경 시 `UI_Text` 갱신과 UGUI/TMP inspector 동작
- provider 등록/미등록 상태의 버튼 클릭음과 theme preset
- 실제 Player 빌드의 TMP outline/underlay shader 유지 여부

## Third-party notice

`Image_Slice`의 개념적 참고와 선택적 DOTween 연동 범위는 [Third Party Notices](<Third Party Notices.md>)를 확인하세요.
