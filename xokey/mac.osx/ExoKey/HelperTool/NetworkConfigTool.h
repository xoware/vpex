/*
     File: NetworkConfigTool.h
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

#import <Foundation/Foundation.h>
#import "ExoKey_Definitions.h"

// kNetworkConfigToolMachServiceName is the Mach service name of the helper tool.  Note that the value 
// here has to match the value in the MachServices dictionary in "NetworkConfigTool-Launchd.plist".

#define kNetworkConfigToolMachServiceName @"ExoKey.NetworkConfigTool"

// NetworkConfigToolProtocol is the NSXPCConnection-based protocol implemented by the helper tool 
// and called by the app.

@protocol NetworkConfigToolProtocol

@required

//Connect to the device with the given IP address (or using DHCP) on the given dev handle.
-(void)configNetwork:(NSString*)ipAddress endPoint:(NSString*)BSDDeviceName subNet:(NSString*)mask;

//Generic routing routine
//-(void)destination:(NSString*)destinationIP gateway:(NSString*)gatewayIP subnet:(NSString*)subnetMask;

//Add routes to ExoNet
-(void)routeToExoNet:(NSString*)exoNetIP gateway:(NSString*)gatewayIP exokeyEndpoint:(NSString*)ekEndpoint;

//Remove route
//-(void)removeDestination:(NSString*)destinationIP gateway:(NSString*)gatewayIP subnet:(NSString*)subnetMask;
-(void)removeDestination:(NSString*)exoNetIP router:(NSString*)routerIP;

//Flush routing table
-(void)flushRoutingTable;

//To setup firewall/IP fowarding
-(void)setupFirewall:(NSString*)endpoint internetEndpoint:(NSString*)inetEndpoint;

//For uninstalling the helper tool. rm will force quit the current running app. SMJobSubmit has a few issues
//TODO: Implement SMJobSubmit instead which will submit the job to launchd without requiring creating the app in any dirs
-(void)uninstallNetworkTool;
@end

// The following is the interface to the class that implements the helper tool.
// It's called by the helper tool's main() function, but not by the app directly.

@interface NetworkConfigTool : NSObject  <NSXPCListenerDelegate, NetworkConfigToolProtocol>

- (id)init;
- (void)run;
- (void)ExoKeyLog:(NSString*)text;
- (BOOL)writePFConfigFile:(NSString*)ExoKeyEndpoint internetEndpoint:(NSString*)inetEndpoint;
- (void)getCurrentDNSServers;
- (void)resetDNSServers;

@end
