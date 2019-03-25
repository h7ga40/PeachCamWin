#include "mbed.h"
#include "SPI.h"

namespace mbed {

SPI::SPI(PinName mosi, PinName miso, PinName sclk, PinName ssel)
{
	TestBench->spi_init(&spi, mosi, miso, sclk, ssel);
}

void SPI::format(int bits, int mode)
{
	TestBench->spi_format(&spi, bits, mode, 0);
}

void SPI::frequency(int hz)
{
	TestBench->spi_frequency(&spi, hz);
}

int SPI::write(int value)
{
	return TestBench->spi_master_write(&spi, value);
}

int SPI::write(const char *tx_buffer, int tx_length, char *rx_buffer, int rx_length)
{
	return TestBench->spi_master_block_write(&spi, (unsigned char *)tx_buffer, tx_length, (unsigned char *)rx_buffer, rx_length, 0xFF);
}

void SPI::lock(void)
{
}

void SPI::unlock(void)
{
}

}
