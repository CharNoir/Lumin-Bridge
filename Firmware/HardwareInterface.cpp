#include "HardwareInterface.h"
#include "MenuSystem.h"
#include "Protocol.h"
#include "DeviceStorage.h"
#include <Adafruit_SSD1306.h>

#define ENCODER_PIN_A 2
#define ENCODER_PIN_B 3
#define ENCODER_BTN   4
#define MODE_BTN      5

extern Adafruit_SSD1306 display;
RotaryEncoder encoder(ENCODER_PIN_A, ENCODER_PIN_B, RotaryEncoder::LatchMode::TWO03);

bool handshakeCompleted = false;

HardwareInterface::HardwareInterface(MenuSystem* ms) : menuSystem(ms) {}

void HardwareInterface::begin() {
    pinMode(ENCODER_BTN, INPUT_PULLUP);
    pinMode(MODE_BTN, INPUT_PULLUP);
    encoder.setPosition(0);
    display.begin(SSD1306_SWITCHCAPVCC, 0x3C);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);

    Serial.begin(115200);
    delay(100);
    Serial.println("LUMIN_READY");
}

void HardwareInterface::update() {
    handleEncoder();
    handleEncoderButton();
    handleModeButton();
    readSerial();
}

void HardwareInterface::handleModeButton() {
    static bool lastState = HIGH;
    bool current = digitalRead(MODE_BTN);
    if (lastState == HIGH && current == LOW) {
        menuSystem->nextMenu();
    }
    lastState = current;
}

void HardwareInterface::handleEncoderButton() {
    static bool lastState = HIGH;
    bool current = digitalRead(ENCODER_BTN);
    if (!current && lastState) {
        encoderHeld = true;
    } else if (current && encoderHeld) {
        encoderHeld = false;
    }
    lastEncoderBtnState = current;
}

void HardwareInterface::handleEncoder() {
    encoder.tick();
    long newPos = encoder.getPosition();
    if (newPos != lastPos) {
        int delta = (int)(newPos - lastPos);
        lastPos = newPos;

        if (encoderHeld) {
            menuSystem->nextDevice();
        } else {
            menuSystem->adjustValue(delta);
            sendUpdate();
        }
    }
}

void HardwareInterface::sendUpdate(uint8_t id, uint8_t value, DeviceType type) {
    ValueReportPacket p = { ValueReport, id, value, type };
    Serial.write(0xAA);
    Serial.write(sizeof(p));
    Serial.write((uint8_t*)&p, sizeof(p));
}

void HardwareInterface::sendUpdate() {
    uint8_t id = selectedDeviceIndex[activeMenuIndex];
    Device& dev = deviceMatrix[activeMenuIndex][id];
    sendUpdate(dev.id, dev.value, dev.deviceType);
}

void HardwareInterface::readSerial() {
    static String inputBuffer;
    static uint8_t buffer[256];
    static uint8_t index = 0;
    static uint8_t expectedLength = 0;
    static bool receiving = false;

    while (Serial.available()) {
        char ch = Serial.read();

        if (isPrintable(ch)) {
            inputBuffer += ch;
            if (inputBuffer.endsWith("HELLO_LUMIN")) {
                Serial.println("LUMIN_ACK");
                handshakeCompleted = true;
                inputBuffer = "";
            } else if (inputBuffer.length() > 64) {
                inputBuffer = "";
            }
            continue;
        }

        if (!handshakeCompleted)
            return;

        if (!receiving) {
            if ((uint8_t)ch == 0xAA) {
                index = 0;
                receiving = true;
                continue;
            }
        } else if (index == 0) {
            expectedLength = (uint8_t)ch;
            if (expectedLength > sizeof(buffer)) {
                receiving = false;
                continue;
            }
        } else {
            buffer[index - 1] = (uint8_t)ch;
        }

        index++;

        if (index == expectedLength + 2) {
            receiving = false;

            PacketType type = (PacketType)buffer[0];
            if (type == FullSync) {
                FullSyncPacket* p = (FullSyncPacket*)buffer;

                for (int i = 0; i < DEVICE_TYPE_COUNT; ++i) {
                    deviceCountPerType[i] = 0;
                }

                for (int i = 0; i < p->count; ++i) {
                    Device& d = p->devices[i];
                    uint8_t typeIndex = (uint8_t)d.deviceType;
                    uint8_t slot = deviceCountPerType[typeIndex];
                    if (slot < MAX_DEVICE_PER_MENU) {
                        deviceMatrix[typeIndex][slot] = d;
                        deviceCountPerType[typeIndex]++;
                    }
                }

                for (int i = 0; i < DEVICE_TYPE_COUNT; ++i) {
                    selectedDeviceIndex[i] = 0;
                }
                activeMenuIndex = 0;
            } else if (type == DeltaUpdate) {
                DeltaUpdatePacket* p = (DeltaUpdatePacket*)buffer;
                Device& d = p->device;
                uint8_t typeIndex = (uint8_t)d.deviceType;
                for (uint8_t i = 0; i < deviceCountPerType[typeIndex]; ++i) {
                    if (deviceMatrix[typeIndex][i].id == d.id) {
                        deviceMatrix[typeIndex][i] = d;
                        break;
                    }
                }
            }
        }
    }
}
