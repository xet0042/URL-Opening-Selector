using WinUIEx;
using System;
using System.IO;
using System.Linq;
using Windows.System;
using Microsoft.Win32;
using Windows.Storage;
using System.Reflection;
using Microsoft.UI.Xaml;
using IWshRuntimeLibrary;
using System.Diagnostics;
using File = System.IO.File;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel;
using System.Collections.Generic;
using Windows.Management.Deployment;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static URL_Opening_Selector.Log;

namespace URL_Opening_Selector
{
    internal abstract class Util
    {
        public static void UrlStartWith(string url, Pattern urlPattern)
        {
            var browser = Globals.AppConfiguration.Configuration.Browsers
                .FirstOrDefault(b => b.Name == urlPattern.Browser);
            if (browser == null)
                return;
            if (browser.IsUwp)
            {
                UrlStartWithUwpApplication(url, urlPattern, browser);
                return;
            }

            var browserPath = browser.Path;
            if (string.IsNullOrEmpty(browserPath))
                // throw new FileNotFoundException($"The bowser \"{urlPattern.Browser}\" is not exist!");
                return;
            var file = new FileInfo(browserPath);
            if (!file.Exists)
                // throw new FileNotFoundException($"The bowser \"{browserPath}\" is not exist!");
                return;
            var arguments = $"\"{url}\"";
            if (urlPattern.Advanced)
                arguments = urlPattern.StartArguments.Replace("{url}", arguments);
            var process = new Process
            {
                StartInfo =
                {
                    FileName = browserPath,
                    Arguments = arguments
                }
            };
            process.Start();
        }

        private static void UrlStartWithUwpApplication(string url, Pattern urlPattern, Browser browser)
        {
            var option = new LauncherOptions
            {
                TargetApplicationPackageFamilyName = browser.Path
            };
            try
            {
                Launcher.LaunchUriAsync(new Uri(url), option);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public static ProgramInfo GetProgramByName(string name)
        {
            return GetAllPrograms().Find(x => x.Name == name);
        }

        public static List<ProgramInfo> GetAllPrograms()
        {
            var sf1 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            var sf2 = Registry.Users.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (sf1 == null || sf2 == null) return [];
            var r = new List<ProgramInfo>();

            foreach (var key in sf1.GetSubKeyNames())
                AddProgram(key, sf1);
            foreach (var key in sf2.GetSubKeyNames())
                AddProgram(key, sf2);

            return r;

            void AddProgram(string key, RegistryKey sf)
            {
                var subKey = sf.OpenSubKey(key)!;
                string installLocation;
                if (subKey.GetValue("InstallLocation") is not string location)
                {
                    if (subKey.GetValue("UninstallString") is not string uninstallString) return;
                    var fileInfo = new FileInfo(uninstallString);
                    installLocation = fileInfo.Directory?.FullName ?? "";
                }
                else
                    installLocation = location;

                var name = subKey.GetValue("DisplayName") as string;
                if (string.IsNullOrEmpty(installLocation) || string.IsNullOrEmpty(name)) return;
                r.Add(new ProgramInfo
                {
                    Name = name,
                    Location = installLocation,
                    Version = (string)(subKey.GetValue("DisplayVersion") ?? "Unknown")
                });
            }
        }


        public static List<Browser> FindBrowsers(string name, string filename)
        {
            return (from program in GetAllPrograms()
                where program.Name.Contains(name)
                select new Browser
                {
                    Name = program.Name,
                    Path = Path.Join(program.Location, filename)
                }).ToList();
        }

        public static void FindBrowsers2(string name, ObservableCollection<Browser> arr,
            Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
        {
            Find(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
            Find(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu));
            Find2();
            return;

            // Find the browsers in the start menu shortcut
            void Find(string path)
            {
                var r = new List<Browser>();
                var dir = new DirectoryInfo(path);
                var files = dir.EnumerateFiles("*.lnk", new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = true,
                        MatchCasing = MatchCasing.CaseInsensitive
                    })
                    .Where(f => f.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var file in files)
                {
                    var shell = new WshShell();
                    try
                    {
                        var link = (IWshShortcut)shell.CreateShortcut(file.FullName);
                        r.Add(new Browser
                        {
                            Name = Path.GetFileNameWithoutExtension(file.Name),
                            Path = link.TargetPath
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(shell);
                    }
                }

                dispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var b in r.GroupBy(b => b.Path).Select(g => g.First()))
                        arr.Add(b);
                });
            }

            // Find the browsers in UWP Applications
            void Find2()
            {
                var packageManager = new PackageManager();
                var packages = packageManager.FindPackagesForUser(string.Empty);
                foreach (var package in packages)
                {
                    if (package.IsFramework || package.IsResourcePackage ||
                        string.IsNullOrEmpty(package.DisplayName)) continue;
                    if (package.DisplayName.Contains(name, StringComparison.OrdinalIgnoreCase))
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            arr.Add(new Browser
                            {
                                Name = package.DisplayName + " (UWP)",
                                Path = package.Id.FamilyName,
                                IsUwp = true
                            });
                        });
                }
            }
        }

