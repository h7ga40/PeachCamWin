// http://penguin.tantin.jp/hard/FLIR_Lepton_with_FT232xx.html
// https://github.com/penguintantin/Flir_lepton/blob/master/lepton3rd.py
#include "mbed.h"
#include "I2C.h"
#include "libMPSSE_spi.h"

namespace mbed {

extern uint8 ft_pin_mode;
extern uint8 ft_pin_onoff;

I2C::I2C(PinName sda, PinName scl) :
	i2c(), sda(sda), scl(scl), hz(100000), wait(10)
{
	TestBench->i2c_init(&i2c, sda, scl);

	ft_pin_mode |= (1 << i2c.ftsda) | (1 << i2c.ftscl);
	ft_pin_mode &= ~(1 << i2c.ftsda);
	ft_pin_onoff |= (1 << i2c.ftsda) | (1 << i2c.ftscl);
	ft_pin_onoff &= ~(1 << i2c.ftsda);
}

void I2C::frequency(int hz)
{
	this->hz = hz;

	if (i2c.fthandle == NULL) {
		TestBench->i2c_frequency(&i2c, hz);
	}
	else {
		wait = 1000 / hz;
	}
}

int I2C::read(int address, char *data, int length, bool repeated)
{
	if (i2c.fthandle == NULL) {
		return TestBench->i2c_read(&i2c, address, (unsigned char *)data, length, repeated);
	}
	else {
		char *pos = data;

		memset(data, 0, length);

		start();

		if (write(address | 0x01)) {
			for (const char *end = &data[length - 1]; pos < end; pos++) {
				*pos = read(1);
			}
			*pos++ = read(0);
		}
		else
			repeated = false;

		if (!repeated)
			stop();

		if (((int)pos - (int)data) != length)
			return 1;
		else
			return 0;
	}
}

int I2C::read(int ack)
{
	if (i2c.fthandle == NULL) {
		return TestBench->i2c_byte_read(&i2c, ack);
	}
	else {
		uint8_t data = 0, val;

		ft_pin_mode &= ~(1 << i2c.ftsda);
		FT_WriteGPIO((FT_HANDLE)i2c.fthandle, ft_pin_mode, ft_pin_onoff);
		if (wait > 0) Sleep(wait);

		for (int i = 0; i < 8; i++) {
			PPinHigh(i2c.ftscl);
			if (wait > 0) Sleep(wait);
			val = 0;
			FT_ReadGPIO((FT_HANDLE)i2c.fthandle, &val);
			if (val & (1 << i2c.ftsda))
				data |= 0x80 >> i;
			PPinLow(i2c.ftscl);
			if (wait > 0) Sleep(wait);
		}
		ft_pin_mode |= 1 << i2c.ftsda;
		if (ack)
			PPinLow(i2c.ftsda);
		else
			PPinHigh(i2c.ftsda);
		if (wait > 0) Sleep(wait);
		PPinHigh(i2c.ftscl);
		if (wait > 0) Sleep(wait);
		PPinLow(i2c.ftscl);
		if (wait > 0) Sleep(wait);
		PPinHigh(i2c.ftsda);

		return data;
	}
}

int I2C::write(int address, const char *data, int length, bool repeated)
{
	if (i2c.fthandle == NULL) {
		return TestBench->i2c_write(&i2c, address, (unsigned char *)data, length, repeated);
	}
	else {
		const char *pos = data;

		start();

		if (write(address)) {
			for (const char *end = &data[length]; pos < end; pos++) {
				if (!write(*pos))
					break;
			}
		}

		if (!repeated)
			stop();

		if (((int)pos - (int)data) != length)
			return 1;
		else
			return 0;
	}
}

int I2C::write(int data)
{
	if (i2c.fthandle == NULL) {
		return TestBench->i2c_byte_write(&i2c, data);
	}
	else {
		int ack;

		for (int i = 0; i < 8; i++) {
			PPinLow(i2c.ftscl);
			if (wait > 0) Sleep(wait);
			if (data & (0x80 >> i))
				PPinHigh(i2c.ftsda);
			else
				PPinLow(i2c.ftsda);
			if (wait > 0) Sleep(wait);
			PPinHigh(i2c.ftscl);
			if (wait > 0) Sleep(wait);
		}
		PPinLow(i2c.ftscl);
		if (wait > 0) Sleep(wait);
		PPinHigh(i2c.ftsda);
		if (wait > 0) Sleep(wait);
		ft_pin_mode &= ~(1 << i2c.ftsda);
		PPinHigh(i2c.ftscl);
		if (wait > 0) Sleep(wait);

		uint8_t val = 0;
		FT_ReadGPIO((FT_HANDLE)i2c.fthandle, &val);
		if ((val & (1 << i2c.ftsda)) == 0)
			ack = 1;
		else
			ack = 0;

		ft_pin_mode |= 1 << i2c.ftsda;
		ft_pin_onoff |= 1 << i2c.ftsda;
		PPinLow(i2c.ftscl);
		if (wait > 0) Sleep(wait);

		return ack;
	}
}

void I2C::start(void)
{
	if (i2c.fthandle == NULL) {
		TestBench->i2c_start(&i2c);
	}
	else {
		PPinHigh(i2c.ftscl);
		if (wait > 0) Sleep(wait);
		PPinLow(i2c.ftsda);
		if (wait > 0) Sleep(wait);
	}
}

void I2C::stop(void)
{
	if (i2c.fthandle == NULL) {
		TestBench->i2c_stop(&i2c);
	}
	else {
		PPinHigh(i2c.ftscl);
		if (wait > 0) Sleep(wait);
		PPinHigh(i2c.ftsda);
		if (wait > 0) Sleep(wait);
	}
}

void I2C::PPinHigh(int pin)
{
	ft_pin_onoff |= (1 << pin);
	FT_WriteGPIO((FT_HANDLE)i2c.fthandle, ft_pin_mode, ft_pin_onoff);
}

void I2C::PPinLow(int pin)
{
	ft_pin_onoff &= ~(1 << pin);
	FT_WriteGPIO((FT_HANDLE)i2c.fthandle, ft_pin_mode, ft_pin_onoff);
}

void I2C::lock(void)
{
}

void I2C::unlock(void)
{
}

}
