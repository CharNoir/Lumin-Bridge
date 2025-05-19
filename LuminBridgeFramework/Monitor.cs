using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using LuminBridgeFramework.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static LuminBridgeFramework.Program;

namespace LuminBridgeFramework
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Monitor
    {
        // ───────────────────────────────────────────────────────────────
        // 🔷 Fields
        // ───────────────────────────────────────────────────────────────
        private int _maxBrightnessNits; // in nits
        private int _maxBrightness = 100;
        private int _minBrightness = 0;
        [JsonProperty("brightness")]
        private int _brightness;
        [JsonProperty("sdrToHdrWhiteLevel")]
        private int _sdrToHdrWhiteLevel;
        private bool _isHdrEnabled = false;
        private static string ConfigDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");

        // ───────────────────────────────────────────────────────────────
        // 🔷 Properties
        // ───────────────────────────────────────────────────────────────
        public IntPtr hmonitor { get; set; }
        [JsonProperty("name")]
        public string Name { get; private set; }
        [JsonProperty("friendlyName")]
        public string FriendlyName { get; set; }
        public IntPtr IconHwnd { get; set; }
        public int IconId { get; set; }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Constructors
        // ───────────────────────────────────────────────────────────────

        public Monitor() { }

        /// <summary>
        /// Initializes a new instance of the Monitor class with a specified monitor name.
        /// </summary>
        /// <param name="monitorName">The monitor's friendly name.</param>
        private Monitor(string monitorName)
        {
            Name = monitorName;
            FriendlyName = ParseUserFriendlyName(GetHwdName(Name));

            Console.WriteLine($"HDR Enabled : {_isHdrEnabled}");
            Console.WriteLine($"FriendlyName: {FriendlyName}");
        }

        public static Monitor Load(string monitorName)
        {
            string path = Path.Combine(ConfigDirectory, $"{MakeSafeFileName(monitorName)}.json");
            if (!File.Exists(path))
            {
                // No config? Create new instance
                return new Monitor(monitorName);
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableAttribute = true
                },
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var monitor = JsonConvert.DeserializeObject<Monitor>(File.ReadAllText(path), settings);
            monitor.UpdateMonitorInfo(); // Optional: refresh runtime values
            return monitor;
        }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Public Methods
        // ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Adjusts monitor brightness by a given value. Chooses HDR or SDR method based on monitor status.
        /// </summary>
        /// <param name="desiredBrightness">Desired brightness percentage change (0–100).</param>
        public void AdjustBrightness(int brightnessChange)
        {
            UpdateMonitorInfo();

            int desiredBrightness = GetBrightness() + brightnessChange;
            Console.WriteLine($"Adjusting brightness for {Name}: {desiredBrightness}%");
            SetBrightness(desiredBrightness);
        }

        /// <summary>
        /// Sets monitor brightness. Chooses HDR or SDR method based on monitor status.
        /// </summary>
        /// <param name="desiredBrightness">Desired brightness percentage (0–100).</param>
        public void SetBrightness(int desiredBrightness)
        {
            desiredBrightness = Clamp(desiredBrightness, _minBrightness, _maxBrightness);

            //UpdateMonitorInfo();

            if (_isHdrEnabled)
                AdjustHdrBrightness(desiredBrightness);
            else
                AdjustSdrBrightness(desiredBrightness);

            SaveConfig();
        }

        /// <summary>
        /// Updates monitor information such as HDR status.
        /// </summary>
        public void UpdateMonitorInfo()
        {
            _isHdrEnabled = HdrHelper.GetMonitorHdrStatus(Name);
        }

        public Device ToProtocolDevice()
        {
            return new Device
            {
                name = FriendlyName.Length > 32 ? FriendlyName.Substring(0, 32) : FriendlyName,
                id = (byte)IconId,
                value = (byte)MathHelper.Clamp(GetBrightness(), 0, 255),
                deviceType = DeviceType.Brightness
            };
        }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Private Methods
        // ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Gets the current brightness level depending on HDR status.
        /// </summary>
        /// <returns>Brightness value (percent or HDR white level).</returns>
        private int GetBrightness()
        {
            return _isHdrEnabled ? _sdrToHdrWhiteLevel : _brightness;
        }

        /// <summary>
        /// Adjusts brightness using SDR-compatible APIs (Dxva2).
        /// </summary>
        /// <param name="desiredBrightness">Brightness percentage (0–100).</param>
        private void AdjustSdrBrightness(int desiredBrightness)
        {
            _brightness = desiredBrightness;
            if (GetPhysicalMonitorsFromHMONITOR(hmonitor, 1, out var monitor))
            {
                SetMonitorBrightness(monitor.hPhysicalMonitor, (uint)desiredBrightness);
                DestroyPhysicalMonitor(monitor.hPhysicalMonitor);
            }
        }

        /// <summary>
        /// Adjusts brightness using HDR-specific white level manipulation.
        /// </summary>
        /// <param name="desiredBrightness">Brightness percentage (0–100).</param>
        private void AdjustHdrBrightness(int desiredBrightness)
        {
            _sdrToHdrWhiteLevel = desiredBrightness;
            if (IconHwnd == IntPtr.Zero)
            {
                Console.WriteLine("Error: Monitor handle (hWnd) is not set.");
                return;
            }

            var hmodule = LoadLibrary("dwmapi.dll");
            var changeBrightness = Marshal.GetDelegateForFunctionPointer<DwmpSDRToHDRBoostPtr>(
                GetProcAddress(hmodule, 171)
            );

            changeBrightness(hmonitor, PercentToMagic(desiredBrightness));
        }

        /// <summary>
        /// Clamps a value between a given minimum and maximum.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="min">Minimum limit.</param>
        /// <param name="max">Maximum limit.</param>
        /// <returns>Clamped value.</returns>
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Converts brightness percentage to HDR "magic number" white level.
        /// </summary>
        /// <param name="percent">Brightness percentage (0–100).</param>
        /// <returns>Converted HDR white level.</returns>
        private double PercentToMagic(int percent)
        {
            // Placeholder for more complex conversion logic
            return percent / 100.0 * 6.0;
        }

        /// <summary>
        /// Retrieves a physical monitor structure from a HMONITOR handle.
        /// </summary>
        /// <param name="hMonitor">Monitor handle.</param>
        /// <param name="count">Number of monitors to retrieve.</param>
        /// <param name="monitor">Out: retrieved monitor struct.</param>
        /// <returns>True if successful.</returns>
        private static bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint count, out PHYSICAL_MONITOR monitor)
        {
            var array = new PHYSICAL_MONITOR[1];
            bool success = GetPhysicalMonitorsFromHMONITOR(hMonitor, count, array);
            monitor = array[0];
            return success;
        }

        private static string GetHwdName(string deviceName)
        {
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);

            bool success = EnumDisplayDevices(deviceName, 0, ref dd, 0);
            if (!success || string.IsNullOrEmpty(dd.DeviceID)) return "";

            return ExtractHardwareId(dd.DeviceID);
        }

        private static string ExtractHardwareId(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return null;

            string[] parts = deviceId.Split('\\');

            return parts.Length >= 2 ? parts[1] : null;
        }

        private static string ParseUserFriendlyName(string szMatch)
        {
            try
            {
                var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorID");

                foreach (var obj in searcher.Get())
                {
                    var instanceName = obj["InstanceName"];
                    var userFriendlyName = ToAscii((ushort[])obj["UserFriendlyName"]);
                    if (szMatch.Equals(ExtractHardwareId(instanceName?.ToString()))) return userFriendlyName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while querying WmiMonitorID:");
                Console.WriteLine(ex);
            }
            return "";
        }

        private static string ToAscii(ushort[] data)
        {
            if (data == null) return "(null)";
            return new string(Array.ConvertAll(data, x => (char)x)).TrimEnd('\0');
        }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Config Persistence
        // ───────────────────────────────────────────────────────────────

        public void SaveConfig()
        {
            if (!Directory.Exists(ConfigDirectory))
                Directory.CreateDirectory(ConfigDirectory);

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableAttribute = true // Only serialize members with [JsonProperty]
                },
                MissingMemberHandling = MissingMemberHandling.Ignore // Enforce opt-in
            };

            File.WriteAllText(GetConfigPath(), JsonConvert.SerializeObject(this, settings));
        }

        public void LoadConfig()
        {
            string path = GetConfigPath();
            if (!File.Exists(path)) return;

            var loaded = JsonConvert.DeserializeObject<Monitor>(File.ReadAllText(path));
            if (loaded?.Name == Name)
            {
                _brightness = loaded._brightness;
                _sdrToHdrWhiteLevel = loaded._sdrToHdrWhiteLevel;
            }
        }

        private string GetConfigPath()
        {
            string safeFileName = MakeSafeFileName(Name);
            return Path.Combine(ConfigDirectory, $"{safeFileName}.json");
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Win32 Interop
        // ───────────────────────────────────────────────────────────────

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, int address);

        private delegate void DwmpSDRToHDRBoostPtr(IntPtr monitor, double brightness);

        [DllImport("Dxva2.dll", SetLastError = true)]
        private static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

        [DllImport("Dxva2.dll", SetLastError = true)]
        private static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

        [DllImport("Dxva2.dll", SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(
            IntPtr hMonitor,
            uint count,
            [Out] PHYSICAL_MONITOR[] monitors
        );

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public int StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

    }
}
