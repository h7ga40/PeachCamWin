// PeachCam.cpp : DLL アプリケーション用にエクスポートされる関数を定義します。
//

#include "mbed.h"
#include "opencv2/imgproc/hal/hal.hpp"
#include "opencv2/imgproc.hpp"
#include "opencv2/videoio.hpp"
#include "libMPSSE_spi.h"
#include "ntshell.h"
#include "ntopt.h"
#include <queue>

CRITICAL_SECTION hCs;

void core_util_critical_section_enter()
{
	EnterCriticalSection(&hCs);
}

void core_util_critical_section_exit()
{
	LeaveCriticalSection(&hCs);
}

static ticker_data_t ticker_data;
static LARGE_INTEGER ticker_count;

const ticker_data_t * get_us_ticker_data(void)
{
	core_util_critical_section_enter();
	QueryPerformanceFrequency(&ticker_data.frequency);
	core_util_critical_section_exit();
	return &ticker_data;
}

us_timestamp_t ticker_read_us(const ticker_data_t *const ticker_data)
{
	core_util_critical_section_enter();
	QueryPerformanceCounter(&ticker_count);
	core_util_critical_section_exit();
	return (1000000ll * ticker_count.QuadPart) / ticker_data->frequency.QuadPart;
}

uint32_t osKernelGetTickCount(void)
{
	return (uint32_t)ticker_read_us(get_us_ticker_data());
}

osEventFlagsId osEventFlagsNew(osEventFlagsAttr_t *attr)
{
	return new EventFlags();
}

void osEventFlagsDelete(osEventFlagsId id)
{
	delete (EventFlags *)id;
}

uint32_t osEventFlagsSet(osEventFlagsId id, uint32_t flags)
{
	EventFlags *evf = (EventFlags *)id;

	return evf->set(flags);
}

uint32_t osEventFlagsClear(osEventFlagsId id, uint32_t flags)
{
	EventFlags *evf = (EventFlags *)id;

	return evf->clear(flags);
}

uint32_t osEventFlagsWait(osEventFlagsId id, uint32_t flags, uint32_t mode, uint32_t timeout)
{
	EventFlags *evf = (EventFlags *)id;
	bool clear = (mode & osFlagsNoClear) == 0;
	if ((mode & osFlagsWaitAll) != 0)
		return evf->wait_all(flags, timeout, clear);
	else
		return evf->wait_any(flags, timeout, clear);
}

osMutexId osMutexNew(osMutexAttr_t *attr)
{
	return new Mutex();
}

void osMutexDelete(osMutexId id)
{
	delete (Mutex *)id;
}

osStatus_t osMutexAcquire(osMutexId id, uint32_t timeout)
{
	Mutex  *mtx = (Mutex  *)id;

	return mtx->Acquire(timeout);
}

osStatus_t osMutexRelease(osMutexId id)
{
	Mutex  *mtx = (Mutex  *)id;

	return mtx->Release();
}

osMutexId singleton_mutex_id;

int main();

void main_task()
{
	main();
}

#define SHELL_STDIN 1

std::queue<char> stdin_queue;

int uart_read(char *buf, int cnt, void *extobj)
{
	int result = 0;
	Thread *thread = (Thread *)extobj;

	core_util_critical_section_enter();

	for (result; result < cnt; result++) {
		while (stdin_queue.size() == 0) {
			core_util_critical_section_exit();

			thread->signal_wait(0);
			thread->signal_clr(SHELL_STDIN);

			core_util_critical_section_enter();
		}

		*buf = stdin_queue.front();
		stdin_queue.pop();
	}

	core_util_critical_section_exit();

	return result;
}

int uart_write(const char *buf, int cnt, void *extobj)
{
	TestBench->ConsoleWrite((uint8_t *)buf, cnt);
	return cnt;
}

typedef int(*USRCMDFUNC)(int argc, char **argv);

typedef struct
{
	const char *cmd;
	const char *desc;
	USRCMDFUNC func;
} cmd_table_t;

