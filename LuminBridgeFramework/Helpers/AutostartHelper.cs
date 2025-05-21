using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminBridgeFramework.Helpers
{
    public static class AutostartHelper
    {
        private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "LuminBridge";

        public static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false))
            {
                return key?.GetValue(AppName) != null;
            }
        }

        public static void Enable()
        {
            string exePath = System.Windows.Forms.Application.ExecutablePath;
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
            {
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
        }

        public static void Disable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
            {
                key?.DeleteValue(AppName, false);
            }
        }
    }
}
