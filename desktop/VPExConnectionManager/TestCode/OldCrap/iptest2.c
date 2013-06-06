#include <sys/types.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <ifaddrs.h>
#include <stdio.h>
void
print_sockaddr(struct sockaddr* addr,const char *name)
{
    char addrbuf[128] ;

    addrbuf[0] = 0;
    if(addr->sa_family == AF_UNSPEC)
        return;
    switch(addr->sa_family) {
        case AF_INET:
            inet_ntop(addr->sa_family,&((struct sockaddr_in*)addr)->sin_addr,addrbuf,sizeof(addrbuf));
            break;
        case AF_INET6:
            inet_ntop(addr->sa_family,&((struct sockaddr_in6*)addr)->sin6_addr,addrbuf,sizeof(addrbuf));
            break;
        default:
            sprintf(addrbuf,"Unknown (%d)",(int)addr->sa_family);
            break;

    }
    printf("%-16s %s\n",name,addrbuf);
}

void
print_ifaddr(struct ifaddrs *addr)
{
    char addrbuf[128] ;

    addrbuf[0] = 0;
    printf("%-16s %s\n","Name",addr->ifa_name);
    if(addr->ifa_addr != NULL)
        print_sockaddr(addr->ifa_addr,"Address");
    if(addr->ifa_netmask != NULL)
        print_sockaddr(addr->ifa_netmask,"Netmask");
    if(addr->ifa_broadaddr != NULL)
        print_sockaddr(addr->ifa_broadaddr,"Broadcast addr.");
    if(addr->ifa_dstaddr != NULL)
        print_sockaddr(addr->ifa_dstaddr,"Peer addr.");
    puts("");
}

int main(int argc,char *argv[])
{
    struct ifaddrs *addrs,*tmp;

    if(getifaddrs(&addrs) != 0) {
        perror("getifaddrs");
        return 1;
    }
    for(tmp = addrs; tmp ; tmp = tmp->ifa_next) {
        print_ifaddr(tmp);
    }

    freeifaddrs(addrs);

    return 0;
}
