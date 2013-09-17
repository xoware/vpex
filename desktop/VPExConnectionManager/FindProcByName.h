#if defined(_WIN32) || defined(__WIN32__) || defined(WIN32)
#include <windows.h>
#include <tlhelp32.h>
#include <iostream>
#endif

#ifndef _FINDPROCBYNAME_H
#define _FINDPROCBYNAME_H
/* function prototype */
int FindProcByName(const char *);
#endif
