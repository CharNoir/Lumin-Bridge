using System;
using System.Runtime.InteropServices;

namespace LuminBridgeFramework
{
    public class Monitor
    {
        // ───────────────────────────────────────────────────────────────
        // 🔷 Fields
        // ───────────────────────────────────────────────────────────────
        private int _maxBrightnessNits; // in nits
        private int _maxBrightness = 100;
        private int _minBrightness = 0;
        private int _brightness;
        private int _sdrToHdrWhiteLevel;
        private bool _isHdrEnabled = false;

        // ───────────────────────────────────────────────────────────────
        // 🔷 Properties
        // ───────────────────────────────────────────────────────────────
        public IntPtr hmonitor { get; set; }
        public string MonitorName { get; private set; }
        public IntPtr IconHwnd { get; set; }
        public int IconId { get; set; }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Constructor
        // ───────────────────────────────────────────────────────────────
        /// <summary>
        /// Initializes a new instance of the Monitor class with a specified monitor name.
        /// </summary>
        /// <param name="monitorName">The monitor's friendly name.</param>
        public Monitor(string monitorName)
        {
            MonitorName = monitorName;
            UpdateMonitorInfo();
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
            Console.WriteLine($"Adjusting brightness for {MonitorName}: {desiredBrightness}%");
            SetBrightness(desiredBrightness);
        }

        /// <summary>
        /// Sets monitor brightness. Chooses HDR or SDR method based on monitor status.
        /// </summary>
        /// <param name="desiredBrightness">Desired brightness percentage (0–100).</param>
        public void SetBrightness(int desiredBrightness)
        {
            desiredBrightness = Clamp(desiredBrightness, _minBrightness, _maxBrightness);
            _brightness = desiredBrightness;

            //UpdateMonitorInfo();

            if (_isHdrEnabled)
                AdjustHdrBrightness(desiredBrightness);
            else
                AdjustSdrBrightness(desiredBrightness);
        }

        /// <summary>
        /// Updates monitor information such as HDR status.
        /// </summary>
        public void UpdateMonitorInfo()
        {
            _isHdrEnabled = HdrHelper.GetMonitorHdrStatus(MonitorName);
            Console.WriteLine($"HDR Enabled: {_isHdrEnabled}");
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
            if (IconHwnd == IntPtr.Zero)
            {
                Console.WriteLine("Error: Monitor handle (hWnd) is not set.");
                return;
            }

            var hmodule = LoadLibrary("dwmapi.dll");
            var changeBrightness = Marshal.GetDelegateForFunctionPointer<DwmpSDRToHDRBoostPtr>(
                GetProcAddress(hmodule, 171)
            );

            _sdrToHdrWhiteLevel = desiredBrightness;
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }
    }
}
