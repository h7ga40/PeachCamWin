#include "mbed.h"
#include "DigitalOut.h"
#include "libMPSSE_spi.h"

namespace mbed {

uint8 ft_pin_mode;
uint8 ft_pin_onoff;

DigitalOut::DigitalOut(PinName pin) :
	gpio(), pin(pin)
{
	TestBench->gpio_init_out(&gpio, pin);
	if (gpio.fthandle != NULL) {
		ft_pin_mode |= 1 << gpio.ftpin;
		FT_WriteGPIO((FT_HANDLE)gpio.ftpin, ft_pin_mode, ft_pin_onoff);
	}
}

void DigitalOut::write(int value)
{
	if (gpio.fthandle == NULL) {
		TestBench->gpio_write(&gpio, value);
	}
	else {
		if (value)
			ft_pin_onoff |= 1 << gpio.ftpin;
		else
			ft_pin_onoff &= ~(1 << gpio.ftpin);
		FT_WriteGPIO((FT_HANDLE)gpio.fthandle, ft_pin_mode, ft_pin_onoff);
	}
}

int DigitalOut::read()
{
	if (gpio.fthandle == NULL) {
		return TestBench->gpio_read(&gpio);
	}
	else{
		uint8 val = 0;
		FT_ReadGPIO((FT_HANDLE)gpio.fthandle, &val);
		return (val & (1 << gpio.ftpin)) != 0;
	}
}

}
