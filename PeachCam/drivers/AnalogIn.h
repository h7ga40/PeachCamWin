
#ifndef _ANALOGIN_H_
#define _ANALOGIN_H_

#include "PinNames.h"

namespace mbed {

class AnalogIn
{
public:
	AnalogIn(PinName pin);
	float read();

	operator float() {
		return read();
	}
private:
	analogin_t analogin;
};

}

#endif // _ANALOGIN_H_
