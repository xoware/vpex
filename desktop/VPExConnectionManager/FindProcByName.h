#ifndef __APPLE__
#include <windows.h>
#include <tlhelp32.h>
#include <iostream.h>
#endif

#ifndef _FINDPROCBYNAME_H
#define _FINDPROCBYNAME_H
/* function prototype */
int FindProcByName(const char *);
#endif