typedef struct
{
	const cmd_table_t *table;
	int count;
} cmd_table_info_t;

extern "C" int usrcmd_lpt(int argc, char **argv);

static const cmd_table_t cmdlist[] = {
	{"lpt", "FLIR Lepton cotrol", usrcmd_lpt },
};
cmd_table_info_t cmd_table_info = { cmdlist, sizeof(cmdlist) / sizeof(cmdlist[0]) };

int usrcmd_ntopt_callback(int argc, char **argv, void *extobj)
{
	const cmd_table_t *p = cmd_table_info.table;
	int result = 0;
	int found = 0;

	if (argc == 0)
		return result;

	for (int i = 0; i < cmd_table_info.count; i++) {
		if (strcmp(*argv, p->cmd) == 0) {
			found = 1;
			result = p->func(argc, argv);
			break;
		}
		p++;
	}

	if ((found == 0) && (*argv[0] != '\0'))
		printf("Unknown command found.\n");

	return result;
}

int cmd_execute(const char *text, void *extobj)
{
	return ntopt_parse(text, usrcmd_ntopt_callback, extobj);
}

ntshell_t ntshell;
Thread mainThread(osPriorityNormal, 0, NULL, "mainThread");
Thread shelThread(osPriorityNormal, 0, NULL, "shelThread");

void shell_task()
{
	ntshell_init(&ntshell, uart_read, uart_write, cmd_execute, &shelThread);
	ntshell_set_prompt(&ntshell, "NTShell>");
	ntshell_execute(&ntshell);
}

extern "C" __declspec(dllexport) void Start(void)
{
	osMutexAttr_t attr;

	InitializeCriticalSection(&hCs);

	singleton_mutex_id = osMutexNew(&attr);

	mainThread.start(main_task);
	shelThread.start(shell_task);
}

extern "C" __declspec(dllexport) void Stop(void)
{
	shelThread.terminate();
	mainThread.terminate();

	osMutexDelete(singleton_mutex_id);

	DeleteCriticalSection(&hCs);
}

extern "C" __declspec(dllexport) void ARGB8888FromARGB4444(uint32_t *dst, const uint16_t *src, int width, int height)
{
	for (int j = 0; j < height; j++) {
		for (int i = 0; i < width; i++) {
			uint16_t tmp1 = *src++;
			uint16_t a = (tmp1 & 0xF000) >> 12;
			uint16_t r = (tmp1 & 0x0F00) >> 8;
			uint16_t g = (tmp1 & 0x00F0) >> 4;
			uint16_t b = (tmp1 & 0x000F);
			*dst++ = (a << 28) | (a << 24) | (r << 20) | (r << 16) | (g << 12) | (g << 8) | (b << 4) | (b << 0);
		}
	}
}

extern "C" __declspec(dllexport) void ARGB8888FromRGB565(uint32_t *dst, const uint16_t *src, int width, int height)
{
	for (int j = 0; j < height; j++) {
		for (int i = 0; i < width; i++) {
			uint16_t tmp1 = *src++;
			uint16_t r = (tmp1 & 0xF800) >> 11;
			uint16_t g = (tmp1 & 0x07E0) >> 5;
			uint16_t b = (tmp1 & 0x001F);
			r = (r << 3) | (r >> 2);
			g = (g << 2) | (g >> 4);
			b = (b << 3) | (b >> 2);
			*dst++ = 0xFF000000 | (r << 16) | (g << 8) | (b << 0);
		}
	}
}

#define _DEBUG_TEST

extern "C" __declspec(dllexport) void ARGB8888FromYCBCR422(uint32_t *dst, const uint16_t *src, int width, int height)
{
#ifndef _DEBUG_TEST
	//cv::hal::cvtOnePlaneYUVtoBGR((const uchar *)src, 2 * width, (uchar *)dst, 4 * width, width, height, 4, false, 0, 0);
	cv::Mat srcMat(width, height, CV_16UC1, (void *)src);
	cv::Mat dstMat(width, height, CV_8UC4, (void *)dst);
	cv::cvtColor(srcMat, dstMat, cv::COLOR_BGR2YCrCb);
#else
	ARGB8888FromRGB565(dst, src, width, height);
#endif
}

