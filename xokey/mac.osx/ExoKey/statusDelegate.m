//
//  statusDelegate.m
//  ExoKey
//
//  Created by user on 6/13/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import "statusDelegate.h"
#import "ExoKeY_Definitions.h"
#import "ExoKeyAppDelegate.h"

@implementation statusDelegate
{
    BOOL connectionFree;         //Determines if app is currently fetching VPN info.
    NSMutableData* statusData;
}
-(id)init{
    self = [super init];
    if(self != nil){
        _VPN_Status = VPN_DISCONNECTED;
        connectionFree = true;
        _exoNetHostName = (NSMutableString*)NOT_SET;
    }
    return self;
}

//Note, NSURLConnection manages cookies already. It will send the appropriate cookie when a request to a domain is matched
//against a cookie containing that domain key.
-(int)pollStatus{
    //Check if
    if (connectionFree) {
        //Setup connection to check for VPN status
        NSMutableURLRequest* vpnRequest = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:@"https://192.168.255.1/api/GetVpnStatus"]
                                                    cachePolicy:NSURLRequestUseProtocolCachePolicy
                                                timeoutInterval:10.0];
/*
        //Fetch the app cookies of the app. The shared cookie storage contains all cookies on the machine.
        NSHTTPCookieStorage* cookieStorage = [NSHTTPCookieStorage sharedHTTPCookieStorage];
        NSArray* sortArray = [NSArray arrayWithObject:[NSSortDescriptor sortDescriptorWithKey:@"name" ascending:YES]];
        NSArray* cookies = [cookieStorage sortedCookiesUsingDescriptors:sortArray];
        NSHTTPCookie* exoKeyCookie = nil;
        //int i = 0;
        //ExoKey cookie info.
        //name:     "xo_session"
        //domain:   "192.168.255.1"
        //
        for(NSHTTPCookie* cookie in cookies){
            //i++;
            //NSLog(@"\n%d: %@ \n",i,cookie);
            if ([[cookie name] isEqualTo:@"xo_session"]) {
                exoKeyCookie = cookie;
            }
        }

        //Attach ExoKey cookie to the header.
        if (exoKeyCookie) {
            NSArray* arrayWrapper = [NSArray arrayWithObject:exoKeyCookie];
            NSDictionary* additionalHeader = [NSHTTPCookie requestHeaderFieldsWithCookies:arrayWrapper];
            [vpnRequest setAllHTTPHeaderFields:additionalHeader];
        }
*/
        statusData = [NSMutableData dataWithCapacity:0];
        NSURLConnection* vpnConnection = [[NSURLConnection alloc]initWithRequest:vpnRequest delegate:self];
        if (!vpnConnection) {
            statusData = nil;
            return _VPN_Status;
        }
        connectionFree = false;
        
    }
    return _VPN_Status;
}

- (void)connection:(NSURLConnection *)connection didReceiveResponse:(NSURLResponse *)response{
    //As suggested by the URL loading guide, the connection might recieve several responses such
    //as redirets so reset the data buffer to 0
    [statusData setLength:0];
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data{
    [statusData appendData:data];
}

- (void)connection:(NSURLConnection *)connection didFailWithError:(NSError *)error{
    
    //Connection failed. It can now be restarted. No need to release objects in ARC
    NSLog(@"VPN status request failed with error %@:",error);
    _VPN_Status = VPN_DISCONNECTED;
    connectionFree = true;
    ExoKeyLog(@"ExoKey is not responding.");
}

- (void)connectionDidFinishLoading:(NSURLConnection *)connection{
    //Done loading data. Connection can be established again.
 
    //////////////////////////////////////
    //Parse JSON data for the VPN status//
    //////////////////////////////////////
    NSError* err;
    NSMutableDictionary* topLevel = [NSJSONSerialization JSONObjectWithData:statusData options:0 error:&err];
    if(err){
        _VPN_Status = VPN_DISCONNECTED;
        connectionFree = true;
        [statusData setLength:0];
        return;
    }
    NSDictionary* activateVPN = [topLevel objectForKey:@"active_vpn"];
    //User is not logged into server.
    if ([activateVPN isEqualTo:[NSNull null]]) {
        _VPN_Status = VPN_DISCONNECTED;
        connectionFree = true;
        [statusData setLength:0];
        return;
    }
    NSString* state = [activateVPN objectForKey:@"state"];
    //NSLog(@"The state of the VPN connection: %@",state);
    //VPN is connected.
    if ([state isEqualToString:@"Connected"]) {
        _VPN_Status = VPN_CONNECTED;
        NSDictionary* exoNetInfo = [[activateVPN objectForKey:@"address"]objectAtIndex:0];
        self.exoNetHostName = [exoNetInfo objectForKey:@"host"];
        connectionFree = true;
        [statusData setLength:0];
        return;
    }
    
    //VPN is disconnected.
    _VPN_Status = VPN_DISCONNECTED;
    connectionFree = true;
    [statusData setLength:0];

}
@end
