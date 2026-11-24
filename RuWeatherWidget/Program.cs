using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Markup;
using Microsoft.Win32;

namespace RuWeatherWidget
{
    public class WeatherData
    {
        public string City = "Санкт-Петербург";
        public string CitySlug = "sankt_peterburg";
        public double Lat = 59.9343;
        public double Lon = 30.3351;
        public string Temp = "0°C";
        public string Condition = "Переменная облачность";
        public string FeelsLike = "-3°C";
        public string Humidity = "88%";
        public string Pressure = "748 мм рт. ст.";
        public string Sunrise = "";
        public string Sunset = "";
        public string DateText = "";
        public string IconType = "partly_cloudy";
        public string ProviderName = "Яндекс.Погода";

        public List<DailyPoint> Daily = new List<DailyPoint>();
    }

    public class DailyPoint
    {
        public string DayName;
        public string Condition;
        public string TempMinMax;
        public string IconType;
    }

    public class AppConfig
    {
        public string City = "Санкт-Петербург";
        public string CitySlug = "sankt_peterburg";
        public double Lat = 59.9343;
        public double Lon = 30.3351;
        public bool AutoStart = true;
        public string PositionMode = "OnTaskbar";
        public int RefreshIntervalMinutes = 15;
        public string WeatherProvider = "Yandex"; // "Yandex", "AccuWeather", "MailRu"
        public string AccuWeatherApiKey = "";
        public List<string> RecentCities = new List<string>() { "Санкт-Петербург", "Москва", "Новосибирск" };
    }

    public static class CityResolver
    {
        private static readonly Dictionary<string, Tuple<double, double, string>> KnownCities = 
            new Dictionary<string, Tuple<double, double, string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Санкт-Петербург", Tuple.Create(59.9343, 30.3351, "sankt_peterburg") },
            { "Москва", Tuple.Create(55.7558, 37.6173, "moskva") },
            { "Новосибирск", Tuple.Create(55.0084, 82.9357, "novosibirsk") },
            { "Екатеринбург", Tuple.Create(56.8389, 60.6057, "ekaterinburg") },
            { "Казань", Tuple.Create(55.7887, 49.1221, "kazan") },
            { "Нижний Новгород", Tuple.Create(56.3269, 44.0059, "nizhniy_novgorod") },
            { "Самара", Tuple.Create(53.1959, 50.1002, "samara") },
            { "Челябинск", Tuple.Create(55.1644, 61.4368, "chelyabinsk") },
            { "Красноярск", Tuple.Create(56.0153, 92.8932, "krasnoyarsk") },
            { "Уфа", Tuple.Create(54.7388, 55.9721, "ufa") },
            { "Ростов-на-Дону", Tuple.Create(47.2357, 39.7015, "rostov_na_donu") },
            { "Краснодар", Tuple.Create(45.0355, 38.9753, "krasnodar") },
            { "Омск", Tuple.Create(54.9885, 73.3242, "omsk") },
            { "Воронеж", Tuple.Create(51.6720, 39.1843, "voronezh") },
            { "Пермь", Tuple.Create(58.0105, 56.2502, "perm") },
            { "Волгоград", Tuple.Create(48.7080, 44.5133, "volgograd") },
            { "Саратов", Tuple.Create(51.5336, 46.0343, "saratov") },
            { "Тюмень", Tuple.Create(57.1522, 65.5272, "tyumen") },
            { "Тольятти", Tuple.Create(53.5303, 49.3461, "tolyatti") },
            { "Ижевск", Tuple.Create(56.8498, 53.2045, "izhevsk") },
            { "Барнаул", Tuple.Create(53.3606, 83.7636, "barnaul") },
            { "Ульяновск", Tuple.Create(54.3142, 48.4031, "ulyanovsk") },
            { "Иркутск", Tuple.Create(52.2870, 104.3050, "irkutsk") },
            { "Хабаровск", Tuple.Create(48.4827, 135.0840, "khabarovsk") },
            { "Ярославль", Tuple.Create(57.6261, 39.8845, "yaroslavl") },
            { "Владивосток", Tuple.Create(43.1198, 131.8869, "vladivostok") },
            { "Махачкала", Tuple.Create(42.9849, 47.5047, "makhachkala") },
            { "Томск", Tuple.Create(56.5010, 84.9924, "tomsk") },
            { "Оренбург", Tuple.Create(51.7727, 55.0988, "orenburg") },
            { "Кемерово", Tuple.Create(55.3547, 86.0872, "kemerovo") },
            { "Новокузнецк", Tuple.Create(53.7596, 87.1216, "novokuznetsk") },
            { "Рязань", Tuple.Create(54.6269, 39.7425, "ryazan") },
            { "Астрахань", Tuple.Create(46.3497, 48.0408, "astrakhan") },
            { "Пенза", Tuple.Create(53.2007, 45.0046, "penza") },
            { "Липецк", Tuple.Create(52.6103, 39.5947, "lipetsk") },
            { "Тула", Tuple.Create(54.1961, 37.6182, "tula") },
            { "Киров", Tuple.Create(58.6035, 49.6679, "kirov") },
            { "Чебоксары", Tuple.Create(56.1428, 47.2547, "cheboksary") },
            { "Калининград", Tuple.Create(54.7104, 20.4522, "kaliningrad") },
            { "Курск", Tuple.Create(51.7304, 36.1927, "kursk") },
            { "Сочи", Tuple.Create(43.6028, 39.7342, "sochi") },
            { "Ставрополь", Tuple.Create(45.0428, 41.9734, "stavropol") },
            { "Тверь", Tuple.Create(56.8587, 35.9176, "tver") },
            { "Магнитогорск", Tuple.Create(53.4186, 58.9813, "magnitogorsk") },
            { "Иваново", Tuple.Create(56.9972, 40.9714, "ivanovo") },
            { "Брянск", Tuple.Create(53.2521, 34.3717, "bryansk") },
            { "Белгород", Tuple.Create(50.5997, 36.5982, "belgorod") },
            { "Сургут", Tuple.Create(61.2559, 73.4178, "surgut") },
            { "Владимир", Tuple.Create(56.1290, 40.4070, "vladimir") },
            { "Чита", Tuple.Create(52.0340, 113.5009, "chita") },
            { "Архангельск", Tuple.Create(64.5473, 40.5433, "arkhangelsk") },
            { "Калуга", Tuple.Create(54.5293, 36.2754, "kaluga") },
            { "Смоленск", Tuple.Create(54.7818, 32.0401, "smolensk") },
            { "Мурманск", Tuple.Create(68.9585, 33.0827, "murmansk") },
            { "Петрозаводск", Tuple.Create(61.7849, 34.3469, "petrozavodsk") },
            { "Кострома", Tuple.Create(57.7665, 40.9269, "kostroma") },
            { "Новороссийск", Tuple.Create(44.7154, 37.7619, "novorossiysk") },
            { "Якутск", Tuple.Create(62.0355, 129.6755, "yakutsk") },
            { "Норильск", Tuple.Create(69.3558, 88.1893, "norilsk") }
        };

        public static string Transliterate(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var map = new Dictionary<char, string>()
            {
                {'а',"a"}, {'б',"b"}, {'в',"v"}, {'г',"g"}, {'д',"d"}, {'е',"e"}, {'ё',"e"}, {'ж',"zh"},
                {'з',"z"}, {'и',"i"}, {'й',"y"}, {'к',"k"}, {'л',"l"}, {'м',"m"}, {'н',"n"}, {'о',"o"},
                {'п',"p"}, {'р',"r"}, {'с',"s"}, {'т',"t"}, {'у',"u"}, {'ф',"f"}, {'х',"kh"}, {'ц',"ts"},
                {'ч',"ch"}, {'ш',"sh"}, {'щ',"shch"}, {'ъ',""}, {'ы',"y"}, {'ь',""}, {'э',"e"}, {'ю',"yu"}, {'я',"ya"},
                {' ',"_"}, {'-',"_"}
            };
            var sb = new StringBuilder();
            foreach (char c in text.ToLower())
            {
                string repl;
                if (map.TryGetValue(c, out repl)) sb.Append(repl);
                else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            return sb.ToString();
        }

        public static bool ResolveCity(string cityName, out string displayName, out string slug, out double lat, out double lon)
        {
            cityName = (cityName ?? "").Trim();
            if (cityName.Length == 0)
            {
                displayName = "Санкт-Петербург"; slug = "sankt_peterburg"; lat = 59.9343; lon = 30.3351;
                return false;
            }

            displayName = char.ToUpper(cityName[0]) + (cityName.Length > 1 ? cityName.Substring(1) : "");

            Tuple<double, double, string> known;
            if (KnownCities.TryGetValue(cityName, out known))
            {
                lat = known.Item1;
                lon = known.Item2;
                slug = known.Item3;
                return true;
            }

            slug = Transliterate(cityName);
            lat = 55.7558; lon = 37.6173;

            // Try geocoding online
            try
            {
                string url = "https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(cityName) + "&count=1&language=ru";
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.Headers["User-Agent"] = "Mozilla/5.0";
                string json = wc.DownloadString(url);
                var mLat = Regex.Match(json, "\"latitude\":\\s*([\\d\\.\\-]+)");
                var mLon = Regex.Match(json, "\"longitude\":\\s*([\\d\\.\\-]+)");
                var mName = Regex.Match(json, "\"name\":\\s*\"([^\"]+)\"");
                if (mLat.Success && mLon.Success)
                {
                    lat = double.Parse(mLat.Groups[1].Value, CultureInfo.InvariantCulture);
                    lon = double.Parse(mLon.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (mName.Success && !string.IsNullOrEmpty(mName.Groups[1].Value))
                    {
                        displayName = mName.Groups[1].Value;
                    }
                    return true;
                }
            }
            catch { }

            return true;
        }
    }

    public class Program : Application
    {
        [DllImport("user32.dll")]
        private static extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, uint dwDesiredAccess);
        [DllImport("user32.dll")]
        private static extern bool SetProcessWindowStation(IntPtr hWinSta);
        [DllImport("user32.dll")]
        private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);
        [DllImport("user32.dll")]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        public static void DisableNativeWindowsWidgets()
        {
            try
            {
                int taskbarDa = 1;
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        if (key != null)
                        {
                            object val = key.GetValue("TaskbarDa");
                            if (val is int) taskbarDa = (int)val;
                        }
                    }
                }
                catch { }

