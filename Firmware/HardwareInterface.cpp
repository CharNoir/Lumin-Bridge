#include "HardwareInterface.h"
#include "DeviceStorage.h"
#include <Wire.h>
#include <Adafruit_SSD1306.h>

extern Adafruit_SSD1306 display;
Button modeBtn = Button(MODE_BTN);
MenuSystem* HardwareInterface::staticMenuSystem = nullptr;
bool handshakeCompleted = false;

HardwareInterface::HardwareInterface(MenuSystem* ms)
    : menuSystem(ms),
      enc(ENCODER_PIN_A, ENCODER_PIN_B, ENCODER_BTN) {}

void HardwareInterface::begin() {
    staticMenuSystem = menuSystem;
    pinMode(MODE_BTN, INPUT_PULLUP);
    pinMode(ENCODER_BTN, INPUT_PULLUP);

    Wire.begin(OLED_SDA, OLED_SCL);
    if (!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
        Serial.println(F("SSD1306 allocation failed"));
        while (true);
    }

    display.clearDisplay();
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.setCursor(28, 28);
    display.println("LUMIN BRIDGE");
    display.display();

    delay(2000);

    enc.setEncType(EB_STEP4_LOW);
    enc.setBtnLevel(LOW);
    modeBtn.setBtnLevel(LOW);

    modeBtn.attach(onModeBtnEvent);
}

void HardwareInterface::update() {
    enc.tick(); 
    modeBtn.tick();

    handleEncoder();
    readSerial();
}

void HardwareInterface::handleEncoder() {
    if (enc.turnH()){
        menuSystem->nextDevice();
    }
    else if (enc.turn()) {
        int delta = enc.dir() > 0 ? ENCODER_VALUE_DELTA : -ENCODER_VALUE_DELTA;
        menuSystem->adjustValue(delta);
        sendUpdate();
    }
}

void HardwareInterface::onModeBtnEvent() {
    switch (modeBtn.action()) {
        /*
        case EB_CLICK:
            Serial.println("MODE button clicked");
            if (staticMenuSystem) staticMenuSystem->nextMenu();
            break;
        */
        case EB_RELEASE:
            if (staticMenuSystem) staticMenuSystem->nextMenu();
            break;
        default:
            break;
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
    static enum { WAIT_FOR_HEADER, WAIT_FOR_LENGTH, WAIT_FOR_PAYLOAD } state = WAIT_FOR_HEADER;
    static uint8_t buffer[256];
    static uint8_t expectedLength = 0;
    static uint8_t index = 0;
    static String inputBuffer;

    while (Serial.available()) {
        uint8_t ch = Serial.read();
        Serial.printf("[SERIAL] Raw byte: 0x%02X\n", ch);

        if (isPrintable(ch)) {
            inputBuffer += (char)ch;
            if (inputBuffer.endsWith("HELLO_LUMIN")) {
                Serial.println("[SERIAL] Received HELLO_LUMIN");
                Serial.println("<< Sending LUMIN_ACK");
                handshakeCompleted = true;
                inputBuffer = "";
            } else if (inputBuffer.length() > 64) {
                inputBuffer = "";
            }
        }

        if (!handshakeCompleted) continue;

        switch (state) {
            case WAIT_FOR_HEADER:
                if (ch == 0xAA) {
                    Serial.println(">> Start of new packet detected (0xAA)");
                    state = WAIT_FOR_LENGTH;
                }
                break;

            case WAIT_FOR_LENGTH:
                expectedLength = ch;
                index = 0;
                Serial.printf(">> Expected packet length: %d\n", expectedLength);
                if (expectedLength > sizeof(buffer)) {
                    Serial.println("!! Packet too large. Resetting.");
                    state = WAIT_FOR_HEADER;
                    break;
                }
                state = WAIT_FOR_PAYLOAD;
                break;

            case WAIT_FOR_PAYLOAD:
                buffer[index++] = ch;
                if (index == expectedLength) {
                    state = WAIT_FOR_HEADER;

                    PacketType type = (PacketType)buffer[0];
                    Serial.printf(">> PacketType received: 0x%02X\n", type);

                    if (type == FullSync) {
                        Serial.println(">> Handling FullSync packet");
                        FullSyncPacket* p = (FullSyncPacket*)buffer;
                        for (int i = 0; i < DEVICE_TYPE_COUNT; ++i)
                            deviceCountPerType[i] = 0;
                        for (int i = 0; i < p->count; ++i) {
                            Device& d = p->devices[i];
                            uint8_t typeIndex = (uint8_t)d.deviceType;
                            uint8_t slot = deviceCountPerType[typeIndex];
                            if (slot < MAX_DEVICE_PER_MENU) {
                                deviceMatrix[typeIndex][slot] = d;
                                deviceCountPerType[typeIndex]++;
                                Serial.printf(">> Added device: name=%s id=%d value=%d type=%d\n",
                                              d.name, d.id, d.value, d.deviceType);
                            }
                        }
                        for (int i = 0; i < DEVICE_TYPE_COUNT; ++i)
                            selectedDeviceIndex[i] = 0;
                        activeMenuIndex = 0;
                        Serial.println(">> FullSync complete.");
                    } else if (type == DeltaUpdate) {
                        Serial.println(">> Handling DeltaUpdate packet");
                        DeltaUpdatePacket* p = (DeltaUpdatePacket*)buffer;
                        Device& d = p->device;
                        Serial.printf(">> Device name: %s\n", d.name);
                        Serial.printf(">> Device id: %d\n", d.id);
                        Serial.printf(">> Device value: %d\n", d.value);
                        Serial.printf(">> Device type: %d\n", d.deviceType);
                        uint8_t typeIndex = (uint8_t)d.deviceType;
                        for (uint8_t i = 0; i < deviceCountPerType[typeIndex]; ++i) {
                            if (deviceMatrix[typeIndex][i].id == d.id) {
                                deviceMatrix[typeIndex][i] = d;
                                Serial.println(">> Updated device in matrix.");
                                break;
                            }
                        }
                    } else {
                        Serial.println("!! Unknown packet type");
                    }

                    Serial.println("--------------------------------------------------");
                }
                break;
        }
    }
}





