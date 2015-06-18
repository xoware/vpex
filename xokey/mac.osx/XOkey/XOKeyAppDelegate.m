//
//  XOkeyAppDelegate.m
//  XOkey
//
//  Created by user on 5/8/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import "XOkeyAppDelegate.h"
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <arpa/inet.h>
#include <ifaddrs.h>
#include <net/if.h>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//      XOkey App + Drivers
//      Purpose:    Drivers to detect the XOkey device and an application to configure it and forward traffic to the ExoNet VPN servers
//
//      Architecture:
//          ---ExoKepAppDelegate.m--
//              Purpose:    Main application
//              init:
//              1)  Initialize USB PnP events
//              2)  Initialize network tool to configure device (with root priviledge)
//              3)  Setup polling function to poll for VPN state, network changes, and EK changes
//              4)  Dertermine interface facing the internet
//
//              devProc:
//              1)  Detect XOkey USB plugin/unplug events
//              2)  Set IP address of EK, determine endpoint, and configure NAT/firewall settings
//              3)  Create connection to web interface
//
//              appPoll:
//              1)  Determine network changes to EK
//              2)  Determine if host has connection to the internet
//              3)  Poll 192.168.255.1 to determine if VPN is up
//              4)  Add/remove routes to ExoNet depending on the status
//
//          ---statusDelegate.m---
//              Purpose:    Handles polling for the VPN status
//
//          ---webViewDelegate.m---
//              Purpose:    Delegate for connection to 192.168.255.1. Enables JS/authentication to the server.
//
//          ---usbObject.m---
//              Purpose:    Handles USB enumeration and PnP detection of EK
//
//          ---NetworkConfigTool.m---
//              Purpose:    Executes BSD networking configuration commands (ifconfig, ipconfig, PF Nat rules, enabling packet forwarding, etc..)
//                          with root priviledge. Executes as a daemon process launched by launchd.
//
//          ---XOkey_Definitions.h---
//              Purpose:    Holds definitions for the EK and device property keys
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//
//  Globals
//
XOkeyAppDelegate* c_pointer;
NSFileHandle* logFileHandle;

//Detection of XOkey is done via USB enumeration using IOKit
/*
struct XoHeartBeatData
{
    uint32_t version;  // version of this struct.  version 1 offset 0
    uint32_t magic; // magic marker to validate 0xDEADBEEF  offset 4
    unsigned int num_addr; // offset 8
    struct in_addr addr[MAX_ADDR];  // My IP 4V addresses // offset 12
    unsigned int addr_prefix[MAX_ADDR];  //  CIDR  prefix    // netmask
    uint32_t product_id;
    time_t time; // once clock is set, this value should never repeat.  Help avoid replay attack.
    uint32_t rand[2]; // random data for hashing.  Enough to seed signature.
    uint32_t signature[4]; // Signature(Hash) of above data.
};
*/

/*
//Socket method to detect XOkey
//Add the while(1) loop for listening for UDP packets on a spearate thread.
//Listen to broadcast packets.
void listenToXOkeyBroadcast(){
    int sd;
    struct sockaddr_in sa;
    sd = socket(AF_INET, SOCK_DGRAM, 0);
    if (sd == -1) {
        XOkeyLog(@"Error creating broadcast socket");
    }
    //Zero the memory of sa
    bzero(&sa, sizeof(sa));
    
    //Set sockaddr info
    sa.sin_family = AF_INET;
    sa.sin_port = htons(1500);
    sa.sin_addr.s_addr = htons(((239<<8|255)<<8|255)<<8|255);    //239.255.255.255
    sa.sin_addr.s_addr = INADDR_ANY;
    
    //Bind to the socket
    if (!bind(sd, (struct sockaddr *)&sa, sizeof(sa))) {
        XOkeyLog(@"Failed to bind socket and listen to XOkey broadcast traffic.");
        return;
    }
#if 0
    //Fetch the global concurrent queue associated with each Mac process
    dispatch_queue_t netWorkQueue = dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT,
                                                        0);

    //Use GCD async and concurrent queues instead of threads.
    dispatch_sync(netWorkQueue, ^(void){
        while (1) {
            char buf[101];
            struct sockaddr from;
            socklen_t sizeFrom = sizeof(from);
            
            //Listen is only used fot connection oriented (TCP) type streams so we use recvfrom instead.
            ssize_t size = recvfrom(sd, buf, sizeof(buf), 0, &from, &sizeFrom);        //recvfrom blocks
            XOkeyLog(@"inside dispatch queue");

            buf[100] = '\n';
            NSLog(@"%s",buf);
            sleep(2.0);
        }
    });
#endif
}
 */
