using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LuminBridgeFramework
{
    /// <summary>
    /// Watches for USB COM port device connection and disconnection events on Windows.
    /// Intended for use with forms to detect serial device (COM port) plug/unplug.
    /// </summary>
    public class ComPortWatcher : NativeWindow, IDisposable
    {
        public const int WM_DEVICECHANGE = 0x0219;
        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

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

        public ComPortWatcher(Form host)
        {
            AssignHandle(host.Handle);
            RegisterForComNotifications();
        }

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

        public void Dispose()
        {
            if (_deviceNotificationHandle != IntPtr.Zero)
            {
                UnregisterDeviceNotification(_deviceNotificationHandle);
                _deviceNotificationHandle = IntPtr.Zero;
            }

            ReleaseHandle();
        }
    }
}