                if (taskbarDa != 0)
                {
                    Log("TaskbarDa=" + taskbarDa + ", applying native widget suppression...");
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        if (key != null) key.SetValue("TaskbarDa", 0, RegistryValueKind.DWord);
                    }
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Dsh"))
                    {
                        if (key != null) key.SetValue("AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                    }
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Widgets"))
                    {
                        if (key != null) key.SetValue("AllowWidgets", 0, RegistryValueKind.DWord);
                    }

                    try
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Dsh"))
                        {
                            if (key != null) key.SetValue("AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                        }
                    }
                    catch { }
                    try
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Widgets"))
                        {
                            if (key != null) key.SetValue("AllowWidgets", 0, RegistryValueKind.DWord);
                        }
                    }
                    catch { }

                    string[] procs = new string[] { "Widgets", "WidgetService", "WindowsWidgets", "NewsAndInterests" };
                    foreach (var pName in procs)
                    {
                        try
                        {
                            foreach (var p in Process.GetProcessesByName(pName))
                            {
                                try { p.Kill(); } catch { }
                            }
                        }
                        catch { }
                    }

                    UIntPtr dummy;
                    SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "TraySettings", 0x0002, 1000, out dummy);
                    SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", 0x0002, 1000, out dummy);
                    SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "Policy", 0x0002, 1000, out dummy);
                    Log("DisableNativeWindowsWidgets finished.");
                }
            }
            catch (Exception ex)
            {
                Log("DisableNativeWindowsWidgets Exception: " + ex);
            }
        }

        public static void ForceDisableNativeWidgetsAndRestartExplorer()
        {
            try
            {
                var psi = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /v \"AllowNewsAndInterests\" /t REG_DWORD /d 0 /f >nul 2>&1 & reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Widgets\" /v \"AllowWidgets\" /t REG_DWORD /d 0 /f >nul 2>&1 & reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v \"TaskbarDa\" /t REG_DWORD /d 0 /f >nul 2>&1 & taskkill /f /im Widgets.exe >nul 2>&1 & taskkill /f /im WidgetService.exe >nul 2>&1 & taskkill /f /im explorer.exe >nul 2>&1 & start explorer.exe",
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var proc = Process.Start(psi);
                if (proc != null) proc.WaitForExit(7000);
            }
            catch (Exception ex)
            {
                Log("ForceDisableNativeWidgetsAndRestartExplorer error: " + ex);
                try
                {
                    Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true });
                }
                catch { }
            }
        }

        public static void Log(string msg)
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuWeatherWidget");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(System.IO.Path.Combine(dir, "widget.log"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + msg + "\r\n");
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                IntPtr hWinsta = OpenWindowStation("winsta0", false, 0x037F);
                if (hWinsta != IntPtr.Zero) SetProcessWindowStation(hWinsta);
                IntPtr hDesk = OpenDesktop("default", 0, false, 0x01FF);

                var appThread = new Thread(() =>
                {
                    try
                    {
                        if (hDesk != IntPtr.Zero)
                        {
                            bool okDesk = SetThreadDesktop(hDesk);
                            Log("appThread SetThreadDesktop=" + okDesk);
                        }

                        // Set browser emulation to IE11 mode for web controls
                        try
                        {
                            string exeName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                            {
                                if (k != null) k.SetValue(exeName, 11001, RegistryValueKind.DWord);
                            }
                        }
                        catch { }

                        Log("Application Main entry");
                        bool createdNew;
                        using (var mutex = new Mutex(true, "RuWeatherWidget_SingleInstance_Mutex", out createdNew))
                        {
                            Log("Mutex createdNew=" + createdNew);
                            if (!createdNew) return;

                            DisableNativeWindowsWidgets();

                            var app = new Program();
                            app.DispatcherUnhandledException += (s, e) => {
                                Log("Dispatcher Exception: " + e.Exception);
                                e.Handled = true;
                            };
                            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                                Log("AppDomain Exception: " + e.ExceptionObject);
                            };
                            app.Run();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("appThread Exception: " + ex);
                    }
                });

                appThread.SetApartmentState(ApartmentState.STA);
                appThread.Start();
                appThread.Join();
            }
            catch (Exception ex)
            {
                Log("Fatal Main Exception: " + ex);
            }
        }

        private TaskbarWidgetWindow _taskbarWindow;
        private FlyoutWindow _flyoutWindow;
        private WeatherData _currentData = new WeatherData();
        private AppConfig _config = new AppConfig();
        private DispatcherTimer _refreshTimer;
        private string _configPath;
        private DateTime _lastToggle = DateTime.MinValue;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                Log("OnStartup begin");
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string localConfig = System.IO.Path.Combine(appDir, "config.json");
                if (File.Exists(localConfig))
                {
                    _configPath = localConfig;
                }
                else
                {
                    string appDataDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuWeatherWidget");
                    if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
                    _configPath = System.IO.Path.Combine(appDataDir, "config.json");
                }

                LoadConfig();
                Log("LoadConfig done: City=" + _config.City + ", Interval=" + _config.RefreshIntervalMinutes + "m");

                _flyoutWindow = new FlyoutWindow(this);
                _taskbarWindow = new TaskbarWidgetWindow(this);
                _taskbarWindow.Show();
                Log("_taskbarWindow.Show() done");

                AppDomain.CurrentDomain.ProcessExit += delegate {
                    if (_taskbarWindow != null) _taskbarWindow.RestoreTaskbar();
                };

                FetchWeatherAsync();

                _refreshTimer = new DispatcherTimer();
                _refreshTimer.Interval = TimeSpan.FromMinutes(_config.RefreshIntervalMinutes > 0 ? _config.RefreshIntervalMinutes : 15);
                _refreshTimer.Tick += (s, args) => FetchWeatherAsync();
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                Log("OnStartup Exception: " + ex);
            }
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string txt = File.ReadAllText(_configPath, Encoding.UTF8);
                    var mCity = Regex.Match(txt, "\"City\":\\s*\"([^\"]+)\"");
                    if (mCity.Success) _config.City = mCity.Groups[1].Value;

                    var mSlug = Regex.Match(txt, "\"CitySlug\":\\s*\"([^\"]+)\"");
                    if (mSlug.Success) _config.CitySlug = mSlug.Groups[1].Value;

                    var mLat = Regex.Match(txt, "\"Lat\":\\s*([\\d\\.\\-]+)");
                    if (mLat.Success) double.TryParse(mLat.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _config.Lat);

                    var mLon = Regex.Match(txt, "\"Lon\":\\s*([\\d\\.\\-]+)");
                    if (mLon.Success) double.TryParse(mLon.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _config.Lon);

                    var mAuto = Regex.Match(txt, "\"AutoStart\":\\s*(true|false)");
                    if (mAuto.Success) _config.AutoStart = bool.Parse(mAuto.Groups[1].Value);

                    var mPos = Regex.Match(txt, "\"PositionMode\":\\s*\"([^\"]+)\"");
                    if (mPos.Success) _config.PositionMode = mPos.Groups[1].Value;

                    var mInterval = Regex.Match(txt, "\"RefreshIntervalMinutes\":\\s*(\\d+)");
                    if (mInterval.Success) _config.RefreshIntervalMinutes = int.Parse(mInterval.Groups[1].Value);
                    if (_config.RefreshIntervalMinutes < 1) _config.RefreshIntervalMinutes = 15;

                    var mProvider = Regex.Match(txt, "\"WeatherProvider\":\\s*\"([^\"]+)\"");
                    if (mProvider.Success) _config.WeatherProvider = mProvider.Groups[1].Value;

                    var mAccuKey = Regex.Match(txt, "\"AccuWeatherApiKey\":\\s*\"([^\"]+)\"");
                    if (mAccuKey.Success) _config.AccuWeatherApiKey = mAccuKey.Groups[1].Value;
                }
            }
            catch { }
        }

        public void SaveConfig()
        {
            try
            {
                string json = string.Format(CultureInfo.InvariantCulture,
                    "{{\r\n  \"City\": \"{0}\",\r\n  \"CitySlug\": \"{1}\",\r\n  \"Lat\": {2:F4},\r\n  \"Lon\": {3:F4},\r\n  \"AutoStart\": {4},\r\n  \"PositionMode\": \"{5}\",\r\n  \"RefreshIntervalMinutes\": {6},\r\n  \"WeatherProvider\": \"{7}\",\r\n  \"AccuWeatherApiKey\": \"{8}\"\r\n}}",
                    _config.City, _config.CitySlug, _config.Lat, _config.Lon, _config.AutoStart ? "true" : "false", _config.PositionMode, _config.RefreshIntervalMinutes, _config.WeatherProvider ?? "Yandex", _config.AccuWeatherApiKey ?? "");
                File.WriteAllText(_configPath, json, Encoding.UTF8);

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        if (_config.AutoStart)
                            key.SetValue("RuWeatherWidget", "\"" + exePath + "\"");
                        else
                            key.DeleteValue("RuWeatherWidget", false);
                    }
                }
            }
            catch { }
        }

        public AppConfig Config { get { return _config; } }
        public WeatherData CurrentData { get { return _currentData; } }
        public TaskbarWidgetWindow TaskbarWindow { get { return _taskbarWindow; } }
        public FlyoutWindow Flyout { get { return _flyoutWindow; } }
        public bool IsContextMenuOpen = false;

        public void SetWeatherProvider(string provider)
        {
            _config.WeatherProvider = provider;
            SaveConfig();
            FetchWeatherAsync();
        }

        public void ToggleFlyout(int startPage = 0)
        {
            Log("ToggleFlyout called: startPage=" + startPage + ", isVis=" + (_flyoutWindow != null && _flyoutWindow.IsVisible));
            if ((DateTime.UtcNow - _lastToggle).TotalMilliseconds < 250) return;
            _lastToggle = DateTime.UtcNow;

            if (_flyoutWindow.IsVisible)
            {
                _flyoutWindow.Hide();
            }
            else
            {
                _flyoutWindow.LastOpenTime = DateTime.UtcNow;
                _flyoutWindow.SetPage(startPage);
                _flyoutWindow.UpdateData(_currentData);
                _flyoutWindow.PositionAbove(_taskbarWindow);
                _flyoutWindow.Show();
                _flyoutWindow.Activate();
                _flyoutWindow.Focus();
            }
        }

        public void SetCity(string displayName, string slug, double lat = 0, double lon = 0)
        {
            _config.City = displayName;
            _config.CitySlug = slug;
            if (lat != 0 && lon != 0)
            {
                _config.Lat = lat;
                _config.Lon = lon;
            }
            else
            {
                double gLat, gLon;
                string d, s;
                CityResolver.ResolveCity(displayName, out d, out s, out gLat, out gLon);
                _config.Lat = gLat;
                _config.Lon = gLon;
            }

            if (_config.RecentCities.Contains(displayName))
            {
                _config.RecentCities.Remove(displayName);
            }
            _config.RecentCities.Insert(0, displayName);
            while (_config.RecentCities.Count > 3)
            {
                _config.RecentCities.RemoveAt(_config.RecentCities.Count - 1);
            }

            SaveConfig();
            FetchWeatherAsync();
        }

        public void SetRefreshInterval(int minutes)
        {
            if (minutes < 1) minutes = 1;
            if (minutes > 1440) minutes = 1440;
            _config.RefreshIntervalMinutes = minutes;
            SaveConfig();

            if (_refreshTimer != null)
            {
                _refreshTimer.Interval = TimeSpan.FromMinutes(minutes);
            }
        }

        public void FetchWeatherAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
                    WeatherData data = null;

                    string provider = _config.WeatherProvider ?? "Yandex";
                    if (string.Equals(provider, "Yandex", StringComparison.OrdinalIgnoreCase))
                    {
                        try { data = FetchWeatherYandex(); } catch (Exception ex) { Log("Yandex fetch failed: " + ex.Message); }
                    }
                    else if (string.Equals(provider, "AccuWeather", StringComparison.OrdinalIgnoreCase))
                    {
                        try { data = FetchWeatherAccuWeather(); } catch (Exception ex) { Log("AccuWeather fetch failed: " + ex.Message); }
                    }
                    else if (string.Equals(provider, "MailRu", StringComparison.OrdinalIgnoreCase))
                    {
                        try { data = FetchWeatherMailRu(); } catch (Exception ex) { Log("MailRu fetch failed: " + ex.Message); }
                    }

                    if (data == null)
                    {
                        try { data = FetchWeatherYandex(); }
                        catch
                        {
                            try { data = FetchWeatherMailRu(); } catch { }
                        }
                    }

                    if (data != null)
                    {
                        _currentData = data;
                        Dispatcher.Invoke(delegate
                        {
                            _taskbarWindow.UpdateWeather(data);
                            if (_flyoutWindow.IsVisible) _flyoutWindow.UpdateData(data);
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(delegate
                        {
                            _taskbarWindow.ShowError("Сеть...");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log("FetchWeatherAsync error: " + ex.Message);
                    Dispatcher.Invoke(delegate
                    {
                        _taskbarWindow.ShowError("Сеть...");
                    });
                }
            });
        }

        private WeatherData FetchWeatherYandex()
        {
            var data = new WeatherData();
            data.City = _config.City;
            data.CitySlug = _config.CitySlug;
            data.Lat = _config.Lat;
            data.Lon = _config.Lon;
            data.ProviderName = "Яндекс.Погода";

            string url;
            if (_config.Lat != 0 && _config.Lon != 0)
            {
                url = string.Format(CultureInfo.InvariantCulture, "https://yandex.ru/pogoda/?lat={0:F4}&lon={1:F4}", _config.Lat, _config.Lon);
            }
            else
            {
                url = "https://yandex.ru/pogoda/" + (_config.CitySlug ?? "saint-petersburg");
            }

            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36";
            wc.Headers["Accept-Language"] = "ru-RU,ru;q=0.9";
            string html = wc.DownloadString(url);

            var mSummary = Regex.Match(html, "погода сейчас:\\s*([^.]+)\\..*?Температура воздуха\\s*([^,]+),\\s*ощущается как\\s*([^.]+)\\..*?Давление\\s*([^.]+)\\..*?Влажность\\s*(\\d+%).*?Восход\\s*(\\d{1,2}:\\d{2}),\\s*Закат\\s*(\\d{1,2}:\\d{2})", RegexOptions.Singleline);
            if (mSummary.Success)
            {
                data.Condition = Capitalize(mSummary.Groups[1].Value.Trim());
                string t = mSummary.Groups[2].Value.Trim().Replace("−", "-");
                if (!t.EndsWith("°C") && !t.EndsWith("°")) t += "°C";
                else if (t.EndsWith("°")) t += "C";
                data.Temp = t;

                string fl = mSummary.Groups[3].Value.Trim().Replace("−", "-");
                if (!fl.EndsWith("°")) fl += "°";
                data.FeelsLike = fl;
                string p = mSummary.Groups[4].Value.Trim();
                if (p.IndexOf("мм рт. ст.", StringComparison.OrdinalIgnoreCase) < 0) p = Regex.Replace(p, "мм.*", "мм рт. ст.");
                data.Pressure = p;
                data.Humidity = mSummary.Groups[5].Value.Trim();
                data.Sunrise = mSummary.Groups[6].Value.Trim();
                data.Sunset = mSummary.Groups[7].Value.Trim();
            }
            else
            {
                var mTemp = Regex.Match(html, "Температура воздуха\\s*([^,]+)");
                if (mTemp.Success)
                {
                    string t = mTemp.Groups[1].Value.Trim().Replace("−", "-");
                    if (!t.EndsWith("°C") && !t.EndsWith("°")) t += "°C";
                    else if (t.EndsWith("°")) t += "C";
                    data.Temp = t;
                }

                var mCond = Regex.Match(html, "погода сейчас:\\s*([^.]+)");
                if (mCond.Success) data.Condition = Capitalize(mCond.Groups[1].Value.Trim());

                var mFeels = Regex.Match(html, "ощущается как\\s*([^.]+)");
                if (mFeels.Success)
                {
                    string fl = mFeels.Groups[1].Value.Trim().Replace("−", "-");
                    if (!fl.EndsWith("°")) fl += "°";
                    data.FeelsLike = fl;
                }

                var mPress = Regex.Match(html, "Давление\\s*([\\d\\s]+мм\\s*рт\\.\\s*ст\\.)");
                if (mPress.Success) data.Pressure = mPress.Groups[1].Value.Trim();

                var mHum = Regex.Match(html, "Влажность\\s*(\\d+%)");
                if (mHum.Success) data.Humidity = mHum.Groups[1].Value.Trim();

                var mSun = Regex.Match(html, "Восход\\s*(\\d{1,2}:\\d{2})");
                if (mSun.Success) data.Sunrise = mSun.Groups[1].Value.Trim();

                var mSet = Regex.Match(html, "Закат\\s*(\\d{1,2}:\\d{2})");
                if (mSet.Success) data.Sunset = mSet.Groups[1].Value.Trim();
            }

            var now = DateTime.Now;
            string[] days = new string[] { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
            string[] months = new string[] { "", "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            data.DateText = days[(int)now.DayOfWeek] + ", " + now.Day + " " + months[now.Month];
            data.IconType = WeatherIcons.DetermineIconType(data.Condition);

            var seenDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dailyCards = Regex.Matches(html, "(?s)<p class=\"[^\"]*visuallyHidden[^\"]*\">((?:Сегодня|Завтра|[А-Яа-я]+),\\s*\\d+\\s*[а-я]+):<br />(.*?)</p>");
            foreach (Match dm in dailyCards)
            {
                string header = dm.Groups[1].Value.Trim();
                if (seenDays.Contains(header)) continue;
                seenDays.Add(header);

                string dayName;
                if (header.StartsWith("Сегодня", StringComparison.OrdinalIgnoreCase))
                    dayName = "Сегодня";
                else if (header.StartsWith("Завтра", StringComparison.OrdinalIgnoreCase))
                    dayName = "Завтра";
                else
                {
                    string[] parts = header.Split(',');
                    dayName = Capitalize(parts[0].Trim());
                }

                string body = dm.Groups[2].Value;
                string dayTemp = "";
                string dayCond = "";
                string nightTemp = "";

                var mDay = Regex.Match(body, "днём\\s*([−\\-\\+]?\\d+)°.*?([а-я\\s,]+),\\s*скорость", RegexOptions.IgnoreCase);
                if (mDay.Success)
                {
                    dayTemp = mDay.Groups[1].Value.Replace("−", "-");
                    dayCond = mDay.Groups[2].Value.Trim().Trim(',', ' ');
                }
                else
                {
                    var mDayAlt = Regex.Match(body, "днём\\s*([−\\-\\+]?\\d+)°", RegexOptions.IgnoreCase);
                    if (mDayAlt.Success) dayTemp = mDayAlt.Groups[1].Value.Replace("−", "-");
                }

                var mNight = Regex.Match(body, "ночью\\s*([−\\-\\+]?\\d+)°", RegexOptions.IgnoreCase);
                if (mNight.Success) nightTemp = mNight.Groups[1].Value.Replace("−", "-");

                if (string.IsNullOrEmpty(dayCond))
                {
                    var mAny = Regex.Match(body, ",\\s*([а-я\\s]{4,30}?),\\s*скорость", RegexOptions.IgnoreCase);
                    if (mAny.Success) dayCond = mAny.Groups[1].Value.Trim();
                    else dayCond = "Пасмурно";
                }

                string tempRange;
                if (!string.IsNullOrEmpty(nightTemp) && !string.IsNullOrEmpty(dayTemp))
                    tempRange = nightTemp + " / " + dayTemp + "°";
                else if (!string.IsNullOrEmpty(dayTemp))
                    tempRange = dayTemp + "°";
                else
                    tempRange = "0°";

                var dp = new DailyPoint();
                dp.DayName = dayName;
                dp.Condition = Capitalize(dayCond);
                dp.TempMinMax = tempRange;
                dp.IconType = WeatherIcons.DetermineIconType(dp.Condition);
                data.Daily.Add(dp);
                if (data.Daily.Count >= 5) break;
            }

            if (data.Daily.Count == 0)
            {
                try
                {
                    var fallback = FetchWeatherMailRu();
                    data.Daily = fallback.Daily;
                }
                catch { }
            }

            return data;
        }

        private WeatherData FetchWeatherAccuWeather()
        {
            var data = new WeatherData();
            data.City = _config.City;
            data.CitySlug = _config.CitySlug;
            data.Lat = _config.Lat;
            data.Lon = _config.Lon;
            data.ProviderName = "AccuWeather";

            if (!string.IsNullOrEmpty(_config.AccuWeatherApiKey))
            {
                try
                {
                    string apiKey = _config.AccuWeatherApiKey.Trim();
                    string geoUrl = string.Format(CultureInfo.InvariantCulture,
                        "https://dataservice.accuweather.com/locations/v1/cities/geoposition/search?apikey={0}&q={1:F4}%2C{2:F4}&language=ru-ru",
                        apiKey, _config.Lat, _config.Lon);

                    var wc = new WebClient();
                    wc.Encoding = Encoding.UTF8;
                    string locJson = wc.DownloadString(geoUrl);
                    var mKey = Regex.Match(locJson, "\"Key\":\\s*\"(\\d+)\"");
                    if (mKey.Success)
                    {
                        string locKey = mKey.Groups[1].Value;
                        string curUrl = string.Format("https://dataservice.accuweather.com/currentconditions/v1/{0}?apikey={1}&language=ru-ru&details=true", locKey, apiKey);
                        string curJson = wc.DownloadString(curUrl);

                        var mTemp = Regex.Match(curJson, "\"Metric\":\\s*\\{[^}]*\"Value\":\\s*([\\d\\.\\-]+)");
                        if (mTemp.Success) data.Temp = Math.Round(double.Parse(mTemp.Groups[1].Value, CultureInfo.InvariantCulture)) + "°C";

                        var mCond = Regex.Match(curJson, "\"WeatherText\":\\s*\"([^\"]+)\"");
                        if (mCond.Success) data.Condition = Capitalize(mCond.Groups[1].Value);

                        var mFeel = Regex.Match(curJson, "\"RealFeelTemperature\":\\s*\\{[^}]*\"Metric\":\\s*\\{[^}]*\"Value\":\\s*([\\d\\.\\-]+)");
                        if (mFeel.Success) data.FeelsLike = Math.Round(double.Parse(mFeel.Groups[1].Value, CultureInfo.InvariantCulture)) + "°";

                        var mHum = Regex.Match(curJson, "\"RelativeHumidity\":\\s*(\\d+)");
                        if (mHum.Success) data.Humidity = mHum.Groups[1].Value + "%";

                        var mPress = Regex.Match(curJson, "\"Pressure\":\\s*\\{[^}]*\"Metric\":\\s*\\{[^}]*\"Value\":\\s*([\\d\\.\\-]+)");
                        if (mPress.Success)
                        {
                            double mb = double.Parse(mPress.Groups[1].Value, CultureInfo.InvariantCulture);
                            data.Pressure = Math.Round(mb * 0.750062) + " мм рт. ст.";
                        }

                        string fcUrl = string.Format("https://dataservice.accuweather.com/forecasts/v1/daily/5day/{0}?apikey={1}&language=ru-ru&metric=true", locKey, apiKey);
                        string fcJson = wc.DownloadString(fcUrl);
                        var fcMatches = Regex.Matches(fcJson, "\"Date\":\\s*\"([^\"]+)\".*?\"Minimum\":\\s*\\{[^}]*\"Value\":\\s*([\\d\\.\\-]+).*?\"Maximum\":\\s*\\{[^}]*\"Value\":\\s*([\\d\\.\\-]+).*?\"IconPhrase\":\\s*\"([^\"]+)\"", RegexOptions.Singleline);
                        foreach (Match fm in fcMatches)
                        {
                            DateTime dt;
                            if (DateTime.TryParse(fm.Groups[1].Value, out dt))
                            {
                                var dp = new DailyPoint();
                                string[] shortDays = new string[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
                                dp.DayName = shortDays[(int)dt.DayOfWeek];
                                double min = Math.Round(double.Parse(fm.Groups[2].Value, CultureInfo.InvariantCulture));
                                double max = Math.Round(double.Parse(fm.Groups[3].Value, CultureInfo.InvariantCulture));
                                dp.TempMinMax = (min > 0 ? "+" : "") + min + "° / " + (max > 0 ? "+" : "") + max + "°";
                                dp.Condition = Capitalize(fm.Groups[4].Value);
                                dp.IconType = WeatherIcons.DetermineIconType(dp.Condition);
                                data.Daily.Add(dp);
                            }
                        }

                        data.IconType = WeatherIcons.DetermineIconType(data.Condition);
                        var now1 = DateTime.Now;
                        string[] d1 = new string[] { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
                        string[] m1 = new string[] { "", "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
                        data.DateText = d1[(int)now1.DayOfWeek] + ", " + now1.Day + " " + m1[now1.Month];
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    Log("AccuWeather API fetch failed: " + ex.Message);
                }
            }

            try
            {
                var yx = FetchWeatherYandex();
                yx.ProviderName = "AccuWeather (Global)";
                return yx;
            }
            catch
            {
                return FetchWeatherMailRu();
            }
        }

        private WeatherData FetchWeatherMailRu()
        {
            string url = "https://pogoda.mail.ru/prognoz/" + _config.CitySlug + "/";
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36";
            wc.Headers["Accept-Language"] = "ru-RU,ru;q=0.9";
            string html = wc.DownloadString(url);

            var data = new WeatherData();
            data.City = _config.City;
            data.CitySlug = _config.CitySlug;
            data.Lat = _config.Lat;
            data.Lon = _config.Lon;
            data.ProviderName = "Погода Mail.ru";

            var mTitle = Regex.Match(html, "В ([^,\\(]+).*?(\\-?\\+?\\d+)&deg;C,\\s*([^\"<\\r\\n]+)");
            if (mTitle.Success)
            {
                data.Temp = mTitle.Groups[2].Value.Trim() + "°C";
                data.Condition = Capitalize(mTitle.Groups[3].Value.Trim());
            }

            var mFeels = Regex.Match(html, "ощущается как\\s*(?:<!--.*?-->)?\\s*<[^>]+>([^<]+)<", RegexOptions.IgnoreCase);
            if (!mFeels.Success)
                mFeels = Regex.Match(html, "ощущается как\\s*([^\\s<\"]+)", RegexOptions.IgnoreCase);
            if (mFeels.Success)
                data.FeelsLike = mFeels.Groups[1].Value.Trim() + "°";

            var mPress = Regex.Match(html, "(\\d+)\\s*мм\\s*рт");
            if (mPress.Success)
                data.Pressure = mPress.Groups[1].Value + " мм рт. ст.";

            var mHum = Regex.Match(html, "(\\d+)%");
            if (mHum.Success)
                data.Humidity = mHum.Groups[1].Value + "%";

            var mSunrise = Regex.Match(html, "Восход.*?<span>(\\d{1,2}:\\d{2})</span>", RegexOptions.Singleline);
            if (mSunrise.Success) data.Sunrise = mSunrise.Groups[1].Value;

            var mSunset = Regex.Match(html, "Заход.*?<span>(\\d{1,2}:\\d{2})</span>", RegexOptions.Singleline);
            if (mSunset.Success) data.Sunset = mSunset.Groups[1].Value;

            var now = DateTime.Now;
            string[] days = new string[] { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
            string[] months = new string[] { "", "января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            data.DateText = days[(int)now.DayOfWeek] + ", " + now.Day + " " + months[now.Month];

            data.IconType = WeatherIcons.DetermineIconType(data.Condition);

            var daysMatches = Regex.Matches(html, "data-qa=\"MainForecastDaysItem\".*?data-qa=\"Text\"[^>]*>([^<]+)</div>.*?data-qa=\"Title\"[^>]*>([^<]+)<!--.*?data-qa=\"Text\"[^>]*>([^<]+)<!--.*?data-qa=\"Text\"[^>]*>([^<]+)</div>", RegexOptions.Singleline);
            foreach (Match dm in daysMatches)
            {
                var dp = new DailyPoint();
                dp.DayName = dm.Groups[1].Value.Trim();
                string max = dm.Groups[2].Value.Trim();
                string min = dm.Groups[3].Value.Trim();
                dp.TempMinMax = min + "° / " + max + "°";
                dp.Condition = Capitalize(dm.Groups[4].Value.Trim());
                dp.IconType = WeatherIcons.DetermineIconType(dp.Condition);
                data.Daily.Add(dp);
                if (data.Daily.Count >= 5) break;
            }

            return data;
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length == 1) return s.ToUpper();
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }


    public static class ThemeManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hDC, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static bool _cachedIsTaskbarLight = false;
        private static DateTime _lastTaskbarCheck = DateTime.MinValue;

        public static bool IsTaskbarLight()
        {
            if ((DateTime.UtcNow - _lastTaskbarCheck).TotalSeconds < 2.0)
            {
                return _cachedIsTaskbarLight;
            }
            _lastTaskbarCheck = DateTime.UtcNow;
            _cachedIsTaskbarLight = CheckTaskbarLightDirect();
            return _cachedIsTaskbarLight;
        }

        private static bool CheckTaskbarLightDirect()
        {
            try
            {
                // 1. Check screen sampling at taskbar location (sample area to right of widget)
                int screenW = GetSystemMetrics(0);
                int screenH = GetSystemMetrics(1);
                if (screenW > 0 && screenH > 0)
                {
                    int taskbarY = screenH - 20;
                    IntPtr hdc = GetDC(IntPtr.Zero);
                    if (hdc != IntPtr.Zero)
                    {
                        long totR = 0, totG = 0, totB = 0;
                        int cnt = 0;
                        int[] sampleX = new int[] { 220, 260, 300, 340, 380 };
                        for (int i = 0; i < sampleX.Length; i++)
                        {
                            if (sampleX[i] < screenW)
                            {
                                uint c = GetPixel(hdc, sampleX[i], taskbarY);
                                if (c != 0xFFFFFFFF)
                                {
                                    totR += (c & 0xFF);
                                    totG += ((c >> 8) & 0xFF);
                                    totB += ((c >> 16) & 0xFF);
                                    cnt++;
                                }
                            }
                        }
                        ReleaseDC(IntPtr.Zero, hdc);

                        if (cnt > 0)
                        {
                            double lum = 0.299 * (totR / cnt) + 0.587 * (totG / cnt) + 0.114 * (totB / cnt);
                            return lum > 135.0;
                        }
                    }
                }
            }
            catch { }

            // 2. Fallback to Windows Themes registry
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object sysTheme = key.GetValue("SystemUsesLightTheme");
                        if (sysTheme is int && (int)sysTheme == 1) return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static bool IsAppLight()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object appTheme = key.GetValue("AppsUseLightTheme");
                        if (appTheme is int && (int)appTheme == 1) return true;
                        if (appTheme is int && (int)appTheme == 0) return false;
                    }
                }
            }
            catch { }
            return IsTaskbarLight();
        }
    }

    public static class FluentTheme
    {
        private static ResourceDictionary _darkDict;
        private static ResourceDictionary _lightDict;

        public static ResourceDictionary GetMenuResources(bool isLight)
        {
            if (isLight)
            {
                if (_lightDict == null) _lightDict = CreateMenuDictionary(true);
                return _lightDict;
            }
            else
            {
                if (_darkDict == null) _darkDict = CreateMenuDictionary(false);
                return _darkDict;
            }
        }

        private static ResourceDictionary CreateMenuDictionary(bool isLight)
        {
            string bg = isLight ? "#F9F9F9" : "#2C2C2C";
            string border = isLight ? "#25000000" : "#35FFFFFF";
            string fg = isLight ? "#1A1A1A" : "#F3F3F3";
            string sep = isLight ? "#15000000" : "#25FFFFFF";
            string hover = isLight ? "#12000000" : "#22FFFFFF";
            string accent = isLight ? "#0078D4" : "#60CDFF";
            string arrow = isLight ? "#80000000" : "#A0FFFFFF";

            string xaml = string.Format(@"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <Style TargetType=""Separator"">
        <Setter Property=""Height"" Value=""1""/>
        <Setter Property=""Margin"" Value=""8,4,8,4""/>
        <Setter Property=""Background"" Value=""{3}""/>
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""Separator"">
                    <Border Height=""1"" Background=""{{TemplateBinding Background}}"" SnapsToDevicePixels=""True""/>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType=""MenuItem"">
        <Setter Property=""Foreground"" Value=""{2}""/>
        <Setter Property=""FontSize"" Value=""12.5""/>
        <Setter Property=""FontFamily"" Value=""Segoe UI Variable Text, Segoe UI""/>
        <Setter Property=""Height"" Value=""32""/>
        <Setter Property=""Margin"" Value=""2,1,2,1""/>
        <Setter Property=""Padding"" Value=""8,0,12,0""/>
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""MenuItem"">
                    <Grid SnapsToDevicePixels=""True"">
                        <Border x:Name=""BgBorder"" CornerRadius=""4"" Background=""Transparent""/>
                        <Grid Margin=""{{TemplateBinding Padding}}"">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width=""20""/>
                                <ColumnDefinition Width=""*""/>
                                <ColumnDefinition Width=""Auto""/>
                            </Grid.ColumnDefinitions>

                            <TextBlock x:Name=""CheckMark"" Text=""✓"" Foreground=""{5}"" FontSize=""13"" FontWeight=""Bold""
                                       VerticalAlignment=""Center"" HorizontalAlignment=""Left"" Visibility=""Collapsed""/>

                            <ContentPresenter Grid.Column=""1"" ContentSource=""Header"" VerticalAlignment=""Center""/>

                            <Path x:Name=""Arrow"" Grid.Column=""2"" Data=""M 0 0 L 4 4 L 0 8 Z"" Fill=""{6}""
                                  VerticalAlignment=""Center"" HorizontalAlignment=""Right"" Visibility=""Collapsed"" Margin=""8,0,0,0""/>

                            <Popup x:Name=""SubMenuPopup"" AllowsTransparency=""True"" Focusable=""False""
                                   IsOpen=""{{Binding IsSubmenuOpen, RelativeSource={{RelativeSource TemplatedParent}}}}""
                                   Placement=""Right"" PlacementTarget=""{{Binding ElementName=BgBorder}}"">
                                <Border Background=""{0}"" BorderBrush=""{1}"" BorderThickness=""1""
                                        CornerRadius=""8"" Padding=""4,4,4,4"" Margin=""0,0,8,8"">
                                    <Border.Effect>
                                        <DropShadowEffect BlurRadius=""12"" ShadowDepth=""3"" Direction=""270"" Opacity=""0.45""/>
                                    </Border.Effect>
                                    <StackPanel IsItemsHost=""True"" KeyboardNavigation.DirectionalNavigation=""Cycle""/>
                                </Border>
                            </Popup>
                        </Grid>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsHighlighted"" Value=""True"">
                            <Setter TargetName=""BgBorder"" Property=""Background"" Value=""{4}""/>
                        </Trigger>
                        <Trigger Property=""IsChecked"" Value=""True"">
                            <Setter TargetName=""CheckMark"" Property=""Visibility"" Value=""Visible""/>
                        </Trigger>
                        <Trigger Property=""HasItems"" Value=""True"">
                            <Setter TargetName=""Arrow"" Property=""Visibility"" Value=""Visible""/>
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter Property=""Foreground"" Value=""#66888888""/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType=""ContextMenu"">
        <Setter Property=""Background"" Value=""{0}""/>
        <Setter Property=""BorderBrush"" Value=""{1}""/>
        <Setter Property=""BorderThickness"" Value=""1""/>
        <Setter Property=""Foreground"" Value=""{2}""/>
        <Setter Property=""FontSize"" Value=""12.5""/>
        <Setter Property=""FontFamily"" Value=""Segoe UI Variable Text, Segoe UI""/>
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""ContextMenu"">
                    <Border Background=""{{TemplateBinding Background}}""
                            BorderBrush=""{{TemplateBinding BorderBrush}}""
                            BorderThickness=""{{TemplateBinding BorderThickness}}""
                            CornerRadius=""8""
                            Padding=""4,4,4,4""
                            Margin=""0,0,8,8"">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius=""16"" ShadowDepth=""4"" Direction=""270"" Opacity=""0.5""/>
                        </Border.Effect>
                        <StackPanel IsItemsHost=""True"" KeyboardNavigation.DirectionalNavigation=""Cycle""/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>", bg, border, fg, sep, hover, accent, arrow);

            return (ResourceDictionary)XamlReader.Parse(xaml);
        }
    }

    public class AccuWeatherKeyDialog : Window
    {
        private Program _app;
        private TextBox _txtKey;

        public AccuWeatherKeyDialog(Program app)
        {
            _app = app;
            Title = "Ключ AccuWeather API";
            Width = 440;
            Height = 240;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            BuildUI();
        }

        private void BuildUI()
        {
            bool isLight = ThemeManager.IsAppLight();
            Background = isLight ? new SolidColorBrush(Color.FromRgb(245, 245, 245)) : new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White;

            var root = new Grid();
            root.Margin = new Thickness(20);
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var lblTitle = new TextBlock();
            lblTitle.Text = "🔑  Настройка AccuWeather API";
            lblTitle.FontSize = 16;
            lblTitle.FontWeight = FontWeights.SemiBold;
            root.Children.Add(lblTitle);

            var lblDesc = new TextBlock();
            lblDesc.Text = "Введите API-ключ с сайта developer.accuweather.com.\r\nЕсли оставить пустым, виджет продолжит работу через глобальный сервис.";
            lblDesc.FontSize = 11.5;
            lblDesc.Foreground = isLight ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(180, 180, 180));
            lblDesc.Margin = new Thickness(0, 8, 0, 12);
            lblDesc.TextWrapping = TextWrapping.Wrap;
            Grid.SetRow(lblDesc, 1);
            root.Children.Add(lblDesc);

            _txtKey = new TextBox();
            _txtKey.Text = _app.Config.AccuWeatherApiKey ?? "";
            _txtKey.FontSize = 13;
            _txtKey.Padding = new Thickness(8, 6, 8, 6);
            _txtKey.Background = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(45, 45, 45));
            _txtKey.Foreground = isLight ? Brushes.Black : Brushes.White;
            _txtKey.BorderBrush = isLight ? new SolidColorBrush(Color.FromRgb(200, 200, 200)) : new SolidColorBrush(Color.FromRgb(70, 70, 70));
            Grid.SetRow(_txtKey, 2);
            root.Children.Add(_txtKey);

            var btnPanel = new StackPanel();
            btnPanel.Orientation = Orientation.Horizontal;
            btnPanel.HorizontalAlignment = HorizontalAlignment.Right;
            btnPanel.Margin = new Thickness(0, 14, 0, 0);

            var btnSave = new Button();
            btnSave.Content = "Сохранить";
            btnSave.Width = 100;
            btnSave.Height = 32;
            btnSave.Margin = new Thickness(0, 0, 10, 0);
            btnSave.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
            btnSave.Foreground = Brushes.White;
            btnSave.FontWeight = FontWeights.SemiBold;
            btnSave.Click += delegate
            {
                _app.Config.AccuWeatherApiKey = _txtKey.Text.Trim();
                _app.SetWeatherProvider("AccuWeather");
                Close();
            };
            btnPanel.Children.Add(btnSave);

            var btnCancel = new Button();
            btnCancel.Content = "Отмена";
            btnCancel.Width = 90;
            btnCancel.Height = 32;
            btnCancel.Click += delegate { Close(); };
            btnPanel.Children.Add(btnCancel);

            Grid.SetRow(btnPanel, 4);
            root.Children.Add(btnPanel);

            Content = root;
        }
    }

    public class TaskbarWidgetWindow : Window
    {
        private Program _app;
        private Border _hoverBorder;
        private Canvas _iconCanvas;
        private TextBlock _txtTemp;
        private TextBlock _txtCond;
        private DispatcherTimer _posTimer;
        private bool _isHovered = false;
        private bool _currentIsLight = false;
        private WeatherData _lastWeatherData = null;

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public const int GWLP_HWNDPARENT = -8;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [ComImport]
        [Guid("56FDF342-43D4-11d6-9D06-00A0C90F2732")]
        [ClassInterface(ClassInterfaceType.None)]
        private class CTaskbarList { }

        [ComImport]
        [Guid("56FDF344-43D4-11d6-9D06-00A0C90F2732")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList
        {
            void HrInit();
            void AddTab(IntPtr hWnd);
            void DeleteTab(IntPtr hWnd);
            void ActivateTab(IntPtr hWnd);
            void SetActiveAlt(IntPtr hWnd);
        }

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_HIDEWINDOW = 0x0080;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_SHOWWINDOW = 0x0018;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MINIMIZE = 0xF020;
        public const int WM_SIZE = 0x0005;
        public const int SIZE_MINIMIZED = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_APPWINDOW = 0x00040000;

        private IntPtr _lastOwnerTray = IntPtr.Zero;

        public TaskbarWidgetWindow(Program app)
        {
            _app = app;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            Width = 165;
            Height = 40;
            Left = 12;
            Top = SystemParameters.PrimaryScreenHeight - 44;

            BuildUI();

            StateChanged += delegate
            {
                if (WindowState != WindowState.Normal)
                {
                    WindowState = WindowState.Normal;
                }
            };

            IsVisibleChanged += delegate
            {
                if (!IsVisible)
                {
                    Show();
                }
            };

            Loaded += delegate
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                Program.Log("TaskbarWidgetWindow Loaded begin: hwnd=" + helper.Handle);

                int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                exStyle &= ~WS_EX_APPWINDOW;
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle);

                IntPtr hTray = FindWindow("Shell_TrayWnd", null);
                if (hTray != IntPtr.Zero)
                {
                    SetWindowLongPtr(helper.Handle, GWLP_HWNDPARENT, hTray);
                    _lastOwnerTray = hTray;
                }

                try
                {
                    var tb = (ITaskbarList)new CTaskbarList();
                    tb.HrInit();
                    tb.DeleteTab(helper.Handle);
                }
                catch { }

                try
                {
                    var src = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
                    if (src != null) src.AddHook(WndProc);
                }
                catch { }

                UpdatePosition();
                Program.Log("TaskbarWidgetWindow Loaded end");
            };

            _posTimer = new DispatcherTimer();
            _posTimer.Interval = TimeSpan.FromMilliseconds(500);
            _posTimer.Tick += (s, e) => UpdatePosition();
            _posTimer.Start();

            Closed += delegate { RestoreTaskbar(); };
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_LBUTTONUP = 0x0202;
            const int WM_RBUTTONUP = 0x0205;
            if (msg == WM_LBUTTONUP)
            {
                _app.ToggleFlyout(0);
                handled = true;
                return IntPtr.Zero;
            }
            if (msg == WM_RBUTTONUP)
            {
                ShowContextMenu();
                handled = true;
                return IntPtr.Zero;
            }
            if (msg == WM_SYSCOMMAND && (wParam.ToInt64() & 0xFFF0) == SC_MINIMIZE)
            {
                // Prevent minimization by "Show Desktop" (Win+D, corner button, etc.)
                handled = true;
                return IntPtr.Zero;
            }
            if (msg == WM_SHOWWINDOW && wParam == IntPtr.Zero)
            {
                // Prevent hiding by "Show Desktop" sweep
                handled = true;
                return IntPtr.Zero;
            }
            if (msg == WM_SIZE && wParam.ToInt64() == SIZE_MINIMIZED)
            {
                // Prevent state transition to minimized
                handled = true;
                return IntPtr.Zero;
            }
            if (msg == WM_WINDOWPOSCHANGING && lParam != IntPtr.Zero)
            {
                try
                {
                    var pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                    bool changed = false;
                    if (pos.x <= -30000 || pos.y <= -30000)
                    {
                        pos.flags |= SWP_NOMOVE;
                        changed = true;
                    }
                    if ((pos.flags & SWP_HIDEWINDOW) != 0)
                    {
                        pos.flags &= ~SWP_HIDEWINDOW;
                        changed = true;
                    }
                    if (changed)
                    {
                        Marshal.StructureToPtr(pos, lParam, false);
                    }
                }
                catch { }
            }
            return IntPtr.Zero;
        }

        private void BuildUI()
        {
            var outerContainer = new Grid();
            outerContainer.Background = Brushes.Transparent;
            outerContainer.Cursor = Cursors.Hand;
            outerContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
            outerContainer.VerticalAlignment = VerticalAlignment.Stretch;

            _hoverBorder = new Border();
            _hoverBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            _hoverBorder.VerticalAlignment = VerticalAlignment.Stretch;
            _hoverBorder.Margin = new Thickness(0);
            _hoverBorder.Padding = new Thickness(6, 2, 8, 2);
            _hoverBorder.Cursor = Cursors.Hand;
            _hoverBorder.CornerRadius = new CornerRadius(5);

            var stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.VerticalAlignment = VerticalAlignment.Center;

            _iconCanvas = new Canvas();
            _iconCanvas.Width = 24;
            _iconCanvas.Height = 24;
            _iconCanvas.VerticalAlignment = VerticalAlignment.Center;
            stack.Children.Add(_iconCanvas);

            var textStack = new StackPanel();
            textStack.Orientation = Orientation.Vertical;
            textStack.Margin = new Thickness(7, 0, 0, 0);
            textStack.VerticalAlignment = VerticalAlignment.Center;

            _txtTemp = new TextBlock();
            _txtTemp.Text = "...";
            _txtTemp.FontSize = 12;
            _txtTemp.FontWeight = FontWeights.SemiBold;
            _txtTemp.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            _txtCond = new TextBlock();
            _txtCond.Text = "Загрузка...";
            _txtCond.FontSize = 10.5;
            _txtCond.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            _txtCond.TextTrimming = TextTrimming.CharacterEllipsis;
            _txtCond.MaxWidth = 120;

            textStack.Children.Add(_txtTemp);
            textStack.Children.Add(_txtCond);
            stack.Children.Add(textStack);

            _hoverBorder.Child = stack;
            outerContainer.Children.Add(_hoverBorder);
            Content = outerContainer;

            ApplyStyle();

            _hoverBorder.MouseEnter += delegate
            {
                _isHovered = true;
                ApplyStyle();
            };

            _hoverBorder.MouseLeave += delegate
            {
                _isHovered = false;
                ApplyStyle();
            };

            MouseButtonEventHandler leftClick = (s, e) =>
            {
                e.Handled = true;
                _app.ToggleFlyout(0);
            };

            MouseButtonEventHandler rightClick = (s, e) =>
            {
                e.Handled = true;
                ShowContextMenu();
            };

            _hoverBorder.PreviewMouseLeftButtonUp += leftClick;
            _hoverBorder.PreviewMouseRightButtonUp += rightClick;
            outerContainer.PreviewMouseLeftButtonUp += leftClick;
            outerContainer.PreviewMouseRightButtonUp += rightClick;
            this.PreviewMouseLeftButtonUp += leftClick;
            this.PreviewMouseRightButtonUp += rightClick;
        }

        private void ShowContextMenu()
        {
            _app.IsContextMenuOpen = true;
            bool isLight = ThemeManager.IsTaskbarLight();
            var menu = new ContextMenu();
            menu.Resources = FluentTheme.GetMenuResources(isLight);
            menu.Closed += delegate { _app.IsContextMenuOpen = false; };

            var mRefresh = new MenuItem();
            mRefresh.Header = "🔄  Обновить погоду";
            mRefresh.Click += delegate { _app.FetchWeatherAsync(); };
            menu.Items.Add(mRefresh);

            menu.Items.Add(new Separator());

            // --- Weather Provider Submenu ---
            var mProviders = new MenuItem();
            string curProv = _app.Config.WeatherProvider ?? "Yandex";
            string provLabel = "Яндекс.Погода";
            if (string.Equals(curProv, "AccuWeather", StringComparison.OrdinalIgnoreCase)) provLabel = "AccuWeather";
            else if (string.Equals(curProv, "MailRu", StringComparison.OrdinalIgnoreCase)) provLabel = "Mail.ru";

            mProviders.Header = "🌐  Сервис погоды (" + provLabel + ")";

            var mProvYandex = new MenuItem();
            mProvYandex.Header = "Яндекс.Погода (рекомендуется)";
            mProvYandex.IsCheckable = true;
            mProvYandex.IsChecked = string.Equals(curProv, "Yandex", StringComparison.OrdinalIgnoreCase);
            mProvYandex.Click += delegate { _app.SetWeatherProvider("Yandex"); };
            mProviders.Items.Add(mProvYandex);

            var mProvAccu = new MenuItem();
            mProvAccu.Header = "AccuWeather (глобальный)";
            mProvAccu.IsCheckable = true;
            mProvAccu.IsChecked = string.Equals(curProv, "AccuWeather", StringComparison.OrdinalIgnoreCase);
            mProvAccu.Click += delegate { _app.SetWeatherProvider("AccuWeather"); };
            mProviders.Items.Add(mProvAccu);

            var mProvMail = new MenuItem();
            mProvMail.Header = "Погода Mail.ru";
            mProvMail.IsCheckable = true;
            mProvMail.IsChecked = string.Equals(curProv, "MailRu", StringComparison.OrdinalIgnoreCase);
            mProvMail.Click += delegate { _app.SetWeatherProvider("MailRu"); };
            mProviders.Items.Add(mProvMail);

            mProviders.Items.Add(new Separator());

            var mAccuKey = new MenuItem();
            mAccuKey.Header = "🔑  Ключ AccuWeather API...";
            mAccuKey.Click += delegate
            {
                var dlg = new AccuWeatherKeyDialog(_app);
                dlg.ShowDialog();
            };
            mProviders.Items.Add(mAccuKey);

            menu.Items.Add(mProviders);

            menu.Items.Add(new Separator());

            var mSearchCity = new MenuItem();
            mSearchCity.Header = "🔍  Выбрать другой город...";
            mSearchCity.FontWeight = FontWeights.SemiBold;
            mSearchCity.Click += delegate
            {
                var dlg = new CitySearchDialog(_app);
                dlg.ShowDialog();
            };
            menu.Items.Add(mSearchCity);

            // Recent cities (max 3)
            int cityCount = 0;
            foreach (var c in _app.Config.RecentCities)
            {
                if (cityCount >= 3) break;
                AddCityItem(menu, c);
                cityCount++;
            }

            menu.Items.Add(new Separator());

            // Refresh interval submenu
            var mIntervals = new MenuItem();
            mIntervals.Header = "⏱️  Интервал обновления (" + _app.Config.RefreshIntervalMinutes + " мин)";
            int[] standardIntervals = new int[] { 5, 10, 15, 30, 60, 120 };
            foreach (int min in standardIntervals)
            {
                int curr = min;
                string label = curr < 60 ? (curr + " минут") : (curr / 60 + " ч");
                if (curr == 15) label += " (стандарт)";
                var item = new MenuItem();
                item.Header = label;
                item.IsCheckable = true;
                item.IsChecked = (_app.Config.RefreshIntervalMinutes == curr);
                item.Click += delegate { _app.SetRefreshInterval(curr); };
                mIntervals.Items.Add(item);
            }
            mIntervals.Items.Add(new Separator());
            var mCustomInterval = new MenuItem();
            mCustomInterval.Header = "Свой интервал...";
            mCustomInterval.Click += delegate
            {
                var dlg = new IntervalDialog(_app);
                dlg.ShowDialog();
            };
            mIntervals.Items.Add(mCustomInterval);
            menu.Items.Add(mIntervals);

            menu.Items.Add(new Separator());

            var mAuto = new MenuItem();
            mAuto.Header = "🚀  Автозапуск с Windows";
            mAuto.IsCheckable = true;
            mAuto.IsChecked = _app.Config.AutoStart;
            mAuto.Click += delegate
            {
                _app.Config.AutoStart = !_app.Config.AutoStart;
                _app.SaveConfig();
            };
            menu.Items.Add(mAuto);

            menu.Items.Add(new Separator());

            var mDisableNative = new MenuItem();
            mDisableNative.Header = "⚙️  Скрыть штатный виджет Windows (принудительно)";
            mDisableNative.Click += delegate
            {
                var res = MessageBox.Show(
                    "Отключить встроенный виджет Windows 11 через групповую политику и перезапустить Проводник?\r\n\r\n(Потребуется подтвердить права администратора в окне UAC)",
                    "Скрыть штатный виджет Windows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    Program.ForceDisableNativeWidgetsAndRestartExplorer();
                }
            };
            menu.Items.Add(mDisableNative);

            var mTaskbarSettings = new MenuItem();
            mTaskbarSettings.Header = "🛠️  Параметры панели задач (выключить вручную)";
            mTaskbarSettings.Click += delegate
            {
                try
                {
                    Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true });
                }
                catch { }
            };
            menu.Items.Add(mTaskbarSettings);

            menu.Items.Add(new Separator());

            var mExit = new MenuItem();
            mExit.Header = "❌  Закрыть виджет";
            mExit.Click += delegate { 
                RestoreTaskbar();
                Application.Current.Shutdown(); 
            };
            menu.Items.Add(mExit);

            menu.IsOpen = true;
        }

        private void AddCityItem(ContextMenu menu, string name)
        {
            var item = new MenuItem();
            item.Header = "📍  " + name;
            item.IsCheckable = true;
            item.IsChecked = string.Equals(_app.Config.City, name, StringComparison.OrdinalIgnoreCase);
            item.Click += delegate
            {
                string disp, slug;
                double lat, lon;
                CityResolver.ResolveCity(name, out disp, out slug, out lat, out lon);
                _app.SetCity(disp, slug, lat, lon);
            };
            menu.Items.Add(item);
        }

        public void UpdateWeather(WeatherData data)
        {
            _lastWeatherData = data;
            _txtTemp.Text = data.Temp;
            _txtCond.Text = data.Condition;
            ApplyStyle();
        }

        public void ShowError(string msg)
        {
            _txtCond.Text = msg;
        }

        private void ApplyStyle()
        {
            if (_hoverBorder == null) return;
            bool isLight = ThemeManager.IsTaskbarLight();
            _currentIsLight = isLight;

            _hoverBorder.BorderThickness = new Thickness(0);
            _hoverBorder.BorderBrush = Brushes.Transparent;
            _hoverBorder.CornerRadius = new CornerRadius(5);
            _hoverBorder.Effect = null;

            if (_isHovered)
            {
                if (isLight)
                {
                    _hoverBorder.Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
                }
                else
                {
                    _hoverBorder.Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255));
                }
            }
            else
            {
                _hoverBorder.Background = Brushes.Transparent;
            }

            if (isLight)
            {
                _txtTemp.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                _txtCond.Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            }
            else
            {
                _txtTemp.Foreground = Brushes.White;
                _txtCond.Foreground = new SolidColorBrush(Color.FromArgb(215, 255, 255, 255));
            }

            if (_iconCanvas != null)
            {
                string icon = _lastWeatherData != null ? _lastWeatherData.IconType : "partly_cloudy";
                WeatherIcons.DrawIcon(_iconCanvas, icon, 24, isLight);
            }
        }

        public void UpdatePosition()
        {
            bool isLight = ThemeManager.IsTaskbarLight();
            if (isLight != _currentIsLight)
            {
                _currentIsLight = isLight;
                ApplyStyle();
            }

            IntPtr hTray = FindWindow("Shell_TrayWnd", null);
            int trayTop = 0;
            int trayHeight = 48;
            if (hTray != IntPtr.Zero)
            {
                RECT r;
                GetWindowRect(hTray, out r);
                trayTop = r.Top;
                trayHeight = r.Bottom - r.Top;
            }
            else
            {
                trayTop = (int)SystemParameters.PrimaryScreenHeight - 48;
            }

            double dpiX = 1.0;
            double dpiY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            int targetLeft = 12;
            int taskbarAl = GetTaskbarAlignment();
            if (taskbarAl == 0 && hTray != IntPtr.Zero)
            {
                IntPtr hNotify = FindWindowEx(hTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hNotify != IntPtr.Zero)
                {
                    RECT nr;
                    GetWindowRect(hNotify, out nr);
                    targetLeft = (int)((nr.Left / dpiX) - Width - 12);
                }
            }

            int targetTop = trayTop + (trayHeight - (int)(Height * dpiY)) / 2;

            if (Double.IsNaN(Left) || Math.Abs(Left - targetLeft) > 1 || Double.IsNaN(Top) || Math.Abs(Top - (targetTop / dpiY)) > 1)
            {
                Left = targetLeft;
                Top = targetTop / dpiY;
            }

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                if (hTray != IntPtr.Zero && hTray != _lastOwnerTray)
                {
                    SetWindowLongPtr(helper.Handle, GWLP_HWNDPARENT, hTray);
                    _lastOwnerTray = hTray;
                    try
                    {
                        var tb = (ITaskbarList)new CTaskbarList();
                        tb.HrInit();
                        tb.DeleteTab(helper.Handle);
                    }
                    catch { }
                }

                RECT curR;
                GetWindowRect(helper.Handle, out curR);
                int targetX = (int)(targetLeft * dpiX);
                int targetY = targetTop;
                int targetW = (int)(Width * dpiX);
                int targetH = (int)(Height * dpiY);

                bool needsShow = !IsWindowVisible(helper.Handle) || WindowState != WindowState.Normal || !IsVisible || IsIconic(helper.Handle);
                if (needsShow)
                {
                    if (WindowState != WindowState.Normal) WindowState = WindowState.Normal;
                    if (!IsVisible) Show();
                }

                if (needsShow || curR.Left != targetX || curR.Top != targetY || (curR.Right - curR.Left) != targetW || (curR.Bottom - curR.Top) != targetH)
                {
                    SetWindowPos(helper.Handle, HWND_TOPMOST, targetX, targetY, targetW, targetH, SWP_SHOWWINDOW | SWP_NOACTIVATE);
                }
            }
        }

        private static int GetTaskbarAlignment()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("TaskbarAl");
                        if (val is int) return (int)val;
                    }
                }
            }
            catch { }
            return 1;
        }

        public void RestoreTaskbar()
        {
            try
            {
                IntPtr hTray = FindWindow("Shell_TrayWnd", null);
                if (hTray != IntPtr.Zero)
                {
                    SetWindowRgn(hTray, IntPtr.Zero, true);
                }
            }
            catch { }
        }
    }
    public class FlyoutWindow : Window
    {
        private Program _app;
        private int _currentPage = 0; // 0 = Forecast, 1 = Radar

        // Root & Container elements
        private Border _mainBorder;

        // Navigation elements
        private Button _tabForecast;
        private Button _tabRadar;
        private Grid _pageForecast;
        private Grid _pageRadar;

        private Button _btnSearch;
        private Button _btnInterval;
        private Button _btnRefresh;
        private Button _btnClose;

        // Page 1 Forecast elements
        private TextBlock _txtCity;
        private TextBlock _txtDate;
        private Canvas _heroIcon;
        private TextBlock _txtHeroTemp;
        private TextBlock _txtHeroCond;
        private TextBlock _txtHeroFeels;
        private Border _tileHum;
        private Border _tilePress;
        private Border _tileSun;
        private Border _tileSet;
        private TextBlock _lblHumTitle;
        private TextBlock _lblPressTitle;
        private TextBlock _lblSunTitle;
        private TextBlock _lblSetTitle;
        private TextBlock _txtHumidity;
        private TextBlock _txtPressure;
        private TextBlock _txtSunrise;
        private TextBlock _txtSunset;
        private TextBlock _lblDailyTitle;
        private StackPanel _dailyPanel;

        // Page 2 Radar elements
        private Border _headCard;
        private TextBlock _txtRadarCity;
        private TextBlock _txtRadarStatus;
        private Microsoft.Web.WebView2.Wpf.WebView2 _webView;
        private TextBlock _lblMapLoading;
        private Border _radarContainer;
        private bool _isWebViewInitialized = false;
        private string _lastRadarUrl = "";
        private string _currentCity = "";
        private WeatherData _lastData = null;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseProc;
        private IntPtr _mouseHook = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        public const int WH_MOUSE_LL = 14;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_NCRBUTTONDOWN = 0x00A4;

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public DateTime LastOpenTime = DateTime.MinValue;

        public void StartMouseHook()
        {
            if (_mouseHook == IntPtr.Zero)
            {
                try
                {
                    _mouseProc = MouseHookCallback;
                    using (var curProcess = Process.GetCurrentProcess())
                    using (var curModule = curProcess.MainModule)
                    {
                        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
                catch { }
            }
        }

        public void StopMouseHook()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                try
                {
                    UnhookWindowsHookEx(_mouseHook);
                    _mouseHook = IntPtr.Zero;
                }
                catch { }
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN || wParam == (IntPtr)WM_NCLBUTTONDOWN || wParam == (IntPtr)WM_NCRBUTTONDOWN))
            {
                try
                {
                    var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    int px = hookStruct.pt.x;
                    int py = hookStruct.pt.y;

                    RECT flyoutRect;
                    var flyoutHelper = new System.Windows.Interop.WindowInteropHelper(this);
                    bool insideFlyout = false;
                    if (flyoutHelper.Handle != IntPtr.Zero && GetWindowRect(flyoutHelper.Handle, out flyoutRect))
                    {
                        if (px >= flyoutRect.Left && px <= flyoutRect.Right && py >= flyoutRect.Top && py <= flyoutRect.Bottom)
                        {
                            insideFlyout = true;
                        }
                    }

                    bool insideWidget = false;
                    if (_app != null && _app.TaskbarWindow != null)
                    {
                        var tbHelper = new System.Windows.Interop.WindowInteropHelper(_app.TaskbarWindow);
                        RECT tbRect;
                        if (tbHelper.Handle != IntPtr.Zero && GetWindowRect(tbHelper.Handle, out tbRect))
                        {
                            if (px >= tbRect.Left && px <= tbRect.Right && py >= tbRect.Top && py <= tbRect.Bottom)
                            {
                                insideWidget = true;
                            }
                        }
                    }

                    bool insideMenu = _app != null && _app.IsContextMenuOpen;

                    if (!insideFlyout && !insideWidget && !insideMenu)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Hide();
                        }));
                    }
                }
                catch { }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == Window.VisibilityProperty)
            {
                if (Visibility == Visibility.Visible)
                {
                    StartMouseHook();
                }
                else
                {
                    StopMouseHook();
                }
            }
        }

        public FlyoutWindow(Program app)
        {
            _app = app;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            Width = 370;
            Height = 530;

            try
            {
                string icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(icoPath));
                }
            }
            catch { }

            BuildUI();

            Deactivated += delegate
            {
                if ((DateTime.UtcNow - LastOpenTime).TotalMilliseconds > 350)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(this);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        POINT curPt;
                        if (GetCursorPos(out curPt))
                        {
                            RECT flyoutRect;
                            if (GetWindowRect(helper.Handle, out flyoutRect))
                            {
                                if (curPt.x >= flyoutRect.Left && curPt.x <= flyoutRect.Right &&
                                    curPt.y >= flyoutRect.Top && curPt.y <= flyoutRect.Bottom)
                                {
                                    return; // Cursor is inside flyout!
                                }
                            }
                        }
                    }

                    uint fgPid = 0;
                    IntPtr fg = IntPtr.Zero;
                    try
                    {
                        fg = GetForegroundWindow();
                        if (fg != IntPtr.Zero)
                        {
                            GetWindowThreadProcessId(fg, out fgPid);
                            uint myPid = (uint)Process.GetCurrentProcess().Id;
                            if (fgPid == myPid)
                            {
                                return; // Focus inside our app
                            }

                            using (var p = Process.GetProcessById((int)fgPid))
                            {
                                if (p.ProcessName.IndexOf("msedge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    p.ProcessName.IndexOf("webview2", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    p.ProcessName.IndexOf("RuWeather", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    return; // WebView2 process
                                }
                            }
                        }
                    }
                    catch { }

                    Program.Log(string.Format("Flyout Deactivated: hiding. fg={0}, fgPid={1}", fg, fgPid));
                    Hide();
                }
            };

            Closed += delegate
            {
                StopMouseHook();
            };

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Hide();
                }
                else if (e.Key == Key.Left)
                {
                    SetPage(0);
                }
                else if (e.Key == Key.Right)
                {
                    SetPage(1);
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int val = DWMWCP_ROUND;
                    DwmSetWindowAttribute(helper.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref val, sizeof(int));
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            _mainBorder = new Border();
            _mainBorder.CornerRadius = new CornerRadius(8);
            _mainBorder.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            _mainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            _mainBorder.BorderThickness = new Thickness(1);
            _mainBorder.Padding = new Thickness(14);

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 0: Top Nav bar
            rootGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // 1: Content Pages

            // --- 0. Top Navigation Bar with Tabs & Action Buttons ---
            var navGrid = new Grid();
            navGrid.Margin = new Thickness(0, 0, 0, 10);
            navGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            // Tab Switcher
            var tabStack = new StackPanel();
            tabStack.Orientation = Orientation.Horizontal;

            _tabForecast = CreateTabButton("☀️ Прогноз", true);
            _tabForecast.Click += delegate { SetPage(0); };
            tabStack.Children.Add(_tabForecast);

            _tabRadar = CreateTabButton("🌧️ Карта осадков", false);
            _tabRadar.Margin = new Thickness(6, 0, 0, 0);
            _tabRadar.Click += delegate { SetPage(1); };
            tabStack.Children.Add(_tabRadar);

            Grid.SetColumn(tabStack, 0);
            navGrid.Children.Add(tabStack);

            // Action Buttons
            var btnStack = new StackPanel();
            btnStack.Orientation = Orientation.Horizontal;

            _btnSearch = CreateIconButton("🔍", "Выбрать город");
            _btnSearch.Click += delegate
            {
                var dlg = new CitySearchDialog(_app);
                dlg.ShowDialog();
            };
            btnStack.Children.Add(_btnSearch);

            _btnInterval = CreateIconButton("⏱️", "Интервал обновления");
            _btnInterval.Click += delegate
            {
                var dlg = new IntervalDialog(_app);
                dlg.ShowDialog();
            };
            btnStack.Children.Add(_btnInterval);

            _btnRefresh = CreateIconButton("🔄", "Обновить погоду");
            _btnRefresh.Click += delegate { _app.FetchWeatherAsync(); };
            btnStack.Children.Add(_btnRefresh);

            _btnClose = CreateIconButton("✕", "Закрыть (Esc)");
            _btnClose.Click += delegate { Hide(); };
            btnStack.Children.Add(_btnClose);

            Grid.SetColumn(btnStack, 1);
            navGrid.Children.Add(btnStack);

            Grid.SetRow(navGrid, 0);
            rootGrid.Children.Add(navGrid);

            // --- 1. Pages Container ---
            var pagesGrid = new Grid();
            Grid.SetRow(pagesGrid, 1);

            // PAGE 1: Forecast
            BuildForecastPage();
            pagesGrid.Children.Add(_pageForecast);

            // PAGE 2: Radar
            BuildRadarPage();
            pagesGrid.Children.Add(_pageRadar);

            rootGrid.Children.Add(pagesGrid);

            _mainBorder.Child = rootGrid;
            Content = _mainBorder;

            ApplyTheme();
            SetPage(0);
        }

        private Button CreateTabButton(string text, bool isActive)
        {
            var btn = new Button();
            btn.Content = text;
            btn.FontSize = 11.5;
            btn.FontWeight = FontWeights.SemiBold;
            btn.Padding = new Thickness(10, 5, 10, 5);
            btn.BorderThickness = new Thickness(1);
            btn.Cursor = Cursors.Hand;
            UpdateTabStyle(btn, isActive);
            return btn;
        }

        private void UpdateTabStyle(Button btn, bool isActive)
        {
            if (btn == null) return;
            bool isLight = ThemeManager.IsAppLight();
            if (isActive)
            {
                if (isLight)
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(40, 0, 103, 192));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(0, 103, 192));
                    btn.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 0, 103, 192));
                }
                else
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(76, 194, 255));
                    btn.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 0, 120, 215));
                }
            }
            else
            {
                if (isLight)
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    btn.BorderBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
                }
                else
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                    btn.BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                }
            }
        }

        private Button CreateIconButton(string icon, string tooltip)
        {
            var btn = new Button();
            btn.Content = icon;
            btn.FontSize = 12;
            btn.Width = 26;
            btn.Height = 26;
            btn.Margin = new Thickness(4, 0, 0, 0);
            btn.BorderThickness = new Thickness(1);
            btn.Cursor = Cursors.Hand;
            btn.ToolTip = tooltip;
            ApplyIconButtonTheme(btn, ThemeManager.IsAppLight());
            return btn;
        }

        private void ApplyIconButtonTheme(Button btn, bool isLight)
        {
            if (btn == null) return;
            if (isLight)
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                btn.Foreground = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            }
        }

        public void SetPage(int page)
        {
            _currentPage = page;
            UpdateTabStyle(_tabForecast, page == 0);
            UpdateTabStyle(_tabRadar, page == 1);
            if (page == 0)
            {
                _pageForecast.Visibility = Visibility.Visible;
                _pageRadar.Visibility = Visibility.Collapsed;
            }
            else
            {
                _pageForecast.Visibility = Visibility.Collapsed;
                _pageRadar.Visibility = Visibility.Visible;
                LoadRadarMap();
            }
        }

        private void BuildForecastPage()
        {
            _pageForecast = new Grid();
            _pageForecast.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Header info
            _pageForecast.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Hero
            _pageForecast.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Metrics
            _pageForecast.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Daily Title
            _pageForecast.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Daily Cards

            // 1. City + Date
            var cityStack = new StackPanel();
            cityStack.Margin = new Thickness(0, 0, 0, 6);
            _txtCity = new TextBlock();
            _txtCity.Text = "Санкт-Петербург";
            _txtCity.FontSize = 16;
            _txtCity.FontWeight = FontWeights.Bold;
            _txtCity.Foreground = Brushes.White;
            _txtCity.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            _txtCity.Cursor = Cursors.Hand;
            _txtCity.ToolTip = "Нажмите для смены города";
            _txtCity.PreviewMouseLeftButtonDown += delegate
            {
                var dlg = new CitySearchDialog(_app);
                dlg.ShowDialog();
            };

            _txtDate = new TextBlock();
            _txtDate.Text = "Загрузка...";
            _txtDate.FontSize = 11;
            _txtDate.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
            _txtDate.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            _txtDate.Margin = new Thickness(0, 2, 0, 0);

            cityStack.Children.Add(_txtCity);
            cityStack.Children.Add(_txtDate);
            Grid.SetRow(cityStack, 0);
            _pageForecast.Children.Add(cityStack);

            // 2. Hero Card
            var heroGrid = new Grid();
            heroGrid.Margin = new Thickness(0, 6, 0, 8);
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            _heroIcon = new Canvas();
            _heroIcon.Width = 52;
            _heroIcon.Height = 52;
            _heroIcon.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(_heroIcon, 0);
            heroGrid.Children.Add(_heroIcon);

            var heroTextStack = new StackPanel();
            heroTextStack.Margin = new Thickness(12, 0, 0, 0);
            heroTextStack.VerticalAlignment = VerticalAlignment.Center;

            var tempRow = new StackPanel();
            tempRow.Orientation = Orientation.Horizontal;

            _txtHeroTemp = new TextBlock();
            _txtHeroTemp.Text = "0°";
            _txtHeroTemp.FontSize = 32;
            _txtHeroTemp.FontWeight = FontWeights.Bold;
            _txtHeroTemp.Foreground = Brushes.White;
            _txtHeroTemp.FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");

            _txtHeroCond = new TextBlock();
            _txtHeroCond.Text = "Переменная облачность";
            _txtHeroCond.FontSize = 13;
            _txtHeroCond.FontWeight = FontWeights.SemiBold;
            _txtHeroCond.Foreground = Brushes.White;
            _txtHeroCond.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            _txtHeroCond.Margin = new Thickness(10, 10, 0, 0);

            tempRow.Children.Add(_txtHeroTemp);
            tempRow.Children.Add(_txtHeroCond);

            _txtHeroFeels = new TextBlock();
            _txtHeroFeels.Text = "Ощущается как -3°";
            _txtHeroFeels.FontSize = 11;
            _txtHeroFeels.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            _txtHeroFeels.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            heroTextStack.Children.Add(tempRow);
            heroTextStack.Children.Add(_txtHeroFeels);
            Grid.SetColumn(heroTextStack, 1);
            heroGrid.Children.Add(heroTextStack);

            Grid.SetRow(heroGrid, 1);
            _pageForecast.Children.Add(heroGrid);

            // 3. Metrics (4 Cards in 2x2 grid: Humidity, Pressure, Sunrise, Sunset)
            var metricsGrid = new Grid();
            metricsGrid.Margin = new Thickness(0, 0, 0, 8);
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            metricsGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            _txtHumidity = new TextBlock();
            _txtPressure = new TextBlock();
            _txtSunrise = new TextBlock();
            _txtSunset = new TextBlock();

            _tileHum = MakeMetricTile("💧 Влажность", _txtHumidity, out _lblHumTitle);
            _tilePress = MakeMetricTile("🌡️ Давление", _txtPressure, out _lblPressTitle);
            _tileSun = MakeMetricTile("🌅 Восход", _txtSunrise, out _lblSunTitle);
            _tileSet = MakeMetricTile("🌇 Закат", _txtSunset, out _lblSetTitle);

            Grid.SetRow(_tileHum, 0); Grid.SetColumn(_tileHum, 0);
            Grid.SetRow(_tilePress, 0); Grid.SetColumn(_tilePress, 1);
            Grid.SetRow(_tileSun, 1); Grid.SetColumn(_tileSun, 0);
            Grid.SetRow(_tileSet, 1); Grid.SetColumn(_tileSet, 1);

            metricsGrid.Children.Add(_tileHum);
            metricsGrid.Children.Add(_tilePress);
            metricsGrid.Children.Add(_tileSun);
            metricsGrid.Children.Add(_tileSet);

            Grid.SetRow(metricsGrid, 2);
            _pageForecast.Children.Add(metricsGrid);

            // 4. Daily Title
            _lblDailyTitle = new TextBlock();
            _lblDailyTitle.Text = "ПРОГНОЗ НА 5 ДНЕЙ";
            _lblDailyTitle.FontSize = 10;
            _lblDailyTitle.FontWeight = FontWeights.Bold;
            _lblDailyTitle.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            _lblDailyTitle.Margin = new Thickness(0, 0, 0, 6);
            Grid.SetRow(_lblDailyTitle, 3);
            _pageForecast.Children.Add(_lblDailyTitle);

            // 5. Daily Cards
            _dailyPanel = new StackPanel();
            _dailyPanel.Orientation = Orientation.Vertical;
            Grid.SetRow(_dailyPanel, 4);
            _pageForecast.Children.Add(_dailyPanel);
        }

        private void BuildRadarPage()
        {
            _pageRadar = new Grid();
            _pageRadar.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 0: Header card
            _pageRadar.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // 1: WebView2
            _pageRadar.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 2: Open in browser button

            // 1. Radar Header Card
            _headCard = new Border();
            _headCard.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            _headCard.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            _headCard.BorderThickness = new Thickness(1);
            _headCard.CornerRadius = new CornerRadius(6);
            _headCard.Padding = new Thickness(10, 7, 10, 7);
            _headCard.Margin = new Thickness(0, 0, 0, 8);

            var headStack = new StackPanel();
            _txtRadarCity = new TextBlock();
            _txtRadarCity.Text = "📍 " + _app.Config.City;
            _txtRadarCity.FontSize = 13;
            _txtRadarCity.FontWeight = FontWeights.Bold;
            _txtRadarCity.Foreground = Brushes.White;

            _txtRadarStatus = new TextBlock();
            _txtRadarStatus.Text = "🌧️ Перемещайте карту мышью • Колёсико — масштаб";
            _txtRadarStatus.FontSize = 11;
            _txtRadarStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 194, 255));
            _txtRadarStatus.Margin = new Thickness(0, 2, 0, 0);

            headStack.Children.Add(_txtRadarCity);
            headStack.Children.Add(_txtRadarStatus);
            _headCard.Child = headStack;
            Grid.SetRow(_headCard, 0);
            _pageRadar.Children.Add(_headCard);

            // 2. WebView2 Container
            _radarContainer = new Border();
            _radarContainer.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            _radarContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            _radarContainer.BorderThickness = new Thickness(1);
            _radarContainer.CornerRadius = new CornerRadius(6);
            _radarContainer.ClipToBounds = true;
            _radarContainer.Margin = new Thickness(0, 0, 0, 8);

            var mapGrid = new Grid();

            _webView = new Microsoft.Web.WebView2.Wpf.WebView2();
            _webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            _webView.VerticalAlignment = VerticalAlignment.Stretch;
            mapGrid.Children.Add(_webView);

            _lblMapLoading = new TextBlock();
            _lblMapLoading.Text = "⏳  Загрузка Яндекс.Радара...";
            _lblMapLoading.FontSize = 12;
            _lblMapLoading.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            _lblMapLoading.HorizontalAlignment = HorizontalAlignment.Center;
            _lblMapLoading.VerticalAlignment = VerticalAlignment.Center;
            _lblMapLoading.IsHitTestVisible = false;
            mapGrid.Children.Add(_lblMapLoading);

            _radarContainer.Child = mapGrid;
            Grid.SetRow(_radarContainer, 1);
            _pageRadar.Children.Add(_radarContainer);

            // 3. Open in Browser Button (Fluent Design)
            var btnYandex = new Button();
            btnYandex.Content = "🌐  Открыть полную карту в Яндекс.Погоде ↗";
            btnYandex.FontSize = 12;
            btnYandex.FontWeight = FontWeights.SemiBold;
            btnYandex.Height = 36;
            btnYandex.Cursor = Cursors.Hand;
            btnYandex.Margin = new Thickness(0, 6, 0, 0);

            var btnTemplate = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 204, 0)));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Black);

            borderFactory.AppendChild(contentFactory);
            btnTemplate.VisualTree = borderFactory;

            var triggerHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 218, 50))));
            btnTemplate.Triggers.Add(triggerHover);

            btnYandex.Template = btnTemplate;
            btnYandex.Click += delegate
            {
                try
                {
                    string url = string.Format(CultureInfo.InvariantCulture, "https://yandex.ru/pogoda/maps/nowcast?lat={0:F4}&lon={1:F4}", _app.Config.Lat, _app.Config.Lon);
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
            };
            Grid.SetRow(btnYandex, 2);
            _pageRadar.Children.Add(btnYandex);
        }

        public void LoadRadarMap()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(async () =>
            {
                try
                {
                    if (_txtRadarCity != null)
                    {
                        _txtRadarCity.Text = "📍 " + _app.Config.City + string.Format(CultureInfo.InvariantCulture, " ({0:F2}°, {1:F2}°)", _app.Config.Lat, _app.Config.Lon);
                    }

                    string url = string.Format(CultureInfo.InvariantCulture,
                        "https://yandex.ru/pogoda/maps/nowcast?lat={0:F4}&lon={1:F4}&ll={1:F4}_{0:F4}&z=9",
                        _app.Config.Lat, _app.Config.Lon);

                    if (!_isWebViewInitialized)
                    {
                        if (_lblMapLoading != null) _lblMapLoading.Visibility = Visibility.Visible;
                        string localData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuWeatherWidget", "WebView2Data");
                        if (!System.IO.Directory.Exists(localData)) System.IO.Directory.CreateDirectory(localData);

                        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, localData);
                        await _webView.EnsureCoreWebView2Async(env);

                        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                        // 1. Navigation control: prevent clicking links that navigate away from the radar map
                        _webView.CoreWebView2.NavigationStarting += (sender, args) =>
                        {
                            try
                            {
                                string targetUri = args.Uri.ToLowerInvariant();
                                if (targetUri.StartsWith("https://yandex.ru/pogoda/maps/nowcast") ||
                                    targetUri.StartsWith("https://yandex.ru/pogoda/maps?") ||
                                    targetUri.StartsWith("https://yandex.ru/pogoda/maps/") ||
                                    targetUri.StartsWith("about:blank"))
                                {
                                    return;
                                }

                                // Cancel ANY external link navigation
                                args.Cancel = true;
                            }
                            catch { }
                        };

                        _webView.CoreWebView2.NewWindowRequested += (sender, args) =>
                        {
                            // Block any popup / new window navigation
                            args.Handled = true;
                        };

                        _webView.CoreWebView2.PermissionRequested += (sender, args) =>
                        {
                            if (args.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Geolocation)
                            {
                                args.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                            }
                        };

                        // Block advert domains
                        try
                        {
                            _webView.CoreWebView2.AddWebResourceRequestedFilter("*an.yandex.ru*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
                            _webView.CoreWebView2.AddWebResourceRequestedFilter("*yabs.yandex.ru*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
                            _webView.CoreWebView2.AddWebResourceRequestedFilter("*adfox.ru*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);

                            _webView.CoreWebView2.WebResourceRequested += (sender, args) =>
                            {
                                try
                                {
                                    string rUri = args.Request.Uri.ToLowerInvariant();
                                    if (rUri.Contains("an.yandex.ru") || rUri.Contains("yabs.yandex.ru") || rUri.Contains("adfox.ru"))
                                    {
                                        args.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
                                    }
                                }
                                catch { }
                            };
                        }
                        catch { }

                        // Inject anti-ad CSS and script
                        try
                        {
                            string adBlockScript = @"
(function() {
    window.__widgetLat = " + _app.Config.Lat.ToString("F4", CultureInfo.InvariantCulture) + @";
    window.__widgetLon = " + _app.Config.Lon.ToString("F4", CultureInfo.InvariantCulture) + @";

    function getFallbackPos() {
        var lt = window.__widgetLat || " + _app.Config.Lat.ToString("F4", CultureInfo.InvariantCulture) + @";
        var ln = window.__widgetLon || " + _app.Config.Lon.ToString("F4", CultureInfo.InvariantCulture) + @";
        return {
            coords: {
                latitude: lt,
                longitude: ln,
                accuracy: 30,
                altitude: null,
                altitudeAccuracy: null,
                heading: null,
                speed: null
            },
            timestamp: Date.now()
        };
    }

    if (navigator.geolocation) {
        var origGet = navigator.geolocation.getCurrentPosition ? navigator.geolocation.getCurrentPosition.bind(navigator.geolocation) : null;
        navigator.geolocation.getCurrentPosition = function(success, error, options) {
            var done = false;
            var timer = setTimeout(function() {
                if (!done) {
                    done = true;
                    if (success) success(getFallbackPos());
                }
            }, 1200);

            if (origGet) {
                try {
                    origGet(function(pos) {
                        if (!done) {
                            done = true;
                            clearTimeout(timer);
                            if (success) success(pos);
                        }
                    }, function(err) {
                        if (!done) {
                            done = true;
                            clearTimeout(timer);
                            if (success) success(getFallbackPos());
                        }
                    }, { timeout: 1100, enableHighAccuracy: true });
                } catch(e) {
                    if (!done) {
                        done = true;
                        clearTimeout(timer);
                        if (success) success(getFallbackPos());
                    }
                }
            } else {
                done = true;
                clearTimeout(timer);
                if (success) success(getFallbackPos());
            }
        };

        navigator.geolocation.watchPosition = function(success, error, options) {
            if (success) success(getFallbackPos());
            return 1;
        };
        navigator.geolocation.clearWatch = function(id) {};
    }

    var css = '[class*=""serviceLogo""],[class*=""AppLogo""],[class*=""service-logo""],a[href*=""/pogoda/ru""],a[href*=""/pogoda?""] { pointer-events: none !important; cursor: default !important; user-select: none !important; } ' +
              '[class*=""AppMapsAdvert""],[class*=""AdvertTop""],[class*=""AppMapsAdvertTop""],[class*=""advert""],[class*=""Advert""],[class*=""Banner""],[class*=""banner""],[class*=""yandex_rtb""],[id*=""yandex_rtb""],[class*=""yap-R-""],[class*=""yap-layout""],iframe[src*=""an.yandex.ru""],iframe[src*=""yabs.yandex.ru""],[class*=""Poll""],[class*=""poll""],[class*=""Feedback""],[class*=""feedback""],[class*=""Rating""],[class*=""rating""],[class*=""Nps""],[class*=""nps""],[class*=""speechBubble""],[class*=""toast""],[class*=""Toast""] { display: none !important; height: 0 !important; min-height: 0 !important; max-height: 0 !important; padding: 0 !important; margin: 0 !important; opacity: 0 !important; pointer-events: none !important; visibility: hidden !important; }';
    
    function injectStyle() {
        if (!document.head && !document.documentElement) return;
        var s = document.createElement('style');
        s.type = 'text/css';
        s.appendChild(document.createTextNode(css));
        (document.head || document.documentElement).appendChild(s);
    }
    injectStyle();

    // Intercept clicks on any external/navigation links, keeping map interaction controls working
    document.addEventListener('click', function(e) {
        var link = e.target ? e.target.closest('a') : null;
        if (link) {
            var href = link.getAttribute('href');
            if (href && (href.indexOf('://') !== -1 || href.startsWith('//') || href.startsWith('/'))) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                return false;
            }
        }
    }, true);

    function cleanAds() {
        var sel = '[class*=""AppMapsAdvert""],[class*=""AdvertTop""],[class*=""AppMapsAdvertTop""],[class*=""advert""],[class*=""Advert""],[class*=""yandex_rtb""],[id*=""yandex_rtb""],[class*=""yap-R-""],[class*=""Poll""],[class*=""poll""],[class*=""Feedback""],[class*=""feedback""],[class*=""speechBubble""],[class*=""Rating""],[class*=""rating""],[class*=""Nps""],[class*=""nps""]';
        var items = document.querySelectorAll(sel);
        for (var i = 0; i < items.length; i++) {
            items[i].style.display = 'none';
            if (items[i].parentNode) items[i].parentNode.removeChild(items[i]);
        }
        // Remove feedback popups like 'Как вам Яндекс Погода?'
        try {
            var all = document.querySelectorAll('div, section, article, [role=""dialog""]');
            for (var j = 0; j < all.length; j++) {
                var t = all[j].innerText || all[j].textContent || '';
                if (t.indexOf('Как вам') !== -1 || t.indexOf('Оцените') !== -1) {
                    var dlg = all[j].closest('[role=""dialog""]') || all[j];
                    dlg.style.display = 'none';
                    if (dlg.parentNode) dlg.parentNode.removeChild(dlg);
                }
            }
        } catch(e) {}
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', cleanAds);
    } else {
        cleanAds();
    }

    var observer = new MutationObserver(function() {
        cleanAds();
    });
    observer.observe(document.documentElement || document.body, { childList: true, subtree: true });
})();";
                            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(adBlockScript);
                        }
                        catch { }

                        _webView.NavigationCompleted += (s, e) =>
                        {
                            if (_lblMapLoading != null) _lblMapLoading.Visibility = Visibility.Collapsed;
                            try
                            {
                                _webView.CoreWebView2.ExecuteScriptAsync(@"
document.querySelectorAll('[class*=""AppMapsAdvert""],[class*=""AdvertTop""],[class*=""AppMapsAdvertTop""],[class*=""Poll""],[class*=""poll""],[class*=""Feedback""],[class*=""feedback""],[class*=""speechBubble""]').forEach(function(e){ e.style.display='none'; e.remove(); });
document.querySelectorAll('[class*=""serviceLogo""],[class*=""AppLogo""],a[href*=""/pogoda/ru""],a[href*=""/pogoda?""]').forEach(function(e){ e.style.pointerEvents='none'; e.style.cursor='default'; });
");
                            }
                            catch { }
                        };
                        _isWebViewInitialized = true;
                    }

                    try
                    {
                        if (_webView.CoreWebView2 != null)
                        {
                            await _webView.CoreWebView2.ExecuteScriptAsync(string.Format(CultureInfo.InvariantCulture,
                                "window.__widgetLat = {0:F4}; window.__widgetLon = {1:F4};",
                                _app.Config.Lat, _app.Config.Lon));
                        }
                    }
                    catch { }

                    if (_lastRadarUrl != url)
                    {
                        _lastRadarUrl = url;
                        if (_lblMapLoading != null) _lblMapLoading.Visibility = Visibility.Visible;
                        _webView.Source = new Uri(url);
                    }
                }
                catch (Exception ex)
                {
                    Program.Log("LoadRadarMap error: " + ex);
                    if (_lblMapLoading != null) _lblMapLoading.Text = "Ошибка загрузки: " + ex.Message;
                }
            }));
        }

        private Border MakeMetricTile(string label, TextBlock valBlock, out TextBlock lblBlock)
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(6);
            b.Padding = new Thickness(10, 6, 10, 6);
            b.Margin = new Thickness(3);

            var sp = new StackPanel();
            lblBlock = new TextBlock();
            lblBlock.Text = label;
            lblBlock.FontSize = 10.5;
            lblBlock.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            valBlock.FontSize = 13;
            valBlock.FontWeight = FontWeights.SemiBold;
            valBlock.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            valBlock.Margin = new Thickness(0, 1, 0, 0);

            sp.Children.Add(lblBlock);
            sp.Children.Add(valBlock);
            b.Child = sp;
            return b;
        }

        private void ApplyTileTheme(Border tile, TextBlock lbl, TextBlock val, bool isLight)
        {
            if (tile == null) return;
            if (isLight)
            {
                tile.Background = Brushes.White;
                tile.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                tile.BorderThickness = new Thickness(1);
                if (lbl != null) lbl.Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 110));
                if (val != null) val.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 24));
            }
            else
            {
                tile.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                tile.BorderBrush = Brushes.Transparent;
                tile.BorderThickness = new Thickness(0);
                if (lbl != null) lbl.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                if (val != null) val.Foreground = Brushes.White;
            }
        }

        public void ApplyTheme()
        {
            bool isLight = ThemeManager.IsAppLight();
            if (isLight)
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                if (_mainBorder != null)
                {
                    _mainBorder.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    _mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(215, 215, 215));
                }
                if (_txtCity != null) _txtCity.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                if (_txtDate != null) _txtDate.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                if (_txtHeroTemp != null) _txtHeroTemp.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                if (_txtHeroCond != null) _txtHeroCond.Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                if (_txtHeroFeels != null) _txtHeroFeels.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                if (_lblDailyTitle != null) _lblDailyTitle.Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 110));

                ApplyTileTheme(_tileHum, _lblHumTitle, _txtHumidity, true);
                ApplyTileTheme(_tilePress, _lblPressTitle, _txtPressure, true);
                ApplyTileTheme(_tileSun, _lblSunTitle, _txtSunrise, true);
                ApplyTileTheme(_tileSet, _lblSetTitle, _txtSunset, true);

                if (_headCard != null)
                {
                    _headCard.Background = Brushes.White;
                    _headCard.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                }
                if (_txtRadarCity != null) _txtRadarCity.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 24));
                if (_radarContainer != null) _radarContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(215, 215, 215));

                ApplyIconButtonTheme(_btnSearch, true);
                ApplyIconButtonTheme(_btnInterval, true);
                ApplyIconButtonTheme(_btnRefresh, true);
                ApplyIconButtonTheme(_btnClose, true);
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                if (_mainBorder != null)
                {
                    _mainBorder.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                    _mainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                }
                if (_txtCity != null) _txtCity.Foreground = Brushes.White;
                if (_txtDate != null) _txtDate.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                if (_txtHeroTemp != null) _txtHeroTemp.Foreground = Brushes.White;
                if (_txtHeroCond != null) _txtHeroCond.Foreground = Brushes.White;
                if (_txtHeroFeels != null) _txtHeroFeels.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                if (_lblDailyTitle != null) _lblDailyTitle.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));

                ApplyTileTheme(_tileHum, _lblHumTitle, _txtHumidity, false);
                ApplyTileTheme(_tilePress, _lblPressTitle, _txtPressure, false);
                ApplyTileTheme(_tileSun, _lblSunTitle, _txtSunrise, false);
                ApplyTileTheme(_tileSet, _lblSetTitle, _txtSunset, false);

                if (_headCard != null)
                {
                    _headCard.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    _headCard.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                }
                if (_txtRadarCity != null) _txtRadarCity.Foreground = Brushes.White;
                if (_radarContainer != null) _radarContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

                ApplyIconButtonTheme(_btnSearch, false);
                ApplyIconButtonTheme(_btnInterval, false);
                ApplyIconButtonTheme(_btnRefresh, false);
                ApplyIconButtonTheme(_btnClose, false);
            }

            UpdateTabStyle(_tabForecast, _currentPage == 0);
            UpdateTabStyle(_tabRadar, _currentPage == 1);
        }

        public void UpdateData(WeatherData d)
        {
            _lastData = d;
            if (_currentCity != d.City)
            {
                _currentCity = d.City;
                if (_currentPage == 1) LoadRadarMap();
            }

            _txtCity.Text = "📍 " + d.City;
            _txtDate.Text = d.DateText + (string.IsNullOrEmpty(d.ProviderName) ? "" : "  •  " + d.ProviderName);
            _txtHeroTemp.Text = d.Temp.Replace("C", "");
            _txtHeroCond.Text = d.Condition;
            _txtHeroFeels.Text = "Ощущается как " + d.FeelsLike;

            _txtHumidity.Text = d.Humidity;
            _txtPressure.Text = d.Pressure;
            _txtSunrise.Text = string.IsNullOrEmpty(d.Sunrise) ? "--:--" : d.Sunrise;
            _txtSunset.Text = string.IsNullOrEmpty(d.Sunset) ? "--:--" : d.Sunset;

            _txtRadarCity.Text = "📍 " + d.City + " (" + d.Lat.ToString("F2", CultureInfo.InvariantCulture) + "°, " + d.Lon.ToString("F2", CultureInfo.InvariantCulture) + "°)";
            if (d.Condition.ToLower().Contains("дожд") || d.Condition.ToLower().Contains("снег") || d.Condition.ToLower().Contains("осадк") || d.Condition.ToLower().Contains("гроз"))
            {
                _txtRadarStatus.Text = "🌧️ В регионе наблюдаются осадки: " + d.Condition;
                _txtRadarStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 185, 0));
            }
            else
            {
                _txtRadarStatus.Text = "☀️ Осадков в данный момент не наблюдается";
                _txtRadarStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 194, 255));
            }

            bool isLight = ThemeManager.IsAppLight();
            WeatherIcons.DrawIcon(_heroIcon, d.IconType, 52, isLight);

            _dailyPanel.Children.Clear();
            foreach (var dp in d.Daily)
            {
                var db = new Border();
                db.CornerRadius = new CornerRadius(6);
                if (isLight)
                {
                    db.Background = Brushes.White;
                    db.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 225, 225));
                    db.BorderThickness = new Thickness(1);
                }
                else
                {
                    db.Background = new SolidColorBrush(Color.FromRgb(38, 38, 38));
                    db.BorderThickness = new Thickness(0);
                }
                db.Padding = new Thickness(10, 5, 10, 5);
                db.Margin = new Thickness(0, 0, 0, 4);

                var dg = new Grid();
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(62) });
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                var dDay = new TextBlock() {
                    Text = dp.DayName,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var dIcon = new Canvas() { Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center };
                WeatherIcons.DrawIcon(dIcon, dp.IconType, 18, isLight);

                var dCond = new TextBlock() {
                    Text = dp.Condition,
                    FontSize = 11,
                    Foreground = isLight ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(8, 0, 8, 0)
                };
                var dTemp = new TextBlock() {
                    Text = dp.TempMinMax,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetColumn(dDay, 0);
                Grid.SetColumn(dIcon, 1);
                Grid.SetColumn(dCond, 2);
                Grid.SetColumn(dTemp, 3);

                dg.Children.Add(dDay);
                dg.Children.Add(dIcon);
                dg.Children.Add(dCond);
                dg.Children.Add(dTemp);

                db.Child = dg;
                _dailyPanel.Children.Add(db);
            }

            if (_currentPage == 1)
            {
                LoadRadarMap();
            }
        }

        public void PositionAbove(TaskbarWidgetWindow taskbarWin)
        {
            ApplyTheme();
            if (_lastData != null)
            {
                UpdateData(_lastData);
            }

            double dpiX = 1.0;
            double dpiY = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            Left = taskbarWin.Left;
            Top = taskbarWin.Top - Height - 8;
            if (Top < 10) Top = 10;
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetWindowPos(helper.Handle, HWND_TOPMOST, (int)(Left * dpiX), (int)(Top * dpiY), (int)(Width * dpiX), (int)(Height * dpiY), SWP_SHOWWINDOW);
            }
        }
    }
    public class CitySearchDialog : Window
    {
        private Program _app;
        private TextBox _txtCity;
        private TextBlock _lblStatus;
        private Button _btnApply;
        private Button _btnCancel;

        public CitySearchDialog(Program app)
        {
            _app = app;

            Title = "Выбор города";
            Width = 420;
            Height = 330;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            BuildUI();
        }

        private void BuildUI()
        {
            bool isLight = ThemeManager.IsAppLight();
            Background = isLight ? new SolidColorBrush(Color.FromRgb(245, 245, 245)) : new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White;

            var root = new Grid();
            root.Margin = new Thickness(20);
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Input
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Fast chips
            root.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Status
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Buttons

            // Header
            var lblTitle = new TextBlock()
            {
                Text = "📍  Выбор города",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(lblTitle, 0);
            root.Children.Add(lblTitle);

            // Input
            var inputStack = new StackPanel() { Margin = new Thickness(0, 0, 0, 10) };
            var lblInput = new TextBlock()
            {
                Text = "Введите название города на русском или выберите ниже:",
                FontSize = 11.5,
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            inputStack.Children.Add(lblInput);

            _txtCity = new TextBox()
            {
                Text = _app.Config.City,
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
                Background = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                BorderBrush = isLight ? new SolidColorBrush(Color.FromRgb(200, 200, 200)) : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1)
            };
            _txtCity.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) ApplyCity();
                else if (e.Key == Key.Escape) Close();
            };
            inputStack.Children.Add(_txtCity);

            Grid.SetRow(inputStack, 1);
            root.Children.Add(inputStack);

            // Fast chips
            var wrap = new WrapPanel() { Margin = new Thickness(0, 0, 0, 10) };
            string[] quick = new string[] { "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань", "Самара", "Сочи", "Владивосток" };
            foreach (var q in quick)
            {
                string c = q;
                var btnChip = new Button()
                {
                    Content = c,
                    FontSize = 11,
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(0, 0, 5, 5),
                    Background = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                    Foreground = isLight ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderBrush = isLight ? new SolidColorBrush(Color.FromRgb(215, 215, 215)) : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                btnChip.Click += delegate
                {
                    _txtCity.Text = c;
                    ApplyCity();
                };
                wrap.Children.Add(btnChip);
            }
            Grid.SetRow(wrap, 2);
            root.Children.Add(wrap);

            // Status
            _lblStatus = new TextBlock()
            {
                Text = "",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_lblStatus, 3);
            root.Children.Add(_lblStatus);

            // Buttons
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _btnCancel = new Button()
            {
                Content = "Отмена",
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Background = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(48, 48, 48)),
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                BorderBrush = isLight ? new SolidColorBrush(Color.FromRgb(210, 210, 210)) : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnCancel.Click += delegate { Close(); };
            Grid.SetColumn(_btnCancel, 1);
            btnGrid.Children.Add(_btnCancel);

            _btnApply = new Button()
            {
                Content = "Применить",
                Width = 100,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            _btnApply.Click += delegate { ApplyCity(); };
            Grid.SetColumn(_btnApply, 2);
            btnGrid.Children.Add(_btnApply);

            Grid.SetRow(btnGrid, 4);
            root.Children.Add(btnGrid);

            Content = root;
            Loaded += delegate { _txtCity.Focus(); _txtCity.SelectAll(); };
        }

        private void ApplyCity()
        {
            string input = _txtCity.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            _btnApply.IsEnabled = false;
            _lblStatus.Text = "Поиск и проверка города...";
            _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215));

            ThreadPool.QueueUserWorkItem(delegate
            {
                string disp, slug;
                double lat, lon;
                CityResolver.ResolveCity(input, out disp, out slug, out lat, out lon);

                Dispatcher.Invoke(new Action(delegate
                {
                    _app.SetCity(disp, slug, lat, lon);
                    _lblStatus.Text = "✅ Город установлен!";
                    _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(40, 180, 40));

                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(400);
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        Close();
                    };
                    timer.Start();
                }));
            });
        }
    }

    public class IntervalDialog : Window
    {
        private Program _app;
        private TextBox _txtMinutes;
        private Button _btnSave;
        private Button _btnCancel;

        public IntervalDialog(Program app)
        {
            _app = app;

            Title = "Интервал обновления";
            Width = 380;
            Height = 270;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            BuildUI();
        }

        private void BuildUI()
        {
            bool isLight = ThemeManager.IsAppLight();
            Background = isLight ? new SolidColorBrush(Color.FromRgb(245, 245, 245)) : new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White;

            var root = new Grid();
            root.Margin = new Thickness(20);
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Preset buttons
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Input box
            root.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Spacer
            root.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Buttons

            // Header
            var lblTitle = new TextBlock()
            {
                Text = "⏱️  Интервал обновления погоды",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid.SetRow(lblTitle, 0);
            root.Children.Add(lblTitle);

            // Preset chips
            var chipWrap = new WrapPanel() { Margin = new Thickness(0, 0, 0, 12) };
            int[] presets = new int[] { 5, 10, 15, 30, 60, 120 };
            foreach (int p in presets)
            {
                int val = p;
                string label = val < 60 ? (val + " мин") : (val / 60 + " ч");
                bool isSelected = (val == _app.Config.RefreshIntervalMinutes);

                Brush chipBg, chipFg, chipBorder;
                if (isSelected)
                {
                    chipBg = isLight ? new SolidColorBrush(Color.FromArgb(40, 0, 120, 215)) : new SolidColorBrush(Color.FromArgb(60, 0, 120, 215));
                    chipFg = isLight ? new SolidColorBrush(Color.FromRgb(0, 100, 200)) : new SolidColorBrush(Color.FromRgb(76, 194, 255));
                    chipBorder = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                }
                else
                {
                    chipBg = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(46, 46, 46));
                    chipFg = isLight ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : new SolidColorBrush(Color.FromRgb(220, 220, 220));
                    chipBorder = isLight ? new SolidColorBrush(Color.FromRgb(215, 215, 215)) : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                }

                var btn = new Button()
                {
                    Content = label,
                    FontSize = 11,
                    Padding = new Thickness(9, 4, 9, 4),
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = chipBg,
                    Foreground = chipFg,
                    BorderBrush = chipBorder,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                btn.Click += delegate
                {
                    _txtMinutes.Text = val.ToString();
                };
                chipWrap.Children.Add(btn);
            }
            Grid.SetRow(chipWrap, 1);
            root.Children.Add(chipWrap);

            // Custom input
            var inputStack = new StackPanel() { Margin = new Thickness(0, 0, 0, 10) };
            var lblInput = new TextBlock()
            {
                Text = "Или укажите интервал в минутах (от 1 до 720):",
                FontSize = 11,
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            inputStack.Children.Add(lblInput);

            _txtMinutes = new TextBox()
            {
                Text = _app.Config.RefreshIntervalMinutes.ToString(),
                FontSize = 13,
                Padding = new Thickness(8, 5, 8, 5),
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                BorderBrush = isLight ? new SolidColorBrush(Color.FromRgb(200, 200, 200)) : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1)
            };
            _txtMinutes.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) SaveInterval();
                else if (e.Key == Key.Escape) Close();
            };
            inputStack.Children.Add(_txtMinutes);
            Grid.SetRow(inputStack, 2);
            root.Children.Add(inputStack);

            // Buttons
            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _btnCancel = new Button()
            {
                Content = "Отмена",
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Background = isLight ? Brushes.White : new SolidColorBrush(Color.FromRgb(48, 48, 48)),
                Foreground = isLight ? new SolidColorBrush(Color.FromRgb(24, 24, 24)) : Brushes.White,
                BorderBrush = isLight ? new SolidColorBrush(Color.FromRgb(210, 210, 210)) : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnCancel.Click += delegate { Close(); };
            Grid.SetColumn(_btnCancel, 1);
            btnGrid.Children.Add(_btnCancel);

            _btnSave = new Button()
            {
                Content = "Сохранить",
                Width = 100,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            _btnSave.Click += delegate { SaveInterval(); };
            Grid.SetColumn(_btnSave, 2);
            btnGrid.Children.Add(_btnSave);

            Grid.SetRow(btnGrid, 4);
            root.Children.Add(btnGrid);

            Content = root;
            Loaded += delegate { _txtMinutes.Focus(); _txtMinutes.SelectAll(); };
        }

        private void SaveInterval()
        {
            int val;
            if (int.TryParse(_txtMinutes.Text.Trim(), out val))
            {
                if (val < 1) val = 1;
                if (val > 720) val = 720;
                _app.SetRefreshInterval(val);
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, укажите число минут (от 1 до 720).", "Неверное значение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public static class WeatherIcons
    {
        public static string DetermineIconType(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return "cloudy";
            string c = condition.ToLower();
            if (c.Contains("ясно") || c.Contains("солнечно")) return "sun";
            if (c.Contains("перемен") || c.Contains("прояснен")) return "partly_cloudy";
            if (c.Contains("гроза")) return "thunder";
            if (c.Contains("снег")) return "snow";
            if (c.Contains("дожд") || c.Contains("осадк") || c.Contains("ливень") || c.Contains("морос")) return "rain";
            if (c.Contains("туман") || c.Contains("дымка") || c.Contains("мгла")) return "fog";
            return "cloudy";
        }

        public static void DrawIcon(Canvas canvas, string iconType, double size, bool isLight = false)
        {
            canvas.Children.Clear();
            canvas.Width = size;
            canvas.Height = size;

            if (iconType == "sun")
                DrawSun(canvas, size, isLight);
            else if (iconType == "partly_cloudy")
                DrawSunBehindCloud(canvas, size, isLight);
            else if (iconType == "thunder")
                DrawThunder(canvas, size, isLight);
            else if (iconType == "snow")
                DrawSnow(canvas, size, isLight);
            else if (iconType == "rain")
                DrawRain(canvas, size, isLight);
            else if (iconType == "fog")
                DrawFog(canvas, size, isLight);
            else
                DrawCloud(canvas, size, isLight ? Color.FromRgb(105, 140, 175) : Color.FromRgb(165, 195, 225));
        }

        private static void DrawSun(Canvas c, double s, bool isLight)
        {
            var sun = new Ellipse();
            sun.Width = s * 0.72;
            sun.Height = s * 0.72;
            sun.Fill = new SolidColorBrush(isLight ? Color.FromRgb(235, 145, 0) : Color.FromRgb(255, 195, 0));
            Canvas.SetLeft(sun, s * 0.14);
            Canvas.SetTop(sun, s * 0.14);
            c.Children.Add(sun);
        }

        private static void DrawSunBehindCloud(Canvas c, double s, bool isLight)
        {
            var sun = new Ellipse();
            sun.Width = s * 0.45;
            sun.Height = s * 0.45;
            sun.Fill = new SolidColorBrush(isLight ? Color.FromRgb(235, 145, 0) : Color.FromRgb(255, 195, 0));
            Canvas.SetLeft(sun, s * 0.45);
            Canvas.SetTop(sun, s * 0.08);
            c.Children.Add(sun);

            DrawCloud(c, s * 0.9, isLight ? Color.FromRgb(120, 155, 185) : Color.FromRgb(190, 215, 240));
        }

        private static void DrawCloud(Canvas c, double s, Color cloudColor)
        {
            var path = new System.Windows.Shapes.Path();
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                double scale = s / 40.0;
                ctx.BeginFigure(new Point(8 * scale, 28 * scale), true, true);
                ctx.ArcTo(new Point(8 * scale, 18 * scale), new Size(6 * scale, 6 * scale), 0, false, SweepDirection.Clockwise, true, true);
                ctx.ArcTo(new Point(18 * scale, 10 * scale), new Size(8 * scale, 8 * scale), 0, false, SweepDirection.Clockwise, true, true);
                ctx.ArcTo(new Point(30 * scale, 16 * scale), new Size(8 * scale, 8 * scale), 0, false, SweepDirection.Clockwise, true, true);
                ctx.ArcTo(new Point(34 * scale, 28 * scale), new Size(6 * scale, 6 * scale), 0, false, SweepDirection.Clockwise, true, true);
                ctx.LineTo(new Point(8 * scale, 28 * scale), true, true);
            }
            geom.Freeze();
            path.Data = geom;
            path.Fill = new SolidColorBrush(cloudColor);
            c.Children.Add(path);
        }

        private static void DrawRain(Canvas c, double s, bool isLight)
        {
            DrawCloud(c, s * 0.85, isLight ? Color.FromRgb(100, 135, 165) : Color.FromRgb(150, 180, 210));
            double scale = s / 40.0;

            for (int i = 0; i < 3; i++)
            {
                var drop = new System.Windows.Shapes.Line();
                drop.X1 = (13 + i * 7) * scale;
                drop.Y1 = 28 * scale;
                drop.X2 = (11 + i * 7) * scale;
                drop.Y2 = 34 * scale;
                drop.Stroke = new SolidColorBrush(isLight ? Color.FromRgb(20, 110, 210) : Color.FromRgb(70, 160, 240));
                drop.StrokeThickness = 2 * scale;
                drop.StrokeStartLineCap = PenLineCap.Round;
                drop.StrokeEndLineCap = PenLineCap.Round;
                c.Children.Add(drop);
            }
        }

        private static void DrawSnow(Canvas c, double s, bool isLight)
        {
            DrawCloud(c, s * 0.85, isLight ? Color.FromRgb(115, 150, 180) : Color.FromRgb(170, 195, 220));
            double scale = s / 40.0;

            for (int i = 0; i < 3; i++)
            {
                var flake = new Ellipse();
                flake.Width = 3.5 * scale;
                flake.Height = 3.5 * scale;
                flake.Fill = isLight ? new SolidColorBrush(Color.FromRgb(60, 105, 150)) : Brushes.White;
                Canvas.SetLeft(flake, (11 + i * 8) * scale);
                Canvas.SetTop(flake, 29 * scale);
                c.Children.Add(flake);
            }
        }

        private static void DrawThunder(Canvas c, double s, bool isLight)
        {
            DrawCloud(c, s * 0.85, isLight ? Color.FromRgb(90, 115, 145) : Color.FromRgb(120, 140, 170));
            double scale = s / 40.0;

            var bolt = new System.Windows.Shapes.Polygon();
            bolt.Points = new PointCollection()
            {
                new Point(20 * scale, 24 * scale),
                new Point(15 * scale, 31 * scale),
                new Point(19 * scale, 31 * scale),
                new Point(16 * scale, 38 * scale),
                new Point(24 * scale, 29 * scale),
                new Point(20 * scale, 29 * scale)
            };
            bolt.Fill = new SolidColorBrush(isLight ? Color.FromRgb(230, 165, 0) : Color.FromRgb(255, 215, 0));
            c.Children.Add(bolt);
        }

        private static void DrawFog(Canvas c, double s, bool isLight)
        {
            DrawCloud(c, s * 0.75, isLight ? Color.FromRgb(120, 140, 165) : Color.FromRgb(170, 185, 205));
            double scale = s / 40.0;

            for (int i = 0; i < 2; i++)
            {
                var line = new System.Windows.Shapes.Line();
                line.X1 = 10 * scale;
                line.Y1 = (28 + i * 5) * scale;
                line.X2 = 30 * scale;
                line.Y2 = (28 + i * 5) * scale;
                line.Stroke = new SolidColorBrush(isLight ? Color.FromRgb(85, 115, 145) : Color.FromRgb(200, 215, 230));
                line.StrokeThickness = 2 * scale;
                line.StrokeStartLineCap = PenLineCap.Round;
                line.StrokeEndLineCap = PenLineCap.Round;
                c.Children.Add(line);
            }
        }
    }
}