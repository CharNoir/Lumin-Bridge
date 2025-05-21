using Gma.System.MouseKeyHook;
using LuminBridgeFramework.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    public partial class MainForm : Form
    {
        private List<NotifyIcon> trayIcons = new List<NotifyIcon>();
        private MonitorController monitorController;
        private SoundOutputController soundOutputController;
        private List<IDeviceController> deviceControllers;

        private SerialController serialController;

        private SettingsForm settingsForm;
        private IKeyboardMouseEvents _hook;

        private ComPortWatcher _comWatcher;
        public MainForm()
        {
            InitializeComponent();
            monitorController = new MonitorController();
            soundOutputController = new SoundOutputController();

            deviceControllers = new List<IDeviceController>();
            deviceControllers.Add(monitorController);
            deviceControllers.Add(soundOutputController);

            serialController = new SerialController();
            serialController.OnValueReportReceived += HandleValueReport;
            serialController.ConnectAndSync(ListDevices());
            CreateTrayIcons();
            //RegisterForComNotifications();

            _comWatcher = new ComPortWatcher(this);
        }

        private void CreateTrayIcons()
        {
            _hook = Hook.GlobalEvents();
            _hook.MouseWheel += Global_MouseWheel;

            foreach (var monitor in monitorController.Monitors)
            {
                var trayIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Information,
                    Text = $"{monitor.FriendlyName}",
                    Visible = true
                };

                trayIcon.MouseClick += (sender, args) =>
                {
                    if (args.Button == MouseButtons.Left)
                    {
                        ShowSettings();
                        //MessageBox.Show($"Settings for {monitor.Name}");
                    }
                };

                // Set the ID and HWND for the tray icon
                SetTrayIconDetails(trayIcon, monitor);

                trayIcons.Add(trayIcon);
            }
        }
        private void ShowSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm();
                settingsForm.LoadSettings(ListDevices(), serialController);
                //settingsForm.LoadSettings(monitorManager.Monitors, audioManager.Devices);
            }

            settingsForm.Show();
            settingsForm.BringToFront();
        }

        private List<BaseDevice> ListDevices()
        {
            return deviceControllers.SelectMany(dc => dc.GetDevices()).ToList();
        }

        private void Global_MouseWheel(object sender, MouseEventArgs e)
        {
            foreach (var monitor in monitorController.Monitors)
            {
                var iconRect = GetTrayIconRect(monitor.IconHwnd, (uint)monitor.IconId);
                var pt = Cursor.Position;

                bool inside = iconRect.Contains(pt);
                //Debug.WriteLine($"Scroll detected at {pt}, icon rect: ({iconRect.left},{iconRect.top} → {iconRect.right},{iconRect.bottom}), inside? {inside}");

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
            monitor.AdjustBrightness(brightnessChange);
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

            monitor.IconId = id - 1;
            monitor.IconHwnd = hwnd;

            Debug.WriteLine($"Monitor {monitor.Name} - Icon ID = {id}");
            Debug.WriteLine($"Monitor {monitor.hmonitor} ");
        }

        public void HandleValueReport(ValueReportPacket packet)
        {
            foreach (IDeviceController controller in deviceControllers)
            {
                controller.TryApplyValue(packet);
            }
            monitorController.TryApplyValue(packet);
        }

        // Dispose the tray icons properly when the form closes
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var trayIcon in trayIcons)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            
            _comWatcher.Dispose();
            serialController.Dispose();

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

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ComPortWatcher.WM_DEVICECHANGE)
            {
                int wParam = m.WParam.ToInt32();
                if (wParam == ComPortWatcher.DBT_DEVICEARRIVAL)
                {
                    Console.WriteLine("[COM] Device connected.");
                    Thread.Sleep(2000);
                    serialController.ConnectAndSync(ListDevices());
                }
                else if (wParam == ComPortWatcher.DBT_DEVICEREMOVECOMPLETE)
                {
                    Console.WriteLine("[COM] Device disconnected.");
                }
            }

            base.WndProc(ref m);
        }

    }
}
