# AI Guide - UI Foundation

This document is shipped with the package so AI assistants in consuming projects can understand the actual structure and compatibility contracts of `com.actionfit.ui.foundation` 2.0.3 without the source project's `Docs/AI`.

## Package Identity

- Package ID: `com.actionfit.ui.foundation`
- Display name: `UI Foundation`
- Current package version at generation time: `2.0.3`
- Minimum Unity: `6000.2`
- Public repository: `https://github.com/ActionFit-Editor/UI_Foundation.git`
- Human guide: `Packages/com.actionfit.ui.foundation/README.md`
- Third-party notice: `Packages/com.actionfit.ui.foundation/Third Party Notices.md`

## Project Router Registration

Requested router entry:

- `Packages/com.actionfit.ui.foundation/AI_GUIDE.md` - UI Foundation owns reusable global UGUI wrappers, serialization/GUID compatibility, project button service contracts, optional animation integration, and sliced-fill behavior. Read when changing its runtime, editors, tests, metadata, migration, or release behavior.

Use `package.json` as the source of truth for the package ID, version, Unity version, and hard dependencies. `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` is the ActionFit catalog source for repository visibility, owner, status, description, and release notes.

## Agent Skills

- `Skills~/manifest.json` registers schema v2 `ui-foundation-help` and `ui-foundation-audit` for Codex and Claude with read-only access.
- Help reads generated `PACKAGE_SKILLS.md` before explaining global wrappers, GUID and serialization compatibility, project services, optional animation, menus, tests, and safety boundaries.
- Audit inspects asmdefs, `.meta` GUIDs, serialized-field and enum contracts, identity tests, and project service adapters without invoking Unity, running migration, saving or reserializing assets, changing symbols, or editing files. It compares Git status before and after inspection.
- Neither skill creates settings or providers, changes runtime behavior, publishes, tags, or updates the catalog.

## Read This Guide When

- Change files under `Packages/com.actionfit.ui.foundation/`.
- Diagnose `UI_Text`, `UI_Button`, `Image_Slice`, or mask/scroll animation behavior in a consuming project.
- Move existing `Assets` sources into the package or review prefab/scene compatibility.
- Handle DOTween symbol configuration, provider adapters, or shader stripping.
- Prepare package metadata or a release.

## Actual 2.0.3 Layout

- `Runtime/com.actionfit.ui.foundation.asmdef`
  - assembly name: `com.actionfit.ui.foundation`
  - empty `rootNamespace`
  - references: `UnityEngine.UI`, `Unity.TextMeshPro`, `Unity.Localization`
  - `autoReferenced: true`
- `Editor/com.actionfit.ui.foundation.Editor.asmdef`
  - Editor only
  - references Runtime, `UnityEngine.UI`, `UnityEditor.UI`, `Unity.TextMeshPro`
- `Runtime/`
  - `AssemblyInfo.cs` grants editor and editor-test assemblies internal access to editor-preview boundaries without adding a runtime-to-Editor dependency
  - global wrapper types: `UI_Rect`, `UI_Image`, `UI_ImageSlice`, `UI_Input`, `UI_InputBtn`, `UI_Scroll`
  - text/localization: `UI_Text`, inline TMP Sprite Asset tags, Sprite-based runtime asset generation/cache, `ILocaleRefreshable`, `UILocalizationRefreshHub`
  - button/provider: direct pointer-driven `UI_Button`, standalone-compatible `UIButtonPressEffect`, `IUIButtonClickSoundPlayer`, `IUIButtonTheme`, `UIButtonServices`
  - mask: `UI_MaskBase`, `UI_Mask`, `UI_Mask2D`
  - sliced fill: `Image_Slice`
  - utilities: `UIEase`, `UIEaseUtility`, `UIAnimationUtility`, `OutlineMaterialCache`, `RuntimeSpriteAssetCache`, inspector attributes
- `Editor/Scripts/`
  - custom inspectors/drawers
  - `UI_TextEditorPreviewCoordinator` owns delayed, event-driven Sprite/Face/Outline/Underlay preview refresh and cleanup
  - `UIFoundationPackageMenu`
  - `UIComponentRefsMigrator`
  - `UI_ButtonLegacyMigrator`
- `Tests/Editor/com.actionfit.ui.foundation.Editor.Tests.asmdef`
  - Editor-only EditMode tests
  - references Runtime, Editor and `UnityEngine.UI`
  - `autoReferenced: false`
  - `UNITY_INCLUDE_TESTS` constraint and `TestAssemblies` optional reference
  - `RuntimeSpriteAssetCacheTests` covers Sprite defaults, TMP glyph mapping, shared reference counting, and original Sprite Asset restoration
  - `UI_TextEditorPreviewTests` covers request coalescing, active and inactive targets, Prefab Mode reopen, Undo/Redo, Sprite/Material cleanup, non-serialization, and dirty-state preservation
