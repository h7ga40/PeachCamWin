
#ifndef _DIGITALOUT_H_
#define _DIGITALOUT_H_

#include "PinNames.h"

namespace mbed {

class DigitalOut
{
public:
	DigitalOut(PinName pin);
	void write(int value);
	int read();

	DigitalOut &operator= (int value){
		write(value);
		return *this;
	}

	operator int() {
		return read();
	}
private:
	gpio_t gpio;
};

}

#endif // _DIGITALOUT_H_
