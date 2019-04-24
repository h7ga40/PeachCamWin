#include "mbed.h"
#include "Thread.h"

namespace rtos
{

DWORD Thread::m_TlsIndex = 0xFFFFFFFF;

Thread::Thread(osPriority priority, uint32_t stack_size, unsigned char *stack_mem,
	const char *name) : 
	_flags(0),
	_count(0)
{
	if (m_TlsIndex == 0xFFFFFFFF) {
		m_TlsIndex = TlsAlloc();
	}

	_evf = CreateEvent(NULL, FALSE, FALSE, NULL);

	_id = CreateThread(NULL, 0, &ThreadProc, (void *)this, CREATE_SUSPENDED, &m_ThreadID);

	SetThreadName(m_ThreadID, name);
}

Thread::~Thread()
{
	CloseHandle(_evf);
	CloseHandle(_id);
}

#define MS_VC_EXCEPTION 0x406D1388

#pragma pack(push,8)
typedef struct tagTHREADNAME_INFO
{
	DWORD dwType;		// Must be 0x1000.
	LPCSTR szName;		// Pointer to name (in user addr space).
	DWORD dwThreadID;	// Thread ID (-1=caller thread).
	DWORD dwFlags;		// Reserved for future use, must be zero.
} THREADNAME_INFO;
#pragma pack(pop)

void Thread::SetThreadName(DWORD dwThreadID, LPCSTR szThreadName)
{
	THREADNAME_INFO info;
	info.dwType = 0x1000;
	info.szName = szThreadName;
	info.dwThreadID = dwThreadID;
	info.dwFlags = 0;

	__try {
		RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(DWORD), (DWORD*)&info);
	}
	__except (EXCEPTION_CONTINUE_EXECUTION)
	{
	}
}

unsigned long __stdcall Thread::ThreadProc(void *param)
{
	Thread *_this = (Thread *)param;

	TlsSetValue(m_TlsIndex, _this);

	_this->_task();

	return 0;
}

osStatus Thread::start(mbed::Callback<void()> task)
{
	_task = task;

	ResumeThread(_id);

	return osOK;
}

osStatus Thread::terminate()
{
	TerminateThread(_id, 0);

	return osOK;
}

int32_t Thread::signal_clr(int32_t signals)
{
	Thread *_this = (Thread *)TlsGetValue(m_TlsIndex);
	return InterlockedAnd((LONG *)&_this->_flags, ~signals);
}

int32_t Thread::signal_set(int32_t signals)
{
	LONG result = InterlockedOr((LONG *)&_flags, signals);
	SetEvent(_evf);
	return result;
}

osEvent Thread::signal_wait(int32_t signals, uint32_t millisec)
{
	Thread *_this = (Thread *)TlsGetValue(m_TlsIndex);
	osEvent result = {
		0,
		_this->_flags
	};

	DWORD ret = WAIT_FAILED, tmo;
	int64_t end;
	uint32_t count = -1, waits;

	if (signals == 0) {
		signals = 0x7FFFFFFF;
		waits = 0;
	}
	else {
		waits = signals;
	}

	if (millisec == osWaitForever)
		end = INT64_MAX;
	else
		end = ((int64_t)ticker_read_us(get_us_ticker_data())) + ((int64_t)millisec * 1000ll);
	do {
		if (millisec != osWaitForever) {
			int64_t now = (int64_t)ticker_read_us(get_us_ticker_data());
			if (end <= now) {
				result.status = osEventTimeout;
				return result;
			}
			tmo = (DWORD)((end - now) / 1000ll);
			if (tmo > INT32_MAX) {
				result.status = osEventTimeout;
				return result;
			}
		}
		else {
			tmo = INFINITE;
		}
		InterlockedIncrement(&_this->_count);
		ret = WaitForSingleObject(_this->_evf, tmo);
		count = InterlockedDecrement(&_this->_count);
		if (ret == WAIT_OBJECT_0) {
			result.status = osEventSignal;
			if (count == 0)
				ResetEvent(_this->_evf);
		}
		else if (ret == WAIT_TIMEOUT) {
			result.status = osEventTimeout;
			continue;
		}
		else {
			result.status = osError;
			return result;
		}
		result.value.signals = _this->_flags;
	} while (((result.value.signals & signals) != waits) && (result.value.signals != 0) && (waits != 0));

	if (count == 0) {
		InterlockedAnd((LONG *)&_this->_flags, ~signals);
	}

	return result;
}

osStatus Thread::yield()
{
	SwitchToThread();
	return osOK;
}

}
