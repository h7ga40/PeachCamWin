using System;

namespace TestBench
{
	public class Gpio : IUnitInterface
	{
		internal PinName pin;
		PinMode mode;
		PinDirection direction;
		int value;

		internal Gpio(PinName pin)
		{
			this.pin = pin;
			InterfaceName = GetString(pin);
		}

		public string TypeName => "Gpio";

		public string InterfaceName { get; }

		public bool Value {
			get { return value != 0; }
			set { this.value = value ? 1 : 0; }
		}

		public static string GetString(PinName pin)
		{
			switch (pin) {
			// mbed Pin Names
			case PinName.LED1: return "LED1";
			case PinName.LED2: return "LED2";
			case PinName.LED3: return "LED3";
			case PinName.LED4: return "LED4";

			case PinName.USBTX: return "USBTX";
			case PinName.USBRX: return "USBRX";

			// Arduiono Pin Names
			case PinName.D0: return "D0";
			case PinName.D1: return "D1";
			case PinName.D2: return "D2";
			case PinName.D3: return "D3";
			case PinName.D4: return "D4";
			case PinName.D5: return "D5";
			case PinName.D6: return "D6";
			case PinName.D7: return "D7";
			case PinName.D8: return "D8";
			case PinName.D9: return "D9";
			case PinName.D10: return "D10";
			case PinName.D11: return "D11";
			case PinName.D12: return "D12";
			case PinName.D13: return "D13";
			case PinName.D14: return "D14";
			case PinName.D15: return "D15";

			case PinName.A0: return "A0";
			case PinName.A1: return "A1";
			case PinName.A2: return "A2";
			case PinName.A3: return "A3";
			case PinName.A4: return "A4";
			case PinName.A5: return "A5";

			case PinName.USER_BUTTON0: return "USER_BUTTON0";
			}
			return pin.ToString();
		}

		internal void SetMode(PinMode mode)
		{
			this.mode = mode;
		}

		internal void SetDirection(PinDirection direction)
		{
			this.direction = direction;
		}

		internal void Write(int value)
		{
			this.value = value;
		}

		internal int Read()
		{
			return value;
		}
	}
}
