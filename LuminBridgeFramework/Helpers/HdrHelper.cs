using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LuminBridgeFramework
{
    public static class HdrHelper
    {
        private static Dictionary<string, HdrMonitorInfo> _cache;
        private static DateTime _lastFetch = DateTime.MinValue;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(1);

        public static Dictionary<string, HdrMonitorInfo> GetHdrStatus(bool forceRefresh = false)
        {
            if (_cache == null || forceRefresh || DateTime.Now - _lastFetch > _cacheDuration)
            {
                _cache = FetchHdrStatus();
                _lastFetch = DateTime.Now;
            }

            return _cache;
        }

        public static HdrMonitorInfo GetMonitorStatus(string deviceName, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(deviceName))
                return null;

            var map = GetHdrStatus(forceRefresh);
            return map.TryGetValue(deviceName, out var info) ? info : null;
        }

        public static bool GetMonitorHdrStatus(string deviceName, bool forceRefresh = false)
        {
            var monitorInfo = GetMonitorStatus(deviceName, forceRefresh);
            if (monitorInfo == null)
            {
                Debug.WriteLine($"Monitor '{deviceName}' not found in HDR report.");
                return false;
            }
            return monitorInfo.HdrEnabled;
        }

        private static Dictionary<string, HdrMonitorInfo> FetchHdrStatus()
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HdrChecker\\HdrChecker.exe");

            if (!File.Exists(exePath))
            {
                string message = $"CRITICAL ERROR: Required HDR helper executable not found.\n\n" +
                                 $"Expected at: {exePath}\n\n" +
                                 $"Make sure 'HdrChecker.exe' is deployed correctly in the 'HdrChecker' subfolder.";
                Debug.WriteLine(message);
                throw new FileNotFoundException(message);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    var monitorList = JsonConvert.DeserializeObject<List<HdrMonitorInfo>>(output);

                    return monitorList?
                        .Where(m => !string.IsNullOrEmpty(m.DeviceName))
                        .ToDictionary(m => m.DeviceName, StringComparer.OrdinalIgnoreCase)
                        ?? new Dictionary<string, HdrMonitorInfo>();
                }
            }
            catch (Exception ex)
            {
                string message = $"CRITICAL ERROR: Failed to execute or parse HDR monitor info.\n\n" +
                                 $"Executable: {exePath}\n" +
                                 $"Exception: {ex.Message}";
                Debug.WriteLine(message);
                throw new ApplicationException(message, ex);
            }
        }
    }
}
