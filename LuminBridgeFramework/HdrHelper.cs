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
            var map = GetHdrStatus(forceRefresh);
            return map.TryGetValue(deviceName, out var info) ? info : null;
        }

        public static bool GetMonitorHdrStatus(string deviceName, bool forceRefresh = false)
        {
            var monitorInfo = GetMonitorStatus(deviceName, forceRefresh);
            return monitorInfo.HdrEnabled;
        }

        private static Dictionary<string, HdrMonitorInfo> FetchHdrStatus()
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HdrChecker.exe");

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
                Debug.WriteLine("Failed to get HDR info: " + ex.Message);
                return new Dictionary<string, HdrMonitorInfo>();
            }
        }
    }
}
