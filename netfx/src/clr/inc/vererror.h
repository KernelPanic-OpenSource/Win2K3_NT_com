// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/**************************************************************************************
 **                                                                                  **
 ** Vererror.h - definitions of data structures, needed to report verifier errors.   **
 **                                                                                  **
 **************************************************************************************/


#pragma once

#ifndef _VER_RAW_STRUCT_FOR_IDL_
#include "CorHdr.h"
#include "OpEnum.h"
#include "CorError.h"


// Set these flags if the error info fields are valid.

#define VER_ERR_FATAL		0x80000000L	// Cannot Continue
#define VER_ERR_OFFSET		0x00000001L
#define VER_ERR_OPCODE	  	0x00000002L
#define VER_ERR_OPERAND		0x00000004L
#define VER_ERR_TOKEN		0x00000008L
#define VER_ERR_EXCEP_NUM_1	0x00000010L
#define VER_ERR_EXCEP_NUM_2	0x00000020L
#define VER_ERR_STACK_SLOT  0x00000040L
#define VER_ERR_ITEM_1      0x00000080L
#define VER_ERR_ITEM_2      0x00000100L
#define VER_ERR_ITEM_F      0x00000200L
#define VER_ERR_ITEM_E      0x00000400L

#define VER_ERR_SIG_MASK	0x07000000L	// Enum
#define VER_ERR_METHOD_SIG 	0x01000000L
#define VER_ERR_LOCAL_SIG  	0x02000000L
#define VER_ERR_FIELD_SIG	0x03000000L
#define VER_ERR_CALL_SIG	0x04000000L

#define VER_ERR_OPCODE_OFFSET (VER_ERR_OPCODE|VER_ERR_OFFSET)

#define VER_ERR_LOCAL_VAR   VER_ERR_LOCAL_SIG
#define VER_ERR_ARGUMENT    VER_ERR_METHOD_SIG

#define VER_ERR_ARG_RET	    0xFFFFFFFEL		// The Argument # is return
#define VER_ERR_NO_ARG	    0xFFFFFFFFL		// Argument # is not valid
#define VER_ERR_NO_LOC	    VER_ERR_NO_ARG	// Local # is not valid

typedef struct
{
	DWORD dwFlags;	// BYREF / BOXED etc.. see veritem.hpp
	void* pv;		// TypeHandle / MethodDesc * etc.
} _VerItem;

// This structure is used to fully define a verification error.
// Verification error codes are found in CorError.h
// The error resource strings are found in src/dlls/mscorrc/mscor.rc

typedef struct
{
	DWORD   dwFlags;            // VER_ERR_XXX

    union {
        OPCODE  opcode;
        unsigned long padding1; // to match with idl generated struct size
    };

    union {
        DWORD   dwOffset;       // #of bytes from start of method
        long    uOffset;        // for backward compat with Metadata validator
    };

    union {
        mdToken         token;
        mdToken         Token;  // for backward compat with metadata validator
        BYTE	        bCallConv;
        CorElementType  elem;
        DWORD           dwStackSlot; // positon in the Stack
        unsigned long   padding2;    // to match with idl generated struct size
    };

    union {
        _VerItem sItem1;
        _VerItem sItemFound;
        DWORD dwException1;		// Exception Record #
        DWORD dwVarNumber;	    // Variable #
        DWORD dwArgNumber;	    // Argument #
        DWORD dwOperand;        // Operand for the opcode
    };

    union {
        _VerItem sItem2;
        _VerItem sItemExpected;
        DWORD dwException2;	    // Exception Record #
    };

} VerError;

#else

// Assert that sizeof(_VerError) == sizeof(VerError) in Verifier.cpp
typedef struct tag_VerError
{
    unsigned long flags;            // DWORD
    unsigned long opcode;           // OPCODE, padded to ulong
    unsigned long uOffset;           // DWORD
    unsigned long Token;            // mdToken
    unsigned long item1_flags;      // _VerItem.DWORD
    int           *item1_data;      // _VerItem.PVOID
    unsigned long item2_flags;      // _VerItem.DWORD
    int           *item2_data;      // _VerItem.PVOID
}  _VerError;
#endif


