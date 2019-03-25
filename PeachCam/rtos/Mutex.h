#ifndef _MUTEX_H_
#define _MUTEX_H_

#include "cmsis_os.h"

namespace rtos
{

class Mutex {
public:
	Mutex();
	~Mutex();
	void lock(void);
	void unlock(void);
	osStatus_t Acquire(uint32_t timeout);
	osStatus_t Release();
private:
	uint32_t _count;
	HANDLE _id;
};

}

#endif // _MUTEX_H_
