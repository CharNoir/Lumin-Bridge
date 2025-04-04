using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using static LuminBridgeFramework.MainForm;
using static LuminBridgeFramework.MonitorManager;
using static LuminBridgeFramework.Program;

namespace LuminBridgeFramework
{
    public class MonitorManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

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
            MonitorBrightnessControl.SetBrightnessAllMonitors(100);

            MONITORINFOEX monitorInfo = new MONITORINFOEX();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));

            // Get monitor info
            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                string deviceName = new string(monitorInfo.szDevice);
                Console.WriteLine($"Monitor Device: {deviceName.TrimEnd('\0')}");

                DEVMODE devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

                if (EnumDisplaySettings(deviceName, 0, ref devMode))
                {
                    Console.WriteLine($"Resolution: {devMode.dmPelsWidth} x {devMode.dmPelsHeight}");
                    Console.WriteLine($"Color Depth: {devMode.dmBitsPerPel} bits per pixel");
                    Console.WriteLine($"Refresh Rate: {devMode.dmDisplayFlags}");
                }
                else
                {
                    Console.WriteLine("Failed to get display settings.");
                }
            }
            else
            {
                Console.WriteLine("Failed to get monitor information.");
            }

            var monitor = new Monitor($"Monitor {hMonitor}");

            monitor.hmonitor = hMonitor;

            // Store the monitor in the list
            Monitors.Add(monitor);

            Console.WriteLine("Monitor Details:");
            Console.WriteLine($"Monitor Handle: {hMonitor}");

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
