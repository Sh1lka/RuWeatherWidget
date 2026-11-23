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
        public List<string> RecentCities = new List<string>() { "Санкт-Петербург", "Москва", "Новосибирск", "Екатеринбург", "Казань", "Самара", "Сочи" };
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
                }
            }
            catch { }
        }

        public void SaveConfig()
        {
            try
            {
                string json = string.Format(CultureInfo.InvariantCulture,
                    "{{\r\n  \"City\": \"{0}\",\r\n  \"CitySlug\": \"{1}\",\r\n  \"Lat\": {2:F4},\r\n  \"Lon\": {3:F4},\r\n  \"AutoStart\": {4},\r\n  \"PositionMode\": \"{5}\",\r\n  \"RefreshIntervalMinutes\": {6}\r\n}}",
                    _config.City, _config.CitySlug, _config.Lat, _config.Lon, _config.AutoStart ? "true" : "false", _config.PositionMode, _config.RefreshIntervalMinutes);
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

        public void ToggleFlyout(int startPage = 0)
        {
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

            if (!_config.RecentCities.Contains(displayName))
            {
                _config.RecentCities.Insert(0, displayName);
                if (_config.RecentCities.Count > 8) _config.RecentCities.RemoveAt(_config.RecentCities.Count - 1);
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

                    _currentData = data;

                    Dispatcher.Invoke(delegate
                    {
                        _taskbarWindow.UpdateWeather(data);
                        if (_flyoutWindow.IsVisible) _flyoutWindow.UpdateData(data);
                    });
                }
                catch (Exception)
                {
                    Dispatcher.Invoke(delegate
                    {
                        _taskbarWindow.ShowError("Сеть...");
                    });
                }
            });
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length == 1) return s.ToUpper();
            return char.ToUpper(s[0]) + s.Substring(1);
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

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("gdi32.dll")]
        public static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        public const int RGN_DIFF = 4;
        private IntPtr _appliedTrayHwnd = IntPtr.Zero;
        private int _lastHoleLeft = -1;
        private int _lastHoleTop = -1;
        private int _holeRefreshCounter = 0;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        public TaskbarWidgetWindow(Program app)
        {
            _app = app;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            Width = 168;
            Height = 40;
            Left = 10;
            Top = SystemParameters.PrimaryScreenHeight - 44;

            BuildUI();

            Loaded += delegate
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

                UpdatePosition();
            };

            _posTimer = new DispatcherTimer();
            _posTimer.Interval = TimeSpan.FromMilliseconds(500);
            _posTimer.Tick += (s, e) => UpdatePosition();
            _posTimer.Start();

            Closed += delegate { RestoreTaskbar(); };
        }

        private void BuildUI()
        {
            var outerContainer = new Grid();
            outerContainer.Background = Brushes.Transparent;

            _hoverBorder = new Border();
            _hoverBorder.Margin = new Thickness(0);
            _hoverBorder.Padding = new Thickness(8, 2, 8, 2);
            _hoverBorder.Cursor = Cursors.Hand;
            ApplyStyle();

            var stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.VerticalAlignment = VerticalAlignment.Center;

            _iconCanvas = new Canvas();
            _iconCanvas.Width = 28;
            _iconCanvas.Height = 28;
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
            _txtTemp.Foreground = Brushes.White;
            _txtTemp.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            _txtCond = new TextBlock();
            _txtCond.Text = "Загрузка...";
            _txtCond.FontSize = 10.5;
            _txtCond.Foreground = new SolidColorBrush(Color.FromRgb(215, 215, 215));
            _txtCond.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            _txtCond.TextTrimming = TextTrimming.CharacterEllipsis;
            _txtCond.MaxWidth = 112;

            textStack.Children.Add(_txtTemp);
            textStack.Children.Add(_txtCond);
            stack.Children.Add(textStack);

            _hoverBorder.Child = stack;
            outerContainer.Children.Add(_hoverBorder);
            Content = outerContainer;

            _hoverBorder.MouseEnter += delegate
            {
                _hoverBorder.Background = new SolidColorBrush(Color.FromRgb(46, 46, 46));
                _hoverBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
            };

            _hoverBorder.MouseLeave += delegate
            {
                ApplyStyle();
            };

            _hoverBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                _app.ToggleFlyout(0);
            };

            _hoverBorder.PreviewMouseRightButtonDown += (s, e) =>
            {
                e.Handled = true;
                ShowContextMenu();
            };
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu();

            var mRefresh = new MenuItem();
            mRefresh.Header = "🔄  Обновить погоду";
            mRefresh.Click += delegate { _app.FetchWeatherAsync(); };
            menu.Items.Add(mRefresh);

            var mRadar = new MenuItem();
            mRadar.Header = "🌧️  Карта осадков Яндекса";
            mRadar.Click += delegate { _app.ToggleFlyout(1); };
            menu.Items.Add(mRadar);

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

            // Recent cities
            foreach (var c in _app.Config.RecentCities)
            {
                AddCityItem(menu, c);
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
                _app.Config.AutoStart = mAuto.IsChecked;
                _app.SaveConfig();
            };
            menu.Items.Add(mAuto);

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
            _txtTemp.Text = data.Temp;
            _txtCond.Text = data.Condition;
            WeatherIcons.DrawIcon(_iconCanvas, data.IconType, 28);
        }

        public void ShowError(string msg)
        {
            _txtCond.Text = msg;
        }

        private void ApplyStyle()
        {
            if (_hoverBorder == null) return;
            _hoverBorder.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            _hoverBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255));
            _hoverBorder.BorderThickness = new Thickness(1);
            _hoverBorder.CornerRadius = new CornerRadius(6);

            var shadow = new DropShadowEffect();
            shadow.BlurRadius = 8;
            shadow.ShadowDepth = 1;
            shadow.Opacity = 0.35;
            shadow.Color = Colors.Black;
            _hoverBorder.Effect = shadow;
        }

        public void UpdatePosition()
        {
            ApplyStyle();

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

            int targetLeft = 10;
            int targetTop = trayTop + (trayHeight - (int)(Height * dpiY)) / 2;

            if (Double.IsNaN(Left) || Math.Abs(Left - targetLeft) > 1 || Double.IsNaN(Top) || Math.Abs(Top - (targetTop / dpiY)) > 1)
            {
                Left = targetLeft;
                Top = targetTop / dpiY;
            }

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetWindowPos(helper.Handle, HWND_TOPMOST, (int)(targetLeft * dpiX), targetTop, (int)(Width * dpiX), (int)(Height * dpiY), SWP_SHOWWINDOW | SWP_NOACTIVATE);
            }

            _holeRefreshCounter++;
            if (_holeRefreshCounter % 10 == 1 || _appliedTrayHwnd == IntPtr.Zero)
            {
                UpdateTaskbarHole(dpiX, dpiY);
            }
        }

        public void UpdateTaskbarHole(double dpiX = 1.0, double dpiY = 1.0)
        {
            try
            {
                IntPtr hTray = FindWindow("Shell_TrayWnd", null);
                if (hTray == IntPtr.Zero) return;

                RECT r;
                GetWindowRect(hTray, out r);
                int w = r.Right - r.Left;
                int h = r.Bottom - r.Top;

                int holeLeft = (int)(10 * dpiX);
                int holeTop = (h - (int)(Height * dpiY)) / 2;
                int holeRight = holeLeft + (int)(Width * dpiX);
                int holeBottom = holeTop + (int)(Height * dpiY);
                int radius = (int)(12 * dpiX);

                if (_appliedTrayHwnd == hTray && _lastHoleLeft == holeLeft && _lastHoleTop == holeTop && (_holeRefreshCounter % 20 != 1))
                    return;

                IntPtr rgnFull = CreateRectRgn(0, 0, w, h);
                IntPtr rgnHole = CreateRoundRectRgn(holeLeft, holeTop, holeRight, holeBottom, radius, radius);
                IntPtr rgnResult = CreateRectRgn(0, 0, 0, 0);

                CombineRgn(rgnResult, rgnFull, rgnHole, RGN_DIFF);
                SetWindowRgn(hTray, rgnResult, true);

                DeleteObject(rgnFull);
                DeleteObject(rgnHole);

                _appliedTrayHwnd = hTray;
                _lastHoleLeft = holeLeft;
                _lastHoleTop = holeTop;
            }
            catch { }
        }

        public void RestoreTaskbar()
        {
            try
            {
                if (_appliedTrayHwnd != IntPtr.Zero)
                {
                    SetWindowRgn(_appliedTrayHwnd, IntPtr.Zero, true);
                    _appliedTrayHwnd = IntPtr.Zero;
                    _lastHoleLeft = -1;
                    _lastHoleTop = -1;
                }
            }
            catch { }
        }
    }

    public class FlyoutWindow : Window
    {
        private Program _app;
        private int _currentPage = 0; // 0 = Forecast, 1 = Radar

        // Navigation elements
        private Button _tabForecast;
        private Button _tabRadar;
        private Grid _pageForecast;
        private Grid _pageRadar;

        // Page 1 Forecast elements
        private TextBlock _txtCity;
        private TextBlock _txtDate;
        private Canvas _heroIcon;
        private TextBlock _txtHeroTemp;
        private TextBlock _txtHeroCond;
        private TextBlock _txtHeroFeels;
        private TextBlock _txtHumidity;
        private TextBlock _txtPressure;
        private TextBlock _txtSunrise;
        private TextBlock _txtSunset;
        private StackPanel _dailyPanel;

        // Page 2 Radar elements
        private TextBlock _txtRadarCity;
        private TextBlock _txtRadarStatus;
        private Microsoft.Web.WebView2.Wpf.WebView2 _webView;
        private TextBlock _lblMapLoading;
        private Border _radarContainer;
        private bool _isWebViewInitialized = false;
        private string _lastRadarUrl = "";
        private string _currentCity = "";

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

        public DateTime LastOpenTime = DateTime.MinValue;

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

            BuildUI();

            Deactivated += delegate
            {
                if ((DateTime.UtcNow - LastOpenTime).TotalMilliseconds > 350)
                {
                    try
                    {
                        IntPtr fg = GetForegroundWindow();
                        if (fg != IntPtr.Zero)
                        {
                            uint fgPid;
                            GetWindowThreadProcessId(fg, out fgPid);
                            uint myPid = (uint)Process.GetCurrentProcess().Id;
                            if (fgPid == myPid)
                            {
                                return; // Focus inside our app/WebView2
                            }
                        }
                    }
                    catch { }
                    Hide();
                }
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
            var mainBorder = new Border();
            mainBorder.CornerRadius = new CornerRadius(8);
            mainBorder.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            mainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            mainBorder.BorderThickness = new Thickness(1);
            mainBorder.Padding = new Thickness(14);

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

            var btnSearch = CreateIconButton("🔍", "Выбрать город");
            btnSearch.Click += delegate
            {
                var dlg = new CitySearchDialog(_app);
                dlg.ShowDialog();
            };
            btnStack.Children.Add(btnSearch);

            var btnInterval = CreateIconButton("⏱️", "Интервал обновления");
            btnInterval.Click += delegate
            {
                var dlg = new IntervalDialog(_app);
                dlg.ShowDialog();
            };
            btnStack.Children.Add(btnInterval);

            var btnRefresh = CreateIconButton("🔄", "Обновить погоду");
            btnRefresh.Click += delegate { _app.FetchWeatherAsync(); };
            btnStack.Children.Add(btnRefresh);

            var btnClose = CreateIconButton("✕", "Закрыть (Esc)");
            btnClose.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            btnClose.Click += delegate { Hide(); };
            btnStack.Children.Add(btnClose);

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

            mainBorder.Child = rootGrid;
            Content = mainBorder;

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
            if (isActive)
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(76, 194, 255));
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 0, 120, 215));
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
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
            btn.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
            btn.Foreground = Brushes.White;
            btn.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            btn.BorderThickness = new Thickness(1);
            btn.Cursor = Cursors.Hand;
            btn.ToolTip = tooltip;
            return btn;
        }

        public void SetPage(int page)
        {
            _currentPage = page;
            if (page == 0)
            {
                UpdateTabStyle(_tabForecast, true);
                UpdateTabStyle(_tabRadar, false);
                _pageForecast.Visibility = Visibility.Visible;
                _pageRadar.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateTabStyle(_tabForecast, false);
                UpdateTabStyle(_tabRadar, true);
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

            var tileHum = MakeMetricTile("💧 Влажность", _txtHumidity);
            var tilePress = MakeMetricTile("🌡️ Давление", _txtPressure);
            var tileSun = MakeMetricTile("🌅 Восход", _txtSunrise);
            var tileSet = MakeMetricTile("🌇 Закат", _txtSunset);

            Grid.SetRow(tileHum, 0); Grid.SetColumn(tileHum, 0);
            Grid.SetRow(tilePress, 0); Grid.SetColumn(tilePress, 1);
            Grid.SetRow(tileSun, 1); Grid.SetColumn(tileSun, 0);
            Grid.SetRow(tileSet, 1); Grid.SetColumn(tileSet, 1);

            metricsGrid.Children.Add(tileHum);
            metricsGrid.Children.Add(tilePress);
            metricsGrid.Children.Add(tileSun);
            metricsGrid.Children.Add(tileSet);

            Grid.SetRow(metricsGrid, 2);
            _pageForecast.Children.Add(metricsGrid);

            // 4. Daily Title
            var lblDaily = new TextBlock();
            lblDaily.Text = "ПРОГНОЗ НА 5 ДНЕЙ";
            lblDaily.FontSize = 10;
            lblDaily.FontWeight = FontWeights.Bold;
            lblDaily.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            lblDaily.Margin = new Thickness(0, 0, 0, 6);
            Grid.SetRow(lblDaily, 3);
            _pageForecast.Children.Add(lblDaily);

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
            var headCard = new Border();
            headCard.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            headCard.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            headCard.BorderThickness = new Thickness(1);
            headCard.CornerRadius = new CornerRadius(6);
            headCard.Padding = new Thickness(10, 7, 10, 7);
            headCard.Margin = new Thickness(0, 0, 0, 8);

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
            headCard.Child = headStack;
            Grid.SetRow(headCard, 0);
            _pageRadar.Children.Add(headCard);

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

            // 3. Open in Browser Button
            var btnYandex = new Button();
            btnYandex.Content = "🌐  Открыть полную карту в Яндекс.Погоде ↗";
            btnYandex.FontSize = 12;
            btnYandex.FontWeight = FontWeights.SemiBold;
            btnYandex.Background = new SolidColorBrush(Color.FromRgb(255, 204, 0));
            btnYandex.Foreground = Brushes.Black;
            btnYandex.BorderThickness = new Thickness(0);
            btnYandex.Padding = new Thickness(10, 7, 10, 7);
            btnYandex.Cursor = Cursors.Hand;
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
    var css = '[class*=""AppMapsAdvert""],[class*=""AdvertTop""],[class*=""AppMapsAdvertTop""],[class*=""advert""],[class*=""Advert""],[class*=""Banner""],[class*=""banner""],[class*=""yandex_rtb""],[id*=""yandex_rtb""],[class*=""yap-R-""],[class*=""yap-layout""],iframe[src*=""an.yandex.ru""],iframe[src*=""yabs.yandex.ru""] { display: none !important; height: 0 !important; min-height: 0 !important; max-height: 0 !important; padding: 0 !important; margin: 0 !important; opacity: 0 !important; pointer-events: none !important; visibility: hidden !important; }';
    
    function injectStyle() {
        if (!document.head && !document.documentElement) return;
        var s = document.createElement('style');
        s.type = 'text/css';
        s.appendChild(document.createTextNode(css));
        (document.head || document.documentElement).appendChild(s);
    }
    injectStyle();

    function cleanAds() {
        var sel = '[class*=""AppMapsAdvert""],[class*=""AdvertTop""],[class*=""AppMapsAdvertTop""],[class*=""advert""],[class*=""Advert""],[class*=""yandex_rtb""],[id*=""yandex_rtb""],[class*=""yap-R-""]';
        var items = document.querySelectorAll(sel);
        for (var i = 0; i < items.length; i++) {
            items[i].style.display = 'none';
            if (items[i].parentNode) items[i].parentNode.removeChild(items[i]);
        }
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
                                _webView.CoreWebView2.ExecuteScriptAsync("document.querySelectorAll('[class*=\"AppMapsAdvert\"],[class*=\"AdvertTop\"],[class*=\"AppMapsAdvertTop\"]').forEach(function(e){ e.style.display='none'; e.remove(); });");
                            }
                            catch { }
                        };
                        _isWebViewInitialized = true;
                    }

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

        private Border MakeMetricTile(string label, TextBlock valBlock)
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(6);
            b.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            b.Padding = new Thickness(10, 6, 10, 6);
            b.Margin = new Thickness(3);

            var sp = new StackPanel();
            var lbl = new TextBlock();
            lbl.Text = label;
            lbl.FontSize = 10.5;
            lbl.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
            lbl.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            valBlock.FontSize = 13;
            valBlock.FontWeight = FontWeights.SemiBold;
            valBlock.Foreground = Brushes.White;
            valBlock.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
            valBlock.Margin = new Thickness(0, 1, 0, 0);

            sp.Children.Add(lbl);
            sp.Children.Add(valBlock);
            b.Child = sp;
            return b;
        }

        public void UpdateData(WeatherData d)
        {
            if (_currentCity != d.City)
            {
                _currentCity = d.City;
                if (_currentPage == 1) LoadRadarMap();
            }

            _txtCity.Text = "📍 " + d.City;
            _txtDate.Text = d.DateText;
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

            WeatherIcons.DrawIcon(_heroIcon, d.IconType, 52);

            _dailyPanel.Children.Clear();
            foreach (var dp in d.Daily)
            {
                var db = new Border();
                db.CornerRadius = new CornerRadius(6);
                db.Background = new SolidColorBrush(Color.FromRgb(38, 38, 38));
                db.Padding = new Thickness(10, 5, 10, 5);
                db.Margin = new Thickness(0, 0, 0, 4);

                var dg = new Grid();
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(45) });
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                dg.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                var dDay = new TextBlock() { Text = dp.DayName, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
                var dIcon = new Canvas() { Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center };
                WeatherIcons.DrawIcon(dIcon, dp.IconType, 18);

                var dCond = new TextBlock() { Text = dp.Condition, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                var dTemp = new TextBlock() { Text = dp.TempMinMax, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };

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
            Left = taskbarWin.Left;
            Top = taskbarWin.Top - Height - 8;
            if (Top < 10) Top = 10;
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetWindowPos(helper.Handle, HWND_TOPMOST, (int)Left, (int)Top, (int)Width, (int)Height, SWP_SHOWWINDOW);
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
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            BuildUI();
        }

        private void BuildUI()
        {
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
                Foreground = Brushes.White,
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
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            inputStack.Children.Add(lblInput);

            _txtCity = new TextBox()
            {
                Text = _app.Config.City,
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
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
                    Background = new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(76, 194, 255)),
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
                Background = new SolidColorBrush(Color.FromRgb(48, 48, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
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
            _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 194, 255));

            ThreadPool.QueueUserWorkItem(delegate
            {
                string disp, slug;
                double lat, lon;
                CityResolver.ResolveCity(input, out disp, out slug, out lat, out lon);

                Dispatcher.Invoke(new Action(delegate
                {
                    _app.SetCity(disp, slug, lat, lon);
                    _lblStatus.Text = "✅ Город установлен!";
                    _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(100, 220, 100));

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
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");

            BuildUI();
        }

        private void BuildUI()
        {
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
                Foreground = Brushes.White,
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
                var btn = new Button()
                {
                    Content = label,
                    FontSize = 11,
                    Padding = new Thickness(9, 4, 9, 4),
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = (val == _app.Config.RefreshIntervalMinutes) ? new SolidColorBrush(Color.FromArgb(60, 0, 120, 215)) : new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                    Foreground = (val == _app.Config.RefreshIntervalMinutes) ? new SolidColorBrush(Color.FromRgb(76, 194, 255)) : new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderBrush = (val == _app.Config.RefreshIntervalMinutes) ? new SolidColorBrush(Color.FromRgb(0, 120, 215)) : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
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
                Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
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
                Background = new SolidColorBrush(Color.FromRgb(48, 48, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
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

        public static void DrawIcon(Canvas canvas, string iconType, double size)
        {
            canvas.Children.Clear();
            canvas.Width = size;
            canvas.Height = size;

            if (iconType == "sun")
                DrawSun(canvas, size);
            else if (iconType == "partly_cloudy")
                DrawSunBehindCloud(canvas, size);
            else if (iconType == "thunder")
                DrawThunder(canvas, size);
            else if (iconType == "snow")
                DrawSnow(canvas, size);
            else if (iconType == "rain")
                DrawRain(canvas, size);
            else if (iconType == "fog")
                DrawFog(canvas, size);
            else
                DrawCloud(canvas, size, Color.FromRgb(165, 195, 225));
        }

        private static void DrawSun(Canvas c, double s)
        {
            var sun = new Ellipse();
            sun.Width = s * 0.72;
            sun.Height = s * 0.72;
            sun.Fill = new SolidColorBrush(Color.FromRgb(255, 195, 0));
            Canvas.SetLeft(sun, s * 0.14);
            Canvas.SetTop(sun, s * 0.14);
            c.Children.Add(sun);
        }

        private static void DrawSunBehindCloud(Canvas c, double s)
        {
            var sun = new Ellipse();
            sun.Width = s * 0.45;
            sun.Height = s * 0.45;
            sun.Fill = new SolidColorBrush(Color.FromRgb(255, 195, 0));
            Canvas.SetLeft(sun, s * 0.45);
            Canvas.SetTop(sun, s * 0.08);
            c.Children.Add(sun);

            DrawCloud(c, s * 0.9, Color.FromRgb(190, 215, 240));
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

        private static void DrawRain(Canvas c, double s)
        {
            DrawCloud(c, s * 0.85, Color.FromRgb(150, 180, 210));
            double scale = s / 40.0;

            for (int i = 0; i < 3; i++)
            {
                var drop = new System.Windows.Shapes.Line();
                drop.X1 = (13 + i * 7) * scale;
                drop.Y1 = 28 * scale;
                drop.X2 = (11 + i * 7) * scale;
                drop.Y2 = 34 * scale;
                drop.Stroke = new SolidColorBrush(Color.FromRgb(70, 160, 240));
                drop.StrokeThickness = 2 * scale;
                drop.StrokeStartLineCap = PenLineCap.Round;
                drop.StrokeEndLineCap = PenLineCap.Round;
                c.Children.Add(drop);
            }
        }

        private static void DrawSnow(Canvas c, double s)
        {
            DrawCloud(c, s * 0.85, Color.FromRgb(170, 195, 220));
            double scale = s / 40.0;

            for (int i = 0; i < 3; i++)
            {
                var flake = new Ellipse();
                flake.Width = 3.5 * scale;
                flake.Height = 3.5 * scale;
                flake.Fill = Brushes.White;
                Canvas.SetLeft(flake, (11 + i * 8) * scale);
                Canvas.SetTop(flake, 29 * scale);
                c.Children.Add(flake);
            }
        }

        private static void DrawThunder(Canvas c, double s)
        {
            DrawCloud(c, s * 0.85, Color.FromRgb(120, 140, 170));
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
            bolt.Fill = new SolidColorBrush(Color.FromRgb(255, 215, 0));
            c.Children.Add(bolt);
        }

        private static void DrawFog(Canvas c, double s)
        {
            DrawCloud(c, s * 0.75, Color.FromRgb(170, 185, 205));
            double scale = s / 40.0;

            for (int i = 0; i < 2; i++)
            {
                var line = new System.Windows.Shapes.Line();
                line.X1 = 10 * scale;
                line.Y1 = (28 + i * 5) * scale;
                line.X2 = 30 * scale;
                line.Y2 = (28 + i * 5) * scale;
                line.Stroke = new SolidColorBrush(Color.FromRgb(200, 215, 230));
                line.StrokeThickness = 2 * scale;
                line.StrokeStartLineCap = PenLineCap.Round;
                line.StrokeEndLineCap = PenLineCap.Round;
                c.Children.Add(line);
            }
        }
    }
}
