using System;

namespace TestBench
{
	public class Serial : IUnitInterface
	{
		UARTName uart;
		internal PinName tx;
		internal PinName rx;
		int baudrate;
		int data_bits;
		SerialParity parity;
		int stop_bits;

		public Serial(UARTName uart, PinName tx, PinName rx)
		{
			this.uart = uart;
			this.tx = tx;
			this.rx = rx;
		}

		public string TypeName => "UART";

		public string InterfaceName => uart.ToString();

		internal void SetBaudRate(int baudrate)
		{
			this.baudrate = baudrate;
		}

		internal void SetFormat(int data_bits, SerialParity parity, int stop_bits)
		{
			this.data_bits = data_bits;
			this.parity = parity;
			this.stop_bits = stop_bits;
		}

		internal int GetC()
		{
			return -1;
		}

		internal void PutC(int c)
		{
		}

		internal bool IsReadable()
		{
			return false;
		}

		internal bool IsWritable()
		{
			return false;
		}
	}
}
