#include "HardwareInterface.h"
#include "DeviceStorage.h"
#include "Logging.h"

#include <Wire.h>
#include <Adafruit_SSD1306.h>

extern Adafruit_SSD1306 display;
Button modeBtn = Button(MODE_BTN);
MenuSystem* HardwareInterface::staticMenuSystem = nullptr;
bool handshakeCompleted = false;

unsigned long lastFastTurnTime = 0;
uint8_t fastTurnCount = 0;
uint8_t prevValues[DEVICE_TYPE_COUNT][MAX_DEVICE_PER_MENU];

HardwareInterface::HardwareInterface(MenuSystem* ms)
    : menuSystem(ms),
      enc(ENCODER_PIN_A, ENCODER_PIN_B, ENCODER_BTN) {}

void HardwareInterface::begin() {
    staticMenuSystem = menuSystem;
    pinMode(MODE_BTN, INPUT_PULLUP);
    pinMode(ENCODER_BTN, INPUT_PULLUP);

    Wire.begin(OLED_SDA, OLED_SCL);
    if (!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
        LOG_ERROR(F("SSD1306 allocation failed"));
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
    if (enc.turnH()) {
        if (millis() - lastFastTurnTime < ENCODER_PRESSED_TIMEOUT) {
            fastTurnCount++;
        } else {
            fastTurnCount = 1;
        }

        lastFastTurnTime = millis();

        if (fastTurnCount >= ENCODER_PRESSED_TURNS) { 
            menuSystem->nextDevice();
            fastTurnCount = 0;
        }
    }
    else if (enc.turn()) {
        int delta = enc.dir() > 0 ? ENCODER_VALUE_DELTA : -ENCODER_VALUE_DELTA;
        if (menuSystem->adjustValue(delta)) 
          sendUpdate();
    }
    else if (enc.click()) {  // short press on encoder
        Device* dev = menuSystem->currentDevice();
        uint8_t type = (uint8_t)dev->deviceType;
        uint8_t index = selectedDeviceIndex[type];

        if (dev->value != 0) {
            prevValues[type][index] = dev->value;
            dev->value = 0;
        } else {
            dev->value = prevValues[type][index];
            prevValues[type][index] = 0;
        }

        menuSystem->requestRedraw();
        sendUpdate(dev->id, dev->value, dev->deviceType);
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
    static uint8_t buffer[1024];
    static uint8_t expectedLength = 0;
    static uint8_t index = 0;
    static String inputBuffer;

    while (Serial.available()) {
        uint8_t ch = Serial.read();

        if (isPrintable(ch)) {
            inputBuffer += (char)ch;
            if (inputBuffer.endsWith("HELLO_LUMIN")) {
                LOG_INFO("[SERIAL] Received HELLO_LUMIN");
                Serial.println("LUMIN_ACK"); // Send acknoledgement
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
                    LOG_INFO(">> Start of new packet detected (0xAA)");
                    state = WAIT_FOR_LENGTH;
                }
                break;

            case WAIT_FOR_LENGTH:
                expectedLength = ch;
                index = 0;
                LOG_INFO(">> Expected packet length: " + expectedLength);
                if (expectedLength > sizeof(buffer)) {
                    LOG_ERROR("!! Packet too large. Resetting.");
                    state = WAIT_FOR_HEADER;
                    break;
                }
                state = WAIT_FOR_PAYLOAD;
                break;

            case WAIT_FOR_PAYLOAD:
                buffer[index++] = ch;
                if (index == expectedLength) {
                    state = WAIT_FOR_HEADER;
                    handlePacket(buffer, expectedLength);
                }
                break;
        }
    }
}

void HardwareInterface::handlePacket(uint8_t* buffer, uint8_t length) {
    PacketType type = (PacketType)buffer[0];

    switch (type) {
        case FullSync:
            handleFullSync((FullSyncPacket*)buffer);
            break;

        case DeltaUpdate:
            handleDeltaUpdate((DeltaUpdatePacket*)buffer);
            break;

        case ResetDeviceMatrix:
            handleResetDeviceMatrix();
            break;

        default:
            LOG_ERROR("!! Unknown packet type: " + String(type));
            break;
    }
}

void HardwareInterface::handleFullSync(FullSyncPacket* p) {
    LOG_INFO(">> Handling FullSync packet");

    for (int i = 0; i < DEVICE_TYPE_COUNT; ++i)
        deviceCountPerType[i] = 0;

    for (int i = 0; i < p->count; ++i) {
        Device& d = p->devices[i];
        uint8_t typeIndex = (uint8_t)d.deviceType;

        if (typeIndex >= DEVICE_TYPE_COUNT) {
            LOG_ERROR("Skipping device " + String(i));
            continue;
        }

        uint8_t slot = deviceCountPerType[typeIndex];
        if (slot < MAX_DEVICE_PER_MENU) {
            deviceMatrix[typeIndex][slot] = d;
            deviceCountPerType[typeIndex]++;
        }
    }

    for (int i = 0; i < DEVICE_TYPE_COUNT; ++i)
        selectedDeviceIndex[i] = 0;

    activeMenuIndex = 0;
    LOG_INFO(">> FullSync complete.");
}

void HardwareInterface::handleDeltaUpdate(DeltaUpdatePacket* p) {
    LOG_INFO(">> Handling DeltaUpdate packet");

    Device& d = p->device;
    uint8_t typeIndex = (uint8_t)d.deviceType;
    bool found = false;

    LOG_INFO(">> Device name: " + String(d.name));
    LOG_INFO(">> Device id: " + String(d.id));
    LOG_INFO(">> Device value: " + String(d.value));
    LOG_INFO(">> Device type: " + String(d.deviceType));

    for (uint8_t i = 0; i < deviceCountPerType[typeIndex]; ++i) {
        if (deviceMatrix[typeIndex][i].id == d.id) {
            deviceMatrix[typeIndex][i] = d;
            LOG_INFO(">> Updated device in matrix.");
            found = true;
            break;
        }
    }

    if (!found && deviceCountPerType[typeIndex] < MAX_DEVICE_PER_MENU) {
        deviceMatrix[typeIndex][deviceCountPerType[typeIndex]++] = d;
        LOG_INFO(">> Inserted new device into matrix.");
    }

    if (staticMenuSystem) staticMenuSystem->requestRedraw();
}

void HardwareInterface::handleResetDeviceMatrix() {
    LOG_INFO(">> Handling ResetDeviceMatrix packet");

    for (int i = 0; i < DEVICE_TYPE_COUNT; ++i) {
        deviceCountPerType[i] = 0;
        selectedDeviceIndex[i] = 0;

        for (int j = 0; j < MAX_DEVICE_PER_MENU; ++j) {
            memset(&deviceMatrix[i][j], 0, sizeof(Device));
        }
    }

    activeMenuIndex = 0;
    memset(prevValues, 0, sizeof(prevValues));
    LOG_INFO(">> Device matrix reset.");
}