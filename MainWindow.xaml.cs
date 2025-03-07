using WinUIEx;
using System.Linq;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx, INotifyPropertyChanged
    {
        public string Url { get; set; }

        public UrlPattern UrlPattern { get; set; }

        private string _pattern;

        // https://www.baidu.com
        public string Pattern
        {
            get => _pattern;
            set
            {
                _pattern = value;
                OnPropertyChanged();
            }
        }
        private bool _p;

        public bool PatternExist
        {
            get => _p;
            set
            {
                CheckBox1.IsEnabled = !value;
                CheckBox1.IsChecked = value;
                _p = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            this.CenterOnScreen();
            this.SetIsAlwaysOnTop(true);
            this.SetForegroundWindow();
            Util.InitializeWindowSystemBackdrop(this);
            // TextBox1.Text = Pattern;
            CheckBox1.IsChecked = PatternExist;
            ComboBox1.ItemsSource = Globals.AppConfiguration.Configuration.Browsers.Select(b => b.Name);
            if (PatternExist)
                ComboBox1.SelectedItem = UrlPattern.Browser;
            else
                ComboBox1.SelectedItem = Globals.AppConfiguration.Configuration.DefaultBrowser;
            OkButton.IsEnabled = Globals.AppConfiguration.Configuration.DefaultBrowser is not null;
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (CheckBox1.IsChecked ?? false)
                await Globals.AppConfiguration.AddUrlPattern(Pattern, (string)ComboBox1.SelectedItem,
                    UrlPatternMethod.Open);
            Util.UrlStartWith(Url,
                Globals.AppConfiguration.Configuration.Browsers.First(b => b.Name == (string)ComboBox1.SelectedItem).Path);
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetForegroundWindow();
        }

        private void ComboBox1_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OkButton.IsEnabled = e.AddedItems.Count > 0;
        }
    }
}