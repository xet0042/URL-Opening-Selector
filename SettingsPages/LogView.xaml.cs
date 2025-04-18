using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogView : Page, INotifyPropertyChanged
    {
        private LogLevel _levels;

        public ObservableCollection<Log> Logs =>
            new(from log in Globals.Logs.Reverse() where _levels.HasFlag(log.Level) select log);

        public LogView()
        {
            InitializeComponent();
        }

        private void TokenView1_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                var levelString = (item as TokenItem)!.Content as string;
                if (!Enum.TryParse(levelString, out LogLevel level))
                    continue;
                _levels |= level;
            }

            foreach (var item in e.RemovedItems)
            {
                var levelString = (item as TokenItem)!.Content as string;
                if (!Enum.TryParse(levelString, out LogLevel level))
                    continue;
                _levels &= ~level;
            }
            OnPropertyChanged(nameof(Logs));
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
    }

    public class LevelToForeground : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not LogLevel level) return value;
            switch (level)
            {
                case LogLevel.Debug:
                    return new SolidColorBrush(Colors.Gray);
                case LogLevel.Info:
                    return new SolidColorBrush(Colors.DodgerBlue);
                case LogLevel.Warning:
                    return new SolidColorBrush(Colors.Yellow);
                case LogLevel.Error:
                    return new SolidColorBrush(Colors.Red);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class LevelToGlyph : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not LogLevel level) return value;
            switch (level)
            {
                case LogLevel.Debug:
                    return "\uEBE8";
                case LogLevel.Info:
                    return "\uF167";
                case LogLevel.Warning:
                    return "\uE814";
                case LogLevel.Error:
                    return "\uEB90";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}