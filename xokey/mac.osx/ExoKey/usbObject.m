//
//  usbObject.m
//  TestApp
//
//  Created by user on 4/8/14.
//  Copyright (c) 2014 AB. All rights reserved.
//
#import "usbObject.h"
#import "ExoKeyAppDelegate.h"

//  Definitions
//#define USE_ASYNC_IO    //Comment this line out if you want to use synchronous calls for reads and writes
#define kTestMessage            "Hello, world test."
#define k8051_USBCS             0x7f92
#define kOurVendorID            0x29B7      //Vendor ID of the USB device
#define kOurProductID           0x0101        //Product ID of device BEFORE it is programmed (raw device)
#define kOurProductIDBulkTest   6969        //Product ID of device AFTER it is programmed (bulk test device)

//  Global variables
static IONotificationPortRef    gNotifyPort;
static io_iterator_t            gRawAddedIter;
static io_iterator_t            gRawRemovedIter;
static io_iterator_t            gBulkTestAddedIter;
static io_iterator_t            gBulkTestRemovedIter;
static char                     gBuffer[64];
static usbObject*               c_Reference;                //Reference to the usbObject for c-styled functions.

@implementation usbObject
-(id)init{
    self = [super init];
    if (self) {
        c_Reference = self;
    }
    return self;
}

void RawDeviceAdded(void *refCon, io_iterator_t iterator){
    kern_return_t               kr;
    io_service_t                usbDevice;
    IOCFPlugInInterface         **plugInInterface = NULL;
    IOUSBDeviceInterface        **dev = NULL;
    HRESULT                     result;
    SInt32                      score;
    UInt16                      vendor;
    UInt16                      product;
    UInt16                      release;
    char                        buf[256];
    
    while ((usbDevice = IOIteratorNext(iterator)))
    {
        
        //Create an intermediate plug-in
        kr = IOCreatePlugInInterfaceForService(usbDevice,
                                               kIOUSBDeviceUserClientTypeID, kIOCFPlugInInterfaceID,
                                               &plugInInterface, &score);
        
        //Don’t need the device object after intermediate plug-in is created
        //kr = IOObjectRelease(usbDevice);
        if ((kIOReturnSuccess != kr) || !plugInInterface)
        {
            ExoKeyLog([NSString stringWithFormat:@"Unable to create a plug-in (%08x)", kr]);
            continue;
        }
        //Now create the device interface
        result = (*plugInInterface)->QueryInterface(plugInInterface,
                                                    CFUUIDGetUUIDBytes(kIOUSBDeviceInterfaceID),
                                                    (LPVOID *)&dev);
        //Don’t need the intermediate plug-in after device interface
        //is created
        (*plugInInterface)->Release(plugInInterface);
        if (result || !dev)
        {
            ExoKeyLog([NSString stringWithFormat:@"Couldn’t create a device interface (%08x)",
                   (int) result]);
            continue;
        }
        
        //Check these values for confirmation
        kr = (*dev)->GetDeviceVendor(dev, &vendor);
        kr = (*dev)->GetDeviceProduct(dev, &product);
        kr = (*dev)->GetDeviceReleaseNumber(dev, &release);
        if ((vendor != kOurVendorID) || (product != kOurProductID) )//||
            //(release != 1))
        {
            ExoKeyLog([NSString stringWithFormat:@"Found unwanted device (vendor = %d, product = %d)",
                   vendor, product]);
            (void) (*dev)->Release(dev);
        }else{
            ExoKeyLog([NSString stringWithFormat:@"Found ExoKey device! (vendor = %d, product = %d)",vendor,product]);
            
            //Get BSDDevice name. The IORegistry device database needs time to update so loop until the BSDDevice name in the OS database is not null
            //since we require the BSDDevice name to configure the device with the BSD networking tools.
            CFStringRef bsdName = nil;
            int retry = 0;
            do{
                sleep(0.5);
                bsdName = ( CFStringRef ) IORegistryEntrySearchCFProperty ( usbDevice,
                                                                                   kIOServicePlane,
                                                                                   CFSTR ( kIOBSDNameKey ),
                                                                                   kCFAllocatorDefault,
                                                                                   kIORegistryIterateRecursively );
                retry++;
                
            }while (bsdName == nil);
             
            c_Reference.BSDDeviceName = (__bridge NSMutableString*)(bsdName);
            [[NSNotificationCenter defaultCenter]postNotificationName:EXOKEY_PLUGIN object:nil];
        }
        kr = IOObjectRelease(usbDevice);
        (*dev)->Release(dev);
    
        //No need to configure the device or open pipes to send data.
/*
        //Open the device to change its state
        kr = (*dev)->USBDeviceClose(dev);
        if(kr != kIOReturnSuccess){
            printf("Unable to close device: %08x\n", kr);
            (void) (*dev)->Release(dev);
            continue;
        }
 
        kr = (*dev)->USBDeviceOpen(dev);
        if (kr == kIOReturnExclusiveAccess){
            
        }
        if (kr != kIOReturnSuccess)
        {
            printf("Unable to open device: %08x\n", kr);
          //  (void) (*dev)->Release(dev);
            continue;
        }
   
        //Configure device
        kr = ConfigureDevice(dev);
        if (kr != kIOReturnSuccess)
        {
            printf("Unable to configure device: %08x\n", kr);
            (void) (*dev)->USBDeviceClose(dev);
            (void) (*dev)->Release(dev);
            continue;
        }
        kr = (*dev)->USBDeviceClose(dev);
        kr = (*dev)->Release(dev);
        [[NSNotificationCenter defaultCenter]postNotificationName:EXOKEY_PLUGIN object:nil];
       //Get the interfaces
        kr = FindInterfaces(dev);
        if (kr != kIOReturnSuccess)
        {
            printf("Unable to find interfaces on device: %08x\n", kr);
            (*dev)->USBDeviceClose(dev);
            (*dev)->Release(dev);
            continue;
        }
//If using synchronous IO, close and release the device interface here
#ifndef USB_ASYNC_IO
        kr = (*dev)->USBDeviceClose(dev);
        kr = (*dev)->Release(dev);
#endif
*/
    }

}


