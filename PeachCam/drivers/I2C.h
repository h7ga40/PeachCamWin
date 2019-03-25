#ifndef _I2C_H_
#define _I2C_H_

#include "PinNames.h"

namespace mbed {

class I2C {
public:
	I2C(PinName sda, PinName scl);
	void frequency(int hz);
	int read(int address, char *data, int length, bool repeated = false);
	int read(int ack);
	int write(int address, const char *data, int length, bool repeated = false);
	int write(int data);
	void start(void);
	void stop(void);
	virtual void lock(void);
	virtual void unlock(void);
private:
	i2c_t i2c;
};

}

#endif // _I2C_H_
