//
//  webViewDelegate.m
//  XOkey
//
//  Created by user on 5/13/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import "webViewDelegate.h"
#import "XOkeyAppDelegate.h"

@implementation webViewDelegate
{
    //  Private variables.
    NSMutableData* receivedData;

    //NSURLSession* defaultSession;
}

-(id)init{
    self = [super init];
    if (self != nil) {

    }
    //  Set the app delegate as a UIDelegate to handle new window requests
    XOkeyAppDelegate* app = (XOkeyAppDelegate*)[[NSApplication sharedApplication]delegate];

    //set this object as frameload delegate to detect when webview has fully loaded
    [app.ek_WebView setFrameLoadDelegate:self];
    
    //must initially load the login page
    _loadLoginPage = true;
    
    return self;
}

//*******************************************************************
//  Attempts to connect to the HTTPS server on the XOkey
//  From Stack overflow, only use this to authenticate the self-signed server certifciate. NSURLConnection
//  doesn't fetch any additional CSS resources from the server. The CSS isn't loading despite having JS embedded
//  in the HTML to load the CSS (hence the image not loading).
//  http://stackoverflow.com/questions/11573164/uiwebview-to-view-self-signed-websites-no-private-api-not-nsurlconnection-i
//*******************************************************************
-(int)connectToXOkey:(NSString*)path{
    NSURLCache *URLCache = [[NSURLCache alloc] initWithMemoryCapacity:4 * 1024 * 1024
                                                         diskCapacity:20 * 1024 * 1024
                                                             diskPath:nil];
    [NSURLCache setSharedURLCache:URLCache];

    receivedData = [NSMutableData dataWithCapacity: 0];
    NSURL* url = [NSURL URLWithString:@"https://192.168.255.1/ek/login.html"];
    NSURLRequest* request = [NSURLRequest requestWithURL:url];
    NSURLConnection* conn = [[NSURLConnection alloc] initWithRequest:request delegate:self];
    if (!conn) {
        XOkeyLog(@"Failed to connect to https://192.168.255.1");
        return 0;
    }else{
        XOkeyLog(@"Connecting to https://192.168.255.1");
    }
    
//TODO: NSURLSession doesn't work well with custom certifications. Must implement in the future.
//    NSURLSessionConfiguration *defaultConfigObject = [NSURLSessionConfiguration defaultSessionConfiguration];
//    defaultSession = [NSURLSession sessionWithConfiguration:defaultConfigObject delegate:self delegateQueue:[NSOperationQueue mainQueue]];
//    NSURLSessionDataTask *dataTask = [self->defaultSession dataTaskWithURL: [NSURL URLWithString:@"https://192.168.255.1/ek/login.html"]];
//    [dataTask resume];

    return 1;
}
#pragma mark    NSURLConnectionDelegate Methods to handle HTTPS authentication of the certificate.

//  Generic method to load the EK login page in the Webview located in the app deleagate.
- (void)webViewloadLoginPage{
    XOkeyAppDelegate* app = (XOkeyAppDelegate*)[[NSApplication sharedApplication]delegate];
    //Stop loading anything that's currently loading. Could have been the reason that the EK
    //server was periodically freezing. Sleep a bit to let the webkit clear up the session before connecting to the server.
    [[app.ek_WebView mainFrame]stopLoading];
    sleep(1.0);
    [[app.ek_WebView mainFrame]loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:@"https://192.168.255.1/ek/login.html"]]];
}

- (BOOL)connection:(NSURLConnection *)connection canAuthenticateAgainstProtectionSpace:(NSURLProtectionSpace *)protectionSpace
{
    return YES;
}

//NSURLConnection doesn't fetch resources such ass CSS and JS files. After initial authorization of the XOkey server,
//cancel the current connection and then reconnect to the server using WebView which handles all the resource fetching.
- (void)connection:(NSURLConnection *)connection didReceiveAuthenticationChallenge:(NSURLAuthenticationChallenge *)challenge
{
    if([challenge.protectionSpace.host isEqualToString:@"192.168.255.1"]){
        [challenge.sender useCredential:[NSURLCredential credentialForTrust:challenge.protectionSpace.serverTrust] forAuthenticationChallenge:challenge];
    }
}

#pragma mark    NSURLConnection delegate methods that are required to be implemented.
- (void)connection:(NSURLConnection *)connection didReceiveResponse:(NSURLResponse *)response
{
    //Cancel the connection since NSURLConnection doesn't handle CS or JS resources. Since the app has now authenticated with the self-signed
    //XOkey, connect to the webserver with Webkit.
    [connection cancel];
    [receivedData setLength:0];
    
    //Released the connection and data resources since they are no longer needed.
    //WebView handles fetching the files.
    receivedData = nil;
    [self webViewloadLoginPage];
}

- (void)connection:(NSURLConnection *)connection
  didFailWithError:(NSError *)error
{
    receivedData = nil;
    
    XOkeyLog([NSString stringWithFormat:@"Connection failed! Error - %@ %@",
          [error localizedDescription],
          [[error userInfo] objectForKey:NSURLErrorFailingURLStringErrorKey]]);
}

- (void)connectionDidFinishLoading:(NSURLConnection *)connection
{
    //Should really never reach here.
    receivedData = nil;
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data
{
    [receivedData appendData:data];
}

#pragma mark    Webview methods
- (void)webView:(WebView *)sender didFinishLoadForFrame:(WebFrame *)frame{
    XOkeyLog(@"finished loading webview");
    
    //  Set the app delegate as a UIDelegate to handle new window requests
    if (_loadLoginPage) {
        XOkeyAppDelegate* app = (XOkeyAppDelegate*)[[NSApplication sharedApplication]delegate];
        [app setDeviceConfigured];
        [app windowSelect];
        _loadLoginPage = false;
    }
}

//Delegate methods that allow a URL to be launched by the default user browser
- (WebView *)webView:(WebView *)sender createWebViewWithRequest:(NSURLRequest *)request
{
    id myDocument = [[NSDocumentController sharedDocumentController] openUntitledDocumentAndDisplay:YES error:nil];
    WebView* webView;
    [myDocument webView:webView createWebViewWithRequest:request];
    return webView;
}

- (void)webView:(WebView *)sender decidePolicyForNavigationAction:(NSDictionary *)actionInformation request:(NSURLRequest *)request frame:(WebFrame *)frame decisionListener:(id<WebPolicyDecisionListener>)listener {
    if( [sender isEqual:self.webViewRef] ) {
        [listener use];
    }
    else {
        [[NSWorkspace sharedWorkspace] openURL:[actionInformation objectForKey:WebActionOriginalURLKey]];
        [listener ignore];
    }
}

- (void)webView:(WebView *)sender decidePolicyForNewWindowAction:(NSDictionary *)actionInformation request:(NSURLRequest *)request newFrameName:(NSString *)frameName decisionListener:(id<WebPolicyDecisionListener>)listener {
    [[NSWorkspace sharedWorkspace] openURL:[actionInformation objectForKey:WebActionOriginalURLKey]];
    [listener ignore];
}

//handler for downloading system log when user selects "Export System Log"
- (void)webView:(WebView *)webView decidePolicyForMIMEType:(NSString *)type
        request:(NSURLRequest *)request
          frame:(WebFrame *)frame
decisionListener:(id < WebPolicyDecisionListener >)listener
{
    XOkeyLog(type);
    if([type isEqualToString:@"text/plain"]){
        //download resource
        [listener download];
        NSURLDownload *downLoad = [[NSURLDownload alloc] initWithRequest:request delegate:self];
        NSString *destinationFileName;
        NSString *homeDirectory = NSHomeDirectory();
        
        //setup date
        NSDate *date = [NSDate date];
        NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
        dateFormatter.dateFormat = @"yyyy:MM:dd_HH-mm";//@"MM-dd-yy_hh:mm";
        //[dateFormatter setTimeStyle:NSDateFormatterMediumStyle];
        //[dateFormatter setDateStyle:NSDateFormatterMediumStyle]; // Set date and time styles
        //[dateFormatter setTimeZone:[NSTimeZone localTimeZone]];
        NSString *dateString = [dateFormatter stringFromDate:date];
        
        
        //create file name: "Date_XOkey_System_Log.txt"
        NSString* filename = [NSString stringWithFormat:@"%@_XOkey_System_Log.txt", dateString];
        destinationFileName = [[homeDirectory stringByAppendingPathComponent:@"Desktop"] stringByAppendingPathComponent:filename];
        [downLoad setDestination:destinationFileName allowOverwrite:YES];
    }
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

#pragma mark    NSURLSession methods to be implemented in the future
//TODO: NSURLSession doesn't work well with custom certifications. Must implement in the future.
/*
- (void)URLSession:(NSURLSession * _Nonnull)session dataTask:(NSURLSessionDataTask * _Nonnull)dataTask didReceiveData:(NSData * _Nonnull)data
{
    [receivedData appendData:data];
    
}
- (void)URLSession:(NSURLSession * _Nonnull)session task:(NSURLSessionTask * _Nonnull)task didCompleteWithError:(NSError * _Nullable)error
{
    //Reset data buffer. Load login page with UIWebview now that self-signed certificate has been accepted
    XOkeyAppDelegate* app = (XOkeyAppDelegate*)[[NSApplication sharedApplication]delegate];
    //Stop loading anything that's currently loading. Could have been the reason that the EK
    //server was periodically freezing. Sleep a bit to let the webkit clear up the session before connecting to the server.
    //[[app.ek_WebView mainFrame]stopLoading];
    //NSURL* url = [NSURL URLWithString:@"https://192.168.255.1"];
    //[[app.ek_WebView mainFrame]loadData:receivedData MIMEType:@"application/html" textEncodingName:@"utf-8" baseURL:url];
    //Sleep a bit to let it stoploading
    //sleep(0.5);
    //[[app.ek_WebView mainFrame]loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:@"https://192.168.255.1/ek/login.html"]]];
    
    
    //[receivedData setLength:0];
    [self loadLoginPage];
}

- (void)URLSession:(NSURLSession * _Nonnull)session didReceiveChallenge:(NSURLAuthenticationChallenge * _Nonnull)challenge completionHandler:(void (^ _Nonnull)(NSURLSessionAuthChallengeDisposition disposition, NSURLCredential * _Nullable credential))completionHandler{
    if([challenge.protectionSpace.host isEqualToString:@"192.168.255.1"]){
        //completionHandler(NSURLSessionAuthChallengeUseCredential, [NSURLCredential credentialForTrust:challenge.protectionSpace.serverTrust]);
        NSURLCredential *credential = [NSURLCredential credentialForTrust:challenge.protectionSpace.serverTrust];
        [[NSURLCredentialStorage sharedCredentialStorage]setCredential:credential forProtectionSpace:[NSURLProtectionSpace shar]];
        completionHandler(NSURLSessionAuthChallengeUseCredential,credential);
        
        [defaultSession finishTasksAndInvalidate];
        [receivedData setLength:0];
        
        // [self loadLoginPage];
        //[defaultSession finishTasksAndInvalidate];
        // [receivedData setLength:0];
    }
}

- (void)URLSession:(NSURLSession *)session
          dataTask:(NSURLSessionDataTask *)dataTask
didReceiveResponse:(NSURLResponse *)response
 completionHandler:(void (^)(NSURLSessionResponseDisposition disposition))completionHandler{
    completionHandler(NSURLSessionResponseAllow);
    [self loadLoginPage];
}
//
*/
@end
