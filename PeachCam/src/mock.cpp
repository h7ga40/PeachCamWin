#include "mbed.h"
#include "DisplayBase.h"
#include "JPEG_Converter.h"
#include "LEPTON_SYS.h"
#include "opencv.hpp"
#include "SocketInterface.h"
#include "http_request.h"
#include "SdUsbConnect.h"
#include "TLV320_RBSP.h"
#include "EasyAttach_CameraAndLCD.h"

DisplayBase::DisplayBase()
{
	TestBench->graphics_create();
}

DisplayBase::graphics_error_t DisplayBase::Graphics_init(struct DisplayBase::lcd_config_t const *pconfig)
{
	struct drv_lcd_config_t config = { 0 };
	if (pconfig != NULL) {
		config.h_disp_widht = pconfig->h_disp_widht;
		config.v_disp_widht = pconfig->v_disp_widht;
	}
	return (graphics_error_t)TestBench->graphics_init(&config);
}

DisplayBase::graphics_error_t DisplayBase::Graphics_Video_init(enum DisplayBase::video_input_sel_t input_sel, struct DisplayBase::video_ext_in_config_t *)
{
	drv_video_ext_in_config_t config = { 0 };
	return (graphics_error_t)TestBench->graphics_video_init((DRV_INPUT_SEL)input_sel, &config);
}

DisplayBase::graphics_error_t DisplayBase::Graphics_Lvds_Port_Init(enum PinName *pins, unsigned int pin_count)
{
	return (graphics_error_t)TestBench->graphics_lvds_port_init(pins, pin_count);
}

DisplayBase::graphics_error_t DisplayBase::Graphics_Read_Setting(DisplayBase::graphics_layer_t layer,
	void *framebuff, unsigned int fb_stride, DisplayBase::graphics_format_t gr_format,
	DisplayBase::wr_rd_swa_t wr_rd_swa, DisplayBase::rect_t *gr_rect, DisplayBase::clut_t *gr_clut)
{
	if (gr_clut != NULL) {
		return (graphics_error_t)TestBench->graphics_read_setting((DRV_GRAPHICS)layer, (intptr_t)framebuff,
			fb_stride, (DRV_GRAPHICS_FORMAT)gr_format, (DRV_WR_RD)wr_rd_swa, (drv_rect_t *)gr_rect,
			(uint8_t *)gr_clut->clut, gr_clut->color_num);
	}
	else {
		return (graphics_error_t)TestBench->graphics_read_setting((DRV_GRAPHICS)layer, (intptr_t)framebuff,
			fb_stride, (DRV_GRAPHICS_FORMAT)gr_format, (DRV_WR_RD)wr_rd_swa, (drv_rect_t *)gr_rect,
			NULL, 0);
	}
}

DisplayBase::graphics_error_t DisplayBase::Graphics_Start(DisplayBase::graphics_layer_t layer)
{
	return (graphics_error_t)TestBench->graphics_start((DRV_GRAPHICS)layer);
}

DisplayBase::graphics_error_t DisplayBase::Graphics_Stop(DisplayBase::graphics_layer_t layer)
{
	return (graphics_error_t)TestBench->graphics_stop((DRV_GRAPHICS)layer);
}

DisplayBase::graphics_error_t DisplayBase::Video_Start(DisplayBase::video_input_channel_t ch)
{
	return (graphics_error_t)TestBench->video_start((VIDEO_INPUT)ch);
}

DisplayBase::graphics_error_t DisplayBase::Video_Stop(DisplayBase::video_input_channel_t ch)
{
	return (graphics_error_t)TestBench->video_stop((VIDEO_INPUT)ch);
}

DisplayBase::graphics_error_t DisplayBase::Video_Write_Setting(
	DisplayBase::video_input_channel_t input, DisplayBase::graphics_video_col_sys_t col_sys,
	void *framebuff, unsigned int fb_stride, DisplayBase::video_format_t video_format,
	DisplayBase::wr_rd_swa_t wr_rd_swa, unsigned short video_write_buff_vw,
	unsigned short video_write_buff_hw, DisplayBase::video_adc_vinsel_t video_adc_vinsel)
{
	return (graphics_error_t)TestBench->video_write_setting((VIDEO_INPUT)input,
		(DRV_COL_SYS)col_sys, (intptr_t)framebuff, fb_stride, (DRV_VIDEO_FORMAT)video_format,
		(DRV_WR_RD)wr_rd_swa, video_write_buff_vw, video_write_buff_hw,
		(DRV_VIDEO_ADC_VINSEL)video_adc_vinsel);
}

R_BSP_Ssif::R_BSP_Ssif(PinName sck, PinName ws, PinName tx, PinName rx, PinName audio_clk)
{
	TestBench->i2s_init(&_i2s, tx, rx, sck, ws, audio_clk);
}

void R_BSP_Ssif::init(ssif_channel_cfg_t *cfg, int32_t max_write_num, int32_t max_read_num)
{
	TestBench->i2s_config(&_i2s, cfg, max_write_num, max_read_num);
}

void R_BSP_Ssif::ConfigChannel(ssif_channel_cfg_t *cfg)
{
	TestBench->i2s_config_channel(&_i2s, cfg);
}

int32_t R_BSP_Ssif::read(void * const p_data, uint32_t data_size, const rbsp_data_conf_t * const p_data_conf)
{
	return TestBench->i2s_read(&_i2s, (uint8_t *)p_data, data_size, (intptr_t)p_data_conf->p_notify_func, (intptr_t)p_data_conf->p_app_data);
}

int32_t R_BSP_Ssif::write(void * const p_data, uint32_t data_size, const rbsp_data_conf_t * const p_data_conf)
{
	return TestBench->i2s_write(&_i2s, (uint8_t *)p_data, data_size, (intptr_t)p_data_conf->p_notify_func, (intptr_t)p_data_conf->p_app_data);
}

LEP_RESULT LEP_GetSysFlirSerialNumber(LEP_CAMERA_PORT_DESC_T_TAG *, unsigned __int64 *)
{
	return LEP_OK;
}

LEP_RESULT LEP_GetSysTelemetryEnableState(LEP_CAMERA_PORT_DESC_T_TAG *, LEP_SYS_TELEMETRY_ENABLE_STATE_E_TAG *)
{
	return LEP_OK;
}

LEP_RESULT LEP_OpenPort(void *, LEP_CAMERA_PORT_E_TAG, unsigned short, LEP_CAMERA_PORT_DESC_T_TAG *)
{
	return LEP_OK;
}

SdUsbConnect::SdUsbConnect(const char *name)
{
}

void SdUsbConnect::wait_connect(void)
{
}

int mkdir(char const *name, mode_t mode)
{
	return 0;
}

int set_time(time_t tm)
{
	return 0;
}

void mbed_die(void)
{
}
