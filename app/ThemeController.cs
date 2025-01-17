using DebounceThrottle;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SeaIce
{
    internal class ThemeController
    {
        public ThemeController()
        {
            _themeChangedHandler = new UserPreferenceChangingEventHandler(SystemEvents_UserPreferenceChanging);
            SystemEvents.UserPreferenceChanging += _themeChangedHandler;

            _displaySettingsChanged = new EventHandler(SystemEvents_DisplaySettingsChanged);
            SystemEvents.DisplaySettingsChanged += _displaySettingsChanged;

            App.Current.Activated += (s, e) =>
            {
                ApplyTheme(ShouldSystemUseDarkMode());
            };
            App.Current.Exit += (s, e) =>
            {
                SystemEvents.UserPreferenceChanging -= _themeChangedHandler;
                SystemEvents.DisplaySettingsChanged -= _displaySettingsChanged;
            };
        }

        // Internal

        readonly UserPreferenceChangingEventHandler _themeChangedHandler;
        readonly EventHandler _displaySettingsChanged;

        readonly DebounceDispatcher _debounceDispatcher = new DebounceDispatcher(500);

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        private static extern bool ShouldSystemUseDarkMode();

        bool _isDarkTheme;

        private void ApplyTheme(bool isDarkTheme)
        {
            _isDarkTheme = isDarkTheme;
            var appTheme = _isDarkTheme ? "Dark" : "Light";
            var themeUri = new Uri($"/themes/{appTheme}.xaml", UriKind.Relative);
            var dicts = App.Current.Resources.MergedDictionaries;
            if (dicts.Count > 0 && dicts[0].Source != null)
            {
                dicts[0].Source = themeUri;
                Debug.WriteLine($"Theme is set to '{appTheme}'");
            }
            else
            {
                Debug.WriteLine($"Theme '{appTheme}' does not exist");
            }
        }

        private void SystemEvents_UserPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
        {
            _debounceDispatcher.Debounce(() =>
            {
                bool isDarkTheme = ShouldSystemUseDarkMode();
                if (isDarkTheme != _isDarkTheme)
                {
                    ApplyTheme(isDarkTheme);
                    Debug.WriteLine($"UserPreferenceChanging {e.Category}");
                }
            });
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            _debounceDispatcher.Debounce(() =>
            {
                Debug.WriteLine("DisplaySettingsChanged");
            });
        }
    }
}
