#include "mbed.h"
#include "SPI.h"
#include "libMPSSE_spi.h"

namespace mbed {

SPI::SPI(PinName mosi, PinName miso, PinName sclk, PinName ssel) :
	spi(),
	hz(1000000),
	bits(8),
	mode(0)
{
	TestBench->spi_init(&spi, mosi, miso, sclk, ssel);
}

void SPI::init()
{
	FT_STATUS ret;
	ChannelConfig config;
	uint8 latency = 2;
	uint32 options = SPI_CONFIG_OPTION_CS_DBUS3 | SPI_CONFIG_OPTION_CS_ACTIVELOW;
	uint32 pin = 0;

	switch (mode) {
	// 正パルス ラッチ先行
	case 0:
		// IN_POS_OUT_NEG_EDGE
		options |= SPI_CONFIG_OPTION_MODE0;
		// SCLKは始めと終わりでLOWとする
		pin = 0x000B000B;
		break;
	// 正パルス シフト先行
	case 1:
		// IN_NEG_OUT_POS_EDGE
		options |= SPI_CONFIG_OPTION_MODE1;
		// SCLKは始めと終わりでLOWとする
		pin = 0x000B000B;
		break;
	// 負パルス ラッチ先行
	case 2:
		// IN_NEG_OUT_POS_EDGE
		options |= SPI_CONFIG_OPTION_MODE2;
		// SCLKは始めと終わりでHIGHとする
		pin = 0x0B0B0B0B;
		break;
	// 負パルス シフト先行
	case 3:
		// IN_POS_OUT_NEG_EDGE
		options |= SPI_CONFIG_OPTION_MODE3;
		// SCLKは始めと終わりでHIGHとする
		pin = 0x0B0B0B0B;
		break;
	}

	config.ClockRate = hz;
	config.LatencyTimer = latency;
	config.configOptions = options;
	config.Pin = pin;
	config.reserved = 0;

	ret = SPI_InitChannel((FT_HANDLE)spi.fthandle, &config);
	if (ret != FT_OK)
		printf("SPI_InitChannel error %d", ret);
}

void SPI::format(int bits, int mode)
{
	this->bits = bits;
	this->mode = mode;

	if (spi.fthandle == NULL) {
		TestBench->spi_format(&spi, bits, mode, 0);
	}
	else {
		init();
	}
}

void SPI::frequency(int hz)
{
	this->hz = hz;

	if (spi.fthandle == NULL) {
		TestBench->spi_frequency(&spi, hz);
	}
	else {
		init();
	}
}

int SPI::write(int value)
{
	if (spi.fthandle == NULL) {
		return TestBench->spi_master_write(&spi, value);
	}
	else {
		FT_STATUS ret;
		uint8_t data = (uint8_t)value;
		uint32 size;

		ret = SPI_Write((FT_HANDLE)spi.fthandle, &data, sizeof(data), &size, 0);
		if (ret != FT_OK)
			return 0;

		return size;
	}
}

int SPI::write(const char *_tx_buffer, int tx_length, char *_rx_buffer, int rx_length)
{
	if (spi.fthandle == NULL) {
		return TestBench->spi_master_block_write(&spi, (unsigned char *)_tx_buffer, tx_length, (unsigned char *)_rx_buffer, rx_length, 0xFF);
	}
	else {
		int result = 0;
		FT_STATUS ret;
		uint8 *tx_buffer = (uint8 *)_tx_buffer;
		uint8 *rx_buffer = (uint8 *)_rx_buffer;
		uint32 max = (tx_length > rx_length) ? tx_length : rx_length;
		uint32 size = 0;
		uint32 options = 0;
	
		if (max <= 0)
			return 0;
	
		if ((rx_buffer == NULL) && (rx_length != 0))
			return 0;
	
		if (tx_buffer == NULL) {
			if (tx_length != 0)
				return 0;
	
			ret = SPI_Read((FT_HANDLE)spi.fthandle, rx_buffer, rx_length, &size, options);
			if (ret != FT_OK)
				return 0;
			return size;
		}
	
		if (rx_length > 0) {
			ret = SPI_ReadWrite((FT_HANDLE)spi.fthandle, rx_buffer, tx_buffer, rx_length, &size, options);
			if (ret != FT_OK)
				return result;
		}
		result += size;
	
		if (tx_length > rx_length) {
			ret = SPI_Write((FT_HANDLE)spi.fthandle, &tx_buffer[rx_length], tx_length - rx_length, &size, options);
			if (ret != FT_OK)
				return result;
			result += size;
		}
	
		return result;
	}
}

void SPI::lock(void)
{
}

void SPI::unlock(void)
{
}

}
