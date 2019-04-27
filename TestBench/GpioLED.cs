using System.Drawing;
using System.Windows.Forms;

namespace TestBench
{
	class GpioLED
	{
		private Gpio gpio;
		private Label label;
		Color on;
		Color off;

		public GpioLED(Gpio gpio, Label label, Color on, Color off)
		{
			this.gpio = gpio;
			this.label = label;
			this.on = on;
			this.off = off;
		}

		public void Update()
		{
			if (gpio.Value) {
				label.BackColor = on;
			}
			else {
				label.BackColor = off;
			}
		}
	}
}
