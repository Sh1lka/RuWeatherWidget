$net = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpf = "$net\WPF"
$destWidget = "C:\Antigravity\RuWeatherWidget"
$refs = @(
    "$wpf\PresentationFramework.dll",
    "$wpf\PresentationCore.dll",
    "$wpf\WindowsBase.dll",
    "$net\System.Xaml.dll",
    "$net\System.Drawing.dll",
    "$net\System.dll",
    "$net\System.Core.dll",
    "$destWidget\Microsoft.Web.WebView2.Wpf.dll",
    "$destWidget\Microsoft.Web.WebView2.Core.dll"
)
$refArgs = ($refs | ForEach-Object { "/r:`"$_`"" }) -join " "
Remove-Item "$destWidget\RuWeatherWidget.exe" -ErrorAction SilentlyContinue
$cmd = "& `"$net\csc.exe`" /target:winexe /out:`"$destWidget\RuWeatherWidget.exe`" $refArgs `"$destWidget\Program.cs`""
Invoke-Expression $cmd
if (Test-Path "$destWidget\RuWeatherWidget.exe") {
    Write-Output "Compilation SUCCESS!"
} else {
    Write-Output "Compilation FAILED!"
}
