using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using URL_Opening_Selector.SettingsPages;
using WinUIEx;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace URL_Opening_Selector
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogViewWindow : WindowEx
    {
        public LogViewWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            Util.InitializeWindowSystemBackdrop(this);
            SettingsPage.SystemBackdropChanged += SystemBackdropChanged;
        }

        private void SelectorBar_OnSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            switch (sender.SelectedItem.Tag)
            {
                case "log":
                    Frame1.Navigate(typeof(LogView), null, new SlideNavigationTransitionInfo 
                    {
                        Effect = SlideNavigationTransitionEffect.FromRight
                    });
                    break;
                case "info":
                    Frame1.Navigate(typeof(StatisticsPage), null, new SlideNavigationTransitionInfo
                    {
                        Effect = SlideNavigationTransitionEffect.FromLeft
                    });
                    break;
            }
        }
        
        private void SystemBackdropChanged(object _, object __)
        {
            Util.InitializeWindowSystemBackdrop(this);
        }

        private void LogViewWindow_OnClosed(object sender, WindowEventArgs args)
        {
            App.LogViewWindow = null;
            SettingsPage.SystemBackdropChanged -= SystemBackdropChanged;
        }
    }
}
