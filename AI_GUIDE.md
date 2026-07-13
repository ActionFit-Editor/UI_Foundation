# AI Guide - UI Foundation

This document is shipped with the package so AI assistants in consuming projects can understand the actual structure and compatibility contracts of `com.actionfit.ui.foundation` 1.0.0 without the source project's `Docs/AI`.

## Package Identity

- Package ID: `com.actionfit.ui.foundation`
- Display name: `UI Foundation`
- Current package version at generation time: `1.0.0`
- Minimum Unity: `6000.2`
- Public repository: `https://github.com/ActionFit-Editor/UI_Foundation.git`
- Human guide: `Packages/com.actionfit.ui.foundation/README.md`
- Third-party notice: `Packages/com.actionfit.ui.foundation/Third Party Notices.md`

## Project Router Registration

Requested router entry:

- `Packages/com.actionfit.ui.foundation/AI_GUIDE.md` - UI Foundation owns reusable global UGUI wrappers, serialization/GUID compatibility, project button service contracts, optional animation integration, and sliced-fill behavior. Read when changing its runtime, editors, tests, metadata, migration, or release behavior.

Use `package.json` as the source of truth for the package ID, version, Unity version, and hard dependencies. `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` is the ActionFit catalog source for repository visibility, owner, status, description, and release notes.

## Read This Guide When

- Change files under `Packages/com.actionfit.ui.foundation/`.
- Diagnose `UI_Text`, `UI_Button`, `Image_Slice`, or mask/scroll animation behavior in a consuming project.
- Move existing `Assets` sources into the package or review prefab/scene compatibility.
- Handle DOTween symbol configuration, provider adapters, or shader stripping.
- Prepare package metadata or a release.

## Actual 1.0.0 Layout

- `Runtime/com.actionfit.ui.foundation.asmdef`
  - assembly name: `com.actionfit.ui.foundation`
  - empty `rootNamespace`
  - references: `UnityEngine.UI`, `Unity.TextMeshPro`, `Unity.Localization`
  - `autoReferenced: true`
- `Editor/com.actionfit.ui.foundation.Editor.asmdef`
  - Editor only
  - references Runtime, `UnityEngine.UI`, `UnityEditor.UI`, `Unity.TextMeshPro`
- `Runtime/`
  - global wrapper types: `UI_Rect`, `UI_Image`, `UI_ImageSlice`, `UI_Input`, `UI_InputBtn`, `UI_Scroll`
  - text/localization: `UI_Text`, `ILocaleRefreshable`, `UILocalizationRefreshHub`
  - button/provider: `UI_Button`, `UIButtonPressEffect`, `IUIButtonClickSoundPlayer`, `IUIButtonTheme`, `UIButtonServices`
  - mask: `UI_MaskBase`, `UI_Mask`, `UI_Mask2D`
  - sliced fill: `Image_Slice`
  - utilities: `UIEase`, `UIEaseUtility`, `UIAnimationUtility`, `OutlineMaterialCache`, inspector attributes
- `Editor/Scripts/`
  - custom inspectors/drawers
  - `UIFoundationPackageMenu`
  - `UIComponentRefsMigrator`
- `Tests/Editor/com.actionfit.ui.foundation.Editor.Tests.asmdef`
  - Editor-only EditMode tests
  - references Runtime, Editor and `UnityEngine.UI`
  - `autoReferenced: false`
  - `UNITY_INCLUDE_TESTS` constraint and `TestAssemblies` optional reference
- `Tests/Runtime/com.actionfit.ui.foundation.Runtime.Tests.asmdef`
  - platform-neutral runtime contract tests
  - `autoReferenced: false`
  - `UNITY_INCLUDE_TESTS` constraint and `TestAssemblies` optional reference

Do not invent a settings ScriptableObject or `Setting SO` menu: this package does not own one in 1.0.0.

## Hard Dependencies

`package.json` declares the following hard dependencies:

- `com.unity.ugui: 2.0.0`
- `com.unity.localization: 1.5.5`

The runtime source directly imports UGUI, TMP and Localization APIs. Do not make Localization optional merely by removing the manifest entry; that requires source/assembly separation and a versioned API decision.

DOTween is not a package dependency. No DOTween source, binary, module or license is bundled.

## Serialization and Script Compatibility Contract

The package was extracted from project-owned scripts while preserving existing scene/prefab references. Treat these as release-blocking invariants:

