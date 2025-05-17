#include "HardwareInterface.h"
#include "MenuSystem.h"
#include <Adafruit_SSD1306.h>

#define ENCODER_PIN_A 2
#define ENCODER_PIN_B 3
#define ENCODER_BTN   4

extern Adafruit_SSD1306 display;
RotaryEncoder encoder(ENCODER_PIN_A, ENCODER_PIN_B, RotaryEncoder::LatchMode::TWO03);

HardwareInterface::HardwareInterface(MenuSystem* ms) : menuSystem(ms) {}

void HardwareInterface::begin() {
    pinMode(ENCODER_BTN, INPUT_PULLUP);
    encoder.setPosition(0);
    display.begin(SSD1306_SWITCHCAPVCC, 0x3C);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
}

void HardwareInterface::update() {
    encoder.tick();
    long newPos = encoder.getPosition();
    if (newPos != lastPos) {
        int delta = (int)(newPos - lastPos);
        lastPos = newPos;
        if (btnPressed) {
            menuSystem->current().nextDevice();
        } else {
            menuSystem->current().adjustValue(delta);
            sendUpdateToPC();
        }
    }

    handleButton();
}

void HardwareInterface::handleButton() {
    bool current = digitalRead(ENCODER_BTN) == LOW;
    if (current && !btnPressed) {
        btnPressTime = millis();
        btnPressed = true;
    } else if (!current && btnPressed) {
        if (millis() - btnPressTime < 500) {
            menuSystem->nextMenu();
        }
        btnPressed = false;
    }
}

void HardwareInterface::sendUpdateToPC() {
    auto& m = menuSystem->current();
    Serial.printf("%s -> %s = %d\n", m.title.c_str(), m.currentDeviceName().c_str(), m.currentDeviceValue());
}
