using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Input;
using Windows.Storage;
using System.IO;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public string FormatBrowserCount(int count) => $"被扫描到的浏览器 - {count} 个";
        public static event EventHandler SystemBackdropChanged;

        public SettingsPage()
        {
            InitializeComponent();
            ComboBox1.SelectedIndex = (int)Globals.AppConfiguration.Configuration.SystemBackdrop;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Button1.IsEnabled = false;
            ProgressBar1.IsEnabled = true;
            ProgressBar1.Visibility = Visibility.Visible;
            Globals.AppConfiguration.Configuration.Browsers.Clear();
            Task.Run(async void () =>
            {
                try
                {
                    foreach (var name in Globals.AppConfiguration.Configuration.AllowBrowsersName)
                        Util.FindBrowsers2(name, Globals.AppConfiguration.Configuration.Browsers, DispatcherQueue);
                    await Globals.AppConfiguration.SaveJson();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
                finally
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        Button1.IsEnabled = true;
                        ProgressBar1.IsEnabled = false;
                        ProgressBar1.Visibility = Visibility.Collapsed;
                    });
                }
            });
        }

        private async void Delete1_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (sender as Button)!;
            var item = (button.Tag as Browser)!;
            Globals.AppConfiguration.Configuration.Browsers.Remove(item);
            await Globals.AppConfiguration.SaveJson();
        }

        private async void Delete2_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (sender as Button)!;
            var item = (button.Tag as string)!;
            Globals.AppConfiguration.Configuration.AllowBrowsersName.Remove(item);
            await Globals.AppConfiguration.SaveJson();
        }

        private async void Add_OnClick(object sender, RoutedEventArgs e)
        {
            var content = new StackPanel();
            var textBox = new TextBox
            {
                PlaceholderText = "请输入浏览器名称",
                Style = (Style)Application.Current.Resources["ValidatableTextBoxStyle"],
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var tips = new TextBlock
            {
                FontSize = 8,
                Visibility = Visibility.Collapsed,
                Foreground = new SolidColorBrush(Colors.Red)
            };

            textBox.TextChanged += (o, args) =>
            {
                VisualStateManager.GoToState(textBox, "Valid", true);
                tips.Visibility = Visibility.Collapsed;
            };
            content.Children.Add(textBox);
            content.Children.Add(tips);
            var contentDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "添加规则",
                Content = content,
                PrimaryButtonText = "添加",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
            };
            contentDialog.PrimaryButtonClick += async (dialog, args) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    VisualStateManager.GoToState(textBox, "Invalid", true);
                    tips.Visibility = Visibility.Visible;
                    tips.Text = "名称不能为空";
                    args.Cancel = true;
                    return;
                }

                if (Globals.AppConfiguration.Configuration.AllowBrowsersName.Contains(textBox.Text))
                {
                    VisualStateManager.GoToState(textBox, "Invalid", true);
                    tips.Visibility = Visibility.Visible;
                    tips.Text = "名称已存在";
                    args.Cancel = true;
                    return;
                }

                Globals.AppConfiguration.Configuration.AllowBrowsersName.Add(textBox.Text);
                await Globals.AppConfiguration.SaveJson();
            };
            await contentDialog.ShowAsync();
        }

        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            if (e.AddedItems.First() is not TextBlock textBlock) return;
            if (!Enum.TryParse<SystemBackdrop>((string)textBlock.Tag, true, out var backDrop)) return;
            Globals.AppConfiguration.Configuration.SystemBackdrop = backDrop;
            SystemBackdropChanged?.Invoke(this, EventArgs.Empty);
            await Globals.AppConfiguration.SaveJson();
        }

        private async void SettingsCard_OnClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:defaultapps"));
        }

        private void SettingsCard_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var settingsCard = (sender as SettingsCard)!;
            // 弹出菜单
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse) return;
            var pointerPoint = e.GetCurrentPoint(settingsCard);
            if (!pointerPoint.Properties.IsRightButtonPressed &&
                pointerPoint.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonReleased) return;
            
            var browser = settingsCard.Tag as Browser;
            if (browser == null) return;
            var menu = new MenuFlyout();
            var menuItem = new MenuFlyoutItem
            {
                Text = "打开文件所在位置",
                IsEnabled = !browser.IsUwp,
                Icon = new FontIcon
                {
                    Glyph = "\uE8DA",
                },
            };
            menuItem.Click += (o, args) =>
            {
                var file = new Uri(browser.Path);
                var folder = file.Segments[0];
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,{file}",
                        UseShellExecute = true,
                    }
                };
                process.Start();
            };
            menu.Items.Add(menuItem);
            menu.ShowAt(settingsCard, pointerPoint.Position);
        }

        private void SettingsCard_Click_1(object sender, RoutedEventArgs e)
        {
            var file = new Uri(Path.Join(ApplicationData.Current.LocalFolder.Path, "config.json"));
            var folder = file.Segments[0];
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,{file}",
                    UseShellExecute = true,
                }
            };
            process.Start();
        }

        private void SettingsCard2_OnClick(object sender, RoutedEventArgs e)
        {
            App.LogViewWindow ??= new LogViewWindow();
            App.LogViewWindow.Activate();
        }
    }
}