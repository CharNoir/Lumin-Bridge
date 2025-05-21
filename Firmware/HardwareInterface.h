#pragma once

#include <Arduino.h>
#include <EncButton.h>
#include "Protocol.h"
#include "MenuSystem.h"

#define ENCODER_PIN_A 11
#define ENCODER_PIN_B 12
#define ENCODER_BTN   10
#define MODE_BTN      8
#define OLED_SDA      21
#define OLED_SCL      34

#define ENCODER_PRESSED_TURNS 2
#define ENCODER_PRESSED_TIMEOUT 300
#define ENCODER_VALUE_DELTA 5

extern Button modeBtn;

class HardwareInterface {
public:
    HardwareInterface(MenuSystem* ms);
    void begin();
    void update();
    void sendUpdate();
    void sendUpdate(uint8_t id, uint8_t value, DeviceType type);
    void readSerial();

private:
    void handleEncoder();
    static void onModeBtnEvent();

    void handlePacket(uint8_t* buffer, uint8_t length);
    void handleFullSync(FullSyncPacket* p);
    void handleDeltaUpdate(DeltaUpdatePacket* p);
    void handleResetDeviceMatrix();

    static MenuSystem* staticMenuSystem;
    MenuSystem* menuSystem;

    EncButton enc = EncButton(ENCODER_PIN_A, ENCODER_PIN_B, ENCODER_BTN);
};
