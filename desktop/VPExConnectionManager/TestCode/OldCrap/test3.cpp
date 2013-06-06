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

char* getTunAddress(void)
{
    PIP_ADAPTER_INFO pAdapterInfo;
    PIP_ADAPTER_INFO pAdapter = NULL;
    DWORD dwRetVal = 0;
    UINT i;
    char* ret = NULL;

    ULONG ulOutBufLen = sizeof (IP_ADAPTER_INFO);
    pAdapterInfo = (IP_ADAPTER_INFO *) MALLOC(sizeof (IP_ADAPTER_INFO));

    if (pAdapterInfo != NULL) {
    	if (GetAdaptersInfo(pAdapterInfo, &ulOutBufLen) == ERROR_BUFFER_OVERFLOW) {
        	FREE(pAdapterInfo);
        	pAdapterInfo = (IP_ADAPTER_INFO *) MALLOC(ulOutBufLen);
        	if (pAdapterInfo != NULL) {
    			if ((dwRetVal = GetAdaptersInfo(pAdapterInfo, &ulOutBufLen)) == NO_ERROR) {
        			pAdapter = pAdapterInfo;
        			while (pAdapter) {
					if (strstr(pAdapter->Description, "TAP-Win32") != NULL)  {
            					/* ret = (char *)malloc(strlen(pAdapter->IpAddressList.IpAddress.String + 1));
	    					strcpy(ret, pAdapter->IpAddressList.IpAddress.String);
						*/
						ret = (char *)malloc(strlen(pAdapter->DhcpServer.IpAddress.String + 1));
	    					strcpy(ret, pAdapter->DhcpServer.IpAddress.String);
						break;
					}
				}
    				if (pAdapterInfo)
        				FREE(pAdapterInfo);
			}
		}
	}
    }

    return ret;
}

int __cdecl main()
{
    char *tunAddress = getTunAddress();
    if (tunAddress != NULL)  {
	printf("tun interface endpoint address is %s\n", tunAddress);
    } else {
	printf("could not determine tun interface endpoint address\n");
    }
 
    return 0;
}
