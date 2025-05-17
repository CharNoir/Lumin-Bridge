#pragma once
#include <Arduino.h>
#include <vector>

struct Device {
    String name;
    int value; // 0–100
};

class Menu {
public:
    Menu(String title, std::initializer_list<Device> devices);

    void nextDevice();
    void adjustValue(int delta);
    String currentDeviceName() const;
    int currentDeviceValue() const;
    int deviceCount() const;

    String title;
    std::vector<Device> devices;
    int selectedDevice = 0;
};

class MenuSystem {
public:
    void begin();
    void nextMenu();
    Menu& current();
    void update();
    void displayCurrent();

private:
    std::vector<Menu> menus;
    int activeMenu = 0;
};
