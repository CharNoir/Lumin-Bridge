#include "MenuSystem.h"
#include "DeviceStorage.h"
#include <Adafruit_SSD1306.h>

extern Adafruit_SSD1306 display;

void MenuSystem::begin() {
    // already initialized via FullSyncPacket
    activeMenuIndex = 0;
    for (int i = 0; i < DEVICE_TYPE_COUNT; ++i) {
        selectedDeviceIndex[i] = 0;
    }
}

void MenuSystem::nextMenu() {
    activeMenuIndex = (activeMenuIndex + 1) % DEVICE_TYPE_COUNT;
}

void MenuSystem::nextDevice() {
    uint8_t& index = selectedDeviceIndex[activeMenuIndex];
    index = (index + 1) % deviceCountPerType[activeMenuIndex];
}

void MenuSystem::adjustValue(int delta) {
    Device& dev = *currentDevice();
    dev.value = constrain(dev.value + delta, 0, 100);
}

Device* MenuSystem::currentDevice() {
    return &deviceMatrix[activeMenuIndex][selectedDeviceIndex[activeMenuIndex]];
}

void MenuSystem::update() {
    displayCurrent();
}

void MenuSystem::displayCurrent() {
    display.clearDisplay();
    Device* dev = currentDevice();

    display.setCursor(0, 0);
    display.printf("Menu: %s\n", activeMenuIndex == Volume ? "Volume" : "Brightness");

    display.setCursor(0, 16);
    display.printf("Device %d/%d:", selectedDeviceIndex[activeMenuIndex] + 1, deviceCountPerType[activeMenuIndex]);

    display.setCursor(0, 32);
    display.println(dev->name);

    display.setCursor(0, 48);
    display.printf("Value: %d %%", dev->value);

    display.display();
}
