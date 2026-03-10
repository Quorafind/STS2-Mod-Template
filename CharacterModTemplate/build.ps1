$ErrorActionPreference = "Stop"

# ============================================================================
# Character Mod Build Script (Windows only)
# Usage: powershell -ExecutionPolicy Bypass -File build.ps1
# ============================================================================

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $projectDir "..\..\..\")).Path
$modName = "MyCharacter"  # <-- Change this to your mod name
$project = Join-Path $projectDir "$modName.csproj"
$outputDir = Join-Path $repoRoot "mods\$modName"
$buildDir = Join-Path $projectDir "bin\Release\net9.0"
$pckRoot = Join-Path $outputDir "_pck_src"
$assetSourceDir = Join-Path $projectDir "assets"
$pckPath = Join-Path $outputDir "$modName.pck"

function Ensure-Dir {
  param([string]$Path)
  New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

Ensure-Dir $outputDir
Ensure-Dir $pckRoot

# Step 1: Build C# project
Write-Host "Building $modName..."
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
  throw "dotnet build failed with exit code $LASTEXITCODE"
}

# Step 2: Copy DLL + PDB to mod output
Copy-Item (Join-Path $buildDir "$modName.dll") (Join-Path $outputDir "$modName.dll") -Force
if (Test-Path (Join-Path $buildDir "$modName.pdb")) {
  Copy-Item (Join-Path $buildDir "$modName.pdb") (Join-Path $outputDir "$modName.pdb") -Force
}

# Step 3: Copy assets to PCK source directory
if (Test-Path $assetSourceDir) {
  Copy-Item (Join-Path $assetSourceDir "*") $pckRoot -Recurse -Force
}
Copy-Item (Join-Path $projectDir "mod_manifest.json") (Join-Path $pckRoot "mod_manifest.json") -Force

# Step 4 (optional): Generate placeholder images if they don't exist
$placeholderScript = Join-Path $projectDir "generate_placeholders.py"
if (Test-Path $placeholderScript) {
  python $placeholderScript $pckRoot
  if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: generate_placeholders.py failed (Pillow may not be installed). Continuing without placeholder images."
  }
}

# Step 5: Pack PCK
python (Join-Path $repoRoot "_tools\pack_godot_pck.py") `
  $pckRoot `
  -o $pckPath `
  --engine-version 4.5.1 `
  --pack-version 3
if ($LASTEXITCODE -ne 0) {
  throw "pack_godot_pck.py failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Build complete:"
Write-Host "  DLL: $outputDir\$modName.dll"
Write-Host "  PCK: $pckPath"
