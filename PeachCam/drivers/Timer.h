
#ifndef _TIMER_H_
#define _TIMER_H_

#include "PinNames.h"

namespace mbed {

class Timer
{
public:
	Timer();
	void start();
	void stop();
	void reset();
	int read_ms();
};

}

#endif // _TIMER_H_