- `Tests/Runtime/com.actionfit.ui.foundation.Runtime.Tests.asmdef`
  - platform-neutral runtime contract tests
  - references Runtime, `UnityEngine.UI`, `Unity.TextMeshPro`, and `Unity.Localization`
  - `autoReferenced: false`
  - `UNITY_INCLUDE_TESTS` constraint and `TestAssemblies` optional reference

Do not invent a settings ScriptableObject or `Setting SO` menu: this package does not own one in 2.0.3.

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

## Localization Refresh Contract

- A consumer that owns locale-dependent text outside `UI_Text` may implement `ILocaleRefreshable.RefreshLocalization()` and register once through `UILocalizationRefreshHub.Register(this)` during its initialization or active lifetime.
- `Register` prevents duplicate object registration. `RefreshAll()` removes destroyed Unity objects before invoking the remaining targets, so the `ILocaleRefreshable` object route has no explicit unregister API.
- `RefreshLocalization()` reapplies the current locale's text. A consumer that already reapplies text on every display or frame does not need a separate hub registration.
- `UILocalizationRefreshHub.OnRegister(Action)` is a separate callback route. Pair it with `OnUnregister(Action)` at the same active lifetime because callback delegates are not removed by destroyed-object cleanup.
- `UI_Text.SetLocalizeKey(table, entry)` enables localization, applies the value immediately, and registers the component. Do not add a second registration path around it.
- `UI_Text.SetLocalizeArguments(params object[])` stores runtime format arguments and immediately reapplies the current locale when the component already has a valid localized reference. It does not enable localization or change the key, so use the Inspector-authored reference or `SetLocalizeKey` as the owner of that configuration.

These are public owner contracts. A consuming project decides which game-specific string source or presenter needs them; this package does not name or depend on that source.

## Component Reference Migration

`Tools/Package/UI Foundation/Migrate Component Refs` scans all prefab and scene assets and fills these hidden serialized caches:

- `UI_Image._image`
- `UI_Input._inputField`
- `UI_Scroll._scrollRect`

The command saves modified prefabs/scenes and opens scenes sequentially. It is a broad write operation: require a clean/committed consumer worktree, warn the user, run once, and review the resulting asset/YAML diff. Do not run it as an unannounced diagnostic step.

## `UI_Button` Pointer and Migration Contract

`UI_Button` 2.0.3 has no `[RequireComponent(typeof(Button))]`, cached native `Button`, or public `UI_Button.Button` accessor. It owns serialized `m_Interactable`, `m_OnClick`, and integrated press-effect configuration. It directly implements enter, exit, down, up, and click pointer handlers; only the left button is accepted. Active/enabled state and the same parent `CanvasGroup` interaction/raycast/`ignoreParentGroups` traversal expected by UGUI gate invocation.

Keep `AddListener`, `RemoveListener`, `RemoveAllListeners`, `SetDisable`, `SetEnable`, `SetInteractable`, `IsDisabled`, click sound, disable visuals, and enable animation behavior compatible. The integrated press effect must preserve exit/re-enter/release/cancellation and must use its own DOTween ID or fallback cancellation owner. Keyboard/gamepad submit and UGUI Navigation parity are deliberately outside this pointer-only contract.

The standalone `UIButtonPressEffect` type, script GUID, and behavior remain for non-`UI_Button` consumers. Do not auto-add it to new `UI_Button` objects.

Use `Tools/Package/UI Foundation/Preview Legacy UI_Button Migration` first. `Apply Legacy UI_Button Migration...` requires an interactive confirmation; batch apply requires both `UI_ButtonLegacyMigrator.ApplyBatch` and `-uiButtonMigrationApply`. Migration copies native `Button.m_Interactable`, persistent `m_OnClick`, and same-GameObject `UIButtonPressEffect` enable/configuration into `UI_Button`, removes only those legacy components, preserves prefab inheritance and scene overrides, reports per-asset failures without saving failed assets, verifies zero remaining candidates, and is idempotent.

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

Five runtime areas use conditional DOTween integration:

- `Runtime/Button/UI_Button.EnableAnimation.cs`
- `Runtime/Button/UI_Button.PressEffect.cs`
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

## `UI_Text` Inline Sprite Contract

