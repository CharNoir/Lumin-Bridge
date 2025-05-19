using System.Runtime.InteropServices;

namespace LuminBridgeFramework.Protocol
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Device
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
        public byte id;
        public byte value;
        public DeviceType deviceType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FullSyncPacket
    {
        public PacketType packetType;
        public byte count;
        public Device[] devices;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ValueReportPacket
    {
        public PacketType packetType;
        public byte id;
        public byte value;
        public DeviceType deviceType;
    }
}