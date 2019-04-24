#ifndef _SPI_H_
#define _SPI_H_

#include "PinNames.h"

namespace mbed {

class SPI {
public:
	SPI(PinName mosi, PinName miso, PinName sclk, PinName ssel = NC);
	void init();
	void format(int bits, int mode = 0);
	void frequency(int hz);
	virtual int write(int value);
	virtual int write(const char *tx_buffer, int tx_length, char *rx_buffer, int rx_length);
	virtual void lock(void);
	virtual void unlock(void);
private:
	spi_t spi;
	int bits;
	int mode;
	int hz;
};

}

#endif // _SPI_H_
