using System;

namespace TestBench
{
	public class SPI : IUnitInterface
	{
		SPIName spi;
		internal PinName mosi;
		internal PinName miso;
		internal PinName sclk;
		internal PinName ssel;
		int frequency;
		int bits;
		int mode;
		bool slave;

		public SPI(SPIName spi, PinName mosi, PinName miso, PinName sclk, PinName ssel)
		{
			this.spi = spi;
			this.mosi = mosi;
			this.miso = miso;
			this.sclk = sclk;
			this.ssel = ssel;
		}

		public string TypeName => "SPI";

		public string InterfaceName => spi.ToString();

		internal void SetFrequency(int hz)
		{
			frequency = hz;
		}

		internal void SetFormat(int bits, int mode, bool slave)
		{
			this.bits = bits;
			this.mode = mode;
			this.slave = slave;
		}

		internal int MasterWrite(int value)
		{
			return 1;
		}

		internal int MasterBlockWrite(byte[] tx_buffer, int tx_length, byte[] rx_buffer, int rx_length, byte write_fill)
		{
			return tx_length;
		}
	}
}
