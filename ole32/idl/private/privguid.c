//+-------------------------------------------------------------------------
//
//  Microsoft Windows
//  Copyright (C) Microsoft Corporation, 2000
//
//  File:
//      privguid.c
//
//  Contents:
//      Definition of guids not defined elsewhere, or that are needed by
//    third-parties (eg COM+) via prvidl.lib.
//
//  History:
//              JSimmons    01-03-00        Created
//
//--------------------------------------------------------------------------

#include <windows.h>
#include <initguid.h>

// Note:  these two guids are defined in ole32\ih\privguid.h.

// RPCSS's info object
DEFINE_OLEGUID(CLSID_RPCSSInfo,                     0x000003FF, 0, 0);

// Actpropsin clsid.
DEFINE_OLEGUID(CLSID_ActivationPropertiesIn,		0x00000338, 0, 0);

// ComActivator clsid
DEFINE_OLEGUID(CLSID_ComActivator,                  0x0000033c, 0, 0);

// Stackwalking
DEFINE_OLEGUID(CLSID_StackWalker,                   0x00000349, 0, 0);

// Local machine name comparisons
DEFINE_OLEGUID(CLSID_LocalMachineNames,             0x0000034a, 0, 0);

// Global options
DEFINE_OLEGUID(CLSID_GlobalOptions,             0x0000034b, 0, 0);

