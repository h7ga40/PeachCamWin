
#ifndef _INTERRUPTIN_H_
#define _INTERRUPTIN_H_

#include "PinNames.h"
#include "platform/Callback.h"

namespace mbed {

class InterruptIn
{
public:
	InterruptIn(PinName pin);
	void rise(Callback<void()> func);
	void fall(Callback<void()> func);
private:
	PinName pin;
};

}

#endif // _INTERRUPTIN_H_
