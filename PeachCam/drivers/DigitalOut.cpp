#include "mbed.h"
#include "DigitalOut.h"

namespace mbed {

DigitalOut::DigitalOut(PinName pin)
{
	TestBench->gpio_init_out(&gpio, pin);
}

void DigitalOut::write(int value)
{
	TestBench->gpio_write(&gpio, value);
}

int DigitalOut::read()
{
	return TestBench->gpio_read(&gpio);
}

}