extern "C" __declspec(dllexport) void *videoio_VideoCapture_new1()
{
	return new cv::VideoCapture();
}

extern "C" __declspec(dllexport) bool videoio_VideoCapture_open2(void *ptr, int index)
{
	return ((cv::VideoCapture *)ptr)->open(index);
}

extern "C" __declspec(dllexport) void videoio_VideoCapture_release(void *ptr)
{
	((cv::VideoCapture *)ptr)->release();
}

extern "C" __declspec(dllexport) void videoio_VideoCapture_delete(void *ptr)
{
	delete (cv::VideoCapture *)ptr;
}

extern "C" __declspec(dllexport) bool VideoCapture_Capture(void *ptr, void *_dst, int width, int height)
{
	cv::VideoCapture *vc = (cv::VideoCapture *)ptr;

	cv::Mat image;
	if (!vc->read(image)) {
		return false;
	}

	//cv::Mat rszimg(width, height, image.type(), _dst);
	//cv::resize(image, dstMat, cv::Size(width, height),
	//	(double)width / (double)image.rows,
	//	(double)height / (double)image.cols);
	cv::Mat rszimg(width, height, image.type());
	cv::resize(image, rszimg, cv::Size(width, height));

	//memcpy(dst, rszimg.data, width * height * 2);
	if (rszimg.type() == CV_8UC3) {
#ifndef _DEBUG_TEST
		//cv::hal::cvtBGRtoYUV((uchar *)rszimg.data, 3 * width, (uchar *)_dst, 2 * width, width, height, CV_MAT_DEPTH(rszimg.type()), CV_MAT_CN(rszimg.type()), false, true);
		cv::Mat dstMat(width, height, CV_16UC1, _dst);
		cv::cvtColor(rszimg, dstMat, cv::COLOR_BGR2YCrCb);
#else
		uint16_t *dst = (uint16_t *)_dst;
		uint8_t *src = (uint8_t *)rszimg.data;
		for (int j = 0; j < height; j++) {
			for (int i = 0; i < width; i++) {
				uint16_t b = (*src++) >> (8 - 5);
				uint16_t g = (*src++) >> (8 - 6);
				uint16_t r = (*src++) >> (8 - 5);
				*dst++ = (r << 11) | (g << 5) | (b << 0);
			}
		}
#endif
	}

	//cv::Mat dstMat(width, height, CV_16UC1, _dst);
	//image.convertTo(dstMat, image.type());

	return true;
}

#include <mmdeviceapi.h>
#include <audioclient.h>
#include <avrt.h>

#pragma comment(lib, "avrt.lib")

int ringbuf_cur = 0;
int ringbuf_size;
short *ringbuf;
HANDLE ShutdownEvent = INVALID_HANDLE_VALUE;

template <class T> inline void SafeRelease(T *ppT)
{
	if (*ppT) {
		(*ppT)->Release();
		*ppT = NULL;
	}
}

