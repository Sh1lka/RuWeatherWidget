using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace RuWeatherInstaller
{
    public class SetupProgram : Application
    {
        public static bool IsAdmin()
        {
            try
            {
                var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                var p = new System.Security.Principal.WindowsPrincipal(id);
                return p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            bool isUninstall = false;
            bool isSilent = false;

            foreach (string arg in args)
            {
                string a = arg.ToLower().TrimStart('-', '/');
                if (a == "uninstall" || a == "u") isUninstall = true;
                if (a == "silent" || a == "s" || a == "q") isSilent = true;
            }

            if (isUninstall)
            {
                RunUninstall(isSilent);
                return;
            }

            if (isSilent)
            {
                RunSilentInstall();
                return;
            }

            var app = new SetupProgram();
            var win = new InstallerWindow();
            app.Run(win);
        }

        public static void RunSilentInstall()
        {
            try
            {
                string targetDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuWeatherWidget");
                PerformInstall(targetDir, true, true, true, true, null);
            }
            catch { }
        }

        public static void RunUninstall(bool silent)
        {
            if (!silent)
            {
                var res = MessageBox.Show("Вы уверены, что хотите удалить Виджет погоды с этого компьютера?", "Удаление Виджета погоды", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;
            }

            try
            {
                // 1. Close running instances
                foreach (var p in Process.GetProcessesByName("RuWeatherWidget"))
                {
                    try { p.Kill(); } catch { }
                }

                // 2. Restore taskbar region
                IntPtr hTray = FindWindow("Shell_TrayWnd", null);
                if (hTray != IntPtr.Zero)
                {
                    SetWindowRgn(hTray, IntPtr.Zero, true);
                }

                // 2.1 Restore default Windows 11 widgets button if previously disabled
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        if (key != null) key.SetValue("TaskbarDa", 1, RegistryValueKind.DWord);
                    }
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Policies\Microsoft\Dsh", false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Policies\Microsoft\Windows\Widgets", false);
                    UIntPtr dummy;
                    SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "TraySettings", 0x0002, 1000, out dummy);
                }
                catch { }

                // 3. Remove autostart
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null) key.DeleteValue("RuWeatherWidget", false);
                }

                // 4. Remove shortcut
                string shortcutPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Виджет Погоды.lnk");
                if (File.Exists(shortcutPath)) File.Delete(shortcutPath);

                // 5. Remove Uninstall registry
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RuWeatherWidget", false);

                // 6. Delete install folder via cmd delay
                string targetDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuWeatherWidget");
                if (Directory.Exists(targetDir))
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c timeout /t 1 /nobreak >nul & rmdir /s /q \"" + targetDir + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }

                if (!silent)
                {
                    MessageBox.Show("Виджет погоды успешно удалён.", "Удаление завершено", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (!silent) MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static bool PerformInstall(string targetDir, bool autostart, bool disableDefault, bool createShortcut, bool launch, Action<string, double> progress)
        {
            if (progress != null) progress("Подготовка к установке...", 0.1);

            // 0. Restore taskbar region before killing old processes
            try
            {
                IntPtr hTray = FindWindow("Shell_TrayWnd", null);
                if (hTray != IntPtr.Zero)
                {
                    SetWindowRgn(hTray, IntPtr.Zero, true);
                }
            }
            catch { }

            // 1. Kill old processes
            foreach (var p in Process.GetProcessesByName("RuWeatherWidget"))
            {
                try { p.Kill(); p.WaitForExit(1000); } catch { }
            }

            if (progress != null) progress("Создание директории...", 0.25);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 2. Extract RuWeatherWidget.exe and WebView2 components
            if (progress != null) progress("Распаковка файлов приложения...", 0.4);
            string targetExe = System.IO.Path.Combine(targetDir, "RuWeatherWidget.exe");

            var asm = Assembly.GetExecutingAssembly();
            string[] embedFiles = new string[]
            {
                "RuWeatherWidget.exe",
                "Microsoft.Web.WebView2.Wpf.dll",
                "Microsoft.Web.WebView2.Core.dll",
                "WebView2Loader.dll",
                "app.ico"
            };

            foreach (var fName in embedFiles)
            {
                using (var src = asm.GetManifestResourceStream(fName))
                {
                    if (src != null)
                    {
                        string destPath = System.IO.Path.Combine(targetDir, fName);
                        using (var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                        {
                            src.CopyTo(dst);
                        }
                    }
                    else if (fName == "RuWeatherWidget.exe")
                    {
                        throw new Exception("Файл RuWeatherWidget.exe не найден в ресурсах установщика.");
                    }
                }
            }

            // 3. Write default config if not exists
            string configPath = System.IO.Path.Combine(targetDir, "config.json");
            if (!File.Exists(configPath))
            {
                string defaultConfig = "{\"City\": \"Санкт-Петербург\", \"CitySlug\": \"sankt_peterburg\", \"Lat\": 59.9343, \"Lon\": 30.3351, \"AutoStart\": true, \"PositionMode\": \"OnTaskbar\", \"RefreshIntervalMinutes\": 15}";
                File.WriteAllText(configPath, defaultConfig, Encoding.UTF8);
            }

            // 4. Save uninstaller
            if (progress != null) progress("Настройка компонентов системы...", 0.6);
            string uninstallerExe = System.IO.Path.Combine(targetDir, "Uninstall.exe");
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                if (!string.Equals(currentExe, uninstallerExe, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(currentExe, uninstallerExe, true);
                }
            }
            catch { }

            // 5. Configure Autostart
            if (progress != null) progress("Настройка автозапуска...", 0.75);
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    if (autostart)
                        key.SetValue("RuWeatherWidget", "\"" + targetExe + "\"");
                    else
                        key.DeleteValue("RuWeatherWidget", false);
                }
            }

            // 6. Disable default Windows 11 widgets button if requested
            if (disableDefault)
            {
                bool widgetsEnabled = AreWindowsWidgetsEnabled();
                if (widgetsEnabled)
                {
                    if (progress != null) progress("Настройка панели задач...", 0.8);
                    try
                    {
                        Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true });
                    }
                    catch { }
                }
                DisableWindows11Widgets(false);
            }

            // 6.1 Create helper batch script for manual widget disabling if needed
            try
            {
                string batPath = System.IO.Path.Combine(targetDir, "Disable_Windows_Widgets.bat");
                string batContent = "@echo off\r\n" +
                    "chcp 65001 >nul\r\n" +
                    "echo ========================================================\r\n" +
                    "echo  Отключение штатного виджета Windows 11\r\n" +
                    "echo ========================================================\r\n" +
                    "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /v \"AllowNewsAndInterests\" /t REG_DWORD /d 0 /f\r\n" +
                    "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Widgets\" /v \"AllowWidgets\" /t REG_DWORD /d 0 /f\r\n" +
                    "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v \"TaskbarDa\" /t REG_DWORD /d 0 /f\r\n" +
                    "taskkill /f /im Widgets.exe >nul 2>&1\r\n" +
                    "taskkill /f /im WidgetService.exe >nul 2>&1\r\n" +
                    "taskkill /f /im explorer.exe >nul 2>&1\r\n" +
                    "start explorer.exe\r\n" +
                    "echo.\r\n" +
                    "echo Готово! Штатный виджет Windows 11 отключен.\r\n" +
                    "timeout /t 3 >nul\r\n";
                File.WriteAllText(batPath, batContent, Encoding.UTF8);
            }
            catch { }

            // 7. Create Start Menu Shortcut
            if (createShortcut)
            {
                try
                {
                    string shortcutPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Виджет Погоды.lnk");
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic shell = Activator.CreateInstance(shellType);
                        dynamic sc = shell.CreateShortcut(shortcutPath);
                        sc.TargetPath = targetExe;
                        sc.WorkingDirectory = targetDir;
                        sc.Description = "Виджет погоды на русском для панели задач Windows 11";
                        sc.Save();
                    }
                }
                catch { }
            }

            // 8. Register in Windows Add/Remove Programs (Uninstall registry)
            if (progress != null) progress("Регистрация приложения в Windows...", 0.9);
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\RuWeatherWidget"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "Виджет погоды (Русский для Windows 11)");
                        key.SetValue("DisplayVersion", "1.4.0");
                        key.SetValue("Publisher", "Sh1lka");
                        key.SetValue("DisplayIcon", targetExe + ",0");
                        key.SetValue("InstallLocation", targetDir);
                        key.SetValue("UninstallString", "\"" + uninstallerExe + "\" /uninstall");
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            // 9. Launch widget if requested
            if (progress != null) progress("Запуск приложения...", 1.0);
            if (launch)
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = targetExe,
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }
                catch { }
            }

            return true;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        public static bool AreWindowsWidgetsEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Dsh"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AllowNewsAndInterests");
                        if (val is int && (int)val == 0) return false;
                    }
                }
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Widgets"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AllowWidgets");
                        if (val is int && (int)val == 0) return false;
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Dsh"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AllowNewsAndInterests");
                        if (val is int && (int)val == 0) return false;
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Widgets"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AllowWidgets");
                        if (val is int && (int)val == 0) return false;
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("TaskbarDa");
                        if (val is int && (int)val == 0) return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void DisableWindows11Widgets(bool restartExplorer)
        {
            try
            {
                // If not running as admin, invoke elevated cmd to ensure HKLM policy is written
                if (!IsAdmin())
                {
                    try
                    {
                        string cmdArgs = "/c reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /v \"AllowNewsAndInterests\" /t REG_DWORD /d 0 /f >nul 2>&1 & reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Widgets\" /v \"AllowWidgets\" /t REG_DWORD /d 0 /f >nul 2>&1 & reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v \"TaskbarDa\" /t REG_DWORD /d 0 /f >nul 2>&1 & taskkill /f /im Widgets.exe >nul 2>&1 & taskkill /f /im WidgetService.exe >nul 2>&1";
                        if (restartExplorer)
                        {
                            cmdArgs += " & taskkill /f /im explorer.exe >nul 2>&1 & start explorer.exe";
                        }
                        var psi = new ProcessStartInfo()
                        {
                            FileName = "cmd.exe",
                            Arguments = cmdArgs,
                            Verb = "runas",
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        var p = Process.Start(psi);
                        if (p != null) p.WaitForExit(6000);
                        return;
                    }
                    catch { }
                }

                // 1. Group Policy (HKLM)
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Dsh"))
                {
                    if (key != null) key.SetValue("AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Widgets"))
                {
                    if (key != null) key.SetValue("AllowWidgets", 0, RegistryValueKind.DWord);
                }

                // 2. User preferences (Advanced\TaskbarDa = 0)
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null) key.SetValue("TaskbarDa", 0, RegistryValueKind.DWord);
                }

                // 3. Group Policy (HKCU)
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Dsh"))
                {
                    if (key != null) key.SetValue("AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Widgets"))
                {
                    if (key != null) key.SetValue("AllowWidgets", 0, RegistryValueKind.DWord);
                }

                // 4. Terminate background Widgets processes
                string[] widgetProcs = new string[] { "Widgets", "WidgetService", "WindowsWidgets", "NewsAndInterests" };
                foreach (var procName in widgetProcs)
                {
                    try
                    {
                        foreach (var p in Process.GetProcessesByName(procName))
                        {
                            try { p.Kill(); } catch { }
                        }
                    }
                    catch { }
                }

                // 5. Broadcast WM_SETTINGCHANGE to all windows including Explorer
                UIntPtr dummy;
                SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "TraySettings", 0x0002, 1000, out dummy);
                SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", 0x0002, 1000, out dummy);
                SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "Policy", 0x0002, 1000, out dummy);

                // 6. Restart explorer.exe if requested so taskbar layout immediately reloads without the widget
                if (restartExplorer)
                {
                    try
                    {
                        foreach (var exp in Process.GetProcessesByName("explorer"))
                        {
                            try { exp.Kill(); } catch { }
                        }
                        Thread.Sleep(800);
                        if (Process.GetProcessesByName("explorer").Length == 0)
                        {
                            Process.Start("explorer.exe");
                        }
                        Thread.Sleep(1200);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    public class InstallerWindow : Window
    {
        private TextBox _txtPath;
        private CheckBox _chkAutostart;
        private CheckBox _chkDisableDefault;
        private CheckBox _chkShortcut;
        private CheckBox _chkLaunch;
        private ProgressBar _progBar;
        private TextBlock _lblStatus;
        private Button _btnInstall;
        private Button _btnCancel;

        public InstallerWindow()
        {
            Title = "Установка виджета погоды для Windows 11";
            Width = 540;
            Height = 490;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("app.ico"))
                {
                    if (s != null)
                    {
                        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(s);
                    }
                }
            }
            catch { }

            BuildUI();
        }

        private void BuildUI()
        {
            var root = new Grid();
            root.Margin = new Thickness(24, 20, 24, 20);
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Path
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Options
            root.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Status
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Buttons

            // --- 1. Header with Icon ---
            var headerGrid = new Grid();
            headerGrid.Margin = new Thickness(0, 0, 0, 18);
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var iconCanvas = new Canvas() { Width = 48, Height = 48, Margin = new Thickness(0, 0, 16, 0) };
            DrawHeaderIcon(iconCanvas);
            Grid.SetColumn(iconCanvas, 0);
            headerGrid.Children.Add(iconCanvas);

            var titleStack = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            var lblTitle = new TextBlock()
            {
                Text = "Виджет погоды на русском",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            var lblSubtitle = new TextBlock()
            {
                Text = "Установка на панель задач Windows 11",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 165)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(lblTitle);
            titleStack.Children.Add(lblSubtitle);
            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(titleStack);

            Grid.SetRow(headerGrid, 0);
            root.Children.Add(headerGrid);

            // --- 2. Installation Path Box ---
            var pathBorder = new Border()
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var pathStack = new StackPanel();
            var lblPathHeader = new TextBlock()
            {
                Text = "Папка установки:",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            pathStack.Children.Add(lblPathHeader);

            string defaultDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuWeatherWidget");
            _txtPath = new TextBox()
            {
                Text = defaultDir,
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 12,
                IsReadOnly = false
            };
            pathStack.Children.Add(_txtPath);
            pathBorder.Child = pathStack;

            Grid.SetRow(pathBorder, 1);
            root.Children.Add(pathBorder);

            // --- 3. Options Checkboxes ---
            var optBorder = new Border()
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var optStack = new StackPanel();

            _chkAutostart = MakeCheckBox("Запускать автоматически при входе в Windows", true);
            _chkDisableDefault = MakeCheckBox("Отключить встроенный виджет Windows (рекомендуется)", true);
            _chkShortcut = MakeCheckBox("Создать ярлык в меню «Пуск»", true);
            _chkLaunch = MakeCheckBox("Запустить виджет сразу после завершения установки", true);

            optStack.Children.Add(_chkAutostart);
            optStack.Children.Add(_chkDisableDefault);
            optStack.Children.Add(_chkShortcut);
            optStack.Children.Add(_chkLaunch);
            optBorder.Child = optStack;

            Grid.SetRow(optBorder, 2);
            root.Children.Add(optBorder);

            // --- 4. Progress & Status ---
            var statusStack = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            _progBar = new ProgressBar()
            {
                Height = 4,
                Visibility = Visibility.Hidden,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                BorderThickness = new Thickness(0)
            };
            _lblStatus = new TextBlock()
            {
                Text = "Нажмите «Установить» для начала установки.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            };
            statusStack.Children.Add(_progBar);
            statusStack.Children.Add(_lblStatus);

            Grid.SetRow(statusStack, 3);
            root.Children.Add(statusStack);

            // --- 5. Action Buttons ---
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _btnCancel = new Button()
            {
                Content = "Отмена",
                Width = 100,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnCancel.Click += delegate { Close(); };
            Grid.SetColumn(_btnCancel, 1);
            btnGrid.Children.Add(_btnCancel);

            _btnInstall = new Button()
            {
                Content = "Установить",
                Width = 120,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            _btnInstall.Click += BtnInstall_Click;
            Grid.SetColumn(_btnInstall, 2);
            btnGrid.Children.Add(_btnInstall);

            Grid.SetRow(btnGrid, 4);
            root.Children.Add(btnGrid);

            Content = root;
        }

        private CheckBox MakeCheckBox(string text, bool isChecked)
        {
            var cb = new CheckBox();
            cb.Content = text;
            cb.IsChecked = isChecked;
            cb.Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            cb.FontSize = 12;
            cb.Margin = new Thickness(0, 3, 0, 3);
            cb.Cursor = Cursors.Hand;
            return cb;
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_btnInstall.Content.ToString() == "Готово")
            {
                Close();
                return;
            }

            string targetDir = _txtPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Укажите папку для установки.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _btnInstall.IsEnabled = false;
            _btnCancel.IsEnabled = false;
            _txtPath.IsEnabled = false;
            _chkAutostart.IsEnabled = false;
            _chkDisableDefault.IsEnabled = false;
            _chkShortcut.IsEnabled = false;
            _chkLaunch.IsEnabled = false;

            _progBar.Visibility = Visibility.Visible;
            _progBar.IsIndeterminate = false;
            _progBar.Value = 0;

            bool autostart = _chkAutostart.IsChecked == true;
            bool disableDefault = _chkDisableDefault.IsChecked == true;
            bool createShortcut = _chkShortcut.IsChecked == true;
            bool launch = _chkLaunch.IsChecked == true;

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SetupProgram.PerformInstall(targetDir, autostart, disableDefault, createShortcut, launch, (msg, val) =>
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            _lblStatus.Text = msg;
                            _progBar.Value = val * 100;
                        }));
                    });

                    Dispatcher.Invoke(new Action(delegate
                    {
                        _progBar.Value = 100;
                        if (disableDefault)
                            _lblStatus.Text = "✅  Установлено! В открывшемся окне выключите тумблер «Виджеты»";
                        else
                            _lblStatus.Text = "✅  Виджет погоды успешно установлен и настроен!";
                        _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(100, 230, 100));
                        _btnInstall.Content = "Готово";
                        _btnInstall.IsEnabled = true;
                        _btnCancel.Visibility = Visibility.Collapsed;
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        _lblStatus.Text = "Ошибка установки: " + ex.Message;
                        _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                        _btnInstall.IsEnabled = true;
                        _btnCancel.IsEnabled = true;
                    }));
                }
            });
        }

        private void DrawHeaderIcon(Canvas canvas)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("app.ico"))
                {
                    if (s != null)
                    {
                        var decoder = new System.Windows.Media.Imaging.IconBitmapDecoder(s, System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                        if (decoder.Frames.Count > 0)
                        {
                            var img = new Image();
                            img.Source = decoder.Frames[0];
                            img.Width = 48;
                            img.Height = 48;
                            canvas.Children.Add(img);
                            return;
                        }
                    }
                }
            }
            catch { }

            // Fallback Sun
            var sun = new Ellipse();
            sun.Width = 24;
            sun.Height = 24;
            sun.Fill = new SolidColorBrush(Color.FromRgb(255, 185, 0));
            Canvas.SetLeft(sun, 18);
            Canvas.SetTop(sun, 4);
            canvas.Children.Add(sun);

            // Cloud base
            var cloud1 = new Ellipse() { Width = 20, Height = 20, Fill = new SolidColorBrush(Color.FromRgb(220, 235, 252)) };
            Canvas.SetLeft(cloud1, 6);
            Canvas.SetTop(cloud1, 16);
            canvas.Children.Add(cloud1);

            var cloud2 = new Ellipse() { Width = 24, Height = 24, Fill = new SolidColorBrush(Color.FromRgb(220, 235, 252)) };
            Canvas.SetLeft(cloud2, 16);
            Canvas.SetTop(cloud2, 12);
            canvas.Children.Add(cloud2);

            var cloud3 = new Ellipse() { Width = 18, Height = 18, Fill = new SolidColorBrush(Color.FromRgb(220, 235, 252)) };
            Canvas.SetLeft(cloud3, 26);
            Canvas.SetTop(cloud3, 16);
            canvas.Children.Add(cloud3);

            var cloudBase = new Rectangle() { Width = 26, Height = 11, Fill = new SolidColorBrush(Color.FromRgb(220, 235, 252)), RadiusX = 4, RadiusY = 4 };
            Canvas.SetLeft(cloudBase, 12);
            Canvas.SetTop(cloudBase, 22);
            canvas.Children.Add(cloudBase);
        }
    }
}
