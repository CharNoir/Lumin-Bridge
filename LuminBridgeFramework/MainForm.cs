using Gma.System.MouseKeyHook;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;

        private IKeyboardMouseEvents _hook;
        private IntPtr _iconHwnd;
        private int _iconId;

        public MainForm()
        {
            InitializeComponent();
            InitializeTray();
        }

        private void InitializeTray()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "Lumin Tray",
                Visible = true
            };

            trayIcon.MouseClick += TrayIcon_MouseClick;

            TestTrayIconLocation(trayIcon);

            _hook = Hook.GlobalEvents();
            _hook.MouseWheel += Global_MouseWheel;
        }

        private void Global_MouseWheel(object sender, MouseEventArgs e)
        {
            var iconRect = GetTrayIconRect(_iconHwnd, (uint)_iconId);
            var pt = Cursor.Position;

            bool inside = iconRect.Contains(pt);
            Debug.WriteLine($"Scroll detected at {pt}, icon rect: ({iconRect.left},{iconRect.top} → {iconRect.right},{iconRect.bottom}), inside? {inside}");

            if (inside)
            {
                Debug.WriteLine("Scroll is over tray icon");
                // TODO
            }
        }


        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                MessageBox.Show("Tray icon clicked!");
        }

        private void TestTrayIconLocation(NotifyIcon icon)
        {
            var idField = typeof(NotifyIcon).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
            var windowField = typeof(NotifyIcon).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance);

            if (idField == null || windowField == null)
            {
                Debug.WriteLine("Cannot access NotifyIcon internals.");
                return;
            }

            int id = (int)idField.GetValue(icon);
            NativeWindow nativeWindow = (NativeWindow)windowField.GetValue(icon);
            IntPtr hwnd = nativeWindow?.Handle ?? IntPtr.Zero;

            Debug.WriteLine($"Icon ID = {id}");
            Debug.WriteLine($"HWND = 0x{hwnd.ToInt64():X}");

            var rect = GetTrayIconRect(hwnd, (uint)id);
            Debug.WriteLine($"Icon Rect: {rect.left},{rect.top} → {rect.right},{rect.bottom}");

            _iconId = id;
            _iconHwnd = hwnd;
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

        public RECT GetTrayIconRect(IntPtr hwnd, uint id)
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