//
//  Capture thread - processes samples from the audio engine
//
DWORD WINAPI WASAPICaptureThread(LPVOID Context)
{
	IMMDevice *device = (IMMDevice *)Context;
	HRESULT hr;
	HANDLE AudioSamplesReadyEvent = INVALID_HANDLE_VALUE;
	IAudioClient *AudioClient = NULL;
	IAudioCaptureClient *CaptureClient = NULL;
	REFERENCE_TIME DefaultDevicePeriod;
	REFERENCE_TIME MinimumDevicePeriod;
	WAVEFORMATEXTENSIBLE WaveFormat;
	UINT32 BufferSize;
	DWORD mmcssTaskIndex = 0;
	HANDLE mmcssHandle;
	HANDLE waitArray[2] = { ShutdownEvent, AudioSamplesReadyEvent };
	bool stillPlaying = true;

	hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
	if (FAILED(hr)) {
		printf("Unable to initialize COM in render thread: %x\n", hr);
		return hr;
	}

	AudioSamplesReadyEvent = CreateEventEx(NULL, NULL, 0, EVENT_MODIFY_STATE | SYNCHRONIZE);
	if (AudioSamplesReadyEvent == NULL) {
		printf("Unable to create samples ready event: %d.\n", GetLastError());
		goto Exit;
	}

	hr = device->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, NULL, reinterpret_cast<void **>(&AudioClient));
	if (FAILED(hr)) {
		printf("Unable to activate audio client: %x.\n", hr);
		goto Exit;
	}

	hr = AudioClient->GetDevicePeriod(&DefaultDevicePeriod, &MinimumDevicePeriod);
	if (FAILED(hr)) {
		printf("Unable to get device period: %x.\n", hr);
		goto Exit;
	}

	WaveFormat.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
	WaveFormat.Format.nChannels = 1;
	WaveFormat.Format.nSamplesPerSec = 16000;
	WaveFormat.Format.wBitsPerSample = 16;
	WaveFormat.Format.nBlockAlign = WaveFormat.Format.wBitsPerSample / 8 * WaveFormat.Format.nChannels;
	WaveFormat.Format.nAvgBytesPerSec = WaveFormat.Format.nSamplesPerSec * WaveFormat.Format.nBlockAlign;
	WaveFormat.Format.cbSize = 22;
	WaveFormat.dwChannelMask = SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT;
	WaveFormat.Samples.wValidBitsPerSample = WaveFormat.Format.wBitsPerSample;
	WaveFormat.SubFormat = KSDATAFORMAT_SUBTYPE_PCM;

	hr = AudioClient->Initialize(AUDCLNT_SHAREMODE_EXCLUSIVE,
		AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_NOPERSIST,
		DefaultDevicePeriod, DefaultDevicePeriod, (WAVEFORMATEX*)&WaveFormat, NULL);
	if (FAILED(hr)) {
		printf("Unable to initialize audio client: %x.\n", hr);
		goto Exit;
	}

	hr = AudioClient->GetBufferSize(&BufferSize);
	if (FAILED(hr)) {
		printf("Unable to get audio client buffer: %x. \n", hr);
		goto Exit;
	}

	hr = AudioClient->GetService(IID_PPV_ARGS(&CaptureClient));
	if (FAILED(hr)) {
		printf("Unable to get new capture client: %x.\n", hr);
		goto Exit;
	}

	hr = AudioClient->Start();
	if (FAILED(hr)) {
		printf("Unable to start capture client: %x.\n", hr);
		goto Exit;
	}

	mmcssHandle = AvSetMmThreadCharacteristics(L"Audio", &mmcssTaskIndex);
	if (mmcssHandle == NULL) {
		printf("Unable to enable MMCSS on capture thread: %d\n", GetLastError());
	}

	while (stillPlaying) {
		DWORD waitResult = WaitForMultipleObjects(2, waitArray, FALSE, INFINITE);
		switch (waitResult) {
		case WAIT_OBJECT_0 + 0:     // ShutdownEvent
			stillPlaying = false;       // We're done, exit the loop.
			break;
		case WAIT_OBJECT_0 + 1:     // AudioSamplesReadyEvent
			//
			//  We need to retrieve the next buffer of samples from the audio capturer.
			//
			short *pData;
			UINT32 framesAvailable;
			DWORD  flags;

			//
			//  Find out how much capture data is available.  We need to make sure we don't run over the length
			//  of our capture buffer.  We'll discard any samples that don't fit in the buffer.
			//
			hr = CaptureClient->GetBuffer((BYTE**)&pData, &framesAvailable, &flags, NULL, NULL);
			if (SUCCEEDED(hr)) {
				if (flags & AUDCLNT_BUFFERFLAGS_SILENT) {
					for (UINT32 i = 0; i < framesAvailable; i++) {
						ringbuf[ringbuf_cur] = 0;
						ringbuf_cur++;
						ringbuf_cur %= ringbuf_size;
					}
				}
				else {
					for (UINT32 i = 0; i < framesAvailable; i++) {
						ringbuf[ringbuf_cur] = pData[i * 2];
						ringbuf_cur++;
						ringbuf_cur %= ringbuf_size;
					}
				}

				hr = CaptureClient->ReleaseBuffer(framesAvailable);
				if (FAILED(hr)) {
					printf("Unable to release capture buffer: %x!\n", hr);
				}
			}
			break;
		}
	}

	hr = AudioClient->Stop();
	if (FAILED(hr)) {
		printf("Unable to stop audio client: %x\n", hr);
	}

