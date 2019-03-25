#include "mbed.h"
#include "JPEG_Converter.h"
#include "jpeglib.h"

JPEG_Converter::JPEG_Converter()
{
}

JPEG_Converter::~JPEG_Converter()
{
}

JPEG_Converter::jpeg_conv_error_t JPEG_Converter::SetQuality(const uint8_t qual)
{
	this->qual = qual;
	return JPEG_Converter::JPEG_CONV_OK;
}


JPEG_Converter::jpeg_conv_error_t JPEG_Converter::decode(void* pJpegBuff,
	bitmap_buff_info_t* psOutputBuff, decode_options_t* pOptions )
{
	JSAMPROW lineBuffer[1] = { 0 };

	struct jpeg_decompress_struct cinfo;
	struct jpeg_error_mgr jerr;
	cinfo.err = jpeg_std_error(&jerr);
	jpeg_create_decompress(&cinfo);

	unsigned long readSize;

	cinfo.image_width = psOutputBuff->width;
	cinfo.image_height = psOutputBuff->height;
	switch (psOutputBuff->format) {
	case JPEG_Converter::WR_RD_YCbCr422:
		cinfo.output_components = 2;
		cinfo.out_color_space = /*JCS_YCbCr*/JCS_RGB;
		readSize = cinfo.image_width * cinfo.image_height * cinfo.output_components;
		break;
	case JPEG_Converter::WR_RD_ARGB8888:
		cinfo.output_components = 4;
		cinfo.out_color_space = JCS_RGB;
		break;
	default:
		cinfo.output_components = 2;
		cinfo.out_color_space = JCS_RGB;
		break;
	}
	jpeg_mem_src(&cinfo, (unsigned char *)pJpegBuff, readSize);
	jpeg_start_decompress(&cinfo);
	uint8_t *imgRGB = (uint8_t *)psOutputBuff->buffer_address;
	for (uint32_t y = 0; y < cinfo.image_height; y++) {
		lineBuffer[0] = (JSAMPROW)imgRGB;
		imgRGB += cinfo.image_width * cinfo.output_components;
		jpeg_read_scanlines(&cinfo, lineBuffer, 1);
	}
	jpeg_finish_decompress(&cinfo);
	jpeg_destroy_decompress(&cinfo);

	return JPEG_Converter::JPEG_CONV_OK;
}

JPEG_Converter::jpeg_conv_error_t JPEG_Converter::encode(bitmap_buff_info_t *psInputBuff,
	void *pJpegBuff, size_t *pEncodeSize, encode_options_t *pOptions)
{
	JSAMPROW lineBuffer[1] = { 0 };

	struct jpeg_compress_struct cinfo;
	struct jpeg_error_mgr jerr;
	cinfo.err = jpeg_std_error(&jerr);
	jpeg_create_compress(&cinfo);

	unsigned long writtenSize = *pEncodeSize;
	jpeg_mem_dest(&cinfo, (unsigned char **)&pJpegBuff, &writtenSize);
	*pEncodeSize = writtenSize;

	cinfo.image_width = psInputBuff->width;
	cinfo.image_height = psInputBuff->height;
	switch (psInputBuff->format) {
	case JPEG_Converter::WR_RD_YCbCr422:
		cinfo.input_components = 2;
		cinfo.in_color_space = /*JCS_YCbCr*/JCS_RGB;
		break;
	case JPEG_Converter::WR_RD_ARGB8888:
		cinfo.input_components = 4;
		cinfo.in_color_space = JCS_RGB;
		break;
	default:
		cinfo.input_components = 2;
		cinfo.in_color_space = JCS_RGB;
		break;
	}
	jpeg_set_defaults(&cinfo);
	jpeg_set_quality(&cinfo, qual, TRUE);
	jpeg_start_compress(&cinfo, TRUE);
	uint8_t *imgRGB = (uint8_t *)psInputBuff->buffer_address;
	for (uint32_t y = 0; y < cinfo.image_height; y++) {
		lineBuffer[0] = (JSAMPROW)imgRGB;
		imgRGB += cinfo.image_width * cinfo.input_components;
		jpeg_write_scanlines(&cinfo, lineBuffer, 1);
	}
	jpeg_finish_compress(&cinfo);
	jpeg_destroy_compress(&cinfo);

	return JPEG_Converter::JPEG_CONV_OK;
}
