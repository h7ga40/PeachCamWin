#include "mbed.h"
#include "Semaphore.h"

namespace rtos
{

Semaphore::Semaphore() :
	_count(0)
{
	_id = CreateSemaphore(NULL, 0, 1, NULL);
}

Semaphore::~Semaphore()
{
	CloseHandle(_id);
}

int32_t Semaphore::wait(uint32_t timeout)
{
	DWORD ret = WAIT_OBJECT_0;

	if (InterlockedIncrement(&_count) == 0) {
		ret = WaitForSingleObject(_id, timeout);
	}

	switch (ret) {
	case WAIT_OBJECT_0:
		return _count + 1;
	case WAIT_TIMEOUT:
		return 0;
	}

	return -1;
}

osStatus Semaphore::release()
{
	BOOL ret = TRUE;

	if (InterlockedDecrement(&_count) == 0) {
		ret = ReleaseSemaphore(_id, 1, NULL);
	}

	if (ret)
		return osOK;
	else
		return osError;
}

}
