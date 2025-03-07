using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI;
using WinUIEx;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using URL_Opening_Selector.SettingsPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : WindowEx
    {
        public readonly ObservableCollection<PatternSettingItem> Items = [];
        private static readonly string[] SourceArray = ["常规设置", "默认规则"];

        // public delegate void OnSystemBackdropChanged(object sender, object e);
        
        public Settings()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            this.CenterOnScreen();
            this.SetForegroundWindow();
            Util.InitializeWindowSystemBackdrop(this);
            var setting = new PatternSettingItem
            {
                Pattern = "常规设置",
                Icon = "\uE713"
            };
            var defaultPattern = new PatternSettingItem
            {
                Pattern = "默认规则",
                Browser = Globals.AppConfiguration.Configuration.DefaultBrowser,
                Method = Globals.AppConfiguration.Configuration.DefaultMethod,
                Icon = "\uE7C4"
            };
            Items.Add(setting);
            Items.Add(defaultPattern);
            ListView1.SelectedIndex = 0;
            _ = Globals.AppConfiguration.GetAllUrlPatterns(Items);
        }

        private void ListView1_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                ListView1.SelectedIndex = 0;
                return;
            }
            if (e.AddedItems.First() is not PatternSettingItem item) return;
            if (item.Pattern != "常规设置")
                Frame1.Navigate(typeof(LinkSetting), item);
            else
                Frame1.Navigate(typeof(SettingsPage));
            RemoveButton.IsEnabled = !SourceArray.Contains(item.Pattern);
        }

        private void Settings_OnClosed(object sender, WindowEventArgs args)
        {
            App.SettingWindow = null;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListView1.SelectedIndex < 2) return;
            if (ListView1.SelectedItem is not PatternSettingItem item) return;
            await Globals.AppConfiguration.RemoveUrlPattern(item.Pattern);
            Items.Remove(item);
        }

        private async void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Globals.AppConfiguration.Configuration.DefaultBrowser))
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = WindowRootElement.XamlRoot,
                    Title = "错误",
                    Content = "请先设置默认浏览器",
                    CloseButtonText = "确定",
                    DefaultButton = ContentDialogButton.Close
                };
                await dialog.ShowAsync();
                return;
            }
            var content = new StackPanel();
            var textBox = new TextBox
            {
                PlaceholderText = "请输入规则",
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
                VisualStateManager.GoToState(textBox,"Valid", true);
                tips.Visibility = Visibility.Collapsed;
            };
            content.Children.Add(textBox);
            content.Children.Add(tips);
            var contentDialog = new ContentDialog
            {
                XamlRoot = WindowRootElement.XamlRoot,
                Title = "添加规则",
                Content = content,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
            };
            contentDialog.PrimaryButtonClick += async (dialog, args) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    VisualStateManager.GoToState(textBox,"Invalid", true);
                    tips.Visibility = Visibility.Visible;
                    tips.Text = "规则不能为空";
                    args.Cancel = true;
                    return;
                }

                if (SourceArray.Contains(textBox.Text))
                {
                    VisualStateManager.GoToState(textBox,"Invalid", true);
                    tips.Visibility = Visibility.Visible;
                    tips.Text = "无效的规则";
                    args.Cancel = true;
                    return;
                }

                if (Items.Any(i => i.Pattern == textBox.Text))
                {
                    VisualStateManager.GoToState(textBox,"Invalid", true);
                    tips.Visibility = Visibility.Visible;
                    tips.Text = "规则已存在";
                    args.Cancel = true;
                    return;
                }

                await Globals.AppConfiguration.AddUrlPattern(textBox.Text,
                    Globals.AppConfiguration.Configuration.DefaultBrowser, UrlPatternMethod.Inquire);
                Items.Add(new PatternSettingItem
                {
                    Pattern = textBox.Text,
                    Browser = Globals.AppConfiguration.Configuration.DefaultBrowser,
                    Method = UrlPatternMethod.Inquire,
                    Icon = "\uE71B"
                });
            };
            await contentDialog.ShowAsync();
        }

        private void SystemBackdropChanged(object _, object __)
        {
            Util.InitializeWindowSystemBackdrop(this);
        }
        
        private void Frame1_OnNavigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is SettingsPage settingsPage)
                settingsPage.SystemBackdropChanged += SystemBackdropChanged;
        }
    }
}