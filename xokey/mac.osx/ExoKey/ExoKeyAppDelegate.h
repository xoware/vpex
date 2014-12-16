//
//  ExoKeyAppDelegate.h
//  ExoKey
//
//  Created by user on 5/8/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import <WebKit/WebKit.h>
#import <Foundation/NSURLConnection.h>
#import <ServiceManagement/ServiceManagement.h>
#import <Security/Authorization.h>
#import "ExoKey_Definitions.h"
#import "usbObject.h"
#import "webViewDelegate.h"
#import "NetworkConfigTool.h"
#import "statusDelegate.h"

void ExoKeyLog(NSString* text);
@protocol serverProtocol
@required

//Wrapper to ExoKeyLog to log text from the network tool daemon
-(void)messageWrapper:(NSString*)text;

@end

@interface ExoKeyAppDelegate : NSObject <NSApplicationDelegate,
                                        NSURLConnectionDelegate,

                                        serverProtocol>
{
    AuthorizationRef        _authRef;
}

//Methods

//GUI
-(void)initializeGUI;

//Network Tool
-(void)authorize;
-(BOOL)initializeNetworkTool;
-(void)setupFirewall;

//ExoKey
-(void)setExoKeyIP;
-(void)devProc:(NSNotification*) notification;

//HTTPS Site/Routing
-(void)appPoll:(NSTimer*)timer;
-(void)findRouter;
-(void)getActiveInterface;

//ExoNet
-(BOOL)resolveHostName:(NSString*)ExoNetHostName;
-(void)routeToExoNet:(NSString*)ExoNetIP;
-(void)removeExoNetRoute;

//Actions
-(IBAction)reconnect:(id)sender;
- (IBAction)closeModalDialog:(id)sender;

//Wait Window Methods
-(void)openWaitWindow;
-(void)closeWaitWindow:(NSTimer*)timer;
-(void)didEndSheet:(NSWindow *)sheet returnCode:(NSInteger)returnCode
        contextInfo:(void *)contextInfo;

//Methods to Call External Programs
- (BOOL)blessHelperWithLabel:(NSString *)label error:(NSError **)errorPtr;
- (BOOL) runProcessAsAdministrator:(NSString*)scriptPath withArguments:(NSArray *)arguments
                            output:(NSString **)output errorDescription:(NSString **)errorDescription;

//Properties
@property (assign) IBOutlet NSWindow *window;
@property (weak) IBOutlet NSButton *ek_ConnectedDisplay;
@property (weak) IBOutlet WebView *ek_WebView;
@property (atomic, copy,   readwrite) NSData *authorization;
@property (unsafe_unretained) IBOutlet NSPanel *waitWindow;

@end