`Runtime/Text/UI_Text.Sprite.cs` remains a separate partial. It exposes the attached TMP component's existing `spriteAsset` through `SpriteAsset` and `SetSpriteAsset` without serializing a duplicate `TMP_SpriteAsset` reference. The opt-in `isSpriteAsset` field instead serializes one project-owned `Sprite` and `RuntimeSpriteGlyphSettings` for temporary asset generation.

`SetTextWithSprite(prefix, spriteName, suffix, tint)` enables TMP Rich Text and routes the final string through the existing `Text` property so resize behavior remains intact. `BuildSpriteTag(string)` produces a name-based tag, while `BuildSpriteTag(int)` is available for index-based callers. Prefer names because Sprite Asset index order can change.

The consuming project owns every `TMP_SpriteAsset`, source texture, atlas, Resources/Addressables decision, and visual glyph metric. Foundation must not bundle game icons, resolve project paths, or introduce automatic asset loading for this feature. Existing localization strings containing TMP `<sprite>` tags continue to pass through unchanged.

When `isSpriteAsset` is enabled, the Inspector exposes automatic or overridden glyph rect `X/Y/W/H` and metrics `W/H/BX/BY/AD/Scale`. Reset derives safe defaults from the Sprite texture rect, `Sprite.rect`, and `Sprite.pivot`. Single-mode Sprites are supported; rotated or tightly packed atlas entries are rejected. Text authoring remains explicit through `<sprite=0>` or `BuildSpriteTag(0)`.

`RuntimeSpriteAssetCache` builds one `TMP_SpriteAsset`, `TMP_SpriteGlyph`, `TMP_SpriteCharacter`, and Sprite Material per resolved Sprite/configuration/material-template key. It updates lookup tables before assigning the Material to avoid TMP legacy asset-upgrade persistence, reference-counts matching users, restores the previous `TMP_Text.spriteAsset`, and destroys generated objects after the final release. Acquire occurs before Localization application. `SubsystemRegistration` clears stale static state for domain-reload-disabled Play Mode.

## `UI_Text` Editor Preview Lifecycle

`UI_Text` stores Sprite, Face, Outline, and Underlay settings. Newly added components default Outline Width to `0.1` and Underlay Offset Y to `-0.5`; disabled toggles keep those authored values inactive. Sprite preview uses a local `HideFlags.HideAndDontSave` `TMP_SpriteAsset` and Material, while Face/Outline/Underlay continue using a local `HideFlags.DontSave` Material. Preview objects are not prefab or scene assets and must never be serialized into TMP `m_spriteAsset`, `m_sharedMaterial`, or `m_fontMaterial` fields.

`UI_TextEditorPreviewCoordinator` lives in the Editor assembly and owns event subscription, delayed scheduling, target filtering, request coalescing, bounded initialization retry, and cleanup. It refreshes after editor initialization, scene open, Prefab Stage open, Inspector Sprite/glyph/material-property changes, Undo/Redo, and Edit Mode re-entry. It restores previews before assembly reload, Prefab Stage close, and Play Mode entry.

The Runtime assembly exposes only internal editor-preview apply/restore boundaries through `InternalsVisibleTo`; it does not reference `UnityEditor`, rename serialized fields, or change the Player-facing runtime API. `OnValidate()` must not create or assign Materials. Prefab Mode scans are limited to the current `prefabContentsRoot`, including inactive descendants. Main-stage scans include only loaded, non-persistent scene objects. There is no per-frame global scan and no `[ExecuteAlways]` component; while Sprite previews exist, one lightweight Editor update callback checks only the tracked preview owners so component removal cannot orphan a temporary asset.

Preview refresh must not mark a prefab or scene dirty, clear an existing dirty state, save an asset, or reserialize YAML. Closing a stage, disabling or destroying a component, reloading assemblies, or entering Play Mode must restore the original `spriteAsset` and `fontSharedMaterial` before destroying preview objects. Keep this editor lifecycle separate from Player `RuntimeSpriteAssetCache` and `OutlineMaterialCache` ownership.

## `OutlineMaterialCache` and Shader Stripping

`OutlineMaterialCache` locates this shader by name:

`TextMeshPro/Mobile/Distance Field Shadow Outline`

The package does not bundle TMP font, material, or shader assets. In a fresh consuming project, use **Window > TextMeshPro > Import TMP Essential Resources**, then assign project-owned font assets before relying on text/material effects.

`OutlineMaterialCache` uses `Shader.Find`, so Editor success does not guarantee Player availability after shader stripping. Validate an actual target Player build. If the shader is stripped, retain it through an actual Material asset reference, Preloaded Assets, or the project's approved Graphics **Always Included Shaders** policy.

