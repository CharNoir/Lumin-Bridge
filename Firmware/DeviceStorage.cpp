#include "DeviceStorage.h"

Device deviceMatrix[DEVICE_TYPE_COUNT][MAX_DEVICE_PER_MENU] = {};
uint8_t deviceCountPerType[DEVICE_TYPE_COUNT] = {};
uint8_t selectedDeviceIndex[DEVICE_TYPE_COUNT] = {};
uint8_t activeMenuIndex = 0;