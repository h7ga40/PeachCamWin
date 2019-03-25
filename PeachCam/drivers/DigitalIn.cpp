#include "mbed.h"
#include "AnalogIn.h"

namespace mbed {

DigitalIn::DigitalIn(PinName pin)
{
	TestBench->gpio_init_in(&gpio, pin);
}

int DigitalIn::read()
{
	return TestBench->gpio_read(&gpio);
}

}
