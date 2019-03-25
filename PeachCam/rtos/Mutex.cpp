#include "mbed.h"
#include "Mutex.h"

namespace rtos
{

Mutex::Mutex() :
	_count(0)
{
	_id = CreateSemaphore(NULL, 0, 1, NULL);
}

Mutex::~Mutex()
{
	CloseHandle(_id);
}

void Mutex::lock(void)
{
	if (InterlockedIncrement(&_count) == 0) {
		WaitForSingleObject(_id, INFINITE);
	}
}

void Mutex::unlock(void)
{
	if (InterlockedDecrement(&_count) == 0) {
		ReleaseSemaphore(_id, 1, NULL);
	}
}

osStatus_t Mutex::Acquire(uint32_t timeout)
{
	DWORD ret = WAIT_OBJECT_0;

	if (InterlockedIncrement(&_count) == 0) {
		ret = WaitForSingleObject(_id, timeout);
	}

	switch (ret) {
	case WAIT_OBJECT_0:
		return osOK;
	case WAIT_TIMEOUT:
		return osErrorTimeout;
	}

	return osError;
}

osStatus_t Mutex::Release()
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
