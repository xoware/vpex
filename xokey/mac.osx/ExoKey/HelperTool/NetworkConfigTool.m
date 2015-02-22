/*
    File: NetworkConfigTool.m
    Abstract: The main object in the helper tool.
    Version: 1.0
    Note: Standard out and standard error are undefined in release mode. In debug mode, we define it as: /etc/ExoKey.log
          The standard output and standard input keys must be removed in order for XPC services to work.
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
            //ipconfig set en3 manual 192.168.255.2 255.255.255.252
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
/*
-(void)destination:(NSString*)destinationIP gateway:(NSString*)gatewayIP subnet:(NSString*)subnetMask{
    NSTask* task = [[NSTask alloc]init];
    NSPipe* pipe = [NSPipe pipe];
    NSString* output;
    //[self ExoKeyLog:[NSString stringWithFormat:@"Routing destination %@ to gateway %@",destinationIP,gatewayIP]];
    [task setLaunchPath:@"/sbin/route"];
    [task setStandardOutput:pipe];
    [task setArguments:@[@"-nv",@"add",@"-rtt",@"0.01",@"-host",destinationIP,gatewayIP,@"-netmask",subnetMask]];
    [task launch];
    output = [[NSString alloc]initWithData:[[pipe fileHandleForReading]readDataToEndOfFile] encoding:NSUTF8StringEncoding];
    [self ExoKeyLog:output];
}
*/
-(void)routeToExoNet:(NSString*)exoNetIP gateway:(NSString*)gatewayIP exokeyEndpoint:(NSString*)ekEndpoint{
    //Add default -> EK 1
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"-nv",@"add",@"-rtt",@"0",@"-net",@"0.0.0.0",@"192.168.255.1",@"-netmask",@"128.0.0.0"]];
    //Add defualt -> EK 2
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"-nv",@"add",@"-rtt",@"0",@"-net",@"128.0.0.0/1",@"192.168.255.1",@"-netmask",@"128.0.0.0"]];
    //Add ExoNet -> Router
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"-nv",@"add",@"-host",exoNetIP,gatewayIP]];
    
    //Delete default -> link layer route
    //route delete -ifscope en3 0.0.0.0/0
    [NSTask launchedTaskWithLaunchPath:@"/sbin/route" arguments:@[@"delete",@"-ifscope",ekEndpoint,@"0.0.0.0/0"]];
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
    [NSTask launchedTaskWithLaunchPath:@"/usr/sbin/sysctl" arguments:@[@"-w",@"net.inet6.ip6.forwarding=1"]];
    
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
                                                    //"scrub-anchor \"com.apple*\"\n"
                                                    //"nat-anchor \"com.apple/*\"\n"
                                                    //"rdr-anchor \"com.apple/*\"\n"
                                                    "nat on %@ from %@:network to any -> (%@)\n",      //New NAT rules for ExoKey
                                                    //"dummynet-anchor \"com.apple/*\"\n"
                                                    //"anchor \"com.apple/*\"\n"
                                                    //"load anchor \"com.apple\" from \"/etc/pf.anchors/com.apple\"\n"
                         
                                                    /* 
                                                     **Filter rules for ExoKey are not necessary
                                                     **since the defualt behavior is to pass all packets
                                                     **to and from any endpoint/IP.
                                                     **
                                                     */
                         
                                                    //"pass in quick on %@ all\n"
                                                    //"pass out quick on %@ all\n"
                                                    //"pass in  on %@ from 192.168.255.1 to any\n"
                                                    //"pass out on %@ from any to 192.168.255.1\n",
                                                    inetEndpoint,ExoKeyEndpoint,inetEndpoint//,
                                                   // ExoKeyEndpoint,
                                                    //ExoKeyEndpoint,
                                                    //inetEndpoint,
                                                    //inetEndpoint
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
