using System;
using System.Linq;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LinkSetting : Page
    {
        private PatternSettingItem _item;
        private int _init;

        public LinkSetting()
        {
            InitializeComponent();
            ComboBox1.ItemsSource = Globals.AppConfiguration.Configuration.Browsers.Select(b => b.Name).ToList();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is not PatternSettingItem item) return;
            _item = item;
            var browser = Globals.AppConfiguration.Configuration.Browsers.FirstOrDefault(b => b.Name == item.Browser);
            if (browser == null) return;
            ComboBox1.SelectedItem = item.Browser;
            ToggleSwitch1.IsEnabled = !browser.IsUwp;
            StartArguments.Text = item.StartArguments;
            ComboBox2.SelectedIndex = (int)item.Method;
            SettingsExpander1.IsEnabled = !browser.IsUwp;
            ToggleSwitch1.IsOn = item.Advanced && !browser.IsUwp;
            // SettingsExpander1.IsExpanded = item.Advanced;
            foreach (Control settingsExpanderItem in SettingsExpander1.Items)
                settingsExpanderItem.IsEnabled = item.Advanced && !browser.IsUwp;


            // 根据当前参数选择合适的模板
            SelectTemplateBasedOnArguments(item.StartArguments);
        }

        // 根据当前参数选择合适的模板
        private void SelectTemplateBasedOnArguments(string arguments)
        {
            if (string.IsNullOrEmpty(arguments) || arguments == "{url}")
            {
                TemplateComboBox.SelectedIndex = 0;
                return;
            }

            for (var i = 0; i < TemplateComboBox.Items.Count; i++)
            {
                if (TemplateComboBox.Items[i] is not ComboBoxItem item ||
                    (string)item.Tag != arguments) continue;
                TemplateComboBox.SelectedIndex = i;
                return;
            }

            // 如果没有匹配的模板，不选择任何项
            TemplateComboBox.SelectedIndex = -1;
        }

        // 添加模板选择事件处理
        private async void TemplateComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init > 0)
            {
                _init--;
                return;
            }

            if (TemplateComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string template = (string)selectedItem.Tag;
                StartArguments.Text = template;

                // 更新配置
                _item.StartArguments = template;
                if (_item.Pattern != "默认规则")
                    await Globals.AppConfiguration.UpdateUrlPattern(_item);
                else
                {
                    Globals.AppConfiguration.Configuration.DefaultSettings.StartArguments = template;
                    await Globals.AppConfiguration.SaveJson();
                }
            }
        }

        // 添加测试启动功能
        private void TestLaunch_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建一个测试URL
                const string testUrl = "https://www.example.com";

                // 获取当前选择的浏览器
                var browser = Globals.AppConfiguration.Configuration.Browsers
                    .FirstOrDefault(b => b.Name == _item.Browser);

                if (browser == null)
                {
                    ShowErrorMessage("未找到选择的浏览器");
                    return;
                }

                // 创建临时设置项用于测试
                var testSettings = new PatternSettingItem
                {
                    Browser = _item.Browser,
                    Advanced = true,
                    StartArguments = StartArguments.Text
                };

                // 启动浏览器
                Util.UrlStartWith(testUrl, testSettings);

                // 显示成功消息
                ShowSuccessMessage("测试启动成功");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"测试启动失败: {ex.Message}");
            }
        }

        // 显示错误消息
        private void ShowErrorMessage(string message)
        {
            InfoBar1.Title = "Error";
            InfoBar1.Severity = InfoBarSeverity.Error;
            InfoBar1.Message = message;
            InfoBar1.IsOpen = true;
        }

        // 显示成功消息
        private void ShowSuccessMessage(string message)
        {
            InfoBar1.Title = "Success";
            InfoBar1.Severity = InfoBarSeverity.Success;
            InfoBar1.Message = message;
            InfoBar1.IsOpen = true;
        }

        private async void ComboBox2_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init > 0)
            {
                _init--;
                return;
            }

            var textBlock = (e.AddedItems.First() as TextBlock)!;
            // string convert to UrlPatternMethod
            if (!Enum.TryParse<UrlPatternMethod>((string)textBlock.Tag, true, out var methods)) return;
            _item.Method = methods;
            if (_item.Pattern != "默认规则")
                await Globals.AppConfiguration.UpdateUrlPattern(_item);
            else
            {
                Globals.AppConfiguration.Configuration.DefaultSettings.Method = methods;
                await Globals.AppConfiguration.SaveJson();
            }
        }

        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init > 0)
            {
                _init--;
                return;
            }

            var browserName = (e.AddedItems.First() as string)!;
            _item.Browser = browserName;
            var browser = Globals.AppConfiguration.Configuration.Browsers.First(b => b.Name == browserName);
            InfoBar1.IsOpen = browser.IsUwp;
            ToggleSwitch1.IsOn = _item.Advanced && !browser.IsUwp;
            ToggleSwitch1.IsEnabled = !browser.IsUwp;
            SettingsExpander1.IsEnabled = !browser.IsUwp;
            if (browser.IsUwp)
                SettingsExpander1.IsExpanded = false;
            // string convert to UrlPatternMethod
            if (_item.Pattern != "默认规则")
                await Globals.AppConfiguration.UpdateUrlPattern(_item);
            else
            {
                Globals.AppConfiguration.Configuration.DefaultSettings.Browser = browserName;
                await Globals.AppConfiguration.SaveJson();
            }
        }

        private async void ToggleSwitch1_OnToggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = (ToggleSwitch)sender;
            if (_init > 0)
            {
                _init--;
                return;
            }

            _item.Advanced = toggleSwitch.IsOn;
            foreach (Control item in SettingsExpander1.Items)
                item.IsEnabled = toggleSwitch.IsOn;
            if (_item.Pattern != "默认规则")
                await Globals.AppConfiguration.UpdateUrlPattern(_item);
            else
            {
                Globals.AppConfiguration.Configuration.DefaultSettings.Advanced = toggleSwitch.IsOn;
                await Globals.AppConfiguration.SaveJson();
            }
        }

        private async void StartArguments_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (_init > 0)
            {
                _init--;
                return;
            }

            _item.StartArguments = textBox.Text;
            if (_item.Pattern != "默认规则")
                await Globals.AppConfiguration.UpdateUrlPattern(_item);
            else
            {
                Globals.AppConfiguration.Configuration.DefaultSettings.StartArguments = textBox.Text;
                await Globals.AppConfiguration.SaveJson();
            }
        }
    }
}