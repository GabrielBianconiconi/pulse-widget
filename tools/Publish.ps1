param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts/publish"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $root $Output

dotnet publish (Join-Path $root "PulseWidget.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $outputPath

if ($env:PULSE_SIGN_CERT_SHA1) {
    $signTool = Get-Command signtool.exe -ErrorAction Stop
    & $signTool.Source sign `
        /sha1 $env:PULSE_SIGN_CERT_SHA1 `
        /fd SHA256 `
        /td SHA256 `
        /tr "http://timestamp.digicert.com" `
        (Join-Path $outputPath "PulseWidget.exe")
}

Write-Output "Publicado em $outputPath"
