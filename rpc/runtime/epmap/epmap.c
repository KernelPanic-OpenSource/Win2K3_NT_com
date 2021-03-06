/*++

Copyright (C) Microsoft Corporation, 1991 - 1999

Module Name:

    epmap.c

Abstract:

    This file contains the EP Mapper startup code and process wide globals.

Author:

    Bharat Shah  (barats) 17-2-92

Revision History:

    06-16-95    MarioGo     Much of the code replaced by ..\wrapper\start.c
                            Renamed from server.c
    Jan 2000    KamenM      Add debugging support

--*/

#include <sysinc.h>
#include <wincrypt.h>
#include <rpc.h>
#include <winsvc.h>
#include "epmp.h"
#include "eptypes.h"
#include "local.h"
#include <DbgComn.h>
#include <DbgIdl.h>
#include <DbgSvr.hxx>

#if DBG && !defined(DEBUGRPC)
#define DEBUGRPC
#endif

//
// Endpoint Mapper Functions
//

extern RPC_STATUS InitializeIpPortManager();

extern RPC_STATUS RPC_ENTRY
LocalEpmpSecurityCallback (
    IN RPC_IF_HANDLE InterfaceUuid,
    IN void *Context
    );

//
// Endpoint Mapper Globals
//

HANDLE           hEpMapperHeap;
CRITICAL_SECTION EpCritSec;
PIFOBJNode       IFObjList = NULL;
PSAVEDCONTEXT    GlobalContextList = NULL;
unsigned long    cTotalEpEntries = 0L;
unsigned long    GlobalIFOBJid = 0xFFL;
unsigned long    GlobalEPid    = 0x00FFFFFFL;
UUID             NilUuid = { 0L, 0, 0, {0, 0, 0, 0, 0, 0, 0, 0} };


DWORD
StartEndpointMapper(
    void
    )
/*++

Routine Description:

    Called during dcomss startup.  Should call Updatestatus()
    if something will take very long.

Arguments:

    None

Return Value:

    0 - success

    non-0 - will cause the service to fail.

--*/
{
    extern void RPC_ENTRY UpdateAddresses( PVOID arg );

    RPC_STATUS status = RPC_S_OK;
    BOOL fAuthInfoNotRegistered = FALSE;

    InitializeCriticalSectionAndSpinCount(&EpCritSec, PREALLOCATE_EVENT_MASK);

    hEpMapperHeap = GetProcessHeap();

    if (hEpMapperHeap == 0)
        {
        ASSERT(GetLastError() != 0);
        return(GetLastError());
        }

    // register snego & kerberos. During clean install, this code can
    // legally fail, as Rpcss is started before there are any
    // security providers. Therefore, we cannot fail Rpcss init if this
    // fails - we just don't register the debug interface, who is the
    // only user of this
    status = RpcServerRegisterAuthInfo(NULL, RPC_C_AUTHN_GSS_NEGOTIATE, NULL, NULL);

    if (status != RPC_S_OK)
        {
        fAuthInfoNotRegistered = TRUE;
        }

    status = RpcServerRegisterAuthInfo(NULL, RPC_C_AUTHN_GSS_KERBEROS, NULL, NULL);

    if (status != RPC_S_OK)
        {
        fAuthInfoNotRegistered = TRUE;
        }

    status = RpcServerRegisterIf2(epmp_ServerIfHandle,
                                  0,
                                  0,
                                  0,
                                  RPC_C_LISTEN_MAX_CALLS_DEFAULT,
                                  epmp_MaxRpcSize,
                                  NULL);

    if (status != RPC_S_OK)
        {
        return(status);
        }

    status = RpcServerRegisterIf2(localepmp_ServerIfHandle,
                                  0,
                                  0,                              
                                  0,
                                  RPC_C_LISTEN_MAX_CALLS_DEFAULT,
                                  localepmp_MaxRpcSize,
                                  LocalEpmpSecurityCallback);
    if (status != RPC_S_OK)
        {
        return(status);
        }

    if (fAuthInfoNotRegistered == FALSE)
        {
        status = RpcServerRegisterIf2(DbgIdl_ServerIfHandle,
                                      0,
                                      0,
                                      0,
                                      RPC_C_LISTEN_MAX_CALLS_DEFAULT,
                                      DbgIdl_MaxRpcSize,
                                      DebugServerSecurityCallback);
        if (status != RPC_S_OK)
            {
            return(status);
            }
        }

    status = I_RpcServerRegisterForwardFunction( GetForwardEp );

#ifndef DOSWIN32RPC
    if (status == RPC_S_OK)
        {
        status = InitializeIpPortManager();
        ASSERT(status == RPC_S_OK);
        }
#endif

    status = I_RpcServerSetAddressChangeFn( UpdateAddresses );

    ASSERT( 0 == status );

    return(status);
}