void RawDeviceRemoved(void *refCon, io_iterator_t iterator){
    kern_return_t   kr;
    io_service_t    object;
    
    while ((object = IOIteratorNext(iterator)))
    {
        kr = IOObjectRelease(object);
        if (kr != kIOReturnSuccess)
        {
            ExoKeyLog([NSString stringWithFormat:@"Couldn’t release raw device object: %08x\n", kr]);
            continue;
        }else{
            ExoKeyLog(@"Released ExoKey object.");
            [[NSNotificationCenter defaultCenter]postNotificationName:EXOKEY_UNPLUG object:nil];
        }
    }
}

IOReturn ConfigureDevice(IOUSBDeviceInterface **dev){
    UInt8                           numConfig;
    IOReturn                        kr;
    IOUSBConfigurationDescriptorPtr configDesc;
    
    //Get the number of configurations. The sample code always chooses
    //the first configuration (at index 0) but your code may need a
    //different one
    kr = (*dev)->GetNumberOfConfigurations(dev, &numConfig);
    if (!numConfig)
        return -1;
    
    //Get the configuration descriptor for index 0
    kr = (*dev)->GetConfigurationDescriptorPtr(dev, 0, &configDesc);
    if (kr)
    {
        ExoKeyLog([NSString stringWithFormat:@"Couldn’t get configuration descriptor for index %d (err =%08x)\n", 0, kr]);
        return -1;
    }
    
    //Set the device’s configuration. The configuration value is found in
    //the bConfigurationValue field of the configuration descriptor
    kr = (*dev)->SetConfiguration(dev, configDesc->bConfigurationValue);
    if (kr)
    {
        ExoKeyLog([NSString stringWithFormat:@"Couldn’t set configuration to value %d (err = %08x)\n", 0, kr]);
        return -1;
    }
    return kIOReturnSuccess;
}

