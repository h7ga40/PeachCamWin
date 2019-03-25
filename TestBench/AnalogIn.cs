namespace TestBench
{
	public class AnalogIn : IUnitInterface
	{
		internal ADCName adc;
		internal PinName pin;

		public AnalogIn(ADCName adc, PinName pin)
		{
			this.adc = adc;
			this.pin = pin;
		}

		public string TypeName => "AnalogIn";

		public string InterfaceName => adc.ToString();

		internal ushort Read()
		{
			return 0;
		}
	}
}