Exit:
	CloseHandle(ShutdownEvent);
	ShutdownEvent = INVALID_HANDLE_VALUE;

	CloseHandle(AudioSamplesReadyEvent);

	SafeRelease(&CaptureClient);
	SafeRelease(&AudioClient);
	SafeRelease(&device);

	CoUninitialize();

	return 0;
}

extern "C" __declspec(dllexport) bool AudioCapture_Start(int deviceIndex, void *buffer, int size)
{
	bool result = false;
	HRESULT hr;
	IMMDevice *device = NULL;
	IMMDeviceCollection *deviceCollection = NULL;
	IMMDeviceEnumerator *deviceEnumerator = NULL;
	HANDLE CaptureThread = INVALID_HANDLE_VALUE;
	UINT deviceCount;

	hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&deviceEnumerator));
	if (FAILED(hr)) {
		printf("Unable to instantiate device enumerator: %x\n", hr);
		goto Exit;
	}

	hr = deviceEnumerator->EnumAudioEndpoints(eCapture, DEVICE_STATE_ACTIVE, &deviceCollection);
	if (FAILED(hr)) {
		printf("Unable to retrieve device collection: %x\n", hr);
		goto Exit;
	}

	hr = deviceCollection->GetCount(&deviceCount);
	if (FAILED(hr) && deviceCount <= deviceIndex) {
		printf("Unable to get device collection length: %x\n", hr);
		goto Exit;
	}

	hr = deviceCollection->Item(deviceIndex, &device);
	if (FAILED(hr)) {
		printf("Unable to retrieve device %d: %x\n", deviceIndex, hr);
		goto Exit;
	}

	ringbuf_cur = 0;
	ringbuf = (short *)buffer;
	ringbuf_size = size;

	CaptureThread = CreateThread(NULL, 0, WASAPICaptureThread, (LPVOID)device, FALSE, NULL);
	if (CaptureThread == NULL) {
		printf("Unable to create transport thread: %x.", GetLastError());
		goto Exit;
	}

	result = true;
Exit:
	SafeRelease(&device);
	SafeRelease(&deviceCollection);
	SafeRelease(&deviceEnumerator);

	return result;
}

extern "C" __declspec(dllexport) bool AudioCapture_Stop()
{
	if (ShutdownEvent != INVALID_HANDLE_VALUE)
		SetEvent(ShutdownEvent);

	return true;
}

extern "C" __declspec(dllexport) void Stdin(uint8_t *data, int len)
{
	core_util_critical_section_enter();
	for (int i = 0; i < len; i++) {
		stdin_queue.push(*(char *)data++);
	}
	core_util_critical_section_exit();

	shelThread.signal_set(SHELL_STDIN);
}

int printf(const char *format, ...)
{
	int ret;
	char text[1024];
	va_list args;

	va_start(args, format);
	ret = vsnprintf(text, sizeof(text), format, args);
	va_end(args);

	if (ret > 0) {
		for (char *pos = text; *pos != '\0'; pos++) {
			if (*pos == '\n')
				ret++;
		}

		char *text2 = (char *)malloc(ret + 1), *dst = text2;

		for (char *src = text; *src != '\0'; src++) {
			if (*src == '\n') {
				*dst++ = '\r';
			}
			*dst++ = *src;
		}
		*dst = '\0';

		TestBench->ConsoleWrite((uint8_t *)text2, ret);

		free(text2);
	}

	return ret;
}
