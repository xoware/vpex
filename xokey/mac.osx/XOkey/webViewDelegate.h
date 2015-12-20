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

-(int)connectToXOkey:(NSString*)path;

@end