        public static void InitializeWindowSystemBackdrop(Window window)
        {
            switch (Globals.AppConfiguration.Configuration.SystemBackdrop)
            {
                case SystemBackdrop.None:
                    window.SystemBackdrop = null;
                    break;
                case SystemBackdrop.Mica:
                    window.SystemBackdrop = new MicaBackdrop();
                    break;
                case SystemBackdrop.Acrylic:
                    window.SystemBackdrop = new DesktopAcrylicBackdrop();
                    break;
                case SystemBackdrop.TransparentTint:
                    window.SystemBackdrop = new TransparentTintBackdrop();
                    break;
            }
        }

        public static string GetAppVersion()
        {
            try
            {
                // 方法 1：打包应用版本
                var packageVersion = Package.Current.Id.Version;
                return
                    $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            }
            catch
            {
                // 方法 2：程序集版本（回退）
                var infoVersion = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;

                return infoVersion ?? "Unknown";
            }
        }
    }

    internal abstract class Logger
    {
        private static StorageFile _file;

        public static async Task Init()
        {
            try
            {
                Globals.Logs ??= [];
                var logFolder =
                    await ApplicationData.Current.LocalFolder.CreateFolderAsync("Logs",
                        CreationCollisionOption.OpenIfExists);
                var date = DateTime.Now.ToString("yyyyMMdd HH-mm-ss");
                var filename = $"{date}.log";
                _file = await logFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                var reader = await _file.OpenReadAsync();
                await using var stream = reader.AsStream();
                using var streamReader = new StreamReader(stream);
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (TryParse(line, out var log))
                        Globals.Logs.Add(log);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async static void _Log(string message, LogLevel level)
        {
            if (Globals.Logs is null)
                await Init();
            if (_file is null)
                await Init();
            var log = new Log
            {
                Level = level,
                Message = message,
                Time = DateTime.Now.ToString("yy-MM-dd HH:mm:ss")
            };
            Globals.Logs!.Add(log);
            await File.AppendAllLinesAsync(_file!.Path, [log.ToString()]);
        }

        public static void Info(string message) => _Log(message, LogLevel.Info);
        public static void Warn(string message) => _Log(message, LogLevel.Warning);
        public static void Error(string message) => _Log(message, LogLevel.Error);
        public static void Debug(string message) => _Log(message, LogLevel.Debug);
    }

    public class Configuration
    {
        public Pattern DefaultSettings { set; get; } = new();
        public SystemBackdrop SystemBackdrop { set; get; } = SystemBackdrop.Mica;
        public ObservableCollection<Browser> Browsers { get; set; } = [];

        public ObservableCollection<string> AllowBrowsersName { get; set; } =
            ["Firefox", "Google Chrome", "Microsoft Edge", "Opera", "Arc"];
    }

    public class Pattern
    {
        public string Browser { get; set; }
        public bool Advanced { get; set; }
        public UrlPatternMethod Method { get; set; }
        public string StartArguments { get; set; } = "";
    }

    public class UrlPattern : Pattern
    {
        public string Pattern { get; set; }
    }

    public class Browser
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsUwp { get; set; }
    }

    public class ProgramInfo
    {
        public string Name { get; init; }
        public string Location { get; init; }
        public string Version { get; init; }
    }

    public enum UrlPatternMethod
    {
        Inquire,
        Open,
        None
    }

    public class PatternSettingItem : UrlPattern
    {
        public string Icon { get; set; }
    }

    public enum SystemBackdrop
    {
        None,
        Mica,
        Acrylic,
        TransparentTint
    }

    [Flags]
    public enum LogLevel
    {
        None,
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
    }

    public partial class Log
    {
        public string Time { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"[{Time}][{Level}] {Message}";
        }

        public static bool TryParse(string content, out Log log)
        {
            var regex = LogRegex();
            var result = regex.Match(content);
            if (!result.Success)
            {
                log = new Log();
                return false;
            }

            ;
            log = new Log
            {
                Time = result.Groups[1].Value,
                Level = (LogLevel)Enum.Parse(typeof(LogLevel), result.Groups[2].Value),
                Message = result.Groups[3].Value
            };
            return true;
        }

        [GeneratedRegex(@"\[(.*?)].?\[(.*?)].?(.*)")]
        private static partial Regex LogRegex();
    }
}