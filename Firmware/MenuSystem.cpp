#include "MenuSystem.h"
#include "DeviceStorage.h"
#include "Logging.h"
#include "Icons.h"

#include <Adafruit_SSD1306.h>

extern Adafruit_SSD1306 display;

void MenuSystem::begin() {
    activeMenuIndex = 0;
    for (int i = 0; i < DEVICE_TYPE_COUNT; ++i) {
        selectedDeviceIndex[i] = 0;
    }
    needsRedraw = true;

    /*
    // --- Mock Data ---
    Device vol1 = { "Realtek Audio", 0, 45, Volume };
    Device vol2 = { "USB DAC",       1, 60, Volume };
    deviceMatrix[Volume][0] = vol1;
    deviceMatrix[Volume][1] = vol2;
    deviceCountPerType[Volume] = 2;
    Device br1 = { "Monitor 1", 0, 80, Brightness };
    Device br2 = { "Monitor 2", 1, 65, Brightness };

    deviceMatrix[Brightness][0] = br1;
    deviceMatrix[Brightness][1] = br2;
    deviceCountPerType[Brightness] = 2;
    */
}

void MenuSystem::nextMenu() {
    activeMenuIndex = (activeMenuIndex + 1) % DEVICE_TYPE_COUNT;
    requestRedraw();
}

void MenuSystem::nextDevice() {
    uint8_t& index = selectedDeviceIndex[activeMenuIndex];
    index = (index + 1) % deviceCountPerType[activeMenuIndex];
    requestRedraw();
}

void MenuSystem::adjustValue(int delta) {
    Device& dev = *currentDevice();
    uint8_t oldValue = dev.value;
    dev.value = constrain(dev.value + delta, 0, 100);
    if (dev.value != oldValue) requestRedraw();
}

Device* MenuSystem::currentDevice() {
    return &deviceMatrix[activeMenuIndex][selectedDeviceIndex[activeMenuIndex]];
}

void MenuSystem::requestRedraw() {
    needsRedraw = true;
}

bool MenuSystem::checkRedraw() {
    if (needsRedraw) {
        needsRedraw = false;
        return true;
    }
    return false;
}

void MenuSystem::update() {
    if (checkRedraw()) {
        displayCurrent();
    }
}

void MenuSystem::displayCurrent() {
    display.clearDisplay();
    Device* dev = currentDevice();

    // Device name
    display.setTextSize(1);
    display.setCursor(0, 0);
    display.println(dev->name);

    // Device index
    display.setCursor(0, 12);
    display.printf("%d/%d", selectedDeviceIndex[activeMenuIndex] + 1,
                   deviceCountPerType[activeMenuIndex]);

    // Value
    display.setTextSize(3);
    int16_t x, y;
    uint16_t w, h;
    char valStr[5];
    snprintf(valStr, sizeof(valStr), "%3d", dev->value);
    display.getTextBounds(valStr, 0, 0, &x, &y, &w, &h);
    display.setCursor(128 - w - 2, 64 - h - 2);
    display.println(valStr);

    // Menu icon
    const uint8_t* icon = nullptr;
    switch (activeMenuIndex) {
        case Volume:
            icon = (dev->value == 0) ? speakerMutedIcon32px : speakerIcon32px;
            break;
        case Brightness:
            icon = (dev->value == 0) ? sunOffIcon32px : sunIcon32px;
            break;
        default:
            break;
    }
    if (icon != nullptr) {
        display.drawBitmap(0, 32, icon, 32, 32, SSD1306_WHITE);
    }

    display.display();
}


