#ifndef _SEMAPHORE_H_
#define _SEMAPHORE_H_

#include "platform/Callback.h"
#include "cmsis_os.h"

namespace rtos
{

class Semaphore {
public:
	Semaphore();
	~Semaphore();
	int32_t wait(uint32_t timeout=osWaitForever);
	osStatus release();
private:
	uint32_t _count;
	HANDLE _id;
};

}

#endif // _SEMAPHORE_H_
