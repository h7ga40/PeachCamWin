// TestBenchInit.cpp : DLL アプリケーション用にエクスポートされる関数を定義します。
//

#include "stdafx.h"

#ifndef _WIN64
#import "..\\ITestBench\\bin\\Debug\\ITestBench32.tlb" no_namespace
#else
#import "..\\ITestBench\\bin\\Debug\\ITestBench64.tlb" no_namespace
#endif

__declspec(dllexport) ITestBench *TestBench;

extern "C" __declspec(dllexport) void SetTestBench(ITestBench *testBench)
{
	TestBench = testBench;
}
