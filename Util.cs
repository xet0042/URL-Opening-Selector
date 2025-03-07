using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace URL_Opening_Selector
{
    internal class Util
    {
        public static void UrlStartWith(string url, string browserPath)
        {
            var file = new FileInfo(browserPath);
            if (!file.Exists)
                throw new FileNotFoundException($"The bowser \"{browserPath}\" is not exist!");
            var process = new Process
            {
                StartInfo =
                {
                    FileName = browserPath,
                    Arguments = $"\"{url}\"",
                }
            };
            process.Start();
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

        public static List<Browser> FindBrowsers2(string name)
        {
            var browsers = new List<Browser>();
            browsers.AddRange(Find(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)));
            browsers.AddRange(Find(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)));
            return browsers;

            // Find the browsers with the start menu shortcut
            List<Browser> Find(string path)
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

                return r.GroupBy(b => b.Path)
                    .Select(g => g.First())
                    .ToList();
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
    }

    public class Configuration
    {
        public string DefaultBrowser { set; get; }
        public UrlPatternMethod DefaultMethod { set; get; }
        public SystemBackdrop SystemBackdrop { set; get; } = SystemBackdrop.Mica;
        public ObservableCollection<Browser> Browsers { get; set; } = [];
        public ObservableCollection<string> AllowBrowsersName { get; set; } = ["Firefox", "Google Chrome", "Microsoft Edge", "Opera"];
    }

    public class UrlPattern
    {
        public string Pattern { get; set; }
        public string Browser { get; set; }
        public UrlPatternMethod Method { get; set; }
    }

    public class Browser
    {
        public string Name { get; set; }
        public string Path { get; set; }
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

    public class PatternSettingItem
    {
        public string Pattern { get; set; }
        public string Browser { get; set; }
        public UrlPatternMethod Method { get; set; }
        public string Icon { get; set; }
    }

    public enum SystemBackdrop
    {
        None,
        Mica,
        Acrylic,
        TransparentTint
    }
}