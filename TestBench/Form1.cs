using MPSSELight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestBench
{
	public partial class Form1 : Form, IStdio
	{
		TestBench testBench;
		PeachCam peachCam;
		Graphics graphics;
		TouchKey touchKey;
		GpioLED[] leds = new GpioLED[4];
		GpioButton sw1;

		public Form1()
		{
			InitializeComponent();

			testBench = new TestBench(this);

			peachCam = new PeachCam();
			peachCam.SetTestBench(testBench);
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			Application.Idle += Application_Idle1;
		}

		private void Application_Idle1(object sender, EventArgs e)
		{
			Application.Idle -= Application_Idle1;

			string video = cmbVideoList.Text;
			string audio = cmbAudioList.Text;

			testBench.UpdateDevices();

			cmbVideoList.Items.Clear();
			cmbVideoList.Items.AddRange(testBench.VideoDevices.ToArray());
			foreach (var i in cmbVideoList.Items) {
				if (i.ToString() == video) {
					cmbVideoList.SelectedItem = i;
					break;
				}
			}
			if (cmbVideoList.Items.Count > 0 && cmbVideoList.SelectedIndex == -1) {
				cmbVideoList.SelectedIndex = 0;
			}

			cmbAudioList.Items.Clear();
			cmbAudioList.Items.AddRange(testBench.AudioDevices.ToArray());
			foreach (var i in cmbAudioList.Items) {
				if (i.ToString() == video) {
					cmbAudioList.SelectedItem = i;
					break;
				}
			}
			if (cmbAudioList.Items.Count > 0 && cmbAudioList.SelectedIndex == -1) {
				cmbAudioList.SelectedIndex = 0;
			}

			Application.Idle += Application_Idle2;
		}

		private void Application_Idle2(object sender, EventArgs e)
		{
			Application.Idle -= Application_Idle2;

			peachCam.Start();

			timer1.Enabled = true;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			foreach (var i in testBench.Interfaces) {
				switch (i.InterfaceName) {
				case "Graphics":
					graphics = (Graphics)i;
					break;
				case "I2C_0":
					touchKey = new TouchKey((I2C)i);
					break;
				case "LED1":
					leds[0] = new GpioLED((Gpio)i, label1, Color.Red, BackColor);
					break;
				case "LED2":
					leds[1] = new GpioLED((Gpio)i, label2, Color.Green, BackColor);
					break;
				case "LED3":
					leds[2] = new GpioLED((Gpio)i, label3, Color.Blue, BackColor);
					break;
				case "LED4":
					leds[3] = new GpioLED((Gpio)i, label4, Color.Red, BackColor);
					break;
				case "USER_BUTTON0":
					sw1 = new GpioButton((Gpio)i, button1);
					break;
				}
			}
			if (graphics == null)
				return;

			graphics.VideoCaptureDeviceIndex = cmbVideoList.SelectedIndex;

			timer1.Tick -= timer1_Tick;
			timer1.Tick += timer1_Tick2;
		}

		private void timer1_Tick2(object sender, EventArgs e)
		{
			foreach (var led in leds) {
				led.Update();
			}

			pictureBox1.Image = graphics.GetImage();
			pictureBox1.Refresh();
		}

		private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
		{
			if (touchKey != null) {
				touchKey.MouseDown = true;
				touchKey.MousePos = e.Location;
			}
		}

		private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
			if (touchKey != null) {
				touchKey.MouseDown = false;
				touchKey.MousePos = e.Location;
			}
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			if (touchKey != null) {
				touchKey.MousePos = e.Location;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{

		}

		public void Stdout(byte[] text)
		{
			BeginInvoke(new MethodInvoker(() => { 
				vtWindow1.Parse(text);
			}));
		}

		private void vtWindow1_DataReceive(object sender, byte[] data)
		{
			testBench.Stdin(data);
		}
	}

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
