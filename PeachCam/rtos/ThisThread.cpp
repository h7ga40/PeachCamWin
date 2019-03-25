#include "mbed.h"
#include "ThisThread.h"

namespace rtos {

namespace ThisThread {
	void sleep_for(uint32_t millisec)
	{
		Sleep(millisec);
	}
}

}
