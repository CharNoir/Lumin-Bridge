#pragma once
#include <RotaryEncoder.h>

class MenuSystem;

class HardwareInterface {
public:
    HardwareInterface(MenuSystem* ms);
    void begin();
    void update();

private:
    void handleButton();
    void sendUpdateToPC();

    MenuSystem* menuSystem;
    long lastPos = 0;
    unsigned long btnPressTime = 0;
    bool btnPressed = false;
};
