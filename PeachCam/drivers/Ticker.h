
#ifndef _TICKER_H_
#define _TICKER_H_

#include "PinNames.h"

namespace mbed {

class Ticker
{
public:
	Ticker();

	void attach_us(Callback<void()> func, us_timestamp_t t);
};

}

#endif // _TICKER_H_
