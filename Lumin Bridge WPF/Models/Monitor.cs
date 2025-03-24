using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumin_Bridge_WPF
{
    public class Monitor
    {
        // External imports
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, int address);
        private delegate void DwmpSDRToHDRBoostPtr(IntPtr monitor, double brightness);

        // Properties
        private nint thisMonitor;
        private string name;

        private int _maxBrightnessNits; // in nits

        // In percent
        private int _maxBrightness;
        private int _minBrightness;
        private int _brightness;
        private int _sdrToHdrWhiteLevel;

        public int getBrightness()
        {
            if (_isHdrEnabled)
                return _sdrToHdrWhiteLevel;
            else
                return _brightness;
        }

        private bool _isHdrEnabled;

        public Monitor() {
            STDM();
        }
        public void STDM()
        {
            thisMonitor = MonitorFromWindow(IntPtr.Zero, 1);
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
            desiredBrightness = Math.Clamp(desiredBrightness, _minBrightness, _maxBrightness);

            UpdateMonitorInfo();
            if (_isHdrEnabled)
                AdjustHdrBrightness(desiredBrightness);
            else
                AdjustSdrBrightness(desiredBrightness);
        }

        private void AdjustSdrBrightness(int desiredBrightness)
        {

        }

        private void AdjustHdrBrightness(int desiredBrightness)
        {
            var hmodule_dwmapi = LoadLibrary("dwmapi.dll");
            DwmpSDRToHDRBoostPtr changeBrightness = Marshal.GetDelegateForFunctionPointer<DwmpSDRToHDRBoostPtr>(GetProcAddress(hmodule_dwmapi, 171));

            _sdrToHdrWhiteLevel = desiredBrightness;
            changeBrightness(thisMonitor, percentToMagic(desiredBrightness));
        }

        // TODO
        private double percentToMagic(int percent) {
            // TODO create more complex conversion
            return percent / 100.0 * 6.0;
        }
        private int magicToPercent(double magic)
        {
            return (int)Math.Round(magic * 100.0 / 6.0);
        }
    }
}
