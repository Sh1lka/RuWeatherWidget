$net = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpf = "$net\WPF"
$refs = @(
    "$wpf\PresentationFramework.dll",
    "$wpf\PresentationCore.dll",
    "$wpf\WindowsBase.dll",
    "$net\System.Xaml.dll",
    "$net\System.dll",
    "$net\System.Core.dll",
    "$net\Microsoft.CSharp.dll"
)
$refArgs = ($refs | ForEach-Object { "/r:`"$_`"" }) -join " "

$widgetDir = "C:\Antigravity\RuWeatherWidget"
$embeds = @(
    "/resource:`"$widgetDir\RuWeatherWidget.exe`",RuWeatherWidget.exe",
    "/resource:`"$widgetDir\Microsoft.Web.WebView2.Wpf.dll`",Microsoft.Web.WebView2.Wpf.dll",
    "/resource:`"$widgetDir\Microsoft.Web.WebView2.Core.dll`",Microsoft.Web.WebView2.Core.dll",
    "/resource:`"$widgetDir\WebView2Loader.dll`",WebView2Loader.dll"
)
$resArgs = $embeds -join " "

if (-not (Test-Path "C:\Antigravity\Release")) {
    New-Item -ItemType Directory -Path "C:\Antigravity\Release" -Force | Out-Null
}

$outExe = "C:\Antigravity\Release\RuWeatherWidget_Setup.exe"
$src = "C:\Antigravity\Installer\SetupProgram.cs"

$argList = "/target:winexe /optimize+ /out:`"$outExe`" $resArgs $refArgs `"$src`""
Write-Host "Compiling installer with embedded WebView2..."
$process = Start-Process -FilePath "$net\csc.exe" -ArgumentList $argList -NoNewWindow -Wait -PassThru

if ($process.ExitCode -eq 0 -and (Test-Path $outExe)) {
    $size = (Get-Item $outExe).Length
    Write-Host "SUCCESS! Installer built at $outExe (Size: $size bytes)"
    
    # Copy to Desktop
    $desktopPath = [System.IO.Path]::Combine([Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop), "RuWeatherWidget_Setup.exe")
    Copy-Item $outExe $desktopPath -Force
    Write-Host "Copied to Desktop: $desktopPath"

    # Copy to Artifact directory
    $artDir = "C:\Users\Shilka\.gemini\antigravity\brain\f7b5fabd-5f6e-46da-ad40-9e002dac123b"
    Copy-Item $outExe (Join-Path $artDir "RuWeatherWidget_Setup.exe") -Force
    Write-Host "Copied to Artifacts folder."
} else {
    Write-Host "FAILED with exit code $($process.ExitCode)"
}
