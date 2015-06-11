//
//  XOkey_Definitions.h
//  XOkey
//
//  Created by user on 5/26/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#ifndef XOkey_XOkey_Definitions_h
#define XOkey_XOkey_Definitions_h
////////
//Keys//
////////

//PnP Keys
#define XOKEY_PLUGIN               @"XOKEY_PLUGIN"
#define XOKEY_UNPLUG               @"XOKEY_UNPLUG"

//EK Keys
#define XOKEY_IP_ADDRESS           @"XOKEY_IP_ADDRESS"
#define XOKEY_ENDPOINT             @"XOKEY_ENDPOINT"
#define XOKEY_SUBNET               @"XOKEY_SUBNET"

//Exonet Keys
#define EXONET_IP                   @"EXO_NET_IP"

//Host Keys
#define ROUTER                      @"DEFAULT_ROUTER"
#define ACTIVE_ENDPOINT             @"XOKEY_ACTIVE_ENDPOINT"

//Default values
#define NOT_SET                     @"XOKEY_NOT_SET"
#define DEFAULT_IP                  @"192.168.255.2"
#define DEFAULT_SUBNET              @"255.255.255.252"
#define VPN_CONNECTED               1
#define VPN_DISCONNECTED            0

//Paths
#define PF_CONF_PATH    @"/etc/XOKEY-pf.conf"              //PF config file path for XOKEY

#endif
