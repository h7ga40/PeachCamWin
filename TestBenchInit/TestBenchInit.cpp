// TestBenchInit.cpp : DLL アプリケーション用にエクスポートされる関数を定義します。
//

#include "stdafx.h"
#include "libMPSSE_spi.h"

#ifndef _WIN64
#import "..\\ITestBench\\bin\\Debug\\ITestBench32.tlb" no_namespace
#else
#import "..\\ITestBench\\bin\\Debug\\ITestBench64.tlb" no_namespace
#endif

__declspec(dllexport) ITestBench *TestBench;

extern "C" __declspec(dllexport) void SetTestBench(ITestBench *testBench)
{
	if (TestBench != NULL)
		TestBench->Release();
	TestBench = testBench;
	TestBench->AddRef();
}

#include "strmif.h"
#include "uuids.h"
#include "vfwmsgs.h"

// https://stackoverflow.com/questions/4286223/how-to-get-a-list-of-deviceType-capture-devices-web-cameras-on-windows-c
#pragma comment(lib, "strmiids")

HRESULT EnumerateDevices(REFGUID category, IEnumMoniker **ppEnum)
{
	// Create the System Device Enumerator.
	ICreateDevEnum *pDevEnum;
	HRESULT hr = CoCreateInstance(CLSID_SystemDeviceEnum, NULL,
		CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pDevEnum));

	if (SUCCEEDED(hr)) {
		// Create an enumerator for the category.
		hr = pDevEnum->CreateClassEnumerator(category, ppEnum, 0);
		if (hr == S_FALSE) {
			hr = VFW_E_NOT_FOUND;  // The category is empty. Treat as an error.
		}
		pDevEnum->Release();
	}
	return hr;
}

void DisplayDeviceInformation(DeviceType deviceType, IEnumMoniker *pEnum)
{
	IMoniker *pMoniker = NULL;
	int index = -1;

	while (pEnum->Next(1, &pMoniker, NULL) == S_OK) {
		index++;
		IPropertyBag *pPropBag;
		HRESULT hr = pMoniker->BindToStorage(0, 0, IID_PPV_ARGS(&pPropBag));
		if (FAILED(hr)) {
			pMoniker->Release();
			continue;
		}

		HRESULT hr1, hr2;
		VARIANT var1, var2;
		VariantInit(&var1);
		VariantInit(&var2);

		// Get description or friendly name.
		hr1 = pPropBag->Read(L"Description", &var1, 0);
		hr2 = pPropBag->Read(L"FriendlyName", &var2, 0);

		TestBench->AddDevice(deviceType, index, var1.bstrVal, var2.bstrVal);

		if (SUCCEEDED(hr1)) {
			VariantClear(&var1);
		}
		if (SUCCEEDED(hr2)) {
			VariantClear(&var2);
		}

		pPropBag->Release();
		pMoniker->Release();
	}
}

extern "C" __declspec(dllexport) void EnumerateDevices()
{
	HRESULT hr;
	IEnumMoniker *pEnum;

	hr = EnumerateDevices(CLSID_VideoInputDeviceCategory, &pEnum);
	if (SUCCEEDED(hr)) {
		DisplayDeviceInformation(DeviceType_Video, pEnum);
		pEnum->Release();
	}
	hr = EnumerateDevices(CLSID_AudioInputDeviceCategory, &pEnum);
	if (SUCCEEDED(hr)) {
		DisplayDeviceInformation(DeviceType_Audio, pEnum);
		pEnum->Release();
	}

	FT_STATUS ret;
	uint32 numChannels;

	ret = SPI_GetNumChannels(&numChannels);
	if (ret != FT_OK)
		return;

	for (int i = 0; i < numChannels; i++) {
		FT_DEVICE_LIST_INFO_NODE chanInfo;
		ret = SPI_GetChannelInfo(i, &chanInfo);
		if (ret != FT_OK)
			continue;

		TestBench->AddDevice(DeviceType_MPSSE, i, chanInfo.SerialNumber, chanInfo.Description);
	}
}

extern "C" __declspec(dllexport) FT_HANDLE OpenMpsse(int index)
{
	FT_HANDLE fthandle = NULL;
	FT_STATUS ret;

	ret = SPI_OpenChannel(index, &fthandle);
	if (ret != FT_OK)
		printf("SPI_OpenChannel error %d", ret);

	return fthandle;
}
