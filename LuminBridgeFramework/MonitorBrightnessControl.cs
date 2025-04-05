using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LuminBridgeFramework
{
    public class MonitorBrightnessControl
    {
        const uint MC_CAPS_BRIGHTNESS = 0x2;

        [DllImport("Dxva2.dll", SetLastError = true)]
        static extern bool GetMonitorCapabilities(IntPtr hMonitor, out uint pdwMonitorCapabilities, out uint pdwSupportedColorTemperatures);

        [DllImport("Dxva2.dll", SetLastError = true)]
        static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

        [DllImport("Dxva2.dll", SetLastError = true)]
        static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

        [DllImport("Dxva2.dll", SetLastError = true)]
        static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [DllImport("Dxva2.dll", SetLastError = true)]
        static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("Dxva2.dll", SetLastError = true)]
        static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        public static void SetBrightnessAllMonitors(uint brightness)
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                new MonitorEnumDelegate((IntPtr hMonitor, IntPtr hdcMonitor, ref MonitorBrightnessControl.RECT rect, IntPtr data) =>
                {
                    if (MonitorBrightnessControl.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, out var monitor))
                    {
                        MonitorBrightnessControl.SetMonitorBrightness(monitor.hPhysicalMonitor, brightness); 
                        MonitorBrightnessControl.DestroyPhysicalMonitor(monitor.hPhysicalMonitor);
                    }
                    return true;
                }), IntPtr.Zero);
        }

        private static bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint count, out PHYSICAL_MONITOR monitor)
        {
            var array = new PHYSICAL_MONITOR[1];
            bool success = GetPhysicalMonitorsFromHMONITOR(hMonitor, count, array);
            monitor = array[0];
            return success;
        }
    }
}
