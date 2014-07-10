//
//  ExoKey_Definitions.h
//  ExoKey
//
//  Created by user on 5/26/14.
//  Copyright (c) 2014 x.o.ware. All rights reserved.
//

#ifndef ExoKey_ExoKey_Definitions_h
#define ExoKey_ExoKey_Definitions_h
////////
//Keys//
////////

//PnP Keys
#define EXOKEY_PLUGIN               @"EXOKEY_PLUGIN"
#define EXOKEY_UNPLUG               @"EXOKEY_UNPLUG"

//EK Keys
#define EXOKEY_IP_ADDRESS           @"EXOKEY_IP_ADDRESS"
#define EXOKEY_ENDPOINT             @"EXOKEY_ENDPOINT"
#define EXOKEY_SUBNET               @"EXOKEY_SUBNET"

//Exonet Keys
#define EXONET_IP                   @"EXO_NET_IP"

//Host Keys
#define ROUTER                      @"DEFAULT_ROUTER"
#define ACTIVE_ENDPOINT             @"EXOKEY_ACTIVE_ENDPOINT"

//Default values
#define NOT_SET                     @"EXOKEY_NOT_SET"
#define DEFAULT_IP                  @"192.168.255.2"
#define DEFAULT_SUBNET              @"255.255.255.252"
#define VPN_CONNECTED               1
#define VPN_DISCONNECTED            0

//Paths
#define PF_CONF_PATH    @"/etc/exokey-pf.conf"              //PF config file path for ExoKey

#endif
