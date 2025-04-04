using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LuminBridgeFramework
{
    public class Monitor
    {
        // External imports
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, int address);

        private delegate void DwmpSDRToHDRBoostPtr(IntPtr monitor, double brightness);

        // Properties
        public IntPtr hmonitor { get; set; }
        public string MonitorName { get; private set; }

        private int _maxBrightnessNits; // in nits

        // In percent
        private int _maxBrightness;
        private int _minBrightness;
        private int _brightness;
        private int _sdrToHdrWhiteLevel;

        public IntPtr IconHwnd { get; set; } 
        public int IconId { get; set; }

        public int getBrightness()
        {
            if (_isHdrEnabled)
                return _sdrToHdrWhiteLevel;
            else
                return _brightness;
        }

        private bool _isHdrEnabled = false;

        public Monitor()
        {
            STDM();
        }

        public Monitor(string monitorId)
        {
            MonitorName = monitorId;
            STDM();
        }

        public void STDM()
        {
            _isHdrEnabled = true;

            _minBrightness = 0;
            _maxBrightness = 100;
        }

        public void UpdateMonitorInfo()
        {

            // Update:
            // _maxBrightnessNits
            // _brightness
            // _sdrToHdrWhiteLevel
            Console.WriteLine(_isHdrEnabled);
        }

        public void AdjustBrightness(int desiredBrightness)
        {
            desiredBrightness = Clamp(desiredBrightness, _minBrightness, _maxBrightness);
            _brightness = desiredBrightness;
            UpdateMonitorInfo();
            if (_isHdrEnabled)
                AdjustHdrBrightness(desiredBrightness);
            else
                AdjustSdrBrightness(desiredBrightness);

            //MonitorBrightnessControl.SetBrightnessAllMonitors((uint)desiredBrightness);
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void AdjustSdrBrightness(int desiredBrightness)
        {

        }

        private void AdjustHdrBrightness(int desiredBrightness)
        {
            if (IconHwnd == IntPtr.Zero)
            {
                Console.WriteLine("Error: Monitor handle (hWnd) is not set.");
                return;
            }

            var hmodule_dwmapi = LoadLibrary("dwmapi.dll");
            DwmpSDRToHDRBoostPtr changeBrightness = Marshal.GetDelegateForFunctionPointer<DwmpSDRToHDRBoostPtr>(GetProcAddress(hmodule_dwmapi, 171));

            _sdrToHdrWhiteLevel = desiredBrightness;
            changeBrightness(hmonitor, percentToMagic(desiredBrightness));
        }

        public int GetBrightness()
        {
            return _brightness;
        }

        // TODO
        private double percentToMagic(int percent)
        {
            // TODO create more complex conversion
            return percent / 100.0 * 6.0;
        }
        private int magicToPercent(double magic)
        {
            return (int)Math.Round(magic * 100.0 / 6.0);
        }
    }
}
