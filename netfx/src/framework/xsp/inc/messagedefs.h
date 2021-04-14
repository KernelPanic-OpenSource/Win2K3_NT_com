/**
 * Message Definitions header file
 *
 * Copyright (c) 1999 Microsoft Corporation
 *
 */

////////////////////////////////////////////////////////////////////////////
// This file defines the structs sent on the named pipes. 
////////////////////////////////////////////////////////////////////////////

#if _MSC_VER > 1000
#pragma once
#endif

#ifndef _MessageDefs_H
#define _MessageDefs_H

/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
// Messages on the async pipe

/////////////////////////////////////////////////////////////////////////////
// Types of async messages
enum EAsyncMessageType
{
    EMessageType_Unknown,
    EMessageType_Request,
    EMessageType_Response,
    EMessageType_Response_And_DoneWithRequest,
    EMessageType_Shutdown,
    EMessageType_ShutdownImmediate,
    EMessageType_GetDataFromIIS,
    EMessageType_Response_ManagedCodeFailure,
    EMessageType_CloseConnection,
    EMessageType_Debug,
    EMessageType_Response_Empty
};

/////////////////////////////////////////////////////////////////////////////
// Header sent with each async message
struct CAsyncMessageHeader
{
    EAsyncMessageType   eType;       // Type of message
    LONG                lRequestID;  // Request ID
    LONG                lDataLength; // Length of data in pData
};

/////////////////////////////////////////////////////////////////////////////
// Async message: NOTE: Actual async message can be smaller/larger that this.
//                      Depends on oHeader.lDataLength
struct CAsyncMessage
{
    CAsyncMessageHeader  oHeader;
    BYTE                 pData[4]; // Dummy variable: This is actually BYTE pData[oHeader.lDataLength]    
};

/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
// Messages on the sync pipe

/////////////////////////////////////////////////////////////////////////////
// Sync message Types 
enum ESyncMessageType
{
    ESyncMessageType_Unknown,
    ESyncMessageType_Ack,
    ESyncMessageType_GetServerVariable,
    //    ESyncMessageType_GetQueryString,
    ESyncMessageType_GetAdditionalPostedContent,
    ESyncMessageType_IsClientConnected,
    ESyncMessageType_CloseConnection,
    ESyncMessageType_MapUrlToPath,
    ESyncMessageType_GetImpersonationToken,
    ESyncMessageType_GetAllServerVariables,
    ESyncMessageType_GetHistory,
    ESyncMessageType_GetClientCert,
    ESyncMessageType_CallISAPI,
    ESyncMessageType_ChangeDebugStatus,
    ESyncMessageType_GetMemoryLimit
};

/////////////////////////////////////////////////////////////////////////////
// Message on the sync pipe
struct CSyncMessage
{
    ESyncMessageType    eType;
    LONG                lRequestID;// Req ID of the message being acked
    INT_PTR             iMiscInfo;
    int                 iOutputSize;
    int                 iSize;
    BYTE                buf[4];
};

/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
// Structs used to package a request

/////////////////////////////////////////////////////////////////////////////
// Request struct
struct CRequestStruct
{
    __int64     qwRequestStartTime; 
    HANDLE      iUserToken;
    HANDLE      iUNCToken;
    DWORD       dwWPPid;
    int         iContentInfo [4]; // Content info generated by EcbGetBasics
    int         iQueryStringOffset; // Start point of query str in bufStrings
    int         iPostedDataOffset; // Start point of posted data
    int         iPostedDataLen;    // Length of posted data
    int         iServerVariablesOffset; // Start point of ServerVariables
    BYTE        bufStrings   [4]; // Variable length Buffer
};

#define NUM_SERVER_VARS                       32
#define DEFINE_SERVER_VARIABLES_ORDER          \
   LPCSTR g_szServerVars[NUM_SERVER_VARS] = {  \
             "APPL_MD_PATH", /*always first*/ \
             "ALL_RAW",\
             "AUTH_PASSWORD",\
             "AUTH_TYPE",\
             "CERT_COOKIE",\
             "CERT_FLAGS",\
             "CERT_ISSUER",\
             "CERT_KEYSIZE",\
             "CERT_SECRETKEYSIZE",\
             "CERT_SERIALNUMBER",\
             "CERT_SERVER_ISSUER",\
             "CERT_SERVER_SUBJECT",\
             "CERT_SUBJECT",\
             "GATEWAY_INTERFACE",\
             "HTTP_COOKIE",\
             "HTTP_USER_AGENT",\
             "HTTPS",\
             "HTTPS_KEYSIZE",\
             "HTTPS_SECRETKEYSIZE",\
             "HTTPS_SERVER_ISSUER",\
             "HTTPS_SERVER_SUBJECT",\
             "INSTANCE_ID",\
             "INSTANCE_META_PATH",\
             "LOCAL_ADDR",\
             "LOGON_USER",\
             "REMOTE_ADDR",\
             "REMOTE_HOST",\
             "SERVER_NAME",\
             "SERVER_PORT",\
             "SERVER_PROTOCOL",\
             "SERVER_SOFTWARE",\
             "REMOTE_PORT"};

/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
// Response Structs

/////////////////////////////////////////////////////////////////////////////
// Type of Write: Calls EcbWriteXXX function
enum EWriteType
{
    EWriteType_Unknown,
    EWriteType_None,
    EWriteType_WriteHeaders,
    EWriteType_WriteBytes,
    EWriteType_AppendToLog,
    EWriteType_FlushCore
};

/////////////////////////////////////////////////////////////////////////////
// Response struct
struct CResponseStruct
{
    EWriteType  eWriteType;
    int         iMiscInfo; // iKeepConnected for EWriteType_WriteHeaders, Buf size for EWriteType_WriteBytes
    BYTE        bufStrings   [4];
};

/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////
// History table entry structs

/////////////////////////////////////////////////////////////////////////////
// Enum describing the reason of death
enum EReasonForDeath
{
    EReasonForDeath_Active                      = 0x0000,
    EReasonForDeath_ShuttingDown                = 0x0001,
    EReasonForDeath_ShutDown                    = 0x0002,
    EReasonForDeath_Terminated                  = 0x0004,
    EReasonForDeath_RemovedFromList             = 0x0008,
    EReasonForDeath_ProcessCrash                = 0x0010,
    EReasonForDeath_TimeoutExpired              = 0x0020,
    EReasonForDeath_IdleTimeoutExpired          = 0x0040,
    EReasonForDeath_MaxRequestsServedExceeded   = 0x0080,
    EReasonForDeath_MaxRequestQLengthExceeded   = 0x0100,
    EReasonForDeath_MemoryLimitExeceeded        = 0x0200,
    EReasonForDeath_PingFailed                  = 0x0400,
    EReasonForDeath_DeadlockSuspected           = 0x0800
};

/////////////////////////////////////////////////////////////////////////////
// CHistoryEntry: History info for each process
//   NOTE: All fields must be of size sizeof(DWORD)
struct CHistoryEntry
{
    // Process identity
    DWORD            dwPID;
    DWORD            dwInternalProcessNumber;

    // Requests stats
    DWORD            dwRequestsExecuted;
    DWORD            dwRequestsPending;
    DWORD            dwRequestsExecuting;

    DWORD            dwPeakMemoryUsed;

    // Times
    __int64          tmCreateTime;
    __int64          tmDeathTime;
    
    // Reason for death
    EReasonForDeath  eReason;
};


#endif