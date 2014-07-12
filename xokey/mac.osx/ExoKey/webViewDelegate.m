//
//  webViewDelegate.m
//  ExoKey
//
//  Created by user on 5/13/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import "webViewDelegate.h"
#import "ExoKeyAppDelegate.h"

@implementation webViewDelegate
{
    //  Private variables.
    NSMutableData* receivedData;
    NSURLConnection* conn;
}

-(id)init{
    self = [super init];
    if (self != nil) {

    }
    return self;
}
//*******************************************************************
//  Attempts to connect to the HTTPS server on the ExoKey
//  From Stack overflow, only use this to authenticate the self-signed server certifciate. NSURLConnection
//  doesn't fetch any additional CSS resources from the server. The CSS isn't loading despite having JS embedded
//  in the HTML to load the CSS (hence the image not loading).
//  http://stackoverflow.com/questions/11573164/uiwebview-to-view-self-signed-websites-no-private-api-not-nsurlconnection-i
//*******************************************************************
-(int)connectToExoKey:(NSString*)path{
    NSURLCache *URLCache = [[NSURLCache alloc] initWithMemoryCapacity:4 * 1024 * 1024
                                                         diskCapacity:20 * 1024 * 1024
                                                             diskPath:nil];
    [NSURLCache setSharedURLCache:URLCache];
   
    receivedData = [NSMutableData dataWithCapacity: 0];
    NSURL* url = [NSURL URLWithString:@"https://192.168.255.1/ek/login.html"];
    NSURLRequest* request = [NSURLRequest requestWithURL:url];
    conn = [[NSURLConnection alloc] initWithRequest:request delegate:self];
    if (!conn) {
        ExoKeyLog(@"Failed to connect to https://192.168.255.1");
        return 0;
    }else{
        ExoKeyLog(@"Connecting to https://192.168.255.1");
    }
    return 1;
}

//
//  NSURLConnectionDelegate Methods to handle HTTPS authentication of the certificate.
//

- (BOOL)connection:(NSURLConnection *)connection canAuthenticateAgainstProtectionSpace:(NSURLProtectionSpace *)protectionSpace
{
    return YES;
}

- (void)connection:(NSURLConnection *)connection didReceiveAuthenticationChallenge:(NSURLAuthenticationChallenge *)challenge
{
    if([challenge.protectionSpace.host isEqualToString:@"192.168.255.1"]){
        [challenge.sender useCredential:[NSURLCredential credentialForTrust:challenge.protectionSpace.serverTrust] forAuthenticationChallenge:challenge];
        ExoKeyLog(@"Validated ExoKey credential.");
        
        //NSURLConnection doesn't fetch resources such ass CSS and JS files. After initial authorization of the ExoKey server,
        //cancel the current connection and then reconnect to the server using WebView which handles all the resource fetching.
        [connection cancel];
        ExoKeyAppDelegate* app = (ExoKeyAppDelegate*)[[NSApplication sharedApplication]delegate];
        [[app.ek_WebView mainFrame]loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:@"https://192.168.255.1/ek/login.html"]]];
        
        //Released the connection and data resources since they are no longer needed (WebView handles fetching the files)
        conn = nil;
        receivedData = nil;
    }
}

//
//  NSURLConnection delegate methods that are required to be implemented.
//

- (void)connection:(NSURLConnection *)connection didReceiveResponse:(NSURLResponse *)response
{
    // This method is called when the server has determined that it
    // has enough information to create the NSURLResponse object.
    
    // It can be called multiple times, for example in the case of a
    // redirect, so each time we reset the data.
    
    // receivedData is an instance variable declared elsewhere.
    [receivedData setLength:0];
}

- (void)connection:(NSURLConnection *)connection
  didFailWithError:(NSError *)error
{
    conn = nil;
    receivedData = nil;
    
    ExoKeyLog([NSString stringWithFormat:@"Connection failed! Error - %@ %@",
          [error localizedDescription],
          [[error userInfo] objectForKey:NSURLErrorFailingURLStringErrorKey]]);
}

- (void)connectionDidFinishLoading:(NSURLConnection *)connection
{
    //Should really never reach here.
    //NSString *path = [[NSBundle mainBundle] bundlePath];
    //NSURL *baseURL = [NSURL fileURLWithPath:path];
    //NSString* htmlData = [[NSString alloc]initWithData:receivedData encoding:NSUTF8StringEncoding];
    //[[app.ek_WebView mainFrame] loadData:receivedData MIMEType: @"text/html" textEncodingName: @"UTF-8" baseURL:baseURL];
    conn = nil;
    receivedData = nil;
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data
{
    [receivedData appendData:data];
}

//To handle JS causing an open file modal dialog to open. resultListener is the UIDelegate
- (void)webView:(WebView *)sender runOpenPanelForFileButtonWithResultListener:(id < WebOpenPanelResultListener >)resultListener
{
    //Open panel code comes from the File System Programming Guide from Apple
    NSOpenPanel* panel = [NSOpenPanel openPanel];
    // This method displays the panel and returns immediately.
    // The completion handler is called when the user selects an
    // item or cancels the panel.
    [panel beginWithCompletionHandler:^(NSInteger result){
        if (result == NSFileHandlingPanelOKButton) {
            NSURL*  theDoc = [[panel URLs] objectAtIndex:0];
            // Send the filename to the listener (webview)
            [resultListener chooseFilename:[theDoc relativePath]];
        }
    }];
}
@end
