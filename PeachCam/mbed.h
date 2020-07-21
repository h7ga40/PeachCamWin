// mbed.h : 標準のシステム インクルード ファイルのインクルード ファイル、
// または、参照回数が多く、かつあまり変更されない、プロジェクト専用のインクルード ファイル
// を記述します。
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Windows ヘッダーからほとんど使用されていない部分を除外する
// Windows ヘッダー ファイル
#include <windows.h>
#include <dshow.h>

#define __attribute(x)
typedef unsigned int mode_t;

int mkdir(const char *name, mode_t mode);

// プログラムに必要な追加ヘッダーをここで参照してください
#include <climits>
#include <cfloat>
#include <vector>
#include <array>
#include <limits>
#include <cstdint>
#include <cstddef>
#include <cstdio>
#include <ctime>
#include <cstring>
#include <stdbool.h>
#include <WinSock2.h>
#include <ws2tcpip.h>

#ifndef _WIN64
#import "..\\ITestBench\\bin\\Debug\\ITestBench32.tlb" no_namespace
#else
#import "..\\ITestBench\\bin\\Debug\\ITestBench64.tlb" no_namespace
#endif

__declspec(dllimport) extern ITestBench *TestBench;

#define TouchKey_LCD_shield TouchKey_4_3inch

int set_time(time_t tm);

void mbed_die(void);
typedef struct {
	LARGE_INTEGER frequency;
} ticker_data_t;
typedef uint64_t us_timestamp_t;
const ticker_data_t *get_us_ticker_data(void);
us_timestamp_t ticker_read_us(const ticker_data_t *const ticker);

#define printf testbench_printf
int printf(const char *format, ...);

#include "mbed_config.h"

#include "platform/mbed_toolchain.h"
#include "platform/CriticalSectionLock.h"
#include "platform/PlatformMutex.h"
#include "platform/SingletonPtr.h"

#include "drivers/PinNames.h"
#include "drivers/DigitalOut.h"
#include "drivers/DigitalIn.h"
#include "drivers/InterruptIn.h"
#include "drivers/AnalogIn.h"
#include "drivers/I2C.h"
#include "drivers/SPI.h"
#include "drivers/Timeout.h"
#include "drivers/Timer.h"
#include "drivers/Ticker.h"

#include "rtos/cmsis_os.h"
#include "rtos/ThisThread.h"
#include "rtos/Thread.h"
#include "rtos/EventFlags.h"
#include "rtos/Mutex.h"
#include "rtos/Semaphore.h"

#include "events/mbed_events.h"

#include "netsocket/nsapi.h"

using namespace mbed;
using namespace rtos;
