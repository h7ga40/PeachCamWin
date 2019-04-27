using System.Windows.Forms;

namespace TestBench
{
	class GpioButton
	{
		private Gpio gpio;
		private Button button;

		public GpioButton(Gpio gpio, Button button)
		{
			this.gpio = gpio;
			this.button = button;

			button.MouseDown += Button_MouseDown;
			button.MouseUp += Button_MouseUp;
		}

		private void Button_MouseDown(object sender, MouseEventArgs e)
		{
			gpio.Value = true;
		}

		private void Button_MouseUp(object sender, MouseEventArgs e)
		{
			gpio.Value = false;
		}
	}
}
