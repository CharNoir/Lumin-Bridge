using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LuminBridgeFramework.Protocol
{
    public class ProtocolHelper
    {
        public static FullSyncPacket CreateFullSyncPacket(List<Monitor> monitors)
        {
            var devices = monitors
                .Select(m => m.ToProtocolDevice())
                .Take(ProtocolConstants.MAX_DEVICE_PER_MENU * ProtocolConstants.DEVICE_TYPE_COUNT)
                .ToArray();

            var paddedDevices = new Device[ProtocolConstants.MAX_DEVICE_PER_MENU * ProtocolConstants.DEVICE_TYPE_COUNT];
            devices.CopyTo(paddedDevices, 0);

            return new FullSyncPacket
            {
                packetType = PacketType.FullSync,
                count = (byte)devices.Length,
                devices = paddedDevices
            };
        }

        public static byte[] SerializeFullSyncPacket(FullSyncPacket packet)
        {
            var deviceSize = Marshal.SizeOf<Device>();
            int totalSize = 1 + 1 + (packet.count * deviceSize);
            byte[] buffer = new byte[totalSize];

            buffer[0] = (byte)packet.packetType;
            buffer[1] = packet.count;

            for (int i = 0; i < packet.count; i++)
            {
                byte[] deviceBytes = StructureToBytes(packet.devices[i]);
                Buffer.BlockCopy(deviceBytes, 0, buffer, 2 + i * deviceSize, deviceSize);
            }

            return buffer;
        }

        public static byte[] StructureToBytes<T>(T str) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        public static T BytesToStructure<T>(byte[] data) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