IOReturn FindInterfaces(IOUSBDeviceInterface **device)
{
    IOReturn                    kr;
    IOUSBFindInterfaceRequest   request;
    io_iterator_t               iterator;
    io_service_t                usbInterface;
    IOCFPlugInInterface         **plugInInterface = NULL;
    IOUSBInterfaceInterface     **interface = NULL;
    HRESULT                     result;
    SInt32                      score;
    UInt8                       interfaceClass;
    UInt8                       interfaceSubClass;
    UInt8                       interfaceNumEndpoints;
    int                         pipeRef;
    
#ifndef USE_ASYNC_IO
    UInt32                      numBytesRead;
    UInt32                      i;
#else
    CFRunLoopSourceRef          runLoopSource;
#endif
    
    //Placing the constant kIOUSBFindInterfaceDontCare into the following
    //fields of the IOUSBFindInterfaceRequest structure will allow you
    //to find all the interfaces
    request.bInterfaceClass = kIOUSBFindInterfaceDontCare;
    request.bInterfaceSubClass = kIOUSBFindInterfaceDontCare;
    request.bInterfaceProtocol = kIOUSBFindInterfaceDontCare;
    request.bAlternateSetting = kIOUSBFindInterfaceDontCare;
    
    //Get an iterator for the interfaces on the device
    kr = (*device)->CreateInterfaceIterator(device,&request, &iterator);
    while ((usbInterface = IOIteratorNext(iterator)))
    {
        //Create an intermediate plug-in
        kr = IOCreatePlugInInterfaceForService(usbInterface,
                                               kIOUSBInterfaceUserClientTypeID,
                                               kIOCFPlugInInterfaceID,
                                               &plugInInterface, &score);
        //Release the usbInterface object after getting the plug-in
        kr = IOObjectRelease(usbInterface);
        if ((kr != kIOReturnSuccess) || !plugInInterface)
        {
            ExoKeyLog([NSString stringWithFormat:@"Unable to create a plug-in (%08x)\n", kr]);
            break;
        }
        
        //Now create the device interface for the interface
        result = (*plugInInterface)->QueryInterface(plugInInterface,
                                                    CFUUIDGetUUIDBytes(kIOUSBInterfaceInterfaceID),
                                                    (LPVOID *) &interface);
        //No longer need the intermediate plug-in
        (*plugInInterface)->Release(plugInInterface);
        
        if (result || !interface)
        {
            ExoKeyLog([NSString stringWithFormat:@"Couldn’t create a device interface for the interface(%08x)\n", (int) result]);
            break;
        }
                   
        //Get interface class and subclass
        kr = (*interface)->GetInterfaceClass(interface,&interfaceClass);
        kr = (*interface)->GetInterfaceSubClass(interface,&interfaceSubClass);
        
        ExoKeyLog([NSString stringWithFormat:@"Interface class %d, subclass %d\n", interfaceClass,interfaceSubClass]);
                   
        //Now open the interface. This will cause the pipes associated with
        //the endpoints in the interface descriptor to be instantiated
        kr = (*interface)->USBInterfaceOpen(interface);
        if (kr != kIOReturnSuccess)
        {
            ExoKeyLog([NSString stringWithFormat:@"Unable to open interface (%08x)\n", kr]);
            (void) (*interface)->Release(interface);
            break;
        }
        
       //Get the number of endpoints associated with this interface
       kr = (*interface)->GetNumEndpoints(interface,&interfaceNumEndpoints);
       if (kr != kIOReturnSuccess)
       {
           ExoKeyLog([NSString stringWithFormat:@"Unable to get number of endpoints (%08x)\n", kr]);
           (void) (*interface)->USBInterfaceClose(interface);
           (void) (*interface)->Release(interface);
           break;
       }
        
        printf("Interface has %d endpoints\n", interfaceNumEndpoints);
       //Access each pipe in turn, starting with the pipe at index 1
       //The pipe at index 0 is the default control pipe and should be
       //accessed using (*usbDevice)->DeviceRequest() instead
       for (pipeRef = 1; pipeRef <= interfaceNumEndpoints; pipeRef++)
       {
           IOReturn        kr2;
           UInt8           direction;
           UInt8           number;
           UInt8           transferType;
           UInt16          maxPacketSize;
           UInt8           interval;
           char            *message;
           
           kr2 = (*interface)->GetPipeProperties(interface, pipeRef, &direction, &number, &transferType,
                                                 &maxPacketSize, &interval);
           
           if (kr2 != kIOReturnSuccess)
               ExoKeyLog([NSString stringWithFormat:@"Unable to get properties of pipe %d (%08x)\n",
                      pipeRef, kr2]);
           else
           {
               ExoKeyLog([NSString stringWithFormat:@"PipeRef %d: ", pipeRef]);
               switch (direction)
               {
                   case kUSBOut:
                       message = "out";
                       break;
                   case kUSBIn:
                       message = "in";
                       break;
                   case kUSBNone:
                       message = "none";
                       break;
                   case kUSBAnyDirn:
                       message = "any";
                       break;
                   default:
                       message = "???";
               }
               ExoKeyLog([NSString stringWithFormat:@"direction %s, ", message]);
               
               switch (transferType)
               {
                   case kUSBControl:
                       message = "control";
                       break;
                   case kUSBIsoc:
                       message = "isoc";
                       break;
                   case kUSBBulk:
                       message = "bulk";
                       break;
                   case kUSBInterrupt:
                       message = "interrupt";
                       break;
                   case kUSBAnyType:
                       message = "any";
                       break;
                   default:
                       message = "???";
               }
               ExoKeyLog([NSString stringWithFormat:@"transfer type %s, maxPacketSize %d\n", message,
                      maxPacketSize]);
           }
       }
        
#ifndef USE_ASYNC_IO    //Demonstrate synchronous I/O
       /*kr = (*interface)->WritePipe(interface, 2, kTestMessage, strlen(kTestMessage));
       if (kr != kIOReturnSuccess)
       {
           printf("Unable to perform bulk write (%08x)\n", kr);
           (void) (*interface)->USBInterfaceClose(interface);
           (void) (*interface)->Release(interface);
           break;
       }
        
       printf("Wrote \"%s\" (%ld bytes) to bulk endpoint\n", kTestMessage,
              (UInt32) strlen(kTestMessage));
       */
       numBytesRead = sizeof(gBuffer) - 1; //leave one byte at the end
       //for NULL termination
       kr = (*interface)->ReadPipe(interface, 1, gBuffer,
                                   &numBytesRead);
       if (kr != kIOReturnSuccess)
       {
           ExoKeyLog([NSString stringWithFormat:@"Unable to perform bulk read (%08x)\n", kr]);
           (void) (*interface)->USBInterfaceClose(interface);
           (void) (*interface)->Release(interface);
           break;
       }
        
       //Because the downloaded firmware echoes the one’s complement of the
       //message, now complement the buffer contents to get the original data
       for (i = 0; i < numBytesRead; i++)
       gBuffer[i] = ~gBuffer[i];
       
       ExoKeyLog([NSString stringWithFormat:@"Read \"%s\" (%u bytes) from bulk endpoint\n", gBuffer, (unsigned int)numBytesRead]);
        
#else   
        //Demonstrate asynchronous I/O
        //As with service matching notifications, to receive asynchronous
        //I/O completion notifications, you must create an event source and
        //add it to the run loop
        kr = (*interface)->CreateInterfaceAsyncEventSource(interface, &runLoopSource);
       if (kr != kIOReturnSuccess)
       ]{
           ExoKeyLog([NSString stringWithFormat:@"Unable to create asynchronous event source(%08x)\n", kr]);
           (void) (*interface)->USBInterfaceClose(interface);
           (void) (*interface)->Release(interface);
           break;
        }
        
      CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource,
                         kCFRunLoopDefaultMode);
      ExoKeyLog(@"Asynchronous event source added to run loop\n");
      bzero(gBuffer, sizeof(gBuffer));
      strcpy(gBuffer, kTestMessage);
      /*kr = (*interface)->WritePipeAsync(interface, 2, gBuffer,strlen(gBuffer),WriteCompletion, (void *) interface);
      if (kr != kIOReturnSuccess)
      {
          printf("Unable to perform asynchronous bulk write (%08x)\n",
                 kr);
          (void) (*interface)->USBInterfaceClose(interface);
          (void) (*interface)->Release(interface);
          break;
      }
       */
