#ifndef _EVENTFLAGS_H_
#define _EVENTFLAGS_H_

#include "cmsis_os.h"

namespace rtos
{

class EventFlags {
public:
	EventFlags();
	~EventFlags();
	uint32_t set(uint32_t flags);
	uint32_t clear(uint32_t flags);
	uint32_t wait_all(uint32_t flags = 0, uint32_t timeout = osWaitForever, bool clear = true);
	uint32_t wait_any(uint32_t flags = 0, uint32_t timeout = osWaitForever, bool clear = true);
private:
	HANDLE _id;
	LONG _flags;
	uint32_t _count;
};

}

#endif // _EVENTFLAGS_H_
