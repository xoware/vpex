//
//  usbObject.h
//  TestApp
//
//  Created by user on 4/8/14.
//  Copyright (c) 2014 AB. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <IOKit/usb/IOUSBLib.h>
#import "ExoKeY_Definitions.h"

@interface usbObject : NSObject
//USB Device Properties
{
    //NSMutableString* BSDDeviceName;
}
@property (strong) NSMutableString* BSDDeviceName;

-(void)testPrint;

- (int)enumerateUSB;
void RawDeviceAdded(void *refCon, io_iterator_t iterator);
void RawDeviceRemoved(void *refCon, io_iterator_t iterator);
IOReturn ConfigureDevice(IOUSBDeviceInterface **dev);
IOReturn FindInterfaces(IOUSBDeviceInterface **device);
void ReadCompletion(void *refCon, IOReturn result, void *arg0);
@end
