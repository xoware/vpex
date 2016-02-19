//
//  webViewDelegate.h
//  XOkey
//
//  Created by user on 5/13/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <Foundation/NSURLConnection.h>
#import <WebKit/WebKit.h>
#import "XOkeyAppDelegate.h"
//  Delegate object to handle the webview connection
@interface webViewDelegate : NSObject < NSURLConnectionDelegate, WebUIDelegate, WebFrameLoadDelegate, WebPolicyDelegate, NSURLDownloadDelegate
                                        /*, NSURLSessionDelegate, NSURLSessionDataDelegate*/>

@property WebView* webViewRef;
@property bool loadLoginPage;

//connect to login page
-(int)connectToXOkey:(NSString*)path;

//generic function to set dns server via firmware api
-(void)setXOkeyDNS:(NSString*)first secondDNSServer:(NSString*)second;

//set the XOkey internal DNS to system assigned via DHCP
-(void)setXOKeyDNS_DHCP;

//set the XOkey internal DNS to XOnet DNS/Google DNS
-(void)setXOkeyDNS_Google;

@end