#pragma mark C-level functions
#define DEBUG_MODE 0            //Logging is completely turned off for release mode
//Universal logger across all the different objects of the app (including the network tool)
void XOkeyLog(NSString* text){
#if DEBUG_MODE
    dispatch_queue_t queue = dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0);
    dispatch_sync(queue, ^{
        NSLog(@"%@",text);

        //Log window only exists in the debug version of the app
        NSDateFormatter *dateFormat = [[NSDateFormatter alloc] init];
        [dateFormat setLocale:[[NSLocale alloc] initWithLocaleIdentifier:@"en_US"]];
        [dateFormat setDateFormat:@"EEEE, dd-MMM-yy HH:mm a"];
        NSString* formattedDate = [dateFormat stringFromDate:[NSDate date]];
      
        //Log to file
        NSMutableString* fileString = [NSMutableString stringWithString:formattedDate];
        [fileString appendString:@": "];
        [fileString appendString:text];
        [fileString appendString:@"\n"];
        if (logFileHandle) {
            //[logFileHandle writeData:[fileString dataUsingEncoding:NSUTF8StringEncoding]];
        }

    });
#endif
}

//
//  Finding the network interface from the IP address usig getifaddrs
//http://stackoverflow.com/questions/427517/finding-an-interface-name-from-an-ip-address
NSString* findNetworkInterface(struct in_addr* addr){
    struct ifaddrs *addrs, *iap;
    struct sockaddr_in *sa;
    
    getifaddrs(&addrs);
    for (iap = addrs; iap != NULL; iap = iap->ifa_next) {
        if (iap->ifa_addr && (iap->ifa_flags & IFF_UP) && iap->ifa_addr->sa_family == AF_INET) {
            sa = (struct sockaddr_in *)(iap->ifa_addr);
            if (sa->sin_addr.s_addr == addr->s_addr) {
                return [NSString stringWithFormat:@"%s",iap->ifa_name];
            }
        }
    }
    return nil;
}

/**
 * @brief Get a local IP address that has a route to the internet.
 * do DNS resolution, and try and create a connection to see which source IP gets bound.
 * @param addr returned value
 * @return int  = 0 if OK, or error code
 */
NSString* XoUtil_getInternetSrcAddr(struct in_addr *addr)
{
    int sd = 0, ret = 0;
    int i;
    unsigned int slen;
    struct sockaddr_in cliAddr, servAddr;
    struct hostent *h = NULL;
    const int SERVER_PORT = 80;
    char *buf = NULL;
    int buflen = 256;
    
    //  Determine if the computer can connect to the internet.
    // some popular hosts on the Internet
    char * hosts[] = {
        "www.xoware.com",
        "www.google.com",
        "www.yahoo.com",
        "www.facebook.com",
        "www.amazon.com",
        NULL, NULL,
    };
    
    addr->s_addr = 0;
    
    buf = calloc(buflen, 1);
    
    //To ensure the domains can be reached (no network issues)
    for (i = 0; !h && i < 5 && hosts[i]; i++) {
        h = gethostbyname2(hosts[i], AF_INET);
        if (h==NULL) {
            //ERROR("lookup failed: %s\n", hosts[i]);
            XOkeyLog([NSString stringWithFormat:@"Failed to fetch info on host: %s",hosts[i]]);
        }
    }
    //  Connection to the internet might not exist
    if (!h) {
        return nil;
    }
    
    //  Find network interface that has internet (facing the internet)
    servAddr.sin_family = AF_INET;
    memcpy((char *) &servAddr.sin_addr.s_addr, h->h_addr_list[0],h->h_length);
    servAddr.sin_port = htons(SERVER_PORT);
    
    sd = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (sd == -1) {
        //	ERROR("Creating socket\n");
        goto out;
    }
    
    ret = connect(sd, (struct sockaddr *) &servAddr, sizeof(servAddr));
    if (ret == -1) {
        //ERROR("connect failed %d, %m \n", errno);
        goto out;
    }
    
    slen = sizeof(cliAddr);
    getsockname(sd, (struct sockaddr *) &cliAddr, &slen);
    //XOkeyLog([NSString stringWithFormat:@"Local Address = %d : %s \n", ntohs(cliAddr.sin_port), inet_ntoa(cliAddr.sin_addr)]);
    
    *addr = cliAddr.sin_addr;
    
    out:
    /* close socket and exit */
    if (sd)
        close(sd);
    
    if (buf)
        free(buf);
    
    return findNetworkInterface(addr);
}

@implementation XOkeyAppDelegate
{
    usbObject* XOkeyUSBObject;
    webViewDelegate* webViewDel;
    statusDelegate* statDelegate;
    NSMutableData* receivedData;
    NSURLConnection* conn;
    NSMutableDictionary* deviceProperties;
    BOOL XOkeyConnected;
    BOOL routedToExoNet;
    BOOL networkToolCreated;
    NSXPCInterface* helperInterface;
    NSXPCConnection *myConnection;
    dispatch_queue_t networkQueue;
    NSWindow* modalWindow;
    NSUserNotification* disconnNote;
}


- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    //  Set c pointer for c functions.
    c_pointer = self;
    
    //  Configure GUI
    [self initializeGUI];
    
    //  Get global concurrent queue
    networkQueue = dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT,
                              0);
    
    //  Initialize file I/O for logging
    /*NSArray* paths = NSSearchPathForDirectoriesInDomains(NSDesktopDirectory, NSUserDomainMask, YES);
    NSString* theDesktopPath = [paths objectAtIndex:0];
    NSMutableString* logPath = [NSMutableString stringWithString:theDesktopPath];
    [logPath appendString:@"/XOkey.log"];
     */
    //A single log file is now located in /etc/XOkey.log
    NSString* logPath = [NSString stringWithFormat:@"/etc/XOkey.log"];
    if (![[NSFileManager defaultManager]fileExistsAtPath:logPath]) {
        [[NSFileManager defaultManager]createFileAtPath:logPath contents:nil attributes:nil];
    }
    logFileHandle = [NSFileHandle fileHandleForUpdatingAtPath:logPath];
    
    //  Initially, the application is unaware if an XOkey device is connected.
    XOkeyConnected = false;
    
    //  Traffic isn't initially routed to the ExoNet
    routedToExoNet = false;
    
    //  Network tool needs to be created.
    networkToolCreated = false;
    
    //  Initialize dictionary variables
    deviceProperties = [[NSMutableDictionary alloc]initWithObjectsAndKeys:
                                                        DEFAULT_IP,         XOKEY_IP_ADDRESS,
                                                        NOT_SET,            XOKEY_ENDPOINT,
                                                        DEFAULT_SUBNET,     XOKEY_SUBNET,
                                                        NOT_SET,            ROUTER,
                                                        NOT_SET,            EXONET_IP,
                                                        NOT_SET,            ACTIVE_ENDPOINT,
                                                        nil];

    //  Initialize private variables and objects.
    webViewDel = [[webViewDelegate alloc]init];
    [self.ek_WebView setUIDelegate:webViewDel];
    [self.ek_WebView setGroupName:@"MyDocument"];
    statDelegate = [[statusDelegate alloc]init];
    
    //  Accept cookies
    [[NSHTTPCookieStorage sharedHTTPCookieStorage] setCookieAcceptPolicy:NSHTTPCookieAcceptPolicyAlways];
    
    //  Setup timer to check for IP address changes from DHCP server and to poll the VPN status.
    [NSTimer scheduledTimerWithTimeInterval:3.0 target:self selector:@selector(appPoll:) userInfo:nil repeats:YES];
    
    //  Setup notifications for PnP events
    [[NSNotificationCenter defaultCenter]addObserver:self selector:@selector(devProc:) name:XOKEY_PLUGIN object:nil];
    [[NSNotificationCenter defaultCenter]addObserver:self selector:@selector(devProc:) name:XOKEY_UNPLUG object:nil];
    
    //  Find the IP address of the router
    [self findRouter];
    
    //  Begin pre-autorization
    [self authorize];
    
    //  Create the network tool daemon.
    const unsigned int networkToolCreationRetries = 3;
    for (int i = 0; i < networkToolCreationRetries; i++) {
        if([self initializeNetworkTool]){
            break;
        }
        XOkeyLog([NSString stringWithFormat:@"Failed to create Network Tool. Trying again: %d",(i+1)]);
    }
    
    //  Find interface facing the internet. We poll for it but it's initially needed before the PF rule are set
    [self getActiveInterface];
    
    //  Listen to broadcast packets from the EK
    //listenToXOkeyBroadcast();
    
    //  Setup disconnect notification
    disconnNote = [[NSUserNotification alloc]init];
    [[NSUserNotificationCenter defaultUserNotificationCenter]setDelegate:self];
    
    //  Initialize and enumerate the bus to find the XOkey and setup callback functions for PnP
    XOkeyUSBObject = [[usbObject alloc]init];
    [XOkeyUSBObject enumerateUSB];
}

//  XPC function allowed to be called from the network tool for logging.
-(void)messageWrapper:(NSString*)text{
    XOkeyLog(text);
}

#pragma mark GUI Methods

//Setup the GUI
-(void)initializeGUI{
    self.ek_ConnectedDisplay.state = NSOffState;
    
    //Present the wait window
    [self openWaitWindow];
    
    //Turn off modal dialog view from blocking the app from closing
    [self.modalDialogView.window setPreventsApplicationTerminationWhenModal:NO];
}

#pragma mark Callbacks and Other

