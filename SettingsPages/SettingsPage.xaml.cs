using System;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public event EventHandler SystemBackdropChanged;

        public SettingsPage()
        {
            InitializeComponent();
            ComboBox1.SelectedIndex = (int)Globals.AppConfiguration.Configuration.SystemBackdrop;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Globals.AppConfiguration.Configuration.Browsers.Clear();
            DispatcherQueue.TryEnqueue(async void () =>
            {
                foreach (var name in Globals.AppConfiguration.Configuration.AllowBrowsersName)
                foreach (var browser in Util.FindBrowsers2(name))
                    Globals.AppConfiguration.Configuration.Browsers.Add(browser);
                await Globals.AppConfiguration.SaveJson();
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

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void SettingsCard_OnClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:defaultapps"));
        }
    }
}