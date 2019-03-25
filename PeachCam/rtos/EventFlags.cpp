
#include "mbed.h"
#include "EventFlags.h"

namespace rtos
{

EventFlags::EventFlags() :
	_flags(0),
	_count(0)
{
	_id = CreateEvent(NULL, TRUE, FALSE, NULL);
}

EventFlags::~EventFlags()
{
	CloseHandle(_id);
}

uint32_t EventFlags::set(uint32_t flags)
{
	LONG result = InterlockedOr(&_flags, flags);
	SetEvent(_id);
	return result;
}

uint32_t EventFlags::clear(uint32_t flags)
{
	return InterlockedAnd(&_flags, ~flags);
}

uint32_t EventFlags::wait_all(uint32_t flags, uint32_t timeout, bool clear)
{
	DWORD ret = WAIT_FAILED, tmo;
	us_timestamp_t end;
	uint32_t count = -1;
	LONG result = _flags;

	end = ticker_read_us(get_us_ticker_data()) + timeout * 1000l;
	while ((result & flags) != flags) {
		tmo = (DWORD)((end - ticker_read_us(get_us_ticker_data())) / 1000);
		if (tmo <= 0) {
			return result | osFlagsError;
		}
		InterlockedIncrement(&_count);
		ret = WaitForSingleObject(_id, tmo);
		count = InterlockedDecrement(&_count);
		if (ret == WAIT_OBJECT_0) {
			if (count == 0)
				ResetEvent(_id);
		}
		else if (ret != WAIT_TIMEOUT) {
			return result | osFlagsError;
		}
		result = _flags;
	}

	if ((clear) && (count == 0)) {
		InterlockedAnd(&_flags, ~flags);
	}

	return result;
}

uint32_t EventFlags::wait_any(uint32_t flags, uint32_t timeout, bool clear)
{
	DWORD ret = WAIT_FAILED, tmo;
	us_timestamp_t end;
	uint32_t count = -1;
	LONG result = _flags;

	end = ticker_read_us(get_us_ticker_data()) + timeout * 1000l;
	while ((result & flags) != 0) {
		tmo = (DWORD)((end - ticker_read_us(get_us_ticker_data())) / 1000);
		if (tmo <= 0) {
			return result | osFlagsError;
		}
		InterlockedIncrement(&_count);
		ret = WaitForSingleObject(_id, tmo);
		count = InterlockedDecrement(&_count);
		if (ret == WAIT_OBJECT_0) {
			if (count == 0)
				ResetEvent(_id);
		}
		else if (ret != WAIT_TIMEOUT) {
			return result | osFlagsError;
		}
		result = _flags;
	}

	if ((clear) && (count == 0)) {
		InterlockedAnd(&_flags, ~flags);
	}

	return result;
}

}
