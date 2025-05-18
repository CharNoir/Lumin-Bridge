#pragma once
#include "Protocol.h"

class MenuSystem {
public:
    void begin();
    void nextMenu();
    void nextDevice();
    void adjustValue(int delta);
    Device* currentDevice();
    void update();

    void requestRedraw();
    bool checkRedraw();

private:
    void displayCurrent();

    bool needsRedraw = true;
};
