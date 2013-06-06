#include "NetUtils.h"

#if defined _WIN32 || defined __CYGWIN__
#include <winsock2.h>
#include <iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <time.h>

#pragma comment(lib, "IPHLPAPI.lib")

#define MALLOC(x) HeapAlloc(GetProcessHeap(), 0, (x))
#define FREE(x) HeapFree(GetProcessHeap(), 0, (x))

typedef int os_error_code_t;

/* Note: could also use malloc() and free() */

int __cdecl main()
{
    char *tunAddress;
    int i;

    for (i = 0; i < 10000; i++)  {
 	tunAddress = getTunAddress();
    if (strlen(tunAddress) != 0)  {
	printf("tun interface endpoint address is %s\n", tunAddress);
    } else {
	printf("could not determine tun interface endpoint address\n");
    }
    Sleep(500);
    }
 
    return 0;
}
#else
#include <sys/types.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <ifaddrs.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <arpa/inet.h>

int main(int argc,char *argv[])
{
    char *tunAddress;
    int i;

    for (i = 0; i < 10000; i++)  {
 	tunAddress = getTunAddress();
    if (strlen(tunAddress) != 0)  {
	printf("tun interface endpoint address is %s\n", tunAddress);
    } else {
	printf("could not determine tun interface endpoint address\n");
    }
    //sleep(1);
    }
 
    return 0;
}
#endif
