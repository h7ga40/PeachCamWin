#ifndef _SDUSBCONNECT_H_
#define _SDUSBCONNECT_H_

class SdUsbConnect {
public:
	SdUsbConnect(const char *name);
	void wait_connect();
};

#endif // _SDUSBCONNECT_H_
