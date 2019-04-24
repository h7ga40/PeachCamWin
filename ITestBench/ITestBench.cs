using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TestBench
{
	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct gpio_t
	{
		public int id;
		public IntPtr fthandle;
		public int ftpin;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct analogin_t
	{
		public int id;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct dac_t
	{
		public int id;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct pwmout_t
	{
		public int id;
	}

	[ComVisible(true)]
	public enum SerialParity
	{
		ParityNone = 0,
		ParityOdd = 1,
		ParityEven = 2,
		ParityForced1 = 3,
		ParityForced0 = 4
	}

	[ComVisible(true)]
	public enum FlowControl
	{
		FlowControlNone,
		FlowControlRTS,
		FlowControlCTS,
		FlowControlRTSCTS
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct serial_t
	{
		public int id;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct i2c_t
	{
		public int id;
		public IntPtr fthandle;
		public int ftsda;
		public int ftscl;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct spi_t
	{
		public int id;
		public IntPtr fthandle;
	}

	[ComVisible(true)]
	public enum SSIF_CFG_CKS
	{
		AUDIO_X1 = 0,
		AUDIO_CLK = 1
	}

	[ComVisible(true)]
	public enum SSIF_CFG_MULTI
	{
		CH_1 = 0,
		CH_2 = 1,
		CH_3 = 2,
		CH_4 = 3
	}

	[ComVisible(true)]
	public enum SSIF_CFG_DATA
	{
		WORD_8 = 0,
		WORD_16 = 1,
		WORD_18 = 2,
		WORD_20 = 3,
		WORD_22 = 4,
		WORD_24 = 5,
		WORD_32 = 6
	}

	[ComVisible(true)]
	public enum SSIF_CFG_SYSTEM
	{
		WORD_8 = 0,
		WORD_16 = 1,
		WORD_24 = 2,
		WORD_32 = 3,
		WORD_48 = 4,
		WORD_64 = 5,
		WORD_128 = 6,
		WORD_256 = 7
	}

	[ComVisible(true)]
	public enum SSIF_CFG : uint
	{
		FALLING = 0,
		RISING = 1,

		DATA_FIRST = 0,
		PADDING_FIRST = 1,

		LEFT = 0,
		RIGHT = 1,

		DELAY = 0,
		NO_DELAY = 1,

		DISABLE_NOISE_CANCEL = 0,
		ENABLE_NOISE_CANCEL = 1,

		DISABLE_TDM = 0,
		ENABLE_TDM = 1,

		DISABLE_ROMDEC_DIRECT = 0x0u,
		ENABLE_ROMDEC_DIRECT = 0xDEC0DEC1,
	}

	[ComVisible(true)]
	public enum SSIF_CFG_WS
	{
		LOW = 0,
		HIGH = 1
	}

	[ComVisible(true)]
	public enum SSIF_CFG_PADDING
	{
		LOW = 0,
		HIGH = 1
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ssif_chcfg_romdec_t
	{
		public uint mode;
		public IntPtr p_cbfunc;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct i2s_t
	{
		public int id;
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ssif_channel_cfg_t
	{
		public bool enabled;
		public byte int_level;
		public bool slave_mode;
		public uint sample_freq;
		public SSIF_CFG_CKS clk_select;
		public SSIF_CFG_MULTI multi_ch;
		public SSIF_CFG_DATA data_word;
		public SSIF_CFG_SYSTEM system_word;
		public SSIF_CFG bclk_pol;
		public SSIF_CFG_WS ws_pol;
		public SSIF_CFG_PADDING padding_pol;
		public SSIF_CFG serial_alignment;
		public SSIF_CFG parallel_alignment;
		public SSIF_CFG ws_delay;
		public SSIF_CFG noise_cancel;
		public SSIF_CFG tdm_mode;
		public ssif_chcfg_romdec_t romdec_direct;
	}

	[ComVisible(true)]
	public enum GRAPHICS
	{
		OK = 0,
		VDC5_ERR = -1,
		FORMA_ERR = -2,
		LAYER_ERR = -3,
		CHANNLE_ERR = -4,
		VIDEO_NTSC_SIZE_ERR = -5,
		VIDEO_PAL_SIZE_ERR = -6,
		PARAM_RANGE_ERR = -7
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct drv_lcd_config_t
	{
		public ushort h_disp_widht;
		public ushort v_disp_widht;
	}

	[ComVisible(true)]
	public enum DRV_INPUT_SEL
	{
		VDEC = 0,
		EXT = 1,
		CEU = 2
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct drv_video_ext_in_config_t
	{
		public int dummy;
	}

	[ComVisible(true)]
	public enum DRV_GRAPHICS
	{
		LAYER_0 = 0,
		LAYER_1,
		LAYER_2,
		LAYER_3
	}

	[ComVisible(true)]
	public enum DRV_GRAPHICS_FORMAT
	{
		YCBCR422 = 0,
		RGB565,
		RGB888,
		ARGB8888,
		ARGB4444,
		CLUT8,
		CLUT4,
		CLUT1
	}

	[ComVisible(true)]
	public enum DRV_WR_RD
	{
		WRSWA_NON = 0,
		WRSWA_8BIT,
		WRSWA_16BIT,
		WRSWA_16_8BIT,
		WRSWA_32BIT,
		WRSWA_32_8BIT,
		WRSWA_32_16BIT,
		WRSWA_32_16_8BIT,
	}

	[ComVisible(true), StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct drv_rect_t
	{
		public ushort vs;
		public ushort vw;
		public ushort hs;
		public ushort hw;
	}

	[ComVisible(true)]
	public enum VIDEO_INPUT
	{
		CHANNEL_0 = 0,
		CHANNEL_1
	}

	[ComVisible(true)]
	public enum DRV_COL_SYS
	{
		NTSC_358 = 0,
		NTSC_443 = 1,
		PAL_443 = 2,
		PAL_M = 3,
		PAL_N = 4,
		SECAM = 5,
		NTSC_443_60 = 6,
		PAL_60 = 7,
	}

	[ComVisible(true)]
	public enum DRV_VIDEO_FORMAT
	{
		YCBCR422 = 0,
		RGB565,
		RGB888
	}

	[ComVisible(true)]
	public enum DRV_VIDEO_ADC_VINSEL
	{
		VIN1 = 0,
		VIN2
	}

	[ComVisible(true)]
	public interface IESP32
	{
		int get_free_id();

		bool open([In, MarshalAs(UnmanagedType.LPStr)]string type, int id, [In, MarshalAs(UnmanagedType.LPStr)]string addr, int port, int opt = 0);
		bool close(int id, bool accept_id);
		bool accept(out int p_id);
		bool send(int id, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] data, uint len);
		bool recv(int id, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] data, uint len);
		void socket_attach(int id, IntPtr callback, IntPtr data);
		bool socket_setopt_i(int id, [In, MarshalAs(UnmanagedType.LPStr)]string optname, int optval);
		bool socket_setopt_s(int id, [In, MarshalAs(UnmanagedType.LPStr)]string optname, [In, MarshalAs(UnmanagedType.LPStr)]string optval);
		bool socket_getopt_i(int id, [In, MarshalAs(UnmanagedType.LPStr)]string optname, out int optval);
		bool socket_getopt_s(int id, [In, MarshalAs(UnmanagedType.LPStr)]string optname, [Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 3)]StringBuilder optval, int optlen);

		bool cre_server(ushort portno);
		bool del_server();

		void attach_wifi_status(IntPtr callback);

		bool dhcp(bool enabled, int mode);
		bool set_network([In, MarshalAs(UnmanagedType.LPStr)]string ip_address, [In, MarshalAs(UnmanagedType.LPStr)]string netmask, [In, MarshalAs(UnmanagedType.LPStr)]string gateway);
		bool connect([In, MarshalAs(UnmanagedType.LPStr)]string ap, [In, MarshalAs(UnmanagedType.LPStr)]string passPhrase);
		bool disconnect();
		bool getMACAddress([Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 1)]string buf, int len);
		bool getIPAddress([Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 1)]string buf, int len);
		bool getGateway([Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 1)]string buf, int len);
		bool getNetmask([Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 1)]string buf, int len);
		byte getRSSI();
		void scan(IntPtr callback, IntPtr usrdata);
		bool ntp(bool enabled, int timezone, [In, MarshalAs(UnmanagedType.LPStr)]string server0, [In, MarshalAs(UnmanagedType.LPStr)]string server1, [In, MarshalAs(UnmanagedType.LPStr)]string server2);
		long ntp_time();
		bool esp_time(out int sec, out int usec);
		int ping([In, MarshalAs(UnmanagedType.LPStr)]string addr);
		bool mdns(bool enabled, [In, MarshalAs(UnmanagedType.LPStr)]string hostname, [In, MarshalAs(UnmanagedType.LPStr)]string service, ushort portno);
		bool mdns_query([In, MarshalAs(UnmanagedType.LPStr)]string hostname, [Out, MarshalAs(UnmanagedType.LPStr, SizeParamIndex = 2)]string addr, int len);
		bool sleep(bool enebled);
	}

	[ComVisible(true)]
	public enum ESP32
	{
		WIFIMODE_STATION = 1,
		WIFIMODE_SOFTAP = 2,
		WIFIMODE_STATION_SOFTAP = 3,
		SOCKET_COUNT = 5,

		STATUS_DISCONNECTED = 0,
		STATUS_CONNECTED = 1,
		STATUS_GOT_IP = 2,
	}

	[ComVisible(true)]
	public enum DeviceType
	{
		Video,
		Audio,
		MPSSE,
	}

	[ComVisible(true)]
	public interface ITestBench
	{
		void gpio_init(ref gpio_t obj, PinName pin);
		void gpio_mode([In]ref gpio_t obj, PinMode mode);
		void gpio_dir([In]ref gpio_t obj, PinDirection direction);

		void gpio_write([In]ref gpio_t obj, int value);
		int gpio_read([In]ref gpio_t obj);

		void gpio_init_in(ref gpio_t obj, PinName pin);
		void gpio_init_out(ref gpio_t obj, PinName pin);

		void analogout_init(ref dac_t obj, PinName pin);
		void analogout_write_u16([In]ref dac_t obj, ushort value);

		void pin_function(PinName pin, int function);
		void pin_mode(PinName pin, PinMode mode);

		void analogin_init(ref analogin_t obj, PinName pin);
		float analogin_read([In]ref analogin_t obj);
		ushort analogin_read_u16([In]ref analogin_t obj);

		void pwmout_init(ref pwmout_t obj, PinName pin);
		void pwmout_free([In]ref pwmout_t obj);
		void pwmout_period_us([In]ref pwmout_t obj, int us);
		void pwmout_pulsewidth_us([In]ref pwmout_t obj, int us);

		void serial_init(ref serial_t obj, PinName tx, PinName rx);
		void serial_free([In]ref serial_t obj);
		void serial_baud([In]ref serial_t obj, int baudrate);
		void serial_format([In]ref serial_t obj, int data_bits, SerialParity parity, int stop_bits);
		int serial_getc([In]ref serial_t obj);
		void serial_putc([In]ref serial_t obj, int c);
		int serial_readable([In]ref serial_t obj);
		int serial_writable([In]ref serial_t obj);

		void i2c_init(ref i2c_t obj, PinName sda, PinName scl);
		void i2c_frequency([In]ref i2c_t obj, int hz);
		int i2c_start([In]ref i2c_t obj);
		int i2c_stop([In]ref i2c_t obj);
		int i2c_read([In]ref i2c_t obj, int address,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] data, int length, int repeated);
		int i2c_write([In]ref i2c_t obj, int address,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] data, int length, int repeated);
		int i2c_byte_read([In]ref i2c_t obj, int ack);
		int i2c_byte_write([In]ref i2c_t obj, int data);

		void spi_init(ref spi_t obj, PinName mosi, PinName miso, PinName sclk, PinName ssel);
		void spi_free([In]ref spi_t obj);
		void spi_format([In]ref spi_t obj, int bits, int mode, int slave);
		void spi_frequency([In]ref spi_t obj, int hz);
		int spi_master_write([In]ref spi_t obj, int value);
		int spi_master_block_write([In]ref spi_t obj,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] tx_buffer, int tx_length,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]byte[] rx_buffer, int rx_length, byte write_fill);

		void rtc_init();
		void rtc_free();
		int rtc_isenabled();
		long rtc_read();
		void rtc_write(long t);

		void us_ticker_init();
		long us_ticker_read();
		void us_ticker_set_interrupt(long timestamp);
		void us_ticker_disable_interrupt();
		void us_ticker_clear_interrupt();

		void wait_ms(int ms);
		void wait_us(int us);

		IESP32 esp32_init(PinName en, PinName io0, PinName tx, PinName rx, bool debug,
			PinName rts, PinName cts, int baudrate);

		void graphics_create();
		GRAPHICS graphics_init([In]ref drv_lcd_config_t config);
		GRAPHICS graphics_video_init(DRV_INPUT_SEL input_sel, [In]ref drv_video_ext_in_config_t config);
		GRAPHICS graphics_lvds_port_init([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]PinName[] pins, uint pin_count);
		GRAPHICS graphics_read_setting(DRV_GRAPHICS layer, IntPtr framebuff, uint fb_stride, DRV_GRAPHICS_FORMAT gr_format,
			DRV_WR_RD wr_rd_swa, [In]ref drv_rect_t gr_rect,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)]byte[] clut, int clut_count);
		GRAPHICS graphics_start(DRV_GRAPHICS layer);
		GRAPHICS graphics_stop(DRV_GRAPHICS layer);
		GRAPHICS video_start(VIDEO_INPUT input);
		GRAPHICS video_stop(VIDEO_INPUT input);
		GRAPHICS video_write_setting(VIDEO_INPUT input, DRV_COL_SYS col_sys, IntPtr framebuff, uint fb_stride,
			DRV_VIDEO_FORMAT video_format, DRV_WR_RD wr_rd_swa, ushort video_write_buff_vw, ushort video_write_buff_hw, DRV_VIDEO_ADC_VINSEL video_adc_vinsel);

		void i2s_init(ref i2s_t obj, PinName tx, PinName rx, PinName sck, PinName ws, PinName audio_clk);
		void i2s_config([In]ref i2s_t obj, [In]ref ssif_channel_cfg_t cfg, int max_write_num, int max_read_num);
		void i2s_config_channel([In]ref i2s_t obj, [In]ref ssif_channel_cfg_t cfg);
		int i2s_read([In]ref i2s_t obj,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] p_data, int data_size, IntPtr p_notify_func, IntPtr p_app_data);
		int i2s_write([In]ref i2s_t obj,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] p_data, int data_size, IntPtr p_notify_func, IntPtr p_app_data);

		void AddDevice(DeviceType deviceType, int index, [MarshalAs(UnmanagedType.BStr)]string description, [MarshalAs(UnmanagedType.BStr)]string friendlyName);

		void ConsoleWrite([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]byte[] text, int len);
	}
}
