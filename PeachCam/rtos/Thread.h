#ifndef _THREAD_H_
#define _THREAD_H_

#include "platform/Callback.h"
#include "cmsis_os.h"

namespace rtos
{

class Thread {
public:
	enum State {
		Deleted,
		Inactive,
	};
	Thread(osPriority priority,
		uint32_t stack_size, unsigned char *stack_mem, const char *name = NULL);
	~Thread();
	osThreadId get_id() { return _id; }
	osStatus start(mbed::Callback<void()> task);
	osStatus terminate();
	static int32_t signal_clr(int32_t signals);
	int32_t signal_set(int32_t signals);
	static osEvent signal_wait(int32_t signals, uint32_t millisec = osWaitForever);
	static osStatus yield();

	State get_state()
	{
		return _state;
	}
private:
	State _state;
	osThreadId _id;
	HANDLE _evf;
	int32_t _flags;
	uint32_t _count;
	static DWORD m_TlsIndex;
	DWORD m_ThreadID;
	mbed::Callback<void()> _task;
	void SetThreadName(DWORD dwThreadID, LPCSTR szThreadName);
	static unsigned long __stdcall ThreadProc(void *param);
};

}

#endif // _THREAD_H_
