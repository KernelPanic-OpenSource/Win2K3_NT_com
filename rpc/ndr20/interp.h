/************************************************************************

Copyright (c) 1993 Microsoft Corporation

Module Name :

    interp.h

Abstract :

    Definitions for the client and server stub interpreter.  Compiled from
    previous files srvcall.h, srvoutp.h, and getargs.h.

Author :

    DKays       October 1994

Revision History :

  ***********************************************************************/

#ifndef _INTERP_
#define _INTERP_

//
// Stack and argument defines.
//

#if defined(_IA64_) || defined(_AMD64_)
#define REGISTER_TYPE               _int64
#else
#define REGISTER_TYPE               int
#endif

#define RETURN_SIZE                 8

//
// Define interpreter limitations.
//

#define ARGUMENT_COUNT_THRESHOLD    16

#define MAX_STACK_SIZE              ARGUMENT_COUNT_THRESHOLD * sizeof(double)

//
// The maximum number of context handles parameters in a procedure that we
// can handle.
//

#define MAX_CONTEXT_HNDL_NUMBER     8

//
// Argument caching data structures.
//

#define QUEUE_LENGTH                ARGUMENT_COUNT_THRESHOLD

typedef struct _ARG_QUEUE_INFO
    {
    PFORMAT_STRING  pFormat;

    uchar *         pArg;
    uchar **        ppArg;

    short           ParamNum;

    short           IsReturn            : 1;
    short           IsBasetype          : 1;
    short           IsIn                : 1;
    short           IsOut               : 1;
    short           IsOutOnly           : 1;

    short           IsDeferredFree      : 1;

    short           IsDontCallFreeInst  : 1;
    } ARG_QUEUE_ELEM, *PARG_QUEUE_ELEM;

typedef struct _ARG_QUEUE
    {
    long                Length;
    ARG_QUEUE_ELEM *    Queue;
    } ARG_QUEUE, *PARG_QUEUE;

//
// Argument retrieval macros.
//

#define INIT_ARG(argptr,arg0)   va_start(argptr, arg0)

//
// Both MIPS and x86 are 4 byte aligned stacks, with MIPS supporting 8byte
// alignment on the stack as well. Their va_list is essentially an
// unsigned char *.
//

#if     defined(_IA64_)
#define GET_FIRST_IN_ARG(argptr)
#define GET_FIRST_OUT_ARG(argptr)
#elif   defined(_AMD64_)
#define GET_FIRST_IN_ARG(argptr)
#define GET_FIRST_OUT_ARG(argptr)
#else
#define GET_FIRST_IN_ARG(argptr)            argptr = *(va_list *)argptr
#define GET_FIRST_OUT_ARG(argptr)           argptr = *(va_list *)argptr
#endif

#define GET_NEXT_C_ARG(argptr,type)         va_arg(argptr,type)

#define SKIP_STRUCT_ON_STACK(ArgPtr, Size)	ArgPtr += Size

#define GET_STACK_START(ArgPtr)			    ArgPtr
#define GET_STACK_POINTER(ArgPtr, mode)		ArgPtr

//
// Use the following macro _after_ argptr has been saved or processed
//
#define SKIP_PROCESSED_ARG(argptr, type) \
                    GET_NEXT_C_ARG(argptr, type); \
                    GET_STACK_POINTER(argptr,type)

#define GET_NEXT_S_ARG(argptr,type)     argptr += sizeof(type)

//
// Some typedefs so that the C compiler front end won't complain about calling
// the server manager function with a specific number of arguments. This may
// help the C compiler code generator too.
//

typedef _int64 (__RPC_API * MANAGER_FUNCTION)(void);
typedef _int64 (__RPC_API * MANAGER_FUNCTION1)(
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION2)(
            REGISTER_TYPE, REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION3)(
            REGISTER_TYPE, REGISTER_TYPE, REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION4)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION5)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION6)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION7)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION8)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION9)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION10)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION11)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION12)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION13)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION14)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION15)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION16)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION17)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION18)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION19)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION20)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION21)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION22)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION23)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION24)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION25)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION26)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION27)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION28)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION29)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION30)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION31)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);
typedef _int64 (__RPC_API * MANAGER_FUNCTION32)(
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,
            REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE,REGISTER_TYPE);


#if !defined(__RPC_WIN64__)

void
NdrServerFree(
    PMIDL_STUB_MESSAGE  pStubMsg,
    PFORMAT_STRING      pFormat,
    void *              pThis
    );

void
NdrCallServerManager (
    MANAGER_FUNCTION    pFtn,
    double *            pArgs,
    ulong               NumRegisterArgs,
    BOOL                fHasReturn
    );

#endif // !defined(__RPC_WIN64__)

void
Ndr64OutInit(
    PMIDL_STUB_MESSAGE      pStubMsg,
    PNDR64_FORMAT           pFormat,
    uchar **                ppArg
    );

#endif