#endif
      //For this test, just use first interface, so exit loop
      break;
      }
      return kr;
}
                   
void WriteCompletion(void *refCon, IOReturn result, void *arg0)
{
    IOUSBInterfaceInterface **interface = (IOUSBInterfaceInterface **) refCon;
    UInt32                  numBytesWritten = (UInt32) arg0;
    UInt32                  numBytesRead;
    
    ExoKeyLog(@"Asynchronous write complete\n");
    if (result != kIOReturnSuccess)
    {
        ExoKeyLog([NSString stringWithFormat:@"error from asynchronous bulk write (%08x)\n", result]);
        (void) (*interface)->USBInterfaceClose(interface);
        (void) (*interface)->Release(interface);
        return;
    }
    ExoKeyLog([NSString stringWithFormat:@"Wrote \"%s\" (%u bytes) to bulk endpoint\n", kTestMessage,
           (unsigned int)numBytesWritten]);
    
    numBytesRead = sizeof(gBuffer) - 1; //leave one byte at the end for
    //NULL termination
    result = (*interface)->ReadPipeAsync(interface, 9, gBuffer,
                                         numBytesRead, ReadCompletion, refCon);
    if (result != kIOReturnSuccess)
    {
        ExoKeyLog([NSString stringWithFormat:@"Unable to perform asynchronous bulk read (%08x)\n", result]);
        (void) (*interface)->USBInterfaceClose(interface);
        (void) (*interface)->Release(interface);
        return;
    }
}


void ReadCompletion(void *refCon, IOReturn result, void *arg0)
{
    IOUSBInterfaceInterface **interface = (IOUSBInterfaceInterface **) refCon;
    UInt32      numBytesRead = (UInt32) arg0;
    UInt32      i;
    
    ExoKeyLog([NSString stringWithFormat:@"Asynchronous bulk read complete\n"]);
    if (result != kIOReturnSuccess) {
        ExoKeyLog([NSString stringWithFormat:@"error from async bulk read (%08x)\n", result]);
        (void) (*interface)->USBInterfaceClose(interface);
        (void) (*interface)->Release(interface);
        return;
    }
    //Check the complement of the buffer’s contents for original data
    for (i = 0; i < numBytesRead; i++)
        gBuffer[i] = ~gBuffer[i];
    
    ExoKeyLog([NSString stringWithFormat:@"Read \"%s\" (%u bytes) from bulk endpoint\n", gBuffer,
           (unsigned int)numBytesRead]);
}

- (int)enumerateUSB{
    mach_port_t             masterPort;
    CFMutableDictionaryRef  matchingDict;
    CFRunLoopSourceRef      runLoopSource;
    kern_return_t           kr;
    SInt32                  usbVendor = kOurVendorID;
    SInt32                  usbProduct = kOurProductID;
    
    
    //Create a master port for communication with the I/O Kit
    kr = IOMasterPort(MACH_PORT_NULL, &masterPort);
    if (kr || !masterPort)
    {
        ExoKeyLog([NSString stringWithFormat:@"ERR: Couldn’t create a master I/O Kit port(%08x)\n", kr]);
        return -1;
    }
    
    //Set up matching dictionary for class IOUSBDevice and its subclasses
    matchingDict = IOServiceMatching(kIOUSBDeviceClassName);
    if (!matchingDict)
    {
        ExoKeyLog([NSString stringWithFormat:@"Couldn’t create a USB matching dictionary\n"]);
        mach_port_deallocate(mach_task_self(), masterPort);
        return -1;
    }
    
    //Add the vendor and product IDs to the matching dictionary.
    //This is the second key in the table of device-matching keys of the
    //USB Common Class Specification
    CFDictionarySetValue(matchingDict, CFSTR(kUSBVendorName),
                         CFNumberCreate(kCFAllocatorDefault,
                                        kCFNumberSInt32Type, &usbVendor));
    CFDictionarySetValue(matchingDict, CFSTR(kUSBProductName),
                         CFNumberCreate(kCFAllocatorDefault,
                                        kCFNumberSInt32Type, &usbProduct));
    
    //To set up asynchronous notifications, create a notification port and
    //add its run loop event source to the program’s run loop
    gNotifyPort = IONotificationPortCreate(masterPort);
    runLoopSource = IONotificationPortGetRunLoopSource(gNotifyPort);
    CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource,
                       kCFRunLoopDefaultMode);
    
    //Retain additional dictionary references because each call to
    //IOServiceAddMatchingNotification consumes one reference
    matchingDict = (CFMutableDictionaryRef) CFRetain(matchingDict);
    matchingDict = (CFMutableDictionaryRef) CFRetain(matchingDict);
    matchingDict = (CFMutableDictionaryRef) CFRetain(matchingDict);
    
    //Now set up two notifications: one to be called when a raw device
    //is first matched by the I/O Kit and another to be called when the
    //device is terminated
    //Notification of first match:
    kr = IOServiceAddMatchingNotification(gNotifyPort,
                                          kIOFirstMatchNotification, matchingDict,
                                          RawDeviceAdded, NULL, &gRawAddedIter);
    
    //(gRawAddedIter must reach 0 for the notifcation to be armed.)
    
    //Iterate over set of matching devices to access already-present devices
    //and to arm the notification
    RawDeviceAdded(NULL, gRawAddedIter);
    
    //Notification of termination:
    kr = IOServiceAddMatchingNotification(gNotifyPort,
                                          kIOTerminatedNotification, matchingDict,
                                          RawDeviceRemoved, NULL, &gRawRemovedIter);
    
    //(gRawRemovedIter must reach 0 for the notifcation to be armed.)
    
    //Iterate over set of matching devices to release each one and to
    //arm the notification
    RawDeviceRemoved(NULL, gRawRemovedIter);
    
    //Bulk test not needed; we are not downloading firmware to the device and changing the PID.
    
    //Finished with master port
    mach_port_deallocate(mach_task_self(), masterPort);
    masterPort = 0;
    
    //Start the run loop so notifications will be received
    //CFRunLoopRun();
    
    //Because the run loop will run forever until interrupted,
    //the program should never reach this point
    return 1;
}

@end

