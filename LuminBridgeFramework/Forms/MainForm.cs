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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    public partial class MainForm : Form
    {
        private List<NotifyIcon> _trayIcons = new List<NotifyIcon>();
        private MonitorController _monitorController;
        private SoundOutputController _soundOutputController;
        private List<IDeviceController> _deviceControllers;

        private SerialController _serialController;

        private SettingsForm _settingsForm;
        private IKeyboardMouseEvents _hook;

        private ComPortWatcher _comWatcher;
        public MainForm()
        {
            InitializeComponent();
            _monitorController = new MonitorController();
            _soundOutputController = new SoundOutputController();
            _soundOutputController.VolumeChanged += OnSoundDeviceVolumeChanged;

            _deviceControllers = new List<IDeviceController>();
            _deviceControllers.Add(_monitorController);
            _deviceControllers.Add(_soundOutputController);

            _serialController = new SerialController();
            _serialController.OnValueReportReceived += HandleValueReport;
            CreateTrayIcons();

            _comWatcher = new ComPortWatcher(this);

            AsyncConnectAndSync(100);
        }

        private void OnSoundDeviceVolumeChanged(SoundOutputDevice device)
        {
            _serialController.SendDeltaUpdatePacket(device);
        }

        private void CreateTrayIcons()
        {
            _hook = Hook.GlobalEvents();
            _hook.MouseWheel += Global_MouseWheel;

            foreach (var monitor in _monitorController.Monitors)
            {
                var trayIcon = new NotifyIcon
                {
                    
                    Text = $"Monitor {monitor.FriendlyName}",
                    Visible = true
                };

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
                contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
                trayIcon.ContextMenuStrip = contextMenu;

                trayIcon.MouseClick += (sender, args) =>
                {
                    if (args.Button == MouseButtons.Left)
                    {
                        ShowSettings();
                    }
                };

                // Set the ID and HWND for the tray icon
                SetTrayIconDetails(trayIcon, monitor);
                trayIcon.Icon = CreateNumberIcon(monitor.IconId);

                _trayIcons.Add(trayIcon);
            }
        }

        private Icon CreateNumberIcon(int number)
        {
            const int size = 24;
            using (var bmp = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Font font = new Font("Arial", 22, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    string text = number.ToString();
                    SizeF textSize = g.MeasureString(text, font);
                    float x = (size - textSize.Width) / 2;
                    float y = (size - textSize.Height) / 2;
                    g.DrawString(text, font, brush, x, y);
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }


        private void ShowSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm();
                _settingsForm.LoadSettings(ListDevices(), _serialController);
            }

            _settingsForm.Show();
            _settingsForm.BringToFront();
        }

        private List<BaseDevice> ListDevices()
        {
            return _deviceControllers.SelectMany(dc => dc.GetDevices()).ToList();
        }

        private void Global_MouseWheel(object sender, MouseEventArgs e)
        {
            foreach (var monitor in _monitorController.Monitors)
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

            monitor.IconId = id;
            monitor.IconHwnd = hwnd;

            Debug.WriteLine($"Monitor {monitor.FriendlyName} - Icon ID = {id}");
            Debug.WriteLine($"Monitor {monitor.HMonitor} ");
        }

        public void HandleValueReport(ValueReportPacket packet)
        {
            foreach (IDeviceController controller in _deviceControllers)
            {
                controller.TryApplyValue(packet);
            }
            _monitorController.TryApplyValue(packet);
        }

        private void AsyncConnectAndSync(int delay = 0)
        {
            Task.Run(() =>
            {
                if (delay > 0) 
                    Thread.Sleep(delay);
                _serialController.ConnectAndSync(ListDevices());
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var trayIcon in _trayIcons)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            
            _comWatcher.Dispose();
            _serialController.Dispose();

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
                    AsyncConnectAndSync(2000);
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
