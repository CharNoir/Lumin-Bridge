#pragma once

#include <stdint.h>

#define MAX_DEVICE_PER_MENU 8
#define DEVICE_TYPE_COUNT 2

enum PacketType : uint8_t {
    ResetDeviceMatrix = 0x00,
    FullSync    = 0x01,
    DeltaUpdate = 0x02,
    ValueReport = 0x10
};

enum DeviceType : uint8_t {
    Volume     = 0,
    Brightness = 1
};

#pragma pack(push, 1)

struct Device {
    char name[32];
    uint8_t id;
    uint8_t value;
    DeviceType deviceType;
};

struct FullSyncPacket {
    PacketType packetType;
    uint8_t count;
    Device devices[MAX_DEVICE_PER_MENU * DEVICE_TYPE_COUNT];
};

struct DeltaUpdatePacket {
    PacketType packetType;
    Device device;
};

struct ValueReportPacket {
    PacketType packetType;
    uint8_t id;
    uint8_t value;
    DeviceType deviceType;
};

#pragma pack(pop)