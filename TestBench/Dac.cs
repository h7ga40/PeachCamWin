namespace TestBench
{
	public class Dac : IUnitInterface
	{
		DACName dac;
		internal PinName pin;
		ushort value;

		public Dac(DACName dac, PinName pin)
		{
			this.dac = dac;
			this.pin = pin;
		}

		public string TypeName => "DAC";

		public string InterfaceName => dac.ToString();

		internal ushort Read()
		{
			return value;
		}

		internal void Write(ushort value)
		{
			this.value = value;
		}
	}
}
