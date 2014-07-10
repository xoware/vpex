//
//  webViewDelegate.h
//  ExoKey
//
//  Created by user on 5/13/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <Foundation/NSURLConnection.h>
#import "ExoKeyAppDelegate.h"
//  Delegate object to handle the webview connection
@interface webViewDelegate : NSObject <NSURLConnectionDelegate>

-(int)connectToExoKey:(NSString*)path;

@end
