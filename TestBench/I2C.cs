using System;

namespace TestBench
{
	public class I2C : IUnitInterface
	{
		I2CName i2c;
		internal PinName sda;
		internal PinName scl;
		int frequency;

		public I2C(I2CName i2c, PinName sda, PinName scl)
		{
			this.i2c = i2c;
			this.sda = sda;
			this.scl = scl;
		}

		public string TypeName => "I2C";

		public string InterfaceName => i2c.ToString();

		internal void SetFrequency(int hz)
		{
			frequency = hz;
		}

		internal bool Start()
		{
			return true;
		}

		internal bool Stop()
		{
			return true;
		}

		internal int Read(int address, byte[] data, int length, int stop)
		{
			return length;
		}

		internal int Write(int address, byte[] data, int length, int stop)
		{
			return length;
		}

		internal int ByteRead(int last)
		{
			return 0;
		}

		internal int ByteWrite(int data)
		{
			return 0;
		}
	}
}
