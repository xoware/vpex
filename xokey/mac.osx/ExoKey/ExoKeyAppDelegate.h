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
-(void)initializeIPAddressBox;

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

//ExoNet
-(BOOL)resolveHostName:(NSString*)ExoNetHostName;
-(void)routeToExoNet:(NSString*)ExoNetIP;
-(void)removeExoNetRoute;

//Actions
- (IBAction)reconnect:(id)sender;
- (IBAction)selectedDHCPButton:(id)sender;
- (IBAction)setIPAddressButton:(id)sender;


//- (BOOL)blessHelperWithLabel:(NSString *)label error:(NSError **)errorPtr;

/*      
Keep in case we need to run a single process as root.
 
 - (BOOL) runProcessAsAdministrator:(NSString*)scriptPath
 withArguments:(NSArray *)arguments
 output:(NSString **)output
 errorDescription:(NSString **)errorDescription;
 
 */

//Properties
@property (assign) IBOutlet NSWindow *window;
@property (weak) IBOutlet NSButton *ek_ConnectedDisplay;
@property (weak) IBOutlet WebView *ek_WebView;
@property (weak) IBOutlet NSTextField *IPBox1;
@property (weak) IBOutlet NSTextField *IPBox2;
@property (weak) IBOutlet NSTextField *IPBox3;
@property (weak) IBOutlet NSTextField *IPBox4;
@property (weak) IBOutlet NSButton *DHCPCheckBox;
@property (atomic, copy,   readwrite) NSData *                  authorization;
@property (unsafe_unretained) IBOutlet NSTextView *GUILog;
@property (unsafe_unretained) IBOutlet NSTextView *devicePropertiesView;

@end
