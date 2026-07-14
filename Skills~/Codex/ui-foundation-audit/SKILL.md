---
name: ui-foundation-audit
description: Audit UI Foundation GUIDs, global type and serialized-field compatibility, assembly contracts, and project service boundaries without migrating or changing scenes, prefabs, assets, or settings. Use when reviewing package changes or consuming-project integration.
---

# Audit UI Foundation

Keep the audit read-only. Do not invoke Unity, run `Migrate Component Refs`, save or reserialize assets, regenerate GUIDs, change scripting symbols, edit files, instantiate UI, or publish package state.

1. Read the repository instructions so project routing, serialized-data, and safety rules apply before inspection.
2. From the repository root, capture `git status --short --untracked-files=all` as the audit baseline and preserve all pre-existing changes.
3. Resolve the physical package root from `Packages/com.actionfit.ui.foundation`; otherwise use `Library/PackageCache/com.actionfit.ui.foundation@*` without editing it. Read the package `README.md`, `AI_GUIDE.md`, third-party notice, and the consuming project's UI architecture and provider adapter when present.
4. Use `rg`, `git`, and read-only file inspection to trace runtime/editor asmdefs, global public wrappers, `.cs.meta` GUIDs, serialized fields, explicit enum values, `UIScriptIdentityTests`, `UIButtonServices`, optional `DOTWEEN` regions, migration code, and package tests. Do not load or save assets through Unity.
5. Verify and report evidence for these contracts:
   - Runtime wrappers remain in the global namespace, the runtime asmdef keeps an empty `rootNamespace` and `autoReferenced: true`, and consumer asmdefs still require an explicit reference.
   - Script GUID expectations in `UIScriptIdentityTests` match the corresponding `.meta` files and no package source duplicates a preserved public type.
   - Serialized field names and explicit enum values remain stable; proposed changes include an approved asset migration and real prefab/scene validation plan.
   - `UIButtonServices` owns only provider contracts and reset/registration points, while game audio, Sprite paths, art, and project lifecycle wiring remain outside the package.
   - Optional `DOTWEEN` code has no bundled dependency and undefined-symbol fallback paths remain available.
   - The component-reference migrator is treated as a separate broad write operation and is not executed during audit.
6. Inspect dependencies, third-party notice coverage, test assemblies, and evidence for runtime/editor tests. Treat compile-only results separately from actual EditMode test execution.
7. Capture the same Git status command again and compare it with the baseline. If state changed during the audit, report the paths and do not claim a no-change result.
8. Return findings grouped as passed contracts, risks, missing evidence, and recommended validation. Report suggested Unity tests and representative prefab/scene checks as follow-up work without running or modifying them.
