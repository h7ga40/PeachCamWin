using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TestBench
{
	class GraphicLayer
	{
		IntPtr framebuff;
		uint fb_stride;
		DRV_GRAPHICS_FORMAT gr_format;
		DRV_WR_RD wr_rd_swa;
		drv_rect_t gr_rect;
		byte[] clut;
		bool started;
		internal bool debug;

		Bitmap bitmap;

		internal GRAPHICS ReadSetting(IntPtr framebuff, uint fb_stride, DRV_GRAPHICS_FORMAT gr_format, DRV_WR_RD wr_rd_swa, ref drv_rect_t gr_rect, byte[] clut, int clut_count)
		{
			this.framebuff = framebuff;
			this.fb_stride = fb_stride;
			this.gr_format = gr_format;
			this.wr_rd_swa = wr_rd_swa;
			this.gr_rect = gr_rect;
			this.clut = clut;

			bitmap = new Bitmap(gr_rect.hw - gr_rect.hs, gr_rect.vw - gr_rect.vs, PixelFormat.Format32bppArgb);
			using (var canvas = System.Drawing.Graphics.FromImage(bitmap)) {
				canvas.Clear(Color.FromArgb(255, Color.White));
			}

			return GRAPHICS.OK;
		}

		internal GRAPHICS Start()
		{
			started = true;
			return GRAPHICS.OK;
		}

		internal GRAPHICS Stop()
		{
			started = false;
			return GRAPHICS.OK;
		}

		internal void Paint(System.Drawing.Graphics canvas)
		{
			if (!started && !debug)
				return;
			if (bitmap == null)
				return;

			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
			try {
				switch (gr_format) {
				case DRV_GRAPHICS_FORMAT.YCBCR422:
					PeachCam.ARGB8888FromYCBCR422(data.Scan0, framebuff, data.Width, data.Height);
					break;
				case DRV_GRAPHICS_FORMAT.ARGB4444:
					PeachCam.ARGB8888FromARGB4444(data.Scan0, framebuff, data.Width, data.Height);
					break;
				case DRV_GRAPHICS_FORMAT.ARGB8888:
					Marshal.Copy(new IntPtr[] { framebuff }, 0, data.Scan0, 4 * data.Width * data.Height);
					break;
				}
			}
			finally {
				bitmap.UnlockBits(data);
			}

			canvas.DrawImage(bitmap, 0, 0);
		}
	}

	public class Graphics : IUnitInterface
	{
		drv_lcd_config_t config;
		DRV_INPUT_SEL input_sel;
		drv_video_ext_in_config_t vidio_config;
		List<PinName> pins = new List<PinName>();
		Bitmap bitmap;
		GraphicLayer[] layers = new GraphicLayer[4];
		VideoCapture video;

		PinName[] lvds_pin = {
			PinName.P5_7, PinName.P5_6, PinName.P5_5, PinName.P5_4, PinName.P5_3, PinName.P5_2, PinName.P5_1, PinName.P5_0
		};

		public Graphics()
		{
			layers[0] = new GraphicLayer();
			layers[0].debug = true;
			layers[1] = null;
			layers[2] = new GraphicLayer();
			layers[3] = new GraphicLayer();
			layers[2].debug = true;
		}

		public string TypeName => "Graphics";

		public string InterfaceName => "Graphics";

		public int VideoCaptureDeviceIndex { get; set; } = -1;

		internal GRAPHICS Init(ref drv_lcd_config_t config)
		{
			this.config = config;

			bitmap = new Bitmap(config.h_disp_widht, config.v_disp_widht, PixelFormat.Format24bppRgb);

			return GRAPHICS.OK;
		}

		internal GRAPHICS VideoInit(DRV_INPUT_SEL input_sel, ref drv_video_ext_in_config_t config)
		{
			this.input_sel = input_sel;
			vidio_config = config;
			return GRAPHICS.OK;
		}

		internal GRAPHICS LvdsPortInit(PinName[] pins)
		{
			foreach (var p in pins) {
				if (!lvds_pin.Contains(p))
					throw new ArgumentException();
			}

			this.pins.AddRange(pins);
			return GRAPHICS.OK;
		}

		internal GRAPHICS ReadSetting(DRV_GRAPHICS layer, IntPtr framebuff, uint fb_stride, DRV_GRAPHICS_FORMAT gr_format, DRV_WR_RD wr_rd_swa, ref drv_rect_t gr_rect, byte[] clut, int clut_count)
		{
			return layers[(int)layer].ReadSetting(framebuff, fb_stride, gr_format, wr_rd_swa, ref gr_rect, clut, clut_count);
		}

		internal GRAPHICS Start(DRV_GRAPHICS layer)
		{
			return layers[(int)layer].Start();
		}

		internal GRAPHICS Stop(DRV_GRAPHICS layer)
		{
			return layers[(int)layer].Stop();
		}

		internal GRAPHICS VideoStart(VIDEO_INPUT input)
		{
			if ((video == null) && (VideoCaptureDeviceIndex >= 0)) {
				video = new VideoCapture();
				video.Open(VideoCaptureDeviceIndex);
			}
			return GRAPHICS.OK;
		}

		internal GRAPHICS VideoStop(VIDEO_INPUT input)
		{
			if (video != null) {
				//video.Release();
				//video.Dispose();
				//video = null;
			}
			return GRAPHICS.OK;
		}

		IntPtr framebuff;

		internal GRAPHICS VideoWriteSetting(VIDEO_INPUT input, DRV_COL_SYS col_sys, IntPtr framebuff, uint fb_stride, DRV_VIDEO_FORMAT video_format, DRV_WR_RD wr_rd_swa, ushort video_write_buff_vw, ushort video_write_buff_hw, DRV_VIDEO_ADC_VINSEL video_adc_vinsel)
		{
			this.framebuff = framebuff;
			return GRAPHICS.OK;
		}

		internal Image GetImage()
		{
			if (bitmap == null)
				return null;

			if (video != null) {
				video.Capture(framebuff, bitmap.Width, bitmap.Height);
			}

			using (var canvas = System.Drawing.Graphics.FromImage(bitmap)) {
				canvas.Clear(Color.FromArgb(255, Color.Black));

				foreach (var l in layers) {
					if (l == null)
						continue;
					l.Paint(canvas);
				}

				return bitmap;
			}
		}
	}
}
