//
//  main.m
//  XOkey_NetworkConfigTool
//
//  Created by user on 5/22/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "NetworkConfigTool.h"
int main(int argc, const char * argv[])
{
    
    @autoreleasepool {
        NetworkConfigTool* helperTool = [[NetworkConfigTool alloc]init];
        assert(helperTool != nil);
        NSLog(@"XOkey network configuration tool created as daemon agent.");
        [helperTool run];
        // insert code here...        
    }
    return 0;
}

