#pragma once
#include "Protocol.h"

extern Device deviceMatrix[DEVICE_TYPE_COUNT][MAX_DEVICE_PER_MENU];
extern uint8_t deviceCountPerType[DEVICE_TYPE_COUNT];
extern uint8_t selectedDeviceIndex[DEVICE_TYPE_COUNT];
extern uint8_t activeMenuIndex;