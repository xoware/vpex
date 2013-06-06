#include <sys/types.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <ifaddrs.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <arpa/inet.h>

char* getTunAddress(void)
{
    char *ret = NULL;

    struct ifaddrs *addrs,*tmp;

    if(!getifaddrs(&addrs) != 0) {
    	for(tmp = addrs; tmp ; tmp = tmp->ifa_next) {
		if (strstr(tmp->ifa_name, "tun") != NULL && tmp->ifa_dstaddr != NULL && tmp->ifa_dstaddr->sa_family == AF_INET)  {
			if(tmp->ifa_dstaddr != NULL)  {
    				ret = (char *)malloc(128);
				inet_ntop(tmp->ifa_dstaddr->sa_family,&((struct sockaddr_in*)tmp->ifa_dstaddr)->sin_addr,ret,128);
				break;
			}
		}
    	}

    	freeifaddrs(addrs);
    }

    return ret;
}

int main(int argc,char *argv[])
{
    char *tunAddress = getTunAddress();
    if (tunAddress != NULL)  {
        printf("tun interface endpoint address is %s\n", tunAddress);
    } else {
        printf("could not determine tun interface endpoint address\n");
    }
 
    return 0;
}
