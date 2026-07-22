param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectRoot)
$publishDir = Join-Path $projectRoot "publish\win-x64"
$artifactDir = Join-Path $repoRoot "artifacts"

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

dotnet publish (Join-Path $projectRoot "CAPETF.Desktop.csproj") `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -o $publishDir

$innoCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ }

if ($innoCandidates.Count -gt 0) {
    & $innoCandidates[0] (Join-Path $projectRoot "installer\CAPETF.iss")
    Write-Host "Installer created in $artifactDir"
} else {
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath (Join-Path $artifactDir "CAPETF-Realtime-win-x64.zip") -Force
    Write-Host "Inno Setup not found. Portable ZIP created in $artifactDir"
}
