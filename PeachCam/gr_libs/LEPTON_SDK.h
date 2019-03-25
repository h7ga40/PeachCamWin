#ifndef _LEPTON_SDK_H_
#define _LEPTON_SDK_H_

#include "LEPTON_Types.h"
#include "LEPTON_ErrorCodes.h"

extern LEP_RESULT LEP_OpenPort(LEP_PORTID portID,
                                LEP_CAMERA_PORT_E portType,
                                LEP_UINT16   portBaudRate,
                                LEP_CAMERA_PORT_DESC_T_PTR portDescPtr);

#endif // _LEPTON_SDK_H_
