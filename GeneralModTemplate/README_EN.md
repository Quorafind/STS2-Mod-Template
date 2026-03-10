# General Mod Template - Guide

## Quick Start

1. Copy this entire folder to `mods_src/YourModName/`
2. Global find-and-replace:
   - `MyMod` -> `YourModName` (class/file names, manifest)
   - `MyModNamespace` -> `YourNamespace`
   - `yourname.mymod` -> `yourname.yourmod` (Harmony ID)
3. Rename `MyMod.csproj` -> `YourModName.csproj`
4. Update paths after moving the template out of `mod_templates/`:
   - `.csproj` HintPath: `..\..\..\` -> `..\..\`
   - `build.ps1` `$repoRoot`: `"..\..\..\"` -> `"..\..\"`
   - `build.ps1` `$modName = "YourModName"`
5. Update `mod_manifest.json` with your mod info
6. Run: `powershell -ExecutionPolicy Bypass -File build.ps1`

## Project Structure

```
YourMod/
  ├── YourMod.csproj           # .NET 9.0 project file
  ├── build.ps1                 # Build script (Windows)
  ├── mod_manifest.json         # Mod metadata
  ├── MyModBootstrap.cs         # Entry point
  ├── MyModPatches.cs           # Harmony patches (your mod logic)
  └── assets/YourMod/           # Localization + images
      └── localization/{eng,zhs}/
```

## How Mods Work

### Entry Point
The game scans all loaded DLLs for classes with `[ModInitializer(methodName)]`.
It calls the named static method at startup. This is where you:
1. Apply Harmony patches via `PatchAll()`
2. Initialize any mod state

### Harmony Patching
Harmony is the core mechanism for modifying game behavior. Key concepts:

| Patch Type | When | Use Case |
|------------|------|----------|
| `Prefix` | Before original | Modify args, skip original (`return false`) |
| `Postfix` | After original | Modify return value, add behavior |
| `Transpiler` | Rewrite IL | Advanced: modify method body |

```csharp
[HarmonyPatch(typeof(GameClass), nameof(GameClass.Method))]
internal static class MyPatch
{
    // Prefix: runs before. Return false to skip original.
    static bool Prefix(GameClass __instance, ref int arg1) { ... }

    // Postfix: runs after. Modify __result to change return value.
    static void Postfix(ref ReturnType __result) { ... }
}
```

### Accessing Private Members
```csharp
// Field reference (cached, fast):
static readonly AccessTools.FieldRef<Type, FieldType> Ref =
    AccessTools.FieldRefAccess<Type, FieldType>("_fieldName");
// Usage: FieldType val = Ref(instance);

// Method reference:
var method = AccessTools.Method(typeof(Type), "MethodName");
// Or: typeof(Type).GetMethod("Name", BindingFlags.NonPublic | BindingFlags.Instance);
```

### Game Architecture Overview

| Namespace | Contents |
|-----------|----------|
| `MegaCrit.Sts2.Core.Models` | Data models (cards, relics, powers, characters) |
| `MegaCrit.Sts2.Core.Models.Cards` | Base game card definitions |
| `MegaCrit.Sts2.Core.Models.Powers` | Power definitions (Strength, Dexterity, etc.) |
| `MegaCrit.Sts2.Core.Entities` | Runtime entities (Player, Creature, Card instances) |
| `MegaCrit.Sts2.Core.Commands` | Game actions (DamageCmd, CreatureCmd, CardPileCmd) |
| `MegaCrit.Sts2.Core.Nodes` | Godot scene nodes (NGame, NRun, NCard, etc.) |
| `MegaCrit.Sts2.Core.Runs` | Run state management |
| `MegaCrit.Sts2.Core.Context` | LocalContext - access current player/run |

### Useful Entry Points for Patching

| Class.Method | When Called | Common Use |
|-------------|------------|------------|
| `NGame._Input` | Every input event | Add hotkeys |
| `NRun._Process` | Every frame during run | Continuous effects |
| `CharacterModel.get_StartingHp` | Character setup | Modify stats |
| `CardModel.OnPlay` | Card played | Modify card effects |
| `PowerModel.AfterApplied` | Power gained | React to buffs/debuffs |

### PCK File
Every mod needs a `.pck` file (Godot resource pack) containing at minimum:
- `mod_manifest.json` - mod metadata
- Localization files (if any)
- Image/audio assets (if any)

The `pack_godot_pck.py` tool in `_tools/` handles PCK creation.

## Build Requirements
- .NET 9.0 SDK
- Python 3.x (for PCK packing)
- Game DLLs in `data_sts2_windows_x86_64/` (auto-referenced)

## Cross-Platform Support

The project uses `AnyCPU` platform target, so the compiled DLL works on both PC (x86_64) and mobile (ARM64 Android).
No separate builds needed — the same DLL runs on all platforms.

> **Note**: Do not change `PlatformTarget` to `x64` in the `.csproj`, or the mod will fail to load on mobile.
