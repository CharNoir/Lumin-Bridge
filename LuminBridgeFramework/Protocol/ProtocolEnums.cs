﻿namespace LuminBridgeFramework.Protocol
{
    public enum PacketType : byte
    {
        ResetDeviceMatrix = 0x00,
        FullSync = 0x01,
        DeltaUpdate = 0x02,
        ValueReport = 0x10
    }
    public enum DeviceType : byte
    {
        Volume = 0,
        Brightness = 1
    }
}