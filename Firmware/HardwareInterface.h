#pragma once
#include <RotaryEncoder.h>
#include "Protocol.h"

class MenuSystem;

class HardwareInterface {
public:
    HardwareInterface(MenuSystem* ms);
    void begin();
    void update();

private:
    void handleButton();
    void handleEncoder();
    void sendUpdate(uint8_t id, uint8_t value, DeviceType type);
    void sendUpdate();
    void readSerial();

    MenuSystem* menuSystem;
    long lastPos = 0;
    unsigned long btnPressTime = 0;
    bool btnPressed = false;
};
