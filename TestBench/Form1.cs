﻿using System;
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
		Graphics graphics;
		TouchKey touchKey;
		GpioLED[] leds = new GpioLED[4];
		GpioButton sw1;
		AnalogInSlider grip;

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			Application.Idle += Application_Idle1;
		}

		private void Application_Idle1(object sender, EventArgs e)
		{
			Application.Idle -= Application_Idle1;

			testBench = new TestBench(this);
			testBench.UpdateDevices();

			string video = cmbVideoList.Text;
			string audio = cmbAudioList.Text;
			string ftdi = cmbMpsseList.Text;

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

			cmbMpsseList.Items.Clear();
			cmbMpsseList.Items.AddRange(testBench.MpsseDevices.ToArray());
			foreach (var i in cmbMpsseList.Items) {
				if (i.ToString() == video) {
					cmbMpsseList.SelectedItem = i;
					break;
				}
			}
			if (cmbMpsseList.Items.Count > 0 && cmbMpsseList.SelectedIndex == -1) {
				cmbMpsseList.SelectedIndex = 0;
			}

			Application.Idle += Application_Idle2;
		}

		private void Application_Idle2(object sender, EventArgs e)
		{
			Application.Idle -= Application_Idle2;

			testBench.Load();

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
				case "AN0":
					grip = new AnalogInSlider((AnalogIn)i, trackBar1);
					break;
				}
			}
			if (graphics == null)
				return;

			graphics.VideoCaptureDeviceIndex = cmbVideoList.SelectedIndex;

			testBench.Start();

			timer1.Enabled = true;
		}

		private void timer1_Tick(object sender, EventArgs e)
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
}
