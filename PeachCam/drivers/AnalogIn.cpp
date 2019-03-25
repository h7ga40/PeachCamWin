#include "mbed.h"
#include "AnalogIn.h"

namespace mbed {

AnalogIn::AnalogIn(PinName pin)
{
	TestBench->analogin_init(&analogin, pin);
}

float AnalogIn::read()
{
	return TestBench->analogin_read(&analogin);
}

}