1. Public runtime types intentionally remain in the **global namespace**. Do not add namespaces or set an asmdef `rootNamespace` without a consumer-wide source migration.
2. Existing `.cs.meta` files and GUIDs identify MonoScripts referenced by prefab/scene YAML. Never regenerate them during a move or cleanup.
3. Public type names and serialized field names are persisted contracts. Do not rename serialized fields without explicit approval. For an approved rename, update only the matching YAML keys while preserving every serialized value, then validate real assets; do not add new `FormerlySerializedAs` attributes. Existing legacy attributes are compatibility history and must not be removed casually.
4. Preserve explicit enum numeric values when values may be serialized. In particular, do not reorder or renumber `UIEase`.
5. The Runtime asmdef is `autoReferenced: true` so predefined assemblies can see it. Consumer-owned asmdefs still require an explicit `com.actionfit.ui.foundation` reference.
6. Never leave the original global-type source and the package copy installed together. Duplicate definitions will fail compilation even if GUIDs match.

When moving or publishing, include every existing `.meta`, compare GUIDs before/after, import a real consuming project, then inspect prefab/scene YAML and the Unity Console for missing scripts or lost serialized values.

## Component Reference Migration

`Tools/Package/UI Foundation/Migrate Component Refs` scans all prefab and scene assets and fills these hidden serialized caches:

- `UI_Image._image`
- `UI_Button._button`
- `UI_Input._inputField`
- `UI_Scroll._scrollRect`

The command saves modified prefabs/scenes and opens scenes sequentially. It is a broad write operation: require a clean/committed consumer worktree, warn the user, run once, and review the resulting asset/YAML diff. Do not run it as an unannounced diagnostic step.

## Project Boundary: `UIButtonServices`

Foundation owns only contracts and a registration point:

- `IUIButtonClickSoundPlayer.PlayClickSound()`
- `IUIButtonTheme.GetButtonSprite(UI_Button.ButtonSprite)`
- `UIButtonServices.ClickSoundPlayer` and `UIButtonServices.Theme`

It must not reference a game's audio singleton, addressables catalog, Resources paths or art assets. With no provider, click sound is a no-op and theme lookup returns `null`; this is a supported standalone state.

`UIButtonServices` clears providers at `RuntimeInitializeLoadType.SubsystemRegistration`. Runtime adapters should register afterward, normally at `BeforeSceneLoad`. Editor preview adapters may also use `InitializeOnLoadMethod`.

The Cat Merge Cafe adapter is deliberately outside this package:

`Assets/_Project/_Common/UI/UI_Prefab.Extensions/CatMergeCafe/CatMergeCafeUIButtonServices.cs`

Only that consuming-project adapter may know `Main.Sound` and Cat-specific Sprite resource paths. If another game consumes Foundation, add its own adapter under that project's `Assets`, not under this package.

## Optional DOTween Compilation

Four runtime areas use conditional DOTween integration:

- `Runtime/Button/UI_Button.EnableAnimation.cs`
- `Runtime/UI_Scroll.cs`
- `Runtime/UI_MaskBase.Animation.cs`
- `Runtime/Util/UIButtonPressEffect.cs`

### `DOTWEEN` defined

- Sources import `DG.Tweening` and call core `DOTween.To`, `DOTween.Sequence`, and `DOTween.Kill` APIs.
- `UI_MaskBase` public animation methods return `DG.Tweening.Tween` and accept `DG.Tweening.Ease?`.
- `UI_Scroll.AnimateToTop(duration, ease, ...)` accepts `DG.Tweening.Ease`.
- Serialized animation ease fields use `DG.Tweening.Ease`.

### `DOTWEEN` not defined

- No `DG.Tweening` type is referenced.
- Animation runs through `UnityEngine.Awaitable`, `Awaitable<bool>` and `CancellationTokenSource`.
- `UI_MaskBase` methods return `UnityEngine.Awaitable<bool>` and accept `UIEase?` through local aliases.
- `UI_Scroll.AnimateToTop(duration, ease, ...)` accepts `UIEase`.
- Disable/re-entry replaces or cancels the prior operation.

### Integration assumptions and API differences

The package asmdef has no explicit `DG.Tweening` assembly reference. Define `DOTWEEN` only when a compatible consumer-supplied DOTween core precompiled assembly is visible through Unity's auto-reference behavior. If DOTween is delivered as a non-auto-referenced asmdef, a deliberate package integration/asmdef change is required; a scripting symbol alone will produce compile errors.

`UIEase` uses explicit values `0` through `35` aligned with DOTween `Ease` serialization slots. The built-in evaluator implements the standard named families through `InOutBounce` (`1` through `31`). `Unset` and the reserved Flash variants (`32` through `35`) fall back to linear. It has no configurable overshoot, amplitude or period and does not promise exact DOTween curve parity.

The fallback is behavioral continuity, not source/binary API compatibility. Consumer code intended to build in both modes must not assume a `DG.Tweening.Tween` return value or call DOTween extension methods on the fallback result. Validate compilation and cancellation/completion behavior in both symbol configurations.

## `Image_Slice` Contract

`Image_Slice : UnityEngine.UI.Image` overrides mesh generation only when all of these are true:

- `isSliceImage` / `IsSliceImage` is enabled
- `overrideSprite` resolves to a Sprite with non-zero 9-slice border
- Image `type == Image.Type.Filled`
- `fillMethod` is Horizontal or Vertical

