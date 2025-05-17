#include "MenuSystem.h"
#include <Adafruit_SSD1306.h>

extern Adafruit_SSD1306 display;

Menu::Menu(String t, std::initializer_list<Device> d) : title(t), devices(d) {}

void Menu::nextDevice() {
    selectedDevice = (selectedDevice + 1) % devices.size();
}

void Menu::adjustValue(int delta) {
    Device& dev = devices[selectedDevice];
    dev.value = constrain(dev.value + delta, 0, 100);
}

String Menu::currentDeviceName() const {
    return devices[selectedDevice].name;
}

int Menu::currentDeviceValue() const {
    return devices[selectedDevice].value;
}

int Menu::deviceCount() const {
    return devices.size();
}

void MenuSystem::begin() {
    menus.push_back(Menu("Volume", {
        { "Realtek Audio", 50 },
        { "CX31993 DAC", 40 }
    }));
    menus.push_back(Menu("Brightness", {
        { "Monitor 1", 80 },
        { "Monitor 2", 70 }
    }));
}

void MenuSystem::nextMenu() {
    activeMenu = (activeMenu + 1) % menus.size();
}

Menu& MenuSystem::current() {
    return menus[activeMenu];
}

void MenuSystem::update() {
    displayCurrent();
}

void MenuSystem::displayCurrent() {
    display.clearDisplay();
    Menu& m = current();

    display.setCursor(0, 0);
    display.printf("Menu: %s\n", m.title.c_str());

    display.setCursor(0, 16);
    display.printf("Device %d/%d:", m.selectedDevice + 1, m.deviceCount());

    display.setCursor(0, 32);
    display.println(m.currentDeviceName());

    display.setCursor(0, 48);
    display.printf("Value: %d %%", m.currentDeviceValue());

    display.display();
}
