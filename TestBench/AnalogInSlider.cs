using System.Windows.Forms;

namespace TestBench
{
	internal class AnalogInSlider
	{
		private AnalogIn analogIn;
		private TrackBar trackBar;

		public AnalogInSlider(AnalogIn analogIn, TrackBar trackBar)
		{
			this.analogIn = analogIn;
			this.trackBar = trackBar;

			trackBar.ValueChanged += TrackBar_ValueChanged;
		}

		private void TrackBar_ValueChanged(object sender, System.EventArgs e)
		{
			var value = (double)(trackBar.Value - trackBar.Minimum)
				/ (double)(trackBar.Maximum - trackBar.Minimum);
			analogIn.Value = (ushort)(ushort.MaxValue * value);
		}
	}
}