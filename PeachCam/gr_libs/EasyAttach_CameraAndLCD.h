#ifndef _EASYATTACH_CAMERAANDLCD_H_
#define _EASYATTACH_CAMERAANDLCD_H_

#include "LCD_shield_config_4_3inch.h"
#include "DisplayBase.h"

extern DisplayBase::graphics_error_t EasyAttach_Init(DisplayBase& Display, uint16_t cap_width = 0, uint16_t cap_height = 0);
extern void EasyAttach_LcdBacklight(float value);
extern void EasyAttach_LcdBacklight(bool type);
extern DisplayBase::graphics_error_t EasyAttach_CameraStart(DisplayBase& Display, DisplayBase::video_input_channel_t channel);

#endif // _EASYATTACH_CAMERAANDLCD_H_
