//
//  XOkeyAppDelegate.h
//  XOkey
//
//  Created by user on 5/8/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import <WebKit/WebKit.h>
#import <Foundation/NSURLConnection.h>
#import <ServiceManagement/ServiceManagement.h>
#import <Security/Authorization.h>
#import "XOkey_Definitions.h"
#import "usbObject.h"
#import "webViewDelegate.h"
#import "NetworkConfigTool.h"
#import "statusDelegate.h"
@import SystemConfiguration;

void XOkeyLog(NSString* text);
@protocol serverProtocol
@required

//Wrapper to XOkeyLog to log text from the network tool daemon
-(void)messageWrapper:(NSString*)text;

@end

@interface XOkeyAppDelegate : NSObject <NSApplicationDelegate,
                                        NSURLConnectionDelegate,
                                        NSUserNotificationCenterDelegate,
                                        serverProtocol>
{
    AuthorizationRef        _authRef;
}

//Methods

//GUI
-(void)initializeGUI;

//Main GUI Actions
-(IBAction)reconnect:(id)sender;
- (IBAction)closeModalDialog:(id)sender;

//Wait Window Methods
-(void)openWaitWindow;
-(void)closeWaitWindow:(NSTimer*)timer;
-(void)didEndSheet:(NSWindow *)sheet returnCode:(NSInteger)returnCode
       contextInfo:(void *)contextInfo;

//Methods to control windows
-(void)openStatusWindow;
-(void)openMainWindow;
-(void)windowSelect;

//Network Tool
-(NSString*) getSha1:(NSString*)toolPath;
-(void)authorize;
-(BOOL)initializeNetworkTool;
-(void)setupFirewall;

//XOkey configuration methods
-(void)setXOkeyIP;
-(void)devProc:(NSNotification*) notification;
-(void)configureDevice;

//Laptop and internet configuration methods
-(void)findRouter;
-(void)getActiveInterface;

//ExoNet
-(BOOL)resolveHostName:(NSString*)ExoNetHostName;
-(void)routeToExoNet:(NSString*)ExoNetIP;
-(void)removeExoNetRoute;



//Methods to delegate from external objects/network tool
- (BOOL)blessHelperWithLabel:(NSString *)label error:(NSError **)errorPtr newTool:(BOOL)isNewTool;
- (BOOL) runProcessAsAdministrator:(NSString*)scriptPath withArguments:(NSArray *)arguments
                            output:(NSString **)output errorDescription:(NSString **)errorDescription;

//Properties
@property (assign) IBOutlet NSWindow *window;
@property (weak) IBOutlet NSButton *ek_ConnectedDisplay;
@property (weak) IBOutlet WebView *ek_WebView;
@property (atomic, copy,   readwrite) NSData *authorization;
@property (unsafe_unretained) IBOutlet NSPanel *waitWindow;
@property (weak) IBOutlet NSView *modalDialogView;

//Status window
@property (weak) IBOutlet NSWindow *statusWindow;
@property (weak) IBOutlet NSTextField *pluginStatus;
@property (weak) IBOutlet NSTextField *connectionStatus;
- (IBAction)loginToMainWindow:(id)sender;
@property (weak) IBOutlet NSButton *statusLoginButton;
@property (weak) IBOutlet NSTextField *configStatus;


@end
