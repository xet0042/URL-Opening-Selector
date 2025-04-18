using System;
using WinUIEx;
using SQLitePCL;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Activation;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Batteries_V2.Init();
            InitializeComponent();
            // var mainDispatcherQueue = DispatcherQueue.GetForCurrentThread();
            // Debug.WriteLine(AppInstance.GetCurrent().GetActivatedEventArgs());
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var activationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();
            Debug.WriteLine("1" + activationArguments);
            if (Globals.Initialed && activationArguments.Kind == ExtendedActivationKind.Protocol)
            {
                OnActivated(activationArguments);
                return;
            }

            await Logger.Init();
            await Globals.AppConfiguration.InitJson();
            var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += async (sender, eventArgs) =>
            {
                Debug.WriteLine("Exit Clicked!");
                TrayIcon.Dispose();
                await Globals.AppConfiguration.Close();
                if (SettingWindow is WindowEx window)
                    window.Close();
                Exit();
                Environment.Exit(0);
            };
            var showSettingsWindowCommand = (XamlUICommand)Resources["ShowSettingsWindowCommand"];
            showSettingsWindowCommand.ExecuteRequested += (sender, eventArgs) =>
            {
                if (SettingWindow is WindowEx window)
                    window.Activate();
                else
                {
                    SettingWindow = new Settings();
                    SettingWindow.Activate();
                }
            };
            var trayIcon = (Resources["TrayIcon"] as H.NotifyIcon.TaskbarIcon)!;
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = Path.Combine(basePath, "Assets", "TrayIcon.ico");
            trayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));
            trayIcon.Icon = new System.Drawing.Icon(iconPath);
            trayIcon.ForceCreate();
            TrayIcon = trayIcon;
            Globals.Initialed = true;
            if (activationArguments.Kind == ExtendedActivationKind.Protocol)
                OnActivated(activationArguments);
            if (SettingWindow is null)
                SettingWindow = new Settings();
        }

        private async void OnActivated(AppActivationArguments args)
        {
            try
            {
                var protocolArgs = (args.Data as ProtocolActivatedEventArgs)!;
                var uri = protocolArgs.Uri.AbsoluteUri;
                Logger.Info($"Open Uri: {uri}");
                Debug.WriteLine($"Uri: {uri}");
                Debug.WriteLine(Globals.AppConfiguration._db?.ToString() ?? "null");
                var r = await Globals.AppConfiguration.GetUrlPatterns(uri);

                void ShowDialog(string pattern, bool exist, UrlPattern p = null)
                {
                    var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                    if (!dispatcherQueue.HasThreadAccess) return;
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        Debug.WriteLine($"Pattern: {pattern}, Exist: {exist}");
                        var window = new MainWindow
                        {
                            UrlPattern = p,
                            Pattern = pattern,
                            Url = uri,
                            PatternExist = exist,
                        };
                        window.Activate();
                    });
                }

                if (r.Count > 0)
                {
                    var p = r.First();
                    switch (p.Method)
                    {
                        case UrlPatternMethod.Inquire:
                            ShowDialog(p.Pattern, true, p);
                            return;
                        case UrlPatternMethod.Open:
                            Util.UrlStartWith(uri, p);
                            return;
                        case UrlPatternMethod.None:
                            return;
                        default:
                            // DllImports.ShowMessageBox("未知的打开方式", "错误", 16);
                            return;
                    }
                }

                switch (Globals.AppConfiguration.Configuration.DefaultSettings.Method)
                {
                    case UrlPatternMethod.Inquire:
                        ShowDialog(uri, false);
                        break;
                    case UrlPatternMethod.Open:
                        Util.UrlStartWith(uri, Globals.AppConfiguration.Configuration.DefaultSettings);
                        break;
                    case UrlPatternMethod.None:
                        break;
                }
            }
            catch (Exception ex)
            {
                // DllImports.ShowMessageBox(ex.Message, "错误", 16);
            }
        }

        public static Window SettingWindow;
        public static H.NotifyIcon.TaskbarIcon TrayIcon { get; set; }
        public static LogViewWindow LogViewWindow;
    }
}