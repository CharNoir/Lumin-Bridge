#pragma once
#include "Config.h"

#if defined(ENABLE_LOGGING) && LOG_LEVEL >= 1
  #define LOG_ERROR(msg) Serial.println(String("[ERROR] ") + msg)
#else
  #define LOG_ERROR(msg)
#endif

#if defined(ENABLE_LOGGING) && LOG_LEVEL >= 2
  #define LOG_INFO(msg) Serial.println(String("[INFO] ") + msg)
#else
  #define LOG_INFO(msg)
#endif

#if defined(ENABLE_LOGGING) && LOG_LEVEL >= 3
  #define LOG_VERBOSE(msg) Serial.println(String("[VERBOSE] ") + msg)
#else
  #define LOG_VERBOSE(msg)
#endif
