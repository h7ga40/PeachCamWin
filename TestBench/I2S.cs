using System;

namespace TestBench
{
	public class I2S : IUnitInterface
	{
		I2SName i2s;
		internal PinName tx;
		internal PinName rx;
		internal PinName sck;
		internal PinName ws;
		internal PinName audio_clk;
		ssif_channel_cfg_t cfg;
		int max_write_num;
		int max_read_num;

		public I2S(I2SName i2s, PinName tx, PinName rx, PinName sck, PinName ws, PinName audio_clk)
		{
			this.i2s = i2s;
			this.tx = tx;
			this.rx = rx;
			this.sck = sck;
			this.ws = ws;
			this.audio_clk = audio_clk;
		}

		public string TypeName => "I2S";

		public string InterfaceName => i2s.ToString();

		internal void Config(ref ssif_channel_cfg_t cfg, int max_write_num, int max_read_num)
		{
			this.cfg = cfg;
			this.max_write_num = max_write_num;
			this.max_read_num = max_read_num;
		}

		internal void ConfigChannel(ref ssif_channel_cfg_t cfg)
		{
			this.cfg = cfg;
		}

		internal int Read(byte[] p_data, int data_size, IntPtr p_notify_func, IntPtr p_app_data)
		{
			return data_size;
		}

		internal int Write(byte[] p_data, int data_size, IntPtr p_notify_func, IntPtr p_app_data)
		{
			return data_size;
		}
	}
}
