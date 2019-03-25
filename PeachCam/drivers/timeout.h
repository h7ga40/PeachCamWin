
#ifndef _TIMEOUT_H_
#define _TIMEOUT_H_

#include "Ticker.h"

namespace mbed {

class Timeout : public Ticker, private NonCopyable<Timeout>
{
};

}

#endif // _TIMEOUT_H_