//Callback function for noification from the USB object that an XOkey object exists
-(void)devProc:(NSNotification*) notification{
    if([notification.name isEqualToString:XOKEY_PLUGIN]){
        XOkeyLog(@"***XOkey plugin event received.***");
        self.ek_ConnectedDisplay.state = NSOnState;

        //Set XOkey connected state to true
        XOkeyConnected = true;
        
        //Set the EK BSD device name and initialize IP address and subnet to default values
        deviceProperties[XOKEY_ENDPOINT] = XOkeyUSBObject.BSDDeviceName;
        deviceProperties[XOKEY_IP_ADDRESS] = DEFAULT_IP;
        deviceProperties[XOKEY_SUBNET] = DEFAULT_SUBNET;
        
        //Create the network tool to execute programs requiring root
        [self setXOkeyIP];
        
        //Sleep a bit after setting the IP
        XOkeyLog(@"Sleep a bit to let EK ip address before connecting to EK.");

        //Setup firewall/NAT rules only when EK is connected
        dispatch_sync(networkQueue,
            ^(void){
                sleep(3.0);
                [self setupFirewall];
        });
        
        //Clear the webview
        [webViewDel connectToXOkey:@""];
        
        //Device has appeared, close the waiting window. Sleep a bit in order to let
        //webkit load the page before the dialog is closed. Note, function calls to the
        //Autolayout engine must occur on the main thread.
        dispatch_async(networkQueue,
            ^(void){
                NSTimer* timer = [NSTimer timerWithTimeInterval:1.5 target:self selector:@selector(closeWaitWindow:) userInfo:nil repeats:NO];
                [[NSRunLoop mainRunLoop] addTimer:timer forMode:NSRunLoopCommonModes];
        });
    }
    if([notification.name isEqualToString:XOKEY_UNPLUG]){
        XOkeyLog(@"***XOkey unplug event received.***");
        XOkeyConnected = false;
        self.ek_ConnectedDisplay.state = NSOffState;
        NSURL* url = [NSURL URLWithString:@"about:blank"];
        NSURLRequest* req = [NSURLRequest requestWithURL:url];
        [[self.ek_WebView mainFrame] loadRequest:req];

        //Clear ExoNet properties
        routedToExoNet = false;
        [self removeExoNetRoute];
        deviceProperties[EXONET_IP] = NOT_SET;
        
        //Clear EK properties
        deviceProperties[XOKEY_ENDPOINT] = NOT_SET;
        deviceProperties[XOKEY_IP_ADDRESS] = NOT_SET;
        deviceProperties[XOKEY_SUBNET] = NOT_SET;
        
        //Launch notification
        disconnNote.title = @"XOkey has been unplugged";
        [[NSUserNotificationCenter defaultUserNotificationCenter]deliverNotification:disconnNote];
        
        //Re-open the waiting window
        [self openWaitWindow];
    }
}

-(void)findRouter{
     //Determine IP of router.
    NSTask* routerTask = [[NSTask alloc]init];
    NSPipe* pipe = [[NSPipe alloc]init];
    NSString* arg = @"netstat -rn | grep -E -m 1 'default'| awk '{print $2}'";
    [routerTask setLaunchPath:@"/bin/sh"];
    [routerTask setArguments:@[@"-c",arg]];
    [routerTask setStandardOutput:pipe];
    [routerTask launch];
    NSString* result = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSASCIIStringEncoding];
    result = [result stringByReplacingOccurrencesOfString:@"\n" withString:@""];
    if ([result isNotEqualTo:deviceProperties[ROUTER]]) {
        //NSLog(@"Router IP address changed from %@ to %@",deviceProperties[ROUTER],result);
        deviceProperties[ROUTER] = result;
    }
}

//DNS lookup for ExoNet hostname returned by getStatus query
-(BOOL)resolveHostName:(NSString*)ExoNetHostName{
    //host www.google.com | grep -m 1 "address" | awk '{print $4}'
    NSTask* task = [[NSTask alloc]init];
    NSPipe* pipe = [[NSPipe alloc]init];
    NSString* arg = [NSString stringWithFormat:@"host %@ | grep -m 1 \"address\" | awk '{print $4}'",ExoNetHostName];
    [task setLaunchPath:@"/bin/sh"];
    [task setArguments:@[@"-c",arg]];
    [task setStandardOutput:pipe];
    [task launch];
    NSString* result = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSASCIIStringEncoding];
    result = [result stringByReplacingOccurrencesOfString:@"\n" withString:@""];
    //Failed host name resolution is just an empty string
    if ([result isEqualToString:@""]) {
        XOkeyLog([NSString stringWithFormat:@"Failed to resolve ExoNet hostnamee %@",ExoNetHostName]);
        deviceProperties[EXONET_IP] = NOT_SET;
        return false;
    }else{
        deviceProperties[EXONET_IP] = result;
        return true;
    }
    
}

-(void)getActiveInterface{
    dispatch_async(networkQueue, ^(void){
        struct in_addr nextHop;
        NSString* activeEndpoint = XoUtil_getInternetSrcAddr(&nextHop);
        if (!activeEndpoint){
            // Internet might be down so remove any ExoNet routes if they exist.
            deviceProperties[ACTIVE_ENDPOINT] = NOT_SET;
            [self removeExoNetRoute];
        }else{
            deviceProperties[ACTIVE_ENDPOINT] = activeEndpoint;
        }
    });
}

//  Polling for ExoNet status and changes on the EK
-(void)appPoll:(NSTimer*)timer{
    //  1) First check if EK is connected.
    if (!XOkeyConnected) return;
    
    //  2) Find the IP address of the EK.
    if (![deviceProperties[XOKEY_ENDPOINT] isEqual: NOT_SET]) {
        //Update IP Address
        NSTask* endpointTask = [[NSTask alloc]init];
        NSPipe* pipe = [NSPipe pipe];
        NSString* arg;
        arg = [NSString stringWithFormat:@"ifconfig %@ | grep -E 'inet' |egrep '[[:digit:]]{1,3}\\.[[:digit:]]{1,3}\\.[[:digit:]]{1,3}\\.[[:digit:]]{1,3}'| awk '{ print $2}'",deviceProperties[XOKEY_ENDPOINT]];
        [endpointTask setLaunchPath:@"/bin/sh"];
        [endpointTask setArguments:@[@"-c",arg]];
        [endpointTask setStandardOutput:pipe];
        [endpointTask launch];
        NSString* result =[[NSString alloc] initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSASCIIStringEncoding];
        result = [result stringByReplacingOccurrencesOfString:@"\n" withString:@""];
        deviceProperties[XOKEY_IP_ADDRESS] = result;
        
    }
    
    //  3) Check if the host has internet by finding the interface facing the internet.
    [self getActiveInterface];
    if ([deviceProperties[ACTIVE_ENDPOINT]  isEqual: NOT_SET]) return;
    
    //  4) Check status of VPN server to determine if the EK is connected to the ExoNet
    {
        int VPN_Status = [statDelegate pollStatus];
        
        //  Case 1: VPN connection is up and  traffic hasn't been routed to the ExoNet
        if ((VPN_Status == VPN_CONNECTED) & !routedToExoNet) {
            //Attempt 2 tries to resolve ExoNet host name (DNS)
            for (UInt16 dnsTry = 0; dnsTry < 2; dnsTry++) {
                if([self resolveHostName:(NSString*)statDelegate.exoNetHostName]){
                    [self routeToExoNet:deviceProperties[EXONET_IP]];
                    routedToExoNet = true;
                    //Minimize the XOkey Window when the connected to a VPN gateway
                    [self.window miniaturize:self];
                    return;
                }
                //Sleep a bit before retrying.
                sleep(0.1);
            }
            //Failed to resolve DNS.
            deviceProperties[EXONET_IP] = NOT_SET;
        }
        
        //  Case 2: VPN connection is down and the traffic has been routed to the ExoNet
        if ((VPN_Status == VPN_DISCONNECTED) & routedToExoNet) {
            //Remove route to ExoNet
            [self removeExoNetRoute];
            routedToExoNet = false;
        }
        
        //  Case 3: If the VPN is up and default traffic already routed to the ExoNet, do nothing
        //if ((VPN_Status == VPN_CONNECTED) & routedToExoNet)
        
        //  Case 4: If the VPN is down and the traffic was never routed to the ExoNet, do nothing
        //if ((VPN_Status == VPN_DISCONNECTED) & !routedToExoNet)
    }
}

-(void)openWaitWindow{
    // This method has been depracated in OS X 10.10.1
    //[NSApp beginSheet:self.waitWindow modalForWindow:_window modalDelegate:self didEndSelector:@selector(didEndSheet:returnCode:contextInfo:) contextInfo:nil];
    [self.window beginSheet:self.waitWindow completionHandler:^(NSModalResponse response){
                //Nothing really needs to be done in the wait window
                }];
}

-(void)closeWaitWindow:(NSTimer*)timer{
    // This method has been depracated in OS X 10.10.1
    //[NSApp endSheet:self.waitWindow];
    [self.window endSheet:self.waitWindow];
}

- (void)didEndSheet:(NSWindow *)sheet returnCode:(NSInteger)returnCode
        contextInfo:(void *)contextInfo{
    [sheet orderOut:self];
}

#pragma mark Sleep, Wake, and Disconnect Notification Handling
- (void) receiveSleepNote: (NSNotification*) note
{
    //Don't really need to do anything
    XOkeyLog([NSString stringWithFormat:@"receiveSleepNote: %@", [note name]]);
}

- (void) receiveWakeNote: (NSNotification*) note
{
    NSLog(@"receiveWakeNote: %@",[note name]);
    XOkeyLog([NSString stringWithFormat:@"receiveWakeNote: %@", [note name]]);
    //After wake, reset the web view, present the wait window modal dialog box, and manually enumerate the USB ports for the XOkey device
    [webViewDel connectToXOkey:@""];
    [self openWaitWindow];
    [XOkeyUSBObject enumerateUSB];
}

- (void) fileNotifications
{
    //These notifications are filed on NSWorkspace's notification center, not the default
    // notification center. You will not receive sleep/wake notifications if you file
    //with the default notification center.
    [[[NSWorkspace sharedWorkspace] notificationCenter] addObserver: self
                                                           selector: @selector(receiveSleepNote:)
                                                               name: NSWorkspaceWillSleepNotification object: NULL];
    
    [[[NSWorkspace sharedWorkspace] notificationCenter] addObserver: self
                                                           selector: @selector(receiveWakeNote:)
                                                               name: NSWorkspaceDidWakeNotification object: NULL];
}

- (BOOL)userNotificationCenter:(NSUserNotificationCenter *)center
     shouldPresentNotification:(NSUserNotification *)notification
{
    return YES;
}

#pragma mark GUI Actions
//Reconnect to the XOkey https server
- (IBAction)reconnect:(id)sender {
    //Only try to connect to the https server if the device is connected.
    if (XOkeyConnected) {
        //If the network tool hasn't been created, create it.
        if (!networkToolCreated) {
            //Set the EK BSD device name and initialize IP address and subnet to default values
            deviceProperties[XOKEY_ENDPOINT] = XOkeyUSBObject.BSDDeviceName;
            deviceProperties[XOKEY_IP_ADDRESS] = DEFAULT_IP;
            deviceProperties[XOKEY_SUBNET] = DEFAULT_SUBNET;
            
            //Create the network tool to execute programs requiring root
            [self setXOkeyIP];
            
            //Setup firewall/NAT rules
            dispatch_sync(networkQueue,
                ^(void){
                    XOkeyLog(@"Sleep a bit to let EK ip addres update before setting up firewall/NAT rules.");
                    sleep(2.0);
                    [self setupFirewall];
            });
        }
        
        //First cancel whatever is loading on the website. The stoploading
        //message doesn't seem to work too well so first load a blank page
        //then send the stopLoading message
        NSURL* url = [NSURL URLWithString:@"about:blank"];
        NSURLRequest* req = [NSURLRequest requestWithURL:url];
        [[self.ek_WebView mainFrame] loadRequest:req];
        [[self.ek_WebView mainFrame] stopLoading];
        sleep(0.5);
        [webViewDel connectToXOkey:@""];
    }else{
        [self openWaitWindow];
        XOkeyLog(@"XOkey is not connected!");
    }
}

- (IBAction)closeModalDialog:(id)sender {
    [self closeWaitWindow:nil];
}

#pragma mark Authorization functions

//Create empty authorization reference.
-(void)authorize{
    OSStatus status = AuthorizationCreate(NULL, kAuthorizationEmptyEnvironment, kAuthorizationFlagDefaults, &self->_authRef);
    if (status != errAuthorizationSuccess) {
        /* AuthorizationCreate really shouldn't fail. */
        assert(NO);
        self->_authRef = NULL;
    }
}

//Function from Apple's SMJobless example project.
//Launches the network tool as a launchd job (admin privledges)
- (BOOL)blessHelperWithLabel:(NSString *)label error:(NSError **)errorPtr newTool:(BOOL)isNewTool;
{
	BOOL result = NO;
    NSError * error = nil;
    
    // kSMRightBlessPrivilegedHelper key is used for the default long prompt
	AuthorizationItem authItem		= { kSMRightBlessPrivilegedHelper, 0, NULL, 0 };
	AuthorizationRights authRights	= { 1, &authItem };
	AuthorizationFlags flags		=	kAuthorizationFlagDefaults				|
    kAuthorizationFlagInteractionAllowed	|
    kAuthorizationFlagPreAuthorize			|
    kAuthorizationFlagExtendRights;
    
	/* Obtain the right to install our privileged helper tool (kSMRightBlessPrivilegedHelper). */
	OSStatus status = AuthorizationCopyRights(self->_authRef, &authRights, kAuthorizationEmptyEnvironment, flags, NULL);
	if (status != errAuthorizationSuccess) {
		error = [NSError errorWithDomain:NSOSStatusErrorDomain code:status userInfo:nil];
	} else {
        CFErrorRef  cfError;
        //Remove the job if it exists.
        //@deprecated SMJobRemove has been deprecated and now causes com.apple.xpc.lanchd to crash
        //SMJobRemove(kSMDomainSystemLaunchd,(CFStringRef)kNetworkConfigToolMachServiceName,self->_authRef,true,&cfError);
        
        
           /*
        @deprecated SMJobSubmit has been deprecated in 10.10.
        NSBundle* myBundle = [NSBundle mainBundle];                                             //NetworkConfigTool is stored in the XOkey app bundle.
        NSMutableString* executablePath =[NSMutableString stringWithString:[myBundle bundlePath]];
        [executablePath appendString:@"/Contents/Library/LaunchServices/XOkey.NetworkConfigTool"];
        NSMutableDictionary* plist = [NSMutableDictionary dictionary];
        [plist setObject:label forKey:@"Label"];
        [plist setObject:[NSNumber numberWithBool:YES] forKey:@"RunAtLoad"];
        [plist setObject:executablePath forKey:@"Program"];
         result = (BOOL)SMJobSubmit(kSMDomainSystemLaunchd, (__bridge CFDictionaryRef)plist, self->_authRef, &cfError);
        */
        
        
		/* This does all the work of verifying the helper tool against the application
		 * and vice-versa. Once verification has passed, the embedded launchd.plist
		 * is extracted and placed in /Library/LaunchDaemons and then loaded. The
		 * executable is placed in /Library/PrivilegedHelperTools.
		 */

		result = (BOOL) SMJobBless(kSMDomainSystemLaunchd, (__bridge CFStringRef)(label), self->_authRef, &cfError);
        if (!result) {
            error = CFBridgingRelease(cfError);
        }
	}
    if ( ! result && (errorPtr != NULL) ) {
        assert(error != nil);
        *errorPtr = error;
    }
    return result;
}

#pragma mark Network Tool methods
-(NSString*) getSha1:(NSString*)toolPath{
    NSTask* digestTask = [[NSTask alloc]init];
    NSPipe* pipe = [[NSPipe alloc]init];
    NSString* arg = [NSString stringWithFormat:@"openssl sha1 %@ | awk '{print $2}'", toolPath];
    [digestTask setLaunchPath:@"/bin/sh"];
    [digestTask setArguments:@[@"-c",arg]];
    [digestTask setStandardOutput:pipe];
    [digestTask launch];
    return [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSASCIIStringEncoding];
}

//Create the helper tool then connect to it.
-(BOOL)initializeNetworkTool{
    BOOL toolsDifferent = false;
    BOOL newTool = false;
    //  1) Detect if the network tool exists
    if ([[NSFileManager defaultManager]fileExistsAtPath:@"/Library/PrivilegedHelperTools/XOkey.NetworkConfigTool"]) {
        //  1a) Check if the network tool installed has changed compared to the bundle version
        
        //      Get the installed network tool's SHA-1 hash
        NSString* installedToolPath = @"/Library/PrivilegedHelperTools/XOkey.NetworkConfigTool";
        NSString* installedToolSha1 = [self getSha1:installedToolPath];
      
        //      Get the bundle network tool's SHA-1 hash
        NSString* bundleToolPath = [NSString stringWithFormat:@"%@/Contents/Library/LaunchServices/XOkey.NetworkConfigTool",[[NSBundle mainBundle]bundlePath]];
        NSString* bundleToolSha1 = [self getSha1:bundleToolPath];
        
        //      Comapre the 2 digests. If different, remove the current tool and reinstall. If the same, submit to SMJobLess
        if ([installedToolSha1 isEqualToString:bundleToolSha1]) {
            XOkeyLog(@"Network tools are the same!");
            toolsDifferent = false;
        }else{
            XOkeyLog(@"Network tools are different!");
            toolsDifferent = true;
        }

    }else{
        // Tool does not exist in directory
        newTool = true;
    }
    // 2) Create the tool if it doesn't exist or the SHA-1 digest of the installed tool is different than the bundle tool's.
    if (toolsDifferent) {
        NSString * output = nil;
        NSString * processErrorDescription = nil;
        BOOL success = [self runProcessAsAdministrator:@"/bin/rm"
                                         withArguments:[NSArray arrayWithObjects:@"/Library/PrivilegedHelperTools/XOkey.NetworkConfigTool", nil]
                                                output:&output
                                      errorDescription:&processErrorDescription];
            if (!success) {
                XOkeyLog(@"Failed to remove XOkey.NetworkConfigTool with error");
                XOkeyLog(processErrorDescription);
            }else{
                XOkeyLog(@"Succeeded in removing old XOkey.NetworkConfigTool. Installing new tool...");
            }
    }
    
    NSError *error = nil;
    if (![self blessHelperWithLabel:kNetworkConfigToolMachServiceName error:&error newTool:(toolsDifferent | newTool)]) {
        XOkeyLog([NSString stringWithFormat:@"Failed to create network config tool. Error: %@ / %d", [error localizedFailureReason], (int) [error code]]);
        return NO;
    }else{
        XOkeyLog(@"Succeeded in initializing network config tool.");
        networkToolCreated = true;
        XOkeyLog(@"Trying to create XPC interface for IPC.");
        helperInterface = [NSXPCInterface interfaceWithProtocol:@protocol(NetworkConfigToolProtocol)];
        myConnection =    [[NSXPCConnection alloc]
                           initWithMachServiceName:kNetworkConfigToolMachServiceName
                           options:NSXPCConnectionPrivileged];
        if (myConnection == nil) {
            XOkeyLog(@"Failed to create interface and connect to network tool.");
        }else{
            XOkeyLog(@"Successfully created interface and connected to network tool.");
        }
        
        //To receive XPC messages
        myConnection.exportedObject = self;
        myConnection.exportedInterface= [NSXPCInterface interfaceWithProtocol:@protocol(serverProtocol)];
        
        //To send XPC messages
        myConnection.remoteObjectInterface = helperInterface;
        [myConnection resume];
        return YES;
    }
}

//Create the helper tool and setup an XPC connection between the app and the helper tool
-(void)setXOkeyIP{
    if (XOkeyConnected) {
        //Assign an IP address to the XOkey interface
        XOkeyLog([NSString stringWithFormat:@"Attempting to set ip address %@ on %@ (XOkey endpoint)",
                   deviceProperties[XOKEY_IP_ADDRESS],deviceProperties[XOKEY_ENDPOINT]]);
        
        [[myConnection remoteObjectProxy]configNetwork:deviceProperties[XOKEY_IP_ADDRESS]
                                              endPoint:deviceProperties[XOKEY_ENDPOINT]
                                                subNet:deviceProperties[XOKEY_SUBNET]];
    }
}

//  Pass inbound and outbound traffic on the XOkey endpoint
-(void)setupFirewall{
    if (XOkeyConnected){
        [[myConnection remoteObjectProxy]setupFirewall:deviceProperties[XOKEY_ENDPOINT] internetEndpoint:deviceProperties[ACTIVE_ENDPOINT]];
    }
}

//Route traffic to the XOkey USB device
//Network topology:
//Mac -> XOkey -> Mac -> Default Gateway -> Internet
-(void)routeToExoNet:(NSString*)ExoNetIP{
    //Ensure that the ExoNet server path exists for the EK to forward traffic to s
    if ([deviceProperties[EXONET_IP] isNotEqualTo:NOT_SET]) {
        dispatch_sync(networkQueue, ^(void){
            [[myConnection remoteObjectProxy]routeToExoNet:ExoNetIP gateway:deviceProperties[ROUTER] XOkeyEndpoint:deviceProperties[XOKEY_ENDPOINT]];
        });
    }
}

//Remove route to XOkey USB Device (should just be inverse of routeToExoNet)
//Network Topology:
//Mac -> Default Gateway -> Internet
-(void)removeExoNetRoute{
    //Ensure that the ExoNet server path exists for the EK to forward traffic to
    if ([deviceProperties[EXONET_IP] isNotEqualTo:NOT_SET]) {
        [[myConnection remoteObjectProxy]removeDestination:deviceProperties[EXONET_IP] router:deviceProperties[ROUTER]];
        deviceProperties[EXONET_IP] = NOT_SET;
        
        //Send disconnect notification
        disconnNote.title = @"Disconnected from XOnet";
        [[NSUserNotificationCenter defaultUserNotificationCenter]deliverNotification:disconnNote];
    }
}

- (void)applicationWillTerminate:(NSNotification *)aNotification{
    //Remove ExoNet routes from the routing table
    [self removeExoNetRoute];
    //Network tool will force quit when rm'd
    [[myConnection remoteObjectProxy]uninstallNetworkTool];
    [myConnection invalidate];
}

//
//
//Executing root programs via AppleScript. This is not the preferred method and is not implemented in this project.
//Code taken from: http://stackoverflow.com/questions/6841937/authorizationexecutewithprivileges-is-deprecated
//
//
 - (BOOL) runProcessAsAdministrator:(NSString*)scriptPath
                      withArguments:(NSArray *)arguments
                             output:(NSString **)output
                   errorDescription:(NSString **)errorDescription {
 
     NSString * allArgs = [arguments componentsJoinedByString:@" "];
     NSString * fullScript = [NSString stringWithFormat:@"'%@' %@", scriptPath, allArgs];
 
     NSDictionary *errorInfo = [NSDictionary new];
     NSString *script =  [NSString stringWithFormat:@"do shell script \"%@\" with administrator privileges", fullScript];
     
     NSAppleScript *appleScript = [[NSAppleScript new] initWithSource:script];
     NSAppleEventDescriptor * eventResult = [appleScript executeAndReturnError:&errorInfo];
     
     // Check errorInfo
     if (! eventResult)
     {
         // Describe common errors
         *errorDescription = nil;
         if ([errorInfo valueForKey:NSAppleScriptErrorNumber])
         {
             NSNumber * errorNumber = (NSNumber *)[errorInfo valueForKey:NSAppleScriptErrorNumber];
             if ([errorNumber intValue] == -128) *errorDescription = @"The administrator password is required to do this.";
         }
         
         // Set error message from provided message
         if (*errorDescription == nil)
         {
             if ([errorInfo valueForKey:NSAppleScriptErrorMessage]) *errorDescription =  (NSString *)[errorInfo valueForKey:NSAppleScriptErrorMessage];
         }
         
         return NO;
    }else{
        // Set output to the AppleScript's output
        *output = [eventResult stringValue];
         
        return YES;
    }
 }
 
@end
