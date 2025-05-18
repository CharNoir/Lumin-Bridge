#include "MenuSystem.h"
#include "HardwareInterface.h"
#include <Adafruit_SSD1306.h>

#define OLED_WIDTH 128
#define OLED_HEIGHT 64

MenuSystem menuSystem;
HardwareInterface hw(&menuSystem);
Adafruit_SSD1306 display(OLED_WIDTH, OLED_HEIGHT, &Wire, -1);

void setup() {
  Serial.begin(115200);
  hw.begin();
  menuSystem.begin();
}

void loop() {
  hw.update();
  menuSystem.update();
}
