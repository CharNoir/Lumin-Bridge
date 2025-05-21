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
            RegisterForComNotifications();
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

            if (_deviceNotificationHandle != IntPtr.Zero)
            {
                UnregisterDeviceNotification(_deviceNotificationHandle);
                _deviceNotificationHandle = IntPtr.Zero;
            }

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



        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        private static readonly Guid GUID_DEVINTERFACE_COMPORT =
            new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73");

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            public short dbcc_name;
        }

        private IntPtr _deviceNotificationHandle = IntPtr.Zero;

        private void RegisterForComNotifications()
        {
            var dbi = new DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                dbcc_reserved = 0,
                dbcc_classguid = GUID_DEVINTERFACE_COMPORT
            };

            IntPtr buffer = Marshal.AllocHGlobal(dbi.dbcc_size);
            Marshal.StructureToPtr(dbi, buffer, false);

            _deviceNotificationHandle = RegisterDeviceNotification(this.Handle, buffer, DEVICE_NOTIFY_WINDOW_HANDLE);

            Marshal.FreeHGlobal(buffer);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE)
            {
                int wParam = m.WParam.ToInt32();
                if (wParam == DBT_DEVICEARRIVAL)
                {
                    Console.WriteLine("[COM] Device connected.");
                    Thread.Sleep(2000);
                    serialController.ConnectAndSync(ListDevices());
                }
                else if (wParam == DBT_DEVICEREMOVECOMPLETE)
                {
                    Console.WriteLine("[COM] Device disconnected.");
                }
            }

            base.WndProc(ref m);
        }

    }
}
