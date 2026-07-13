# UI Foundation

`UI_Text`, `UI_Button`, `UI_Image`를 비롯한 ActionFit 공용 UGUI wrapper와 기존 prefab/scene의 직렬화 호환 계약을 제공하는 Unity Package Manager 패키지입니다.

- Package ID: `com.actionfit.ui.foundation`
- Version: `1.0.0`
- Minimum Unity: `6000.2`
- Public repository: <https://github.com/ActionFit-Editor/UI_Foundation>

## 설치

`Packages/manifest.json`의 `dependencies`에 버전 태그가 포함된 Git URL을 추가합니다.

```json
{
  "dependencies": {
    "com.actionfit.ui.foundation": "https://github.com/ActionFit-Editor/UI_Foundation.git#1.0.0"
  }
}
```

Unity Package Manager의 **Add package from git URL**에도 아래 URL을 그대로 사용할 수 있습니다.

```text
https://github.com/ActionFit-Editor/UI_Foundation.git#1.0.0
```

재현 가능한 빌드를 위해 브랜치나 저장소 HEAD 대신 릴리스 태그를 고정하세요.

## 의존성

| 구분 | 패키지/어셈블리 | 비고 |
| --- | --- | --- |
| 필수 | `com.unity.ugui` `2.0.0` | UGUI와 TextMeshPro API를 사용합니다. |
| 필수 | `com.unity.localization` `1.5.5` | `LocalizedString` 및 locale 갱신 기능을 사용합니다. |
| 선택 | DOTween core | 패키지에 포함되지 않습니다. `DOTWEEN` 심벌을 정의한 프로젝트에서만 연동됩니다. |

UGUI와 Localization은 `package.json`의 hard dependency이므로 설치 시 함께 해석되어야 합니다. Localization을 사용하지 않는 소비 프로젝트라도 1.0.0에서는 의존성을 제거할 수 없습니다.

이 패키지는 TMP font/material/shader 자산을 번들하지 않습니다. 새 소비 프로젝트에서는 **Window > TextMeshPro > Import TMP Essential Resources**를 실행하고, 프로젝트가 소유한 TMP Font Asset을 `UI_Text`와 sample에 연결하세요.

## 포함 기능

- 기본 wrapper: `UI_Rect`, `UI_Image`, `UI_ImageSlice`, `UI_Input`, `UI_InputBtn`, `UI_Scroll`
- 텍스트: `UI_Text`, Localization 연동, 자동 크기 조절, TMP face/outline/underlay 재질 관리
- 버튼: `UI_Button`, 활성/비활성 비주얼, 클릭음/테마 provider, press/enable 애니메이션
- 마스크: `UI_MaskBase`, `UI_Mask`, `UI_Mask2D`와 reveal 애니메이션
- 이미지: `Image_Slice`의 9-slice + linear fill 결합
- Editor: 전용 inspector와 기존 prefab/scene의 component reference migration 메뉴
- Tests: script GUID/type/assembly identity, `UIEase` 숫자 계약, `Image_Slice` mesh 경계를 검증하는 Editor tests

Runtime 어셈블리는 `com.actionfit.ui.foundation`, Editor 어셈블리는 `com.actionfit.ui.foundation.Editor`입니다. Runtime 어셈블리는 `autoReferenced: true`이므로 일반적인 `Assembly-CSharp` 코드에서 별도 asmdef 참조 없이 public API를 사용할 수 있습니다. 자체 asmdef를 사용하는 소비 코드는 `com.actionfit.ui.foundation`을 명시적으로 참조해야 합니다.

## 기존 프로젝트 호환 계약

1.0.0은 기존 냥카페 prefab/scene 호환을 위해 다음을 의도적으로 유지합니다.

