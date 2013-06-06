#include "NetUtils.h"

#if defined _WIN32 || defined __CYGWIN__

char* getTunAddress(void)
{
    PIP_ADAPTER_INFO pAdapterInfo;
    PIP_ADAPTER_INFO pAdapter = NULL;
    DWORD dwRetVal = 0;
    UINT i;
    static char ret[128];
    ret[0] = NULL;

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
						if (strncmp(pAdapter->IpAddressList.IpAddress.String, "0.0.0.0", 7) != 0)  {
            						/* ret = (char *)malloc(strlen(pAdapter->IpAddressList.IpAddress.String + 1));
	    						strcpy(ret, pAdapter->IpAddressList.IpAddress.String);
							*/
							//ret = (char *)malloc(strlen(pAdapter->DhcpServer.IpAddress.String + 1));
	    						strcpy(ret, pAdapter->DhcpServer.IpAddress.String);
							break;
						}
					}
					pAdapter = pAdapter->Next;
				}
			}
			FREE(pAdapterInfo);
		}
	}
    }

    // this is UGLY
    char c;
    int ii;
    int count = 0;

    for (ii = 0; ii < strlen(ret); ii++)  {
	c = ret[ii];
        if (c == '.')  {
		count++;
		if (count == 3)  {
			break;
		}
	}
    }
	
    ret[ii+1] = '1';
    ret[ii+2] = '\0';
    return ret;
}

#else

char* getTunAddress(void)
{
    static char ret[128];

    struct ifaddrs *addrs,*tmp;

    if(!getifaddrs(&addrs) != 0) {
    	for(tmp = addrs; tmp ; tmp = tmp->ifa_next) {
		if (strstr(tmp->ifa_name, "tun") != NULL && tmp->ifa_dstaddr != NULL && tmp->ifa_dstaddr->sa_family == AF_INET)  {
			if(tmp->ifa_dstaddr != NULL)  {
				inet_ntop(tmp->ifa_dstaddr->sa_family,&((struct sockaddr_in*)tmp->ifa_dstaddr)->sin_addr,ret,128);
				break;
			}
		}
    	}

    	freeifaddrs(addrs);
    }

    // this is UGLY
    char c;
    int ii;
    int count = 0;

    for (ii = 0; ii < strlen(ret); ii++)  {
	c = ret[ii];
        if (c == '.')  {
		count++;
		if (count == 3)  {
			break;
		}
	}
    }
	
    ret[ii+1] = '1';
    ret[ii+2] = '\0';
    return ret;
}

#endif
