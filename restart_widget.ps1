Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")]
    public static extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, uint dwDesiredAccess);
    [DllImport("user32.dll")]
    public static extern bool SetProcessWindowStation(IntPtr hWinSta);
    [DllImport("user32.dll")]
    public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);
    [DllImport("user32.dll")]
    public static extern bool SetThreadDesktop(IntPtr hDesktop);
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")]
    public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
}
"@

# 1. Restore taskbar window region to remove any hole / frames
try {
    $hWinsta = [Win32]::OpenWindowStation("winsta0", $false, 0x037F)
    if ($hWinsta -ne [IntPtr]::Zero) { [Win32]::SetProcessWindowStation($hWinsta) }
    $hDesk = [Win32]::OpenDesktop("default", 0, $false, 0x01FF)
    if ($hDesk -ne [IntPtr]::Zero) { [Win32]::SetThreadDesktop($hDesk) }

    $hTray = [Win32]::FindWindow("Shell_TrayWnd", $null)
    if ($hTray -ne [IntPtr]::Zero) {
        [Win32]::SetWindowRgn($hTray, [IntPtr]::Zero, $true)
        Write-Host "Restored taskbar region on Shell_TrayWnd."
    }
} catch {
    Write-Host "Error restoring taskbar: $_"
}

# 2. Stop running widget process
Stop-Process -Name RuWeatherWidget -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800

# 3. Update executable in AppData
$appDataDir = Join-Path $env:LOCALAPPDATA "RuWeatherWidget"
if (-not (Test-Path $appDataDir)) {
    New-Item -ItemType Directory -Path $appDataDir -Force | Out-Null
}
$targetExe = Join-Path $appDataDir "RuWeatherWidget.exe"
$srcExe = "C:\Antigravity\RuWeatherWidget\RuWeatherWidget.exe"

if (Test-Path $srcExe) {
    Copy-Item $srcExe $targetExe -Force
    Write-Host "Copied $srcExe to $targetExe"
}

# Copy WebView2 dependencies and app icon
$dlls = @("Microsoft.Web.WebView2.Wpf.dll", "Microsoft.Web.WebView2.Core.dll", "WebView2Loader.dll", "app.ico")
foreach ($d in $dlls) {
    $srcDll = "C:\Antigravity\RuWeatherWidget\$d"
    if (Test-Path $srcDll) {
        Copy-Item $srcDll (Join-Path $appDataDir $d) -Force
        Write-Host "Copied $d to $appDataDir"
    }
}

# 4. Launch in user interactive session via explorer.exe
Start-Process "explorer.exe" -ArgumentList "`"$targetExe`""
Start-Sleep -Seconds 2

$proc = Get-Process -Name RuWeatherWidget -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "RuWeatherWidget restarted successfully! PID: $($proc.Id), Session: $($proc.SessionId)"
} else {
    Write-Host "WARNING: RuWeatherWidget did not start."
}
