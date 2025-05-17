#include "MenuSystem.h"
#include "HardwareInterface.h"

MenuSystem menuSystem;
HardwareInterface hw(&menuSystem);

void setup() {
  Serial.begin(115200);
  hw.begin();
  menuSystem.begin();
}

void loop() {
  hw.update();
  menuSystem.update();
}