Otherwise it delegates to the base `Image`, including all Radial modes.

Linear direction mapping is fixed:

- Horizontal + Left origin -> Right
- Horizontal + Right origin -> Left
- Vertical + Bottom origin -> Up
- Vertical + Top origin -> Down

The implementation intersects the fill interval with each 3x3 slice cell and clips both geometry and UVs. `fillCenter == false` skips only the center cell. Preserve support for `fillAmount`, `fillCenter`, `pixelsPerUnitMultiplier`, `overrideSprite`, color, border and padding when changing mesh code.

The custom inspector is `Editor/Scripts/Image_SliceEditor.cs`. `ImageSliceMeshTests` covers all four half-fill directions, `fillCenter`, tiny/zero geometry and a RectTransform smaller than its opposite borders. Any mesh change must keep those tests green and add visual checks for real Sprite padding, PPU, `overrideSprite`, tint and fallback modes.

The sliced+filled concept was informed by yasirkula's `SlicedFilledImage` gist. Keep `Third Party Notices.md` with the package and update it if provenance or implementation scope changes. Do not claim this implementation is a verbatim copy without source evidence.

## `OutlineMaterialCache` and Shader Stripping

`OutlineMaterialCache` locates this shader by name:

`TextMeshPro/Mobile/Distance Field Shadow Outline`

The package does not bundle TMP font, material, or shader assets. In a fresh consuming project, use **Window > TextMeshPro > Import TMP Essential Resources**, then assign project-owned font assets before relying on text/material effects.

`OutlineMaterialCache` uses `Shader.Find`, so Editor success does not guarantee Player availability after shader stripping. Validate an actual target Player build. If the shader is stripped, retain it through an actual Material asset reference, Preloaded Assets, or the project's approved Graphics **Always Included Shaders** policy.

`Acquire(in Config)` increments a shared material reference count; callers must call `Release(in Config)` with the same resolved config. `Clear()` destroys package-created clones and empties all caches/pools; invoke it only at a lifetime boundary where no active text still expects those materials.

Do not silently replace the shader name or keyword/property IDs. Such a change needs visual regression checks for face, outline, underlay, disabled text darkening, batching and Player stripping.

## Menus

- `Tools/Package/UI Foundation/README`: opens the packaged README.
- `Tools/Package/UI Foundation/Migrate Component Refs`: performs the broad serialized-reference migration described above.

Keep new package commands under `Tools/Package/UI Foundation/`. There is no settings asset/menu in 1.0.0.

## Test and Validation Gate

Run `com.actionfit.ui.foundation.Runtime.Tests` and `com.actionfit.ui.foundation.Editor.Tests` in Unity Test Runner. The shipped test fixtures cover:

- `UIScriptIdentityTests`: preserved script GUID -> path, type and assembly mappings, plus fixed GUIDs for new runtime utilities
- `UIEaseCompatibilityTests`: stable enum names/numeric slots, finite endpoints and linear fallbacks
- `ImageSliceMeshTests`: four directions, `fillCenter`, tiny fill/zero rect and oversized-border geometry
- `UIRuntimeContractTests` and `UIWrapperBehaviorTests`: runtime assembly identity and baseline Image/Text/Button/Scroll/Mask behavior

Do not report these tests as passed when only script compilation was checked. They also do not replace the following integration/manual gates:

1. Runtime and Editor compilation with `DOTWEEN` undefined.
2. Runtime and Editor compilation with a compatible DOTween core supplied and `DOTWEEN` defined.
3. Existing representative prefab/scene import with no missing scripts and no serialized-value loss.
4. Component-ref migrator diff review on a disposable/clean consumer branch.
5. Real-sprite `Image_Slice` visual checks for padding, PPU, `overrideSprite`, tint and base-Image fallback.
6. Localization refresh after locale changes.
7. `UIButtonServices` with providers absent and registered.
8. Target Player verification for TMP outline/underlay shader retention.

Keep future tests in separate test asmdefs and test-only dependencies out of the Runtime assembly. When public/serialized identity, mesh behavior or `UIEase` changes intentionally, update the corresponding fixture in the same versioned change rather than weakening it.

## Editing and Release Rules

- Keep `README.md` human-focused and this guide architecture/constraint-focused.
- Update both documents when setup, public API, dependency, compatibility or validation behavior changes.
- Preserve `Third Party Notices.md` and its `.meta` in releases.
- Treat published Git tags as immutable. If `1.0.0` already exists remotely, prepare the next unused patch version instead of retagging changed content.
- Publishing is manual through ActionFit Custom Package Manager.
- `ActionFitPackageInfo_SO.ReleaseNote` is Korean, contains one version only, and should not embed a duplicate version heading.
- Register this guide in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md` when that router exists in a consuming project.