`Acquire(in Config)` increments a shared material reference count; callers must call `Release(in Config)` with the same resolved config. `Clear()` destroys package-created clones and empties all caches/pools; invoke it only at a lifetime boundary where no active text still expects those materials.

Do not silently replace the shader name or keyword/property IDs. Such a change needs visual regression checks for face, outline, underlay, disabled text darkening, batching and Player stripping.

`RuntimeSpriteAssetCache` prefers a valid TMP default Sprite Material, then the previous Sprite Asset Material, and otherwise resolves `TextMeshPro/Sprite` by name. The package still bundles no shader or Material asset. Player validation must prove that the Sprite shader is retained; a project-owned Material reference or approved Graphics retention policy remains the consuming project's responsibility.

## Menus

- `Tools/Package/UI Foundation/README`: opens the packaged README.
- `Tools/Package/UI Foundation/Migrate Component Refs`: performs the broad serialized-reference migration described above.
- `Tools/Package/UI Foundation/Preview Legacy UI_Button Migration`: read-only report for legacy native Button/press-effect pairs.
- `Tools/Package/UI Foundation/Apply Legacy UI_Button Migration...`: preview, explicit confirmation, write, and verification flow.

Keep new package commands under `Tools/Package/UI Foundation/`. There is no settings asset/menu in 2.0.3.

## Test and Validation Gate

Run `com.actionfit.ui.foundation.Runtime.Tests` and `com.actionfit.ui.foundation.Editor.Tests` in Unity Test Runner. The shipped test fixtures cover:

- `UIScriptIdentityTests`: preserved script GUID -> path, type and assembly mappings, plus fixed GUIDs for new runtime utilities
- `UIEaseCompatibilityTests`: stable enum names/numeric slots, finite endpoints and linear fallbacks
- `ImageSliceMeshTests`: four directions, `fillCenter`, tiny fill/zero rect and oversized-border geometry
- `UIRuntimeContractTests` and `UIWrapperBehaviorTests`: runtime assembly identity, baseline Image/Text/Button/Scroll/Mask behavior, and inline `UI_Text` sprite tags
- `UI_ButtonLegacyMigratorTests`: exact legacy field/event copy, prefab variant and scene overrides, idempotence, and missing-script checks
- `RuntimeSpriteAssetCacheTests`: Sprite-derived defaults, glyph/character tables, cache reuse/reference count, and original asset restoration
- `UI_TextEditorPreviewTests`: delayed request coalescing, active/inactive targets, Prefab Stage reopen, Undo/Redo, Sprite/Material preview cleanup, YAML non-serialization, and unchanged scene/prefab dirty state

Do not report these tests as passed when only script compilation was checked. They also do not replace the following integration/manual gates:

1. Runtime and Editor compilation with `DOTWEEN` undefined.
2. Runtime and Editor compilation with a compatible DOTween core supplied and `DOTWEEN` defined.
3. Existing representative prefab/scene import with no missing scripts and no serialized-value loss.
4. Component-ref and legacy `UI_Button` migrator preview/apply diff review on a disposable/clean consumer branch.
5. Real-sprite `Image_Slice` visual checks for padding, PPU, `overrideSprite`, tint and base-Image fallback.
6. Localization refresh after locale changes.
7. `UIButtonServices` with providers absent and registered.
8. Pointer/CanvasGroup gating and integrated press exit/re-entry/release/cancellation.
9. Target Player verification for TMP outline/underlay shader retention.

Editor-preview tests do not authorize saving representative consumer prefabs or scenes. Compare Git and YAML state before and after manual visual checks and preserve unrelated dirty assets.

Keep future tests in separate test asmdefs and test-only dependencies out of the Runtime assembly. When public/serialized identity, mesh behavior or `UIEase` changes intentionally, update the corresponding fixture in the same versioned change rather than weakening it.

## Editing and Release Rules

- Keep `README.md` human-focused and this guide architecture/constraint-focused.
- Update both documents when setup, public API, dependency, compatibility or validation behavior changes.
- Preserve `Third Party Notices.md` and its `.meta` in releases.
- Treat published Git tags as immutable. Check the remote before every release and prepare the next unused patch version instead of retagging changed content.
- Publishing is manual through ActionFit Custom Package Manager.
- `ActionFitPackageInfo_SO.ReleaseNote` is Korean, contains one version only, and should not embed a duplicate version heading.
- Register this guide in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md` when that router exists in a consuming project.
