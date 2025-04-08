using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static LuminBridgeFramework.MainForm;

namespace LuminBridgeFramework
{
    public class MonitorManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprcClip, IntPtr dwData);

        public List<Monitor> Monitors { get; private set; }

        public MonitorManager()
        {
            Monitors = new List<Monitor>();
            EnumAllMonitors();
        }

        private void EnumAllMonitors()
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
        }

        private bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdc, ref RECT lprcClip, IntPtr dwData)
        {
            MONITORINFOEX monitorInfo = new MONITORINFOEX();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));

            string deviceName = "";
            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                deviceName = new string(monitorInfo.szDevice);
                Console.WriteLine($"\nMonitor Device: {deviceName.TrimEnd('\0')}");
                Console.WriteLine($"Monitor Handle: {hMonitor}");
            }
            else
            {
                Console.WriteLine("Failed to get monitor information.");
            }

            var monitor = new Monitor(deviceName.TrimEnd('\0'));

            monitor.hmonitor = hMonitor;

            // Store the monitor in the list
            Monitors.Add(monitor);

            return true; 
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmNup;
            public uint dmBpp;
        }
    }
}
