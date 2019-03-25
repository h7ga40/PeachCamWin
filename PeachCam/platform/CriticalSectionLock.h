#ifndef _CRITICALSECTIONLOCK_H_
#define _CRITICALSECTIONLOCK_H_

void core_util_critical_section_enter();
void core_util_critical_section_exit();

class CriticalSectionLock {
public:
    CriticalSectionLock()
	{
		core_util_critical_section_enter();
	}
    ~CriticalSectionLock()
	{
		core_util_critical_section_exit();
	}
};

#endif // _CRITICALSECTIONLOCK_H_
