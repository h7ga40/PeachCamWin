
#ifndef _DIGITALIN_H_
#define _DIGITALIN_H_

#include "PinNames.h"

namespace mbed {

class DigitalIn
{
public:
	DigitalIn(PinName pin);
	int read();

	operator int() {
		return read();
	}
private:
	gpio_t gpio;
};

}

#endif // _DIGITALIN_H_
