namespace TestBench
{
	public class PwmOut : IUnitInterface
	{
		PWMName pwm;
		internal PinName pin;
		ushort value;
		private int periodUs;
		private int pulseWidthUs;

		public PwmOut(PWMName pwm, PinName pin)
		{
			this.pwm = pwm;
			this.pin = pin;
		}

		public string TypeName => "PWM";

		public string InterfaceName => pwm.ToString();

		internal ushort Read()
		{
			return value;
		}

		internal void Write(ushort value)
		{
			this.value = value;
		}

		internal void PeriodUs(int us)
		{
			this.periodUs = us;
		}

		internal void PulseWidthUs(int us)
		{
			this.pulseWidthUs = us;
		}
	}
}
