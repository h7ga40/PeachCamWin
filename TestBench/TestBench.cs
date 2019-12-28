using System;
using System.Collections.Generic;

namespace TestBench
{
	public interface IUnitInterface
	{
		string TypeName { get; }
		string InterfaceName { get; }
	}

	public interface IStdio
	{
		void Stdout(byte[] text);
	}

	public class TestBench : ITestBench
	{
		private Dictionary<int, IUnitInterface> interfaces = new Dictionary<int, IUnitInterface>();
		private Dictionary<PinName, IUnitInterface> pin_if = new Dictionary<PinName, IUnitInterface>();

		public IEnumerable<IUnitInterface> Interfaces => interfaces.Values;
		public List<Tuple<int, string, string>> VideoDevices { get; } = new List<Tuple<int, string, string>>();
		public List<Tuple<int, string, string>> AudioDevices { get; } = new List<Tuple<int, string, string>>();
		public List<Tuple<int, string, string>> MpsseDevices { get; } = new List<Tuple<int, string, string>>();
		public PeachCam PeachCam { get; }

		private Graphics graphics;
		private ESP32Driver esp;
		private IStdio stdio;
		private IntPtr fthandle;

		public TestBench(IStdio stdio)
		{
			this.stdio = stdio;

			PeachCam = new PeachCam();

			fthandle = PeachCam.OpenMpsse(0);
			PeachCam.SetTestBench(this);
		}

		internal void Load()
		{
			PeachCam.Load();
		}

		internal void Start()
		{
			PeachCam.Start();
		}

		private IUnitInterface CreateGpio(ref gpio_t obj, PinName pin)
		{
			if (pin == PinName.NC)
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(pin)) {
					throw new ArgumentException();
				}
				uif = new Gpio(pin);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);

