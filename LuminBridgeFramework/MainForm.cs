using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    public partial class MainForm : Form
    {
        private List<NotifyIcon> trayIcons = new List<NotifyIcon>();
        private MonitorManager monitorManager;
        private IKeyboardMouseEvents _hook;

        public MainForm()
        {
            InitializeComponent();
            monitorManager = new MonitorManager(); // Get all monitors
            CreateTrayIcons();
        }

        private void CreateTrayIcons()
        {
            _hook = Hook.GlobalEvents();
            _hook.MouseWheel += Global_MouseWheel;

            foreach (var monitor in monitorManager.Monitors)
            {
                var trayIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Information,
                    Text = $"Monitor {monitor.MonitorName}",
                    Visible = true
                };

                trayIcon.MouseClick += (sender, args) =>
                {
                    if (args.Button == MouseButtons.Left)
                    {
                        MessageBox.Show($"Settings for {monitor.MonitorName}");
                    }
                };

                // Set the ID and HWND for the tray icon
                SetTrayIconDetails(trayIcon, monitor);

                trayIcons.Add(trayIcon);
            }
        }

        private void Global_MouseWheel(object sender, MouseEventArgs e)
        {
            foreach (var monitor in monitorManager.Monitors)
            {
                var iconRect = GetTrayIconRect(monitor.IconHwnd, (uint)monitor.IconId);
                var pt = Cursor.Position;

                bool inside = iconRect.Contains(pt);
                Debug.WriteLine($"Scroll detected at {pt}, icon rect: ({iconRect.left},{iconRect.top} → {iconRect.right},{iconRect.bottom}), inside? {inside}");

                if (inside)
                {
                    // Scroll detected over this tray icon
                    AdjustMonitorBrightness(monitor, e.Delta);
                }
            }
        }

        private void AdjustMonitorBrightness(Monitor monitor, int delta)
        {
            int brightnessChange = delta > 0 ? 5 : -5;
            monitor.AdjustBrightness(monitor.GetBrightness() + brightnessChange);
            Console.WriteLine($"Adjusting brightness for {monitor.MonitorName}: {monitor.GetBrightness() + brightnessChange}%");
        }

        private void SetTrayIconDetails(NotifyIcon trayIcon, Monitor monitor)
        {
            var idField = typeof(NotifyIcon).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
            var windowField = typeof(NotifyIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);

            if (idField == null || windowField == null)
            {
                Debug.WriteLine("Cannot access NotifyIcon internals.");
                return;
            }

            int id = (int)idField.GetValue(trayIcon);
            NativeWindow nativeWindow = (NativeWindow)windowField.GetValue(trayIcon);
            IntPtr hwnd = nativeWindow?.Handle ?? IntPtr.Zero;

            monitor.IconId = id;
            monitor.IconHwnd = hwnd;

            Debug.WriteLine($"Monitor {monitor.MonitorName} - Icon ID = {id}");
            Debug.WriteLine($"Monitor {monitor.hmonitor} ");
        }

        // Dispose the tray icons properly when the form closes
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var trayIcon in trayIcons)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            base.OnFormClosing(e);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public bool Contains(Point pt)
            {
                return pt.X >= left && pt.X <= right &&
                       pt.Y >= top && pt.Y <= bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NOTIFYICONIDENTIFIER
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public Guid guidItem;
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

        private RECT GetTrayIconRect(IntPtr hwnd, uint id)
        {
            var nid = new NOTIFYICONIDENTIFIER
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
                hWnd = hwnd,
                uID = id,
                guidItem = Guid.Empty
            };

            Shell_NotifyIconGetRect(ref nid, out RECT rect);
            return rect;
        }
    }
}
