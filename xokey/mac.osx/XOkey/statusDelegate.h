//
//  statusDelegate.h
//  XOkey
//
//  Created by user on 6/13/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import <Foundation/Foundation.h>
typedef void (^CompletionHandlerType)();

//  Delegate object to handle polling of the server status.
@interface statusDelegate : NSObject </*NSURLConnectionDelegate, NSURLConnectionDataDelegate,*/ NSURLSessionDelegate, NSURLSessionDataDelegate>

@property int VPN_Status;
@property NSMutableString* exoNetHostName;

-(int)pollStatus;

//

//
@end
