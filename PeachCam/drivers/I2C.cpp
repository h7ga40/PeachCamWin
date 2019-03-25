#include "mbed.h"
#include "I2C.h"

namespace mbed {

I2C::I2C(PinName sda, PinName scl)
{
	TestBench->i2c_init(&i2c, sda, scl);
}

void I2C::frequency(int hz)
{
	TestBench->i2c_frequency(&i2c, hz);
}

int I2C::read(int address, char *data, int length, bool repeated)
{
	return TestBench->i2c_read(&i2c, address, (unsigned char *)data, length, repeated);
}

int I2C::read(int ack)
{
	return TestBench->i2c_byte_read(&i2c, ack);
}

int I2C::write(int address, const char *data, int length, bool repeated)
{
	return TestBench->i2c_write(&i2c, address, (unsigned char *)data, length, repeated);
}

int I2C::write(int data)
{
	return TestBench->i2c_byte_write(&i2c, data);
}

void I2C::start(void)
{
	TestBench->i2c_start(&i2c);
}

void I2C::stop(void)
{
	TestBench->i2c_stop(&i2c);
}

void I2C::lock(void)
{
}

void I2C::unlock(void)
{
}

}
