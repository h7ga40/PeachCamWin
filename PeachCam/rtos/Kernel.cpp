#include "mbed.h"
#include "Kernel.h"

namespace rtos {

namespace Kernel {

uint64_t get_ms_count()
{
	return TestBench->us_ticker_read() / 1000;
}

}

}
