# Character Mod Template

A complete template for creating custom playable characters with cards, relics, powers, card pools, and all required Harmony patches.

## Quick Start

1. Copy this folder to `mods_src/YourCharacterName/`
2. Find-and-replace:
    - `MyCharacter` → `YourCharacterName` (class names, file names)
    - `MyCharacterMod` → `YourNamespace`
    - `mycharacter` → `yourcharactername` (asset paths, lowercase)
    - `MY_CHARACTER` → `YOUR_CHARACTER_ID` (localization keys, uppercase snake case)
    - `my_character` → `your_character_id` (top-panel icon file names, lowercase snake case)
    - `yourname.mycharacter` → `yourname.yourmod` (Harmony ID)
3. Rename `MyCharacter.csproj` → `YourCharacterName.csproj`
4. Update paths:
   - `.csproj` HintPath: `..\..\..\` → `..\..\` (one less `..` since you moved up from mod_templates)
   - `build.ps1` `$repoRoot`: `"..\..\..\"` → `"..\..\"`
   - `build.ps1` `$modName = "YourCharacterName"`
5. Update `mod_manifest.json`
6. Rename localization dir: `assets/MyCharacter/` → `assets/YourCharacterName/`
7. Build: `powershell -ExecutionPolicy Bypass -File build.ps1`

## Key Concepts

### ModelDb Name Collision (Critical)
`ModelDb.GetId(type)` uses ONLY `type.Name` to generate IDs. If your class name matches a base game class, add `_P` suffix.

### Required Harmony Patches
All patches in `MyCharacterPatches.cs` are required for character mods to function. See the Chinese README.md for the complete patch table.

### STS1 Migration
If you're migrating from a STS1 mod (BaseMod + ModTheSpire), see `README.md` for a detailed migration guide with side-by-side comparisons of:
- Card implementation (`CustomCard` → `CardModel`)
- Power implementation (`AbstractPower` → `PowerModel`)
- Relic implementation (`CustomRelic` → `RelicModel`)
- Action queue (`addToBot` → `async/await`)
- Localization format (`!D!` → `{Damage:diff()}`)

## Build Requirements
- .NET 9.0 SDK
- Python 3.x (for PCK packing)
- Pillow (`pip install Pillow`) - optional, for placeholder images
- Game DLLs in `data_sts2_windows_x86_64/` (auto-referenced)

## Cross-Platform Support

The project uses `AnyCPU` platform target, so the compiled DLL works on both PC (x86_64) and mobile (ARM64 Android).
No separate builds needed — the same DLL runs on all platforms.

> **Note**: Do not change `PlatformTarget` to `x64` in the `.csproj`, or the mod will fail to load on mobile.
