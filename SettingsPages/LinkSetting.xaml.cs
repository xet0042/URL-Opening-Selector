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
#if DEBUG
            _init += 2;
#endif
            ComboBox1.SelectedItem = item.Browser;
            ComboBox2.SelectedIndex = (int)item.Method;
            ToggleSwitch1.IsOn = item.Advanced;
            StartArguments.Text = item.StartArguments;
            SettingsExpander1.IsExpanded = item.Advanced;
            foreach (SettingsCard settingsExpanderItem in SettingsExpander1.Items)
                settingsExpanderItem.IsEnabled = item.Advanced;
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

            var browser = (e.AddedItems.First() as string)!;
            _item.Browser = browser;
            // string convert to UrlPatternMethod
            if (_item.Pattern != "默认规则")
                await Globals.AppConfiguration.UpdateUrlPattern(_item);
            else
            {
                Globals.AppConfiguration.Configuration.DefaultSettings.Browser = browser;
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
            foreach (SettingsCard item in SettingsExpander1.Items)
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