/*++

Copyright (C) Microsoft Corporation, 1992 - 1999

Module Name:

    dispatch.h

Abstract:

Author:

    Michael Montague (mikemon) 11-Jun-1992

Revision History:

--*/

#include <sysinc.h>
#include <rpc.h>
#include <rpcdcep.h>
#include <dispatch.h>


unsigned int
DispatchToStubInCNoAvrf (
    IN RPC_DISPATCH_FUNCTION Stub,
    IN OUT PRPC_MESSAGE Message,
    OUT RPC_STATUS * ExceptionCode
    )
/*++

Routine Description:

    Dispatch a remote procedure call to a stub.  This must be in C
    because cfront does not support try-except on MIPS.

Arguments:

    Stub - Supplies the pointer to the function to dispatch to.

    Message - Supplies the request and returns the response.

    ExceptionCode - Returns the exception code if an exception
        occured.

Return Value:

    A non-zero value will be returned in an exception occured.

--*/
{
    unsigned int ExceptionHappened = 0;

    RpcTryExcept
        {
        (*Stub)(Message);
        }

    // Return "non-fatal" errors to clients.  Catching fatal errors
    // makes it harder to debug.
    RpcExcept(I_RpcExceptionFilter(RpcExceptionCode()))
        {
        ExceptionHappened = 1;
        *ExceptionCode = RpcExceptionCode();
        ASSERT(*ExceptionCode != RPC_S_OK);
        }
    RpcEndExcept

    return(ExceptionHappened);
}


unsigned int
DispatchToStubInCAvrf (
    IN RPC_DISPATCH_FUNCTION Stub,
    IN OUT PRPC_MESSAGE Message,
    OUT RPC_STATUS * ExceptionCode
    )
/*++

Routine Description:

    Dispatch a remote procedure call to a stub.  This is a wrapper
    around DispatchToStubInCNoAvrf that is called when app verifier is enabled.
    It makes sure that the server routine has not orphaned a critical section.

Arguments:

    Same as for DispatchToStubInCNoAvrf.

Return Value:

    A non-zero value will be returned in an exception occured.

--*/
{
    unsigned int ExceptionHappened;

    ExceptionHappened = DispatchToStubInCNoAvrf (Stub, Message, ExceptionCode);

    // Make sure this is not a callback.
    // A thread dispatching a callback may be legitimately holding a critsec in user code
    // or a connection mutex in the case of DG.
    if (!IsCallbackMessage(Message))
        {
        RtlCheckForOrphanedCriticalSections(NtCurrentThread());
        }

    return(ExceptionHappened);
}

// Initialize the dispatch routine to the default one to be used in the
// absence of app verifier.
DISPATCH_TO_STUB DispatchToStubInC = DispatchToStubInCNoAvrf;