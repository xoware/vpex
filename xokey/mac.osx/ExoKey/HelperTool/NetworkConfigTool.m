/*
     File: NetworkConfigTool.m
 Abstract: The main object in the helper tool.
  Version: 1.0
 
 Disclaimer: IMPORTANT:  This Apple software is supplied to you by Apple
 Inc. ("Apple") in consideration of your agreement to the following
 terms, and your use, installation, modification or redistribution of
 this Apple software constitutes acceptance of these terms.  If you do
 not agree with these terms, please do not use, install, modify or
 redistribute this Apple software.
 
 In consideration of your agreement to abide by the following terms, and
 subject to these terms, Apple grants you a personal, non-exclusive
 license, under Apple's copyrights in this original Apple software (the
 "Apple Software"), to use, reproduce, modify and redistribute the Apple
 Software, with or without modifications, in source and/or binary forms;
 provided that if you redistribute the Apple Software in its entirety and
 without modifications, you must retain this notice and the following
 text and disclaimers in all such redistributions of the Apple Software.
 Neither the name, trademarks, service marks or logos of Apple Inc. may
 be used to endorse or promote products derived from the Apple Software
 without specific prior written permission from Apple.  Except as
 expressly stated in this notice, no other rights or licenses, express or
 implied, are granted by Apple herein, including but not limited to any
 patent rights that may be infringed by your derivative works or by other
 works in which the Apple Software may be incorporated.
 
 The Apple Software is provided by Apple on an "AS IS" basis.  APPLE
 MAKES NO WARRANTIES, EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
 THE IMPLIED WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY AND FITNESS
 FOR A PARTICULAR PURPOSE, REGARDING THE APPLE SOFTWARE OR ITS USE AND
 OPERATION ALONE OR IN COMBINATION WITH YOUR PRODUCTS.
 
 IN NO EVENT SHALL APPLE BE LIABLE FOR ANY SPECIAL, INDIRECT, INCIDENTAL
 OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 INTERRUPTION) ARISING IN ANY WAY OUT OF THE USE, REPRODUCTION,
 MODIFICATION AND/OR DISTRIBUTION OF THE APPLE SOFTWARE, HOWEVER CAUSED
 AND WHETHER UNDER THEORY OF CONTRACT, TORT (INCLUDING NEGLIGENCE),
 STRICT LIABILITY OR OTHERWISE, EVEN IF APPLE HAS BEEN ADVISED OF THE
 POSSIBILITY OF SUCH DAMAGE.
 
 Copyright (C) 2013 Apple Inc. All Rights Reserved.
 
 */

#import "NetworkConfigTool.h"
#import "../ExoKeyAppDelegate.h"

#include <sys/socket.h>
#include <netinet/in.h>
#include <errno.h>

@interface NetworkConfigTool ()

@end

@implementation NetworkConfigTool{
    NSXPCListener* helperListener;
    NSXPCConnection* conn;
    AuthorizationRef authRef;
}

- (id)init
{
    self = [super init];
    if (self != nil) {
        // Set up our XPC listener to handle requests on our Mach service.
        helperListener = [[NSXPCListener alloc] initWithMachServiceName:kNetworkConfigToolMachServiceName];
        helperListener.delegate = self;
    }
    return self;
}

- (void)run
{
    // Tell the XPC listener to start processing requests.
    [helperListener resume];
    // Run the run loop forever.
    [[NSRunLoop currentRunLoop] run];
}

- (BOOL)listener:(NSXPCListener *)listener shouldAcceptNewConnection:(NSXPCConnection *)newConnection
    // Called by our XPC listener when a new connection comes in.  We configure the connection
    // with our protocol and ourselves as the main object.
{
    assert(listener == helperListener);
    assert(newConnection != nil);
    [self ExoKeyLog:@"Creating XPC connection in the ExoKey network configuration tool."];
    newConnection.exportedInterface = [NSXPCInterface interfaceWithProtocol:@protocol(NetworkConfigToolProtocol)];
    newConnection.exportedObject = self;
    conn = newConnection;
    [newConnection resume];
    return true;
}

//  The client already handles ip address validation
-(void)configNetwork:(NSString*)ipAddress endPoint:(NSString *)BSDDeviceName subNet:(NSString *)mask{
    //  First check if an endpoint has been created for the device.
    if ([BSDDeviceName isNotEqualTo:NOT_SET]) {
        //  Execute the terminal program ipconfig using NSTask.
        NSTask* networkTask = [[NSTask alloc]init];
        NSPipe* pipe = [NSPipe pipe];
        [networkTask setLaunchPath:@"/usr/sbin/ipconfig"];
        if ([ipAddress isEqualTo:@"DHCP"]) {
            [networkTask setArguments:@[@"set",BSDDeviceName,@"DHCP"]];
            [networkTask setStandardOutput:pipe];
            [networkTask launch];
            
            //ipconfig output returns nothing so don't log
            //NSString* outPut = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSASCIIStringEncoding];
            //[self ExoKeyLog:outPut];
        }else{
            //Alans-MacBook-Air-2:LaunchDaemons user$ ipconfig set en3 manual 192.168.255.2 255.255.255.252
            [networkTask setArguments:@[@"set",BSDDeviceName,@"manual",ipAddress,mask]];
            [networkTask setStandardOutput:pipe];
            [networkTask launch];
            
            //ipconfig output returns nothing so don't log
            //NSString* outPut = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSASCIIStringEncoding];
            //[self ExoKeyLog:outPut];
        }
        [self ExoKeyLog:[NSString stringWithFormat:@"Succeeded in assigning ExoKey the IP Address %@ on endpoint %@",ipAddress,BSDDeviceName]];
    }else{
        [self ExoKeyLog:@"Failed to configure the device. Endpoint of the device not set."];
    }
}

//Add entry in the routing table
-(void)destination:(NSString*)destinationIP gateway:(NSString*)gatewayIP subnet:(NSString*)subnetMask{
    NSTask* task = [[NSTask alloc]init];
    NSPipe* pipe = [NSPipe pipe];
    NSString* output;
    //[self ExoKeyLog:[NSString stringWithFormat:@"Routing destination %@ to gateway %@",destinationIP,gatewayIP]];
    [task setLaunchPath:@"/sbin/route"];
    [task setStandardOutput:pipe];
    [task setArguments:@[@"add",@"-rtt",@"0",destinationIP,gatewayIP,@"-netmask",subnetMask]];
    [task launch];
    output = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSUTF8StringEncoding];
    [self ExoKeyLog:output];
}

//Remove entry in the routing table
/*
-(void)removeDestination:(NSString*)destinationIP gateway:(NSString*)gatewayIP subnet:(NSString*)subnetMask{
    NSTask* task = [[NSTask alloc]init];
    NSPipe* pipe = [NSPipe pipe];
    NSString* output;
    //[self ExoKeyLog:[NSString stringWithFormat:@"Routing destination %@ to gateway %@",destinationIP,gatewayIP]];
    [task setLaunchPath:@"/sbin/route"];
    [task setStandardOutput:pipe];
    [task setArguments:@[@"delete",destinationIP,gatewayIP,@"-netmask",subnetMask]];
    [task launch];
    output = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSUTF8StringEncoding];
    [self ExoKeyLog:output];
}
 */
-(void)removeDestination:(NSString*)exoNetIP router:(NSString*)routerIP{
    //Remove default -> EK 1
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"delete",@"0.0.0.0/1",@"192.168.255.1"]];
    //Remove defualt -> EK 2
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"delete",@"128.0.0.0/1",@"192.168.255.1"]];
    //Remove ExoNet -> Router
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"delete",exoNetIP,routerIP]];
}

//Flushing the current routing table requires root access.
-(void)flushRoutingTable{
    NSTask* networkTask = [[NSTask alloc]init];
    NSPipe* pipe = [NSPipe pipe];
    [networkTask setLaunchPath:@"/sbin/route"];
    [networkTask setArguments:@[@"-n",@"flush"]];
    [networkTask setStandardOutput:pipe];
    [networkTask launch];
    NSString* output = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile]encoding:NSUTF8StringEncoding];
    output = [output stringByReplacingOccurrencesOfString:@"\n" withString:@""];
    [self ExoKeyLog:output];
}

//To setup firewall/IP fowarding
-(void)setupFirewall:(NSString*)ExoKeyEndpoint internetEndpoint:(NSString*)inetEndpoint{
    [self ExoKeyLog:@"***Configuring firewall and NAT rules for ExoKey***"];
    //Create the basic ExoKey config file
    
    // 1) Enable PF
    //[NSTask launchedTaskWithLaunchPath:@"/sbin/pfctl" arguments:@[@"-e"]];
    /*[self ExoKeyLog:@"Enable PF"];
    NSTask* enablePF = [[NSTask alloc]init];
    NSPipe* pipe = [NSPipe pipe];
    [enablePF setLaunchPath:@"/sbin/pfctl"];
    [enablePF setArguments:@[@"-e"]];
    [enablePF setStandardOutput:pipe];
    [enablePF launch];
    NSString* output = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile]encoding:NSUTF8StringEncoding];
    [self ExoKeyLog:output];
*/
    //2) Write the config file rules to enable traffic through the EK and NAT rules for the EK
    [self ExoKeyLog:[NSString stringWithFormat:@"Writing PF config file for ExoKey at path %@",PF_CONF_PATH]];
    if([self writePFConfigFile:ExoKeyEndpoint internetEndpoint:inetEndpoint]){
        [self ExoKeyLog:[NSString stringWithFormat:@"Successfully wrote PF config file for ExoKey at path %@",PF_CONF_PATH]];
    }else{
        [self ExoKeyLog:[NSString stringWithFormat:@"Failed to write PF config file for ExoKey at path %@",PF_CONF_PATH]];
        return;
    }

    
    //3)    Enable packet forwarding (NAT) for IPv4
    [self ExoKeyLog:@"Enable IPv4 packet forwarding (net.inet.ip.forwarding=1)"];
    [NSTask launchedTaskWithLaunchPath:@"/usr/sbin/sysctl" arguments:@[@"-w",@"net.inet.ip.forwarding=1"]];
    
    //4)    Enable packet forwarding (NAT) for IPv6
    [self ExoKeyLog:@"Enable IPv6 packet forwarding (net.inet6.ip6.forwarding=1)"];
    [NSTask launchedTaskWithLaunchPath:@"/usr/sbin/sysctl" arguments:@[@"-w",@"inet6.ip6.forwarding=1"]];
    
    //5)    Enable PF
    [NSTask launchedTaskWithLaunchPath:@"/sbin/pfctl" arguments:@[@"-e"]];
    
    //6)    Load the PF configuration file
    [NSTask launchedTaskWithLaunchPath:@"/sbin/pfctl" arguments:@[@"-f",PF_CONF_PATH]];
    [NSTask launchedTaskWithLaunchPath:@"/sbin/pfctl" arguments:@[@"-sr"]];

    /*
    //Allow all incoming traffic on the ExoKey endpoint using IPFW
    [NSTask launchedTaskWithLaunchPath:@"/sbin/ipfw" arguments:@[@"add",@"allow",@"all",@"from",
                                                                 @"any",@"to",@"any",@"via",ExoKeyEndpoint]];
    
    
    //Enable IPFW using sysctl
    NSTask* task = [[NSTask alloc]init];
    NSPipe* pipe = [NSPipe pipe];
    [task setStandardOutput:pipe];
    [task setLaunchPath:@"/usr/sbin/sysctl"];
    [task setArguments:@[@"-w",@"net.inet.ip.fw.enable=1"]];
    [task launch];
    NSString* output = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile]encoding:NSUTF8StringEncoding];
    output = [output stringByReplacingOccurrencesOfString:@"\n" withString:@""];
    [self ExoKeyLog:output];
     */

}

-(BOOL)writePFConfigFile:(NSString*)ExoKeyEndpoint internetEndpoint:(NSString*)inetEndpoint{
    NSFileManager* fileManager = [NSFileManager defaultManager];
    //NSString* path = @"/etc/exokey-pf.conf";
    NSString* ruleSet = [NSString stringWithFormat:/*Original rules from /etc/pf.conf*/
                                                    @"# Packet filter and NAT rules for ExoKey\n"
                                                    "scrub-anchor \"com.apple*\"\n"
                                                    "nat-anchor \"com.apple/*\"\n"
                                                    "rdr-anchor \"com.apple/*\"\n"
                                                    "nat on %@ from %@:network to any -> (%@)\n"        //New NAT rules for ExoKey
                                                   // "pass out on %@ from %@:net to any nat-to %@\n"           //Try new rule
                                                    "dummynet-anchor \"com.apple/*\"\n"
                                                    "anchor \"com.apple/*\"\n"
                                                    "load anchor \"com.apple\" from \"/etc/pf.anchors/com.apple\"\n"
                                                    /*New filter rules for ExoKey*/
                                                    "pass in quick on %@ all\n"
                                                    "pass out quick on %@ all\n",
                                                    inetEndpoint,ExoKeyEndpoint,inetEndpoint,
                                                    ExoKeyEndpoint,
                                                    ExoKeyEndpoint
                                                    ];
    NSData* data = [ruleSet dataUsingEncoding:NSUTF8StringEncoding];
    return [fileManager createFileAtPath:PF_CONF_PATH contents:data attributes:nil];
}

//SMJobRemove doesn't remove the network tool nor the .plist from any directories so we remove it manually
-(void)uninstallNetworkTool{
    //Remove PF rules for EK
    [self ExoKeyLog:@"Remove PF rules for ExoKey"];
    [NSTask launchedTaskWithLaunchPath:@"/sbin/pfctl" arguments:@[@"-F",/*PF_CONF_PATH*/@"all"]];
     //Unload the network tool from launchd
    [self ExoKeyLog:@"Unloading the network tool from launchd"];
    [NSTask launchedTaskWithLaunchPath:@"/bin/launchctl" arguments:@[@"-w",@"unload",@"ExoKey.NetworkConfigTool"]];
    //Remove the ExoKey pf rules
    [self ExoKeyLog:@"Removing ExoKey pf config file from /etc"];
    [NSTask launchedTaskWithLaunchPath:@"/bin/rm" arguments:@[@"/etc/exokey-pf.conf"]];
    //Remove the launchd plist
    [self ExoKeyLog:@"Removing the network tool launchd plist from dir /Library/LaunchDaemons"];
    [NSTask launchedTaskWithLaunchPath:@"/bin/rm" arguments:@[@"/Library/LaunchDaemons/ExoKey.NetworkConfigTool.plist"]];
    //Remove the network tool. This will cause a force quit.
    [self ExoKeyLog:@"Removing the network tool from dir /Library/PrivilegedHelperTools"];
    [NSTask launchedTaskWithLaunchPath:@"/bin/rm" arguments:@[@"/Library/PrivilegedHelperTools/ExoKey.NetworkConfigTool"]];
}

//Log text to server application for handling
-(void)ExoKeyLog:(NSString*)text{
    //To send XPC messages
    conn.remoteObjectInterface = [NSXPCInterface interfaceWithProtocol:@protocol(serverProtocol)];
    [[conn remoteObjectProxy]messageWrapper:text];
}
@end