- 공개 MonoBehaviour 및 helper 타입은 **global namespace**에 있고 Runtime asmdef의 `rootNamespace`도 비어 있습니다.
- 원본 스크립트의 `.meta`와 GUID를 보존해 prefab/scene의 `MonoScript` 참조가 패키지 이동 후에도 이어지도록 했습니다.
- 타입명과 `[SerializeField]` 필드명은 저장 데이터 계약입니다. 승인된 필드 rename은 기존 값을 유지한 채 해당 prefab/scene YAML key만 직접 migration하고 실제 자산을 검증하세요. 새 `FormerlySerializedAs`를 추가하지 말고, 이미 존재하는 legacy attribute는 호환 이력으로 보존합니다.
- 기존 구현 파일과 패키지 구현을 동시에 두면 global 타입 중복으로 컴파일되지 않습니다. 기존 프로젝트 전환은 원본 제거와 패키지 추가를 같은 변경에서 수행하고, GUID를 재생성하지 마세요.

패키지를 적용한 뒤 prefab/scene에 `Missing Script`가 없는지 확인하고, 필요하면 **Tools > Package > UI Foundation > Migrate Component Refs**를 실행해 `_image`, `_button`, `_inputField`, `_scrollRect` 캐시를 채우세요.

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

`DOTWEEN`을 정의하려면 소비 프로젝트에 호환되는 DOTween core가 설치되어 있고, 그 precompiled assembly가 `com.actionfit.ui.foundation` asmdef에서 auto-reference 가능한 상태여야 합니다. DOTween을 별도 asmdef 소스로 구성했다면 현재 1.0.0 asmdef에는 해당 참조가 없으므로 심벌만 추가해서는 안 됩니다.

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

## TMP outline/shadow와 Player 빌드

`OutlineMaterialCache`는 `Shader.Find("TextMeshPro/Mobile/Distance Field Shadow Outline")`로 shader를 찾고 동일 설정의 Material을 공유합니다. 이 shader는 Foundation에 포함되지 않으므로 먼저 TMP Essential Resources를 import해야 합니다. `Acquire`로 얻은 Material은 같은 `Config`로 `Release`해야 하며, 더 이상 복제 Material을 사용하지 않는 수명 경계에서는 `Clear`를 호출할 수 있습니다.

Editor에서는 보이더라도 Player shader stripping으로 `Shader.Find`가 실패할 수 있습니다. 대상 플랫폼 빌드에서 outline/underlay를 확인하고, 필요하면 실제 Material asset 참조, Preloaded Assets 또는 **Project Settings > Graphics > Always Included Shaders** 등 프로젝트 정책에 맞는 방법으로 shader를 보존하세요.

## Unity 메뉴

- **Tools > Package > UI Foundation > README**: 이 문서를 엽니다.
- **Tools > Package > UI Foundation > Migrate Component Refs**: 프로젝트의 모든 prefab과 scene을 순회해 wrapper의 직렬화 component cache를 채우고 변경 자산을 저장합니다.

Migration 메뉴는 다수 자산을 수정하고 scene을 차례로 열기 때문에 실행 전에 작업을 commit하거나 백업하고, 실행 후 diff를 반드시 검토하세요.

## 검증

1.0.0은 `Tests/Runtime`의 `com.actionfit.ui.foundation.Runtime.Tests`와 `Tests/Editor`의 `com.actionfit.ui.foundation.Editor.Tests`를 포함합니다. 두 어셈블리 모두 `autoReferenced: false`, `UNITY_INCLUDE_TESTS` 조건이며 Unity Test Framework의 `TestAssemblies` 참조를 사용합니다. **Window > General > Test Runner**에서 실행하세요.

현재 자동 테스트 범위는 다음과 같습니다.

- `UIScriptIdentityTests`: 기존 runtime/editor script GUID, package 경로, public type, assembly identity
- `UIEaseCompatibilityTests`: enum 이름/숫자값, endpoint 안정성, unsupported/unknown 값의 linear fallback
- `ImageSliceMeshTests`: 네 fill 방향, `fillCenter`, tiny fill/zero rect, border가 rect보다 큰 경우의 유효 mesh
- `UIRuntimeContractTests`와 `UIWrapperBehaviorTests`: runtime assembly identity 및 Image/Text/Button/Scroll/Mask의 기본 wrapper 계약

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