				if (pin == PinName.P4_5) {
					obj.fthandle = fthandle;
					obj.ftpin = 3;
				}
			}
			return uif;
		}

		private IUnitInterface CreateAnalogIn(ref analogin_t obj, PinName pin)
		{
			var p = PinMap.Peripheral(pin, PinMap.PinMap_ADC);
			if (p == (int)PinName.NC)
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(pin)) {
					throw new ArgumentException();
				}
				uif = new AnalogIn((ADCName)p, pin);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);
			}
			return uif;
		}

		private IUnitInterface CreateDac(ref dac_t obj, PinName pin)
		{
			var p = PinMap.Peripheral(pin, PinMap.PinMap_DAC);
			if (p == (int)PinName.NC)
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(pin)) {
					throw new ArgumentException();
				}
				uif = new Dac((DACName)p, pin);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);
			}
			return uif;
		}

		private IUnitInterface CreatePwmOut(ref pwmout_t obj, PinName pin)
		{
			var p = PinMap.Peripheral(pin, PinMap.PinMap_PWM);
			if (p == (int)PinName.NC)
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(pin)) {
					throw new ArgumentException();
				}
				uif = new PwmOut((PWMName)p, pin);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);
			}
			return uif;
		}

		private void DeletePwmOut(ref pwmout_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif))
				return;

			pin_if.Remove(((PwmOut)uif).pin);
			interfaces.Remove(obj.id);
		}

		private IUnitInterface CreateSerial(ref serial_t obj, PinName tx, PinName rx,
			PinName rts = PinName.NC, PinName cts = PinName.NC)
		{
			var p = PinMap.Peripheral(tx, PinMap.PinMap_UART_TX);
			if (p == (int)PinName.NC)
				return null;
			var t = PinMap.Peripheral(rx, PinMap.PinMap_UART_RX);
			if ((t == (int)PinName.NC) || (p != t))
				return null;
			var r = PinMap.Peripheral(rts, PinMap.PinMap_UART_RTS);
			if ((rts != PinName.NC) && ((r == (int)PinName.NC) || (p != r)))
				return null;
			var s = PinMap.Peripheral(cts, PinMap.PinMap_UART_CTS);
			if ((cts != PinName.NC) && ((s == (int)PinName.NC) || (p != s)))
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(tx) || pin_if.ContainsKey(rx)) {
					throw new ArgumentException();
				}
				uif = new Serial((UARTName)p, tx, rx, rts, cts);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);
			}
			return uif;
		}

		private void DeleteSerial(ref serial_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif))
				return;

			pin_if.Remove(((Serial)uif).tx);
			pin_if.Remove(((Serial)uif).rx);
			interfaces.Remove(obj.id);
		}

		private IUnitInterface CreateI2C(ref i2c_t obj, PinName sda, PinName scl)
		{
			var p = PinMap.Peripheral(sda, PinMap.PinMap_I2C_SDA);
			if (p == (int)PinName.NC)
				return null;
			var t = PinMap.Peripheral(scl, PinMap.PinMap_I2C_SCL);
			if ((t == (int)PinName.NC) || (p != t))
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(sda) || pin_if.ContainsKey(scl)) {
					throw new ArgumentException();
				}
				uif = new I2C((I2CName)p, sda, scl);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);

				if ((sda == PinName.P1_7) && (scl == PinName.P1_6)) {
					obj.fthandle = fthandle;
					obj.ftsda = 5;
					obj.ftscl = 4;
				}
			}
			return uif;
		}

		private void DeleteI2C(ref i2c_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif))
				return;

			pin_if.Remove(((I2C)uif).sda);
			pin_if.Remove(((I2C)uif).scl);
			interfaces.Remove(obj.id);
		}

		private IUnitInterface CreateSPI(ref spi_t obj, PinName mosi, PinName miso, PinName sclk, PinName ssel)
		{
			var p = PinMap.Peripheral(mosi, PinMap.PinMap_SPI_MOSI);
			if (p == (int)PinName.NC)
				return null;
			var t = PinMap.Peripheral(miso, PinMap.PinMap_SPI_MISO);
			if ((t == (int)PinName.NC) || (t != p))
				return null;
			t = PinMap.Peripheral(sclk, PinMap.PinMap_SPI_SCLK);
			if ((t == (int)PinName.NC) || (t != p))
				return null;
			t = PinMap.Peripheral(ssel, PinMap.PinMap_SPI_SSEL);
			if ((t != (int)PinName.NC) && (t != p))
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(mosi) || pin_if.ContainsKey(miso) || pin_if.ContainsKey(sclk) || pin_if.ContainsKey(ssel)) {
					throw new ArgumentException();
				}
				uif = new SPI((SPIName)p, mosi, miso, sclk, ssel);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);

				if ((mosi == PinName.P4_6) && (miso == PinName.P4_7) && (sclk == PinName.P4_4)) {
					obj.fthandle = fthandle;
				}
			}
			return uif;
		}

		private void DeleteSPI(ref spi_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif))
				return;

			pin_if.Remove(((SPI)uif).mosi);
			pin_if.Remove(((SPI)uif).miso);
			pin_if.Remove(((SPI)uif).sclk);
			pin_if.Remove(((SPI)uif).ssel);
			interfaces.Remove(obj.id);
		}

		private IUnitInterface CreateI2S(ref i2s_t obj, PinName tx, PinName rx, PinName sck, PinName ws, PinName audio_clk)
		{
			var p = PinMap.Peripheral(tx, PinMap.PinMap_I2S_TX);
			if (p == (int)PinName.NC)
				return null;
			var t = PinMap.Peripheral(rx, PinMap.PinMap_I2S_RX);
			if ((t == (int)PinName.NC) || (t != p))
				return null;
			t = PinMap.Peripheral(sck, PinMap.PinMap_I2S_SCK);
			if ((t == (int)PinName.NC) || (t != p))
				return null;
			t = PinMap.Peripheral(ws, PinMap.PinMap_I2S_WS);
			if ((t == (int)PinName.NC) || (t != p))
				return null;
			t = PinMap.Peripheral(audio_clk, PinMap.PinMap_I2S_AUDIO_CLK);
			if ((t != (int)PinName.NC) && (t != p))
				return null;

			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				if (pin_if.ContainsKey(tx) || pin_if.ContainsKey(rx) || pin_if.ContainsKey(sck) || pin_if.ContainsKey(ws)) {
					throw new ArgumentException();
				}
				uif = new I2S((I2SName)p, tx, rx, sck, ws, audio_clk);
				obj.id = uif.GetHashCode();
				interfaces.Add(obj.id, uif);
			}
			return uif;
		}

		private void DeleteI2S(ref i2s_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif))
				return;

			pin_if.Remove(((I2S)uif).tx);
			pin_if.Remove(((I2S)uif).rx);
			pin_if.Remove(((I2S)uif).sck);
			pin_if.Remove(((I2S)uif).ws);
			pin_if.Remove(((I2S)uif).audio_clk);
			interfaces.Remove(obj.id);
		}

		public void gpio_init(ref gpio_t obj, PinName pin)
		{
			CreateGpio(ref obj, pin);
		}

		public void gpio_mode(ref gpio_t obj, PinMode mode)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Gpio)uif).SetMode(mode);
		}

		public void gpio_dir(ref gpio_t obj, PinDirection direction)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Gpio)uif).SetDirection(direction);
		}

		public void gpio_write(ref gpio_t obj, int value)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Gpio)uif).Write(value);
		}

		public int gpio_read(ref gpio_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((Gpio)uif).Read();
		}

		public void gpio_init_in(ref gpio_t obj, PinName pin)
		{
			var uif = CreateGpio(ref obj, pin);

			((Gpio)uif).SetDirection(PinDirection.PIN_INPUT);
		}

		public void gpio_init_out(ref gpio_t obj, PinName pin)
		{
			var uif = CreateGpio(ref obj, pin);

			((Gpio)uif).SetDirection(PinDirection.PIN_OUTPUT);
		}

		public void pin_function(PinName pin, int function)
		{
			PinMap.PinFunction(pin, function);
		}

		public void pin_mode(PinName pin, PinMode mode)
		{
			PinMap.PinMode(pin, mode);
		}

		public void analogin_init(ref analogin_t obj, PinName pin)
		{
			CreateAnalogIn(ref obj, pin);
		}

		public float analogin_read(ref analogin_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((AnalogIn)uif).Read() / 65535.9f;
		}

		public ushort analogin_read_u16(ref analogin_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((AnalogIn)uif).Read();
		}

		public void analogout_init(ref dac_t obj, PinName pin)
		{
			CreateDac(ref obj, pin);
		}

		public void analogout_write_u16(ref dac_t obj, ushort value)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Dac)uif).Write(value);
		}

		public void pwmout_init(ref pwmout_t obj, PinName pin)
		{
			CreatePwmOut(ref obj, pin);
		}

		public void pwmout_free(ref pwmout_t obj)
		{
			DeletePwmOut(ref obj);
		}

		public void pwmout_period_us(ref pwmout_t obj, int us)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((PwmOut)uif).PeriodUs(us);
		}

		public void pwmout_pulsewidth_us(ref pwmout_t obj, int us)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((PwmOut)uif).PulseWidthUs(us);
		}

		public void serial_init(ref serial_t obj, PinName tx, PinName rx)
		{
			CreateSerial(ref obj, tx, rx);
		}

		public void serial_free(ref serial_t obj)
		{
			DeleteSerial(ref obj);
		}

		public void serial_baud(ref serial_t obj, int baudrate)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Serial)uif).SetBaudRate(baudrate);
		}

		public void serial_format(ref serial_t obj, int data_bits, SerialParity parity, int stop_bits)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Serial)uif).SetFormat(data_bits, parity, stop_bits);
		}

		public int serial_getc(ref serial_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((Serial)uif).GetC();
		}

		public void serial_putc(ref serial_t obj, int c)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((Serial)uif).PutC(c);
		}

		public int serial_readable(ref serial_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((Serial)uif).IsReadable() ? 1 : 0;
		}

		public int serial_writable(ref serial_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((Serial)uif).IsWritable() ? 1 : 0;
		}

		public void i2c_init(ref i2c_t obj, PinName sda, PinName scl)
		{
			CreateI2C(ref obj, sda, scl);
		}

		public void i2c_frequency(ref i2c_t obj, int hz)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((I2C)uif).SetFrequency(hz);
		}

		public int i2c_start(ref i2c_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2C)uif).Start() ? 1 : 0;
		}

		public int i2c_stop(ref i2c_t obj)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2C)uif).Stop() ? 1 : 0;
		}

		public int i2c_read(ref i2c_t obj, int address,
			byte[] data, int length, int repeated)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2C)uif).Read(address, data, length, repeated);
		}

		public int i2c_write(ref i2c_t obj, int address,
			byte[] data, int length, int repeated)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2C)uif).Write(address, data, length, repeated);
		}

		public int i2c_byte_read(ref i2c_t obj, int ack)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2C)uif).ByteRead(ack);
		}

		public int i2c_byte_write(ref i2c_t obj, int data)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2C)uif).ByteWrite(data);
		}

		public void spi_init(ref spi_t obj, PinName mosi, PinName miso, PinName sclk, PinName ssel)
		{
			CreateSPI(ref obj, mosi, miso, sclk, ssel);
		}

		public void spi_free(ref spi_t obj)
		{
			DeleteSPI(ref obj);
		}

		public void spi_format(ref spi_t obj, int bits, int mode, int slave)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((SPI)uif).SetFormat(bits, mode, slave != 0);
		}

		public void spi_frequency(ref spi_t obj, int hz)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((SPI)uif).SetFrequency(hz);
		}

		public int spi_master_write(ref spi_t obj, int value)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((SPI)uif).MasterWrite(value);
		}

		public int spi_master_block_write(ref spi_t obj,
			byte[] tx_buffer, int tx_length,
			byte[] rx_buffer, int rx_length, byte write_fill)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((SPI)uif).MasterBlockWrite(tx_buffer, tx_length,
				rx_buffer, rx_length, write_fill);
		}

		private static readonly long m_Start = (new DateTime(1970, 1, 1)).Ticks;

		public void rtc_init() { }
		public void rtc_free() { }
		public int rtc_isenabled() { return true ? 1 : 0; }
		public long rtc_read() { return (DateTime.Now.Ticks - m_Start) / TimeSpan.TicksPerSecond; }
		public void rtc_write(long t) { }

		public void us_ticker_init() { }
		public long us_ticker_read() { return (DateTime.Now.Ticks - m_Start) / (1000 * TimeSpan.TicksPerMillisecond); }
		public void us_ticker_set_interrupt(long timestamp) { }
		public void us_ticker_disable_interrupt() { }
		public void us_ticker_clear_interrupt() { }

		public void wait_ms(int ms) { System.Threading.Thread.Sleep(ms); }
		public void wait_us(int us)
		{
			var end = us_ticker_read() + us;
			var ms = us / 1000;
			System.Threading.Thread.Sleep(ms);
			while (us_ticker_read() < end)
				System.Threading.Thread.Yield();
		}

		public IESP32 esp32_init(PinName en, PinName io0, PinName tx, PinName rx,
			bool debug, PinName rts, PinName cts, int baudrate)
		{
			if (esp != null)
				return esp;

			Gpio gpio_en = null;
			if (en != PinName.NC) {
				var en_obj = new gpio_t();
				gpio_en = (Gpio)CreateGpio(ref en_obj, en);
				gpio_en.SetDirection(PinDirection.PIN_OUTPUT);
			}

			Gpio gpio_io0 = null;
			if (io0 != PinName.NC) {
				var io0_obj = new gpio_t();
				gpio_io0 = (Gpio)CreateGpio(ref io0_obj, io0);
				gpio_io0.SetDirection(PinDirection.PIN_OUTPUT);
			}

			var serial_obj = new serial_t();
			var serial = (Serial)CreateSerial(ref serial_obj, tx, rx, rts, cts);
			serial.SetBaudRate(baudrate);

			esp = new ESP32Driver(this, gpio_en, gpio_io0, serial, debug);

			return esp;
		}

		public void graphics_create()
		{
			if (graphics != null)
				throw new InvalidOperationException();

			graphics = new Graphics();

			interfaces.Add(graphics.GetHashCode(), graphics);
		}

		public GRAPHICS graphics_init(ref drv_lcd_config_t config)
		{
			return graphics.Init(ref config);
		}

		public GRAPHICS graphics_video_init(DRV_INPUT_SEL input_sel, ref drv_video_ext_in_config_t config)
		{
			return graphics.VideoInit(input_sel, ref config);
		}

		public GRAPHICS graphics_lvds_port_init(PinName[] pins, uint pin_count)
		{
			return graphics.LvdsPortInit(pins);
		}

		public GRAPHICS graphics_read_setting(DRV_GRAPHICS layer, IntPtr framebuff,
			uint fb_stride, DRV_GRAPHICS_FORMAT gr_format, DRV_WR_RD wr_rd_swa,
			ref drv_rect_t gr_rect, byte[] clut, int clut_count)
		{
			return graphics.ReadSetting(layer, framebuff, fb_stride, gr_format, wr_rd_swa, ref gr_rect, clut, clut_count);
		}

		public GRAPHICS graphics_start(DRV_GRAPHICS layer)
		{
			return graphics.Start(layer);
		}

		public GRAPHICS graphics_stop(DRV_GRAPHICS layer)
		{
			return graphics.Stop(layer);
		}

		public GRAPHICS video_start(VIDEO_INPUT input)
		{
			return graphics.VideoStart(input);
		}

		public GRAPHICS video_stop(VIDEO_INPUT input)
		{
			return graphics.VideoStop(input);
		}

		public GRAPHICS video_write_setting(VIDEO_INPUT input, DRV_COL_SYS col_sys, IntPtr framebuff, uint fb_stride, DRV_VIDEO_FORMAT video_format, DRV_WR_RD wr_rd_swa, ushort video_write_buff_vw, ushort video_write_buff_hw, DRV_VIDEO_ADC_VINSEL video_adc_vinsel)
		{
			return graphics.VideoWriteSetting(input, col_sys, framebuff, fb_stride, video_format, wr_rd_swa, video_write_buff_vw, video_write_buff_hw, video_adc_vinsel);
		}

		public void i2s_init(ref i2s_t obj, PinName tx, PinName rx, PinName sck, PinName ws, PinName audio_clk)
		{
			CreateI2S(ref obj, tx, rx, sck, ws, audio_clk);
		}

		public void i2s_config(ref i2s_t obj, ref ssif_channel_cfg_t cfg, int max_write_num, int max_read_num)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((I2S)uif).Config(ref cfg, max_write_num, max_read_num);
		}

		public void i2s_config_channel(ref i2s_t obj, ref ssif_channel_cfg_t cfg)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			((I2S)uif).ConfigChannel(ref cfg);
		}

		public int i2s_read(ref i2s_t obj, byte[] p_data, int data_size, IntPtr p_notify_func, IntPtr p_app_data)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2S)uif).Read(p_data, data_size, p_notify_func, p_app_data);
		}

		public int i2s_write(ref i2s_t obj, byte[] p_data, int data_size, IntPtr p_notify_func, IntPtr p_app_data)
		{
			if (!interfaces.TryGetValue(obj.id, out var uif)) {
				throw new ArgumentException();
			}

			return ((I2S)uif).Write(p_data, data_size, p_notify_func, p_app_data);
		}

		public void AddDevice(DeviceType deviceType, int index, string description, string friendlyName)
		{
			switch (deviceType) {
			case DeviceType.Video:
				VideoDevices.Add(new Tuple<int, string, string>(index, description, friendlyName));
				break;
			case DeviceType.Audio:
				AudioDevices.Add(new Tuple<int, string, string>(index, description, friendlyName));
				break;
			case DeviceType.MPSSE:
				MpsseDevices.Add(new Tuple<int, string, string>(index, description, friendlyName));
				break;
			}
		}

		public void UpdateDevices()
		{
			VideoDevices.Clear();
			AudioDevices.Clear();
			MpsseDevices.Clear();
			PeachCam.EnumerateDevices();
		}

		public void ConsoleWrite(byte[] text, int len)
		{
			stdio.Stdout(text);
		}

		public void Stdin(byte[] data)
		{
			PeachCam.Stdin(data);
		}
	}
}
