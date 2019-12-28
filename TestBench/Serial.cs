using System;
using System.IO.Ports;

namespace TestBench
{
	public class Serial : IUnitInterface, IDisposable
	{
		private readonly UARTName uart;
		internal readonly PinName tx;
		internal readonly PinName rx;
		internal PinName rts;
		internal PinName cts;
		internal int baudrate;
		internal int data_bits;
		internal SerialParity parity;
		internal int stop_bits;
		internal FlowControl flow_control;
		private SerialPort serial;

		public Serial(UARTName uart, PinName tx, PinName rx, PinName rts = PinName.NC, PinName cts = PinName.NC)
		{
			this.uart = uart;
			this.tx = tx;
			this.rx = rx;
			this.rts = rts;
			this.cts = cts;

			serial = new SerialPort();
		}

		public void Dispose()
		{
			serial.Dispose();
		}

		public string TypeName => "UART";

		public string InterfaceName => uart.ToString();

		internal void SetBaudRate(int baudrate)
		{
			this.baudrate = baudrate;

			if (serial != null)
				serial.BaudRate = baudrate;
		}

		internal void SetFormat(int data_bits, SerialParity parity, int stop_bits)
		{
			this.data_bits = data_bits;
			this.parity = parity;
			this.stop_bits = stop_bits;

			if (serial != null) {
				serial.DataBits = data_bits;
				switch (parity) {
				case SerialParity.ParityNone:
					serial.Parity = Parity.None;
					break;
				case SerialParity.ParityOdd:
					serial.Parity = Parity.Odd;
					break;
				case SerialParity.ParityEven:
					serial.Parity = Parity.Even;
					break;
				case SerialParity.ParityForced1:
					serial.Parity = Parity.Mark;
					break;
				case SerialParity.ParityForced0:
					serial.Parity = Parity.Space;
					break;
				}
				switch (stop_bits) {
				case 0:
					serial.StopBits = StopBits.None;
					break;
				case 1:
					serial.StopBits = StopBits.One;
					break;
				case 2:
					serial.StopBits = StopBits.Two;
					break;
				case 15:
					serial.StopBits = StopBits.OnePointFive;
					break;
				}
			}
		}

		internal void SetTimeout(int timeout)
		{
			serial.ReadTimeout = timeout;
			serial.WriteTimeout = timeout;
		}

		internal int GetC()
		{
			if ((serial == null) || !serial.IsOpen) {
				return -1;
			}
			else {
				return serial.ReadByte();
			}
		}

		private readonly byte[] buf = new byte[256];

		internal void PutC(int c)
		{
			buf[0] = (byte)c;

			if ((serial != null) && serial.IsOpen) {
				serial.Write(buf, 0, 1);
			}
		}

		internal bool IsReadable()
		{
			if (serial == null) {
				return false;
			}
			else {
				return serial.BytesToRead > 0;
			}
		}

		internal bool IsWritable()
		{
			if (serial == null) {
				return false;
			}
			else {
				return serial.IsOpen;
			}
		}

		internal void SetFlowControl(FlowControl flowControl, PinName rts = PinName.NC, PinName cts = PinName.NC)
		{
			flow_control = flowControl;
			this.rts = rts;
			this.cts = cts;
		}
	}
}
