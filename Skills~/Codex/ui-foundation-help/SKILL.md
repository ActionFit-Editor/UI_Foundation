---
name: ui-foundation-help
description: Explain UI Foundation, its installed skills, global UGUI wrappers, GUID and serialization compatibility, project services, optional animation, menus, tests, and safety boundaries. Use when a user asks about package usage, compatibility, or skills.
---

# UI Foundation Help

Answer in the user's language. Explain the package without opening migration tools, changing scenes or prefabs, or modifying project and package state.

1. Read `PACKAGE_SKILLS.md` first. Treat its generated package identity, related-skill table, `$skill-name` invocations, descriptions, and access boundaries as authoritative.
2. Read `Packages/com.actionfit.ui.foundation/README.md` and `Packages/com.actionfit.ui.foundation/AI_GUIDE.md` when present. If downloaded, resolve `Library/PackageCache/com.actionfit.ui.foundation@*` without editing it.
3. Explain the global UGUI wrapper types, preserved MonoScript GUIDs and serialized fields, empty runtime `rootNamespace`, explicit enum values, `UIButtonServices` provider boundary, optional `DOTWEEN` compilation, `Image_Slice`, localization, and TMP shader-retention considerations.
4. Keep game audio, art, Resources paths, project adapters, migration approval, and prefab/scene ownership in the consuming project. Absence of `UIButtonServices` providers is a supported no-op/null state.
5. Distinguish the read-only `README` menu from `Tools > Package > UI Foundation > Migrate Component Refs`, which is a broad write operation requiring separate approval, a clean branch, and diff review. Identify the runtime and editor test assemblies without claiming that compile-only validation ran them.
6. State that help and audit must not run the migrator; save, reserialize, rename, move, or delete assets; regenerate GUIDs; edit serialized fields; create providers or settings; change scripting symbols; run Unity; publish; tag; or update the package catalog.
