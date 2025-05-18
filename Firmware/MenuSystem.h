#pragma once

#include <Arduino.h>
#include "Protocol.h"

class MenuSystem {
public:
    void begin();
    void nextMenu();
    void nextDevice();
    void adjustValue(int delta);
    void update();
    void displayCurrent();

private:
    Device* currentDevice();
};
