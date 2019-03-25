#ifndef CMSIS_OS_H_
#define CMSIS_OS_H_

typedef enum {
  osPriorityIdle          = -3,         ///< Priority: idle (lowest)
  osPriorityLow           = -2,         ///< Priority: low
  osPriorityBelowNormal   = -1,         ///< Priority: below normal
  osPriorityNormal        =  0,         ///< Priority: normal (default)
  osPriorityAboveNormal   = +1,         ///< Priority: above normal
  osPriorityHigh          = +2,         ///< Priority: high
  osPriorityRealtime      = +3,         ///< Priority: realtime (highest)
  osPriorityError         = 0x84,       ///< System cannot determine priority or illegal priority.
  osPriorityReserved      = 0x7FFFFFFF  ///< Prevents enum down-size compiler optimization.
} osPriority;

typedef void *osThreadId;
typedef void *osMessageQId;
typedef void *osMailQId;
typedef void *osMutexId;
typedef void *osEventFlagsId;

#define OS_STACK_SIZE 1024

#define osWaitForever 0xFFFFFFFF

typedef enum {
	osOK = 0,         ///< Operation completed successfully.
	osError = -1,         ///< Unspecified RTOS error: run-time error but no other error message fits.
	osErrorTimeout = -2,         ///< Operation not completed within the timeout period.
	osErrorResource = -3,         ///< Resource not available.
	osErrorParameter = -4,         ///< Parameter error.
	osErrorNoMemory = -5,         ///< System is out of memory: it was impossible to allocate or reserve memory for the operation.
	osErrorISR = -6,         ///< Not allowed in ISR context: the function cannot be called from interrupt service routines.
	osStatusReserved = 0x7FFFFFFF  ///< Prevents enum down-size compiler optimization.
} osStatus_t;

typedef int32_t                  osStatus;
#define osEventSignal           (0x08)
#define osEventMessage          (0x10)
#define osEventMail             (0x20)
#define osEventTimeout          (0x40)
#define osErrorOS               osError
#define osErrorTimeoutResource  osErrorTimeout
#define osErrorISRRecursive     (-126)
#define osErrorValue            (-127)
#define osErrorPriority         (-128)

typedef struct {
  osStatus                    status;   ///< status code: event or error information
  union {
    uint32_t                       v;   ///< message as 32-bit value
    void                          *p;   ///< message or mail as void pointer
    int32_t                  signals;   ///< signal flags
  } value;                              ///< event value
  union {
    osMailQId                mail_id;   ///< mail id obtained by \ref osMailCreate
    osMessageQId          message_id;   ///< message id obtained by \ref osMessageCreate
  } def;                                ///< event definition
} osEvent;

typedef struct {
	int dummy;
} mbed_rtos_storage_event_flags_t;

uint32_t osKernelGetTickCount(void);

typedef struct {
	void *cb_mem;
	uint32_t cb_size;
} osEventFlagsAttr_t;

osEventFlagsId osEventFlagsNew(osEventFlagsAttr_t *attr);
void osEventFlagsDelete(osEventFlagsId id);
uint32_t osEventFlagsSet(osEventFlagsId id, uint32_t flags);
uint32_t osEventFlagsClear(osEventFlagsId id, uint32_t flags);
uint32_t osEventFlagsWait(osEventFlagsId id, uint32_t flags, uint32_t mode, uint32_t timeout);

#define osFlagsWaitAny 0
#define osFlagsWaitAll 1
#define osFlagsNoClear 2
#define osFlagsError 0x80000000

typedef struct {
	void *cb_mem;
	uint32_t cb_size;
} osMutexAttr_t;

osMutexId osMutexNew(osMutexAttr_t *attr);
void osMutexDelete(osMutexId id);
osStatus_t osMutexAcquire(osMutexId mutex_id, uint32_t timeout);
osStatus_t osMutexRelease(osMutexId mutex_id);

#endif  // CMSIS_OS_H_
