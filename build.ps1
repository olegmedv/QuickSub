param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$NoTrimming = $false,
    [switch]$SafeMode = $false,
    [switch]$ConservativeMode = $false
)

Write-Host "========================================" -ForegroundColor Green
Write-Host "Building QuickSub into single exe file" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

$mode = if ($SafeMode) { "Safe (no trimming)" } 
        elseif ($ConservativeMode) { "Conservative (compression without trimming)" }
        elseif ($NoTrimming) { "Standard (no trimming)" }
        else { "Compact (soft trimming)" }

Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Mode: $mode" -ForegroundColor Yellow

Write-Host "`nCleaning previous builds..." -ForegroundColor Cyan
if (Test-Path "bin\$Configuration") {
    Remove-Item "bin\$Configuration" -Recurse -Force
}
if (Test-Path "obj\$Configuration") {
    Remove-Item "obj\$Configuration" -Recurse -Force
}

Write-Host "Building project..." -ForegroundColor Cyan

$publishArgs = @(
    "publish"
    "-c", $Configuration
    "-r", $Runtime
    "--self-contained", "true"
    "-p:PublishSingleFile=true"
    "-p:IncludeNativeLibrariesForSelfExtract=true"
    "-p:IncludeAllContentForSelfExtract=true"
)

if ($SafeMode) {
    # Safe build with full size
    $publishArgs += "-p:PublishTrimmed=false"
    $publishArgs += "-p:PublishReadyToRun=true"
} elseif ($ConservativeMode) {
    # Conservative build with compression but no trimming
    $publishArgs += "-p:PublishTrimmed=false"
    $publishArgs += "-p:PublishReadyToRun=false"
    $publishArgs += "-p:EnableCompressionInSingleFile=true"
    $publishArgs += "-p:DebuggerSupport=false"
    $publishArgs += "-p:EventSourceSupport=false"
} elseif ($NoTrimming) {
    # Standard build without trimming
    $publishArgs += "-p:PublishTrimmed=false"
    $publishArgs += "-p:PublishReadyToRun=true"
} else {
    # Compact build with soft trimming
    $publishArgs += "-p:PublishTrimmed=true"
    $publishArgs += "-p:TrimMode=link"
    $publishArgs += "-p:EnableCompressionInSingleFile=true"
    $publishArgs += "-p:SuppressTrimAnalysisWarnings=true"
    $publishArgs += "-p:PublishReadyToRun=false"
}

& dotnet @publishArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    $outputPath = "bin\$Configuration\net8.0-windows\$Runtime\publish\"
    $exePath = Join-Path $outputPath "QuickSub.exe"
    
    Write-Host "Output location: $outputPath" -ForegroundColor Yellow
    Write-Host "File: QuickSub.exe" -ForegroundColor Yellow
    
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        Write-Host "File size: $fileSizeMB MB" -ForegroundColor Yellow
        
        Write-Host "Mode: $mode" -ForegroundColor Green
        
        if (-not $SafeMode -and -not $ConservativeMode -and -not $NoTrimming) {
            Write-Host "If you encounter issues, try:" -ForegroundColor Cyan
            Write-Host "  .\build.ps1 -ConservativeMode  # Compression without trimming" -ForegroundColor White
            Write-Host "  .\build.ps1 -SafeMode         # Full safety" -ForegroundColor White
        }
    }
    
    Write-Host "`nTo run, use:" -ForegroundColor Cyan
    Write-Host "cd `"$outputPath`"" -ForegroundColor White
    Write-Host ".\QuickSub.exe" -ForegroundColor White
} else {
    Write-Host "`n========================================" -ForegroundColor Red
    Write-Host "Build error!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Try other modes:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -ConservativeMode  # Conservative build" -ForegroundColor White
    Write-Host "  .\build.ps1 -SafeMode         # Safe build" -ForegroundColor White
}

Write-Host "`nPress any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 