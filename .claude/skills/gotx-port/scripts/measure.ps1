# One-shot fidelity measurement of a ported view: build -> screenshot (ShotProgram) -> pixel diff -> layout compare.
#   pwsh measure.ps1 -Project <shot.csproj> -Ref <capture prefix> [-View Type] [-Width 1440] [-Height 900] [-Theme light|dark]
#                    [-Full] [-Fonts dir] [-Out measure-out] [-ArtifactsPath dir] [-Top 15] [-PositionOnly]
# -Ref is the prefix of a capture-html.mjs result: <prefix>.png and <prefix>.json (e.g. cap\card-800-light). The width/theme in the prefix
# should equal -Width/-Theme (the script warns otherwise). -Project is a project made from templates/ShotProgram.cs.txt.
# Output in -Out: gotx-<w>-<theme>.png/.tree.json, diff.png (dimmed reference; magenta delta > 24, amber 1-24, blue only in one image).
# Needs: dotnet (DOTNET env var, else C:\Program Files\dotnet\dotnet.exe, else PATH), node (C:\Program Files\nodejs\node.exe, else PATH).
param(
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$Ref,
    [string]$View,
    [int]$Width = 1440,
    [int]$Height = 900,
    [ValidateSet('light', 'dark')][string]$Theme = 'light',
    [string]$Out = 'measure-out',
    [switch]$Full,
    [string]$Fonts,
    [string]$ArtifactsPath,
    [int]$Top = 15,
    [switch]$PositionOnly
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$dotnet = if ($env:DOTNET) { $env:DOTNET } elseif (Test-Path 'C:\Program Files\dotnet\dotnet.exe') { 'C:\Program Files\dotnet\dotnet.exe' } else { 'dotnet' }
$node = if (Test-Path 'C:\Program Files\nodejs\node.exe') { 'C:\Program Files\nodejs\node.exe' } else { 'node' }
$art = if ($ArtifactsPath) { @('--artifacts-path', $ArtifactsPath) } else { @() }
$out = [IO.Path]::GetFullPath($Out)
$refPng = "$Ref.png"; $refJson = "$Ref.json"
foreach ($f in $refPng, $refJson) { if (-not (Test-Path $f)) { throw "missing $f (capture it with capture-html.mjs)" } }
if ($Ref -notmatch "-$Width-$Theme$") { Write-Warning "-Ref '$Ref' does not end with -${Width}-${Theme}: check that reference and GotX use the same width/theme" }

Write-Host "== build $Project"
& $dotnet build $Project @art -v q --nologo | Select-String -Pattern 'error|warning CS|Warn|Error' | ForEach-Object { $_.Line }
if ($LASTEXITCODE -ne 0) { throw 'build failed' }

Write-Host "== shot"
$shot = @('--project', $Project) + $art + @('--no-build', '--', $out, '--width', $Width, '--height', $Height, '--theme', $Theme, '--name', 'gotx')
if ($View) { $shot += @('--view', $View) }
if ($Full) { $shot += '--full' }
if ($Fonts) { $shot += @('--fonts', $Fonts) }
& $dotnet run @shot
$shotExit = $LASTEXITCODE
if ($shotExit -eq 2) { throw 'shot failed' }   # 1 = unknown Tailwind classes (listed above), the images were still written
$gotxPng = Join-Path $out "gotx-$Width-$Theme.png"; $gotxTree = Join-Path $out "gotx-$Width-$Theme.tree.json"

Write-Host "== diff"
& $dotnet run --project (Join-Path $repo 'tools\GotX.Port') @art -v q -- diff $refPng $gotxPng (Join-Path $out 'diff.png')

Write-Host "== layout"
$cmp = @((Join-Path $PSScriptRoot 'compare-layout.mjs'), $refJson, $gotxTree, '--top', $Top)
if ($PositionOnly) { $cmp += '--position-only' }
& $node @cmp
if ($shotExit -eq 1) { Write-Warning 'unknown Tailwind classes were reported by the shot step (fix them first)' }
