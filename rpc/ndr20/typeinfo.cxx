//************************************************************************
//
//  Copyright (C) Microsoft Corporation, 1996-2000.
//
//  File:       typeinfo.cxx
//
//  Contents:   Generates -Oi2 proxies and stubs from an ITypeInfo.
//
//  Functions:  CacheRegister
//              CacheRelease
//              CacheLookup
//
//  History:    26-Apr-96 ShannonC  Created
//              June 1997 YongQu    Add UDT support
//              Oct  1998 YongQu    arbitrary length vtbl
//
//----------------------------------------------------------------------------
#include <typeinfo.h>
#include <interp.h>
#include <stddef.h>
#include <ndrtypes.h>
#include <tiutil.h>
#ifdef DOSWIN32RPC
#include <critsec.hxx>
#endif
#include <sysinc.h>
#include <limits.h>
#include "fmtstr.h"

#define GetWireSize(x) (x->WireSize)
#define GetMemorySize(x) (x->MemorySize)
#define GetMemoryAlignment(x) (x->MemoryAlignment)
#define GetWireAlignment(x) (x->WireAlignment)

extern const IRpcProxyBufferVtbl CStdProxyBuffer3Vtbl = {
    CStdProxyBuffer_QueryInterface,
    CStdProxyBuffer_AddRef,
    CStdProxyBuffer3_Release,
    CStdProxyBuffer2_Connect,
    CStdProxyBuffer2_Disconnect };


HRESULT NdrpInitializeStublessVtbl(ULONG numMethods);
void GetTemplateVtbl(void *** pVtbl);
void ReleaseTemplateVtbl(void ** pVtbl);
void GetTemplateForwardVtbl(void *** pVtbl);
void ReleaseTemplateForwardVtbl(void ** pVtbl);
extern const IReleaseMarshalBuffersVtbl CStdProxyBuffer_ReleaseMarshalBuffersVtbl;
extern const IReleaseMarshalBuffersVtbl CStdStubBuffer_ReleaseMarshalBuffersVtbl;


static I_RPC_MUTEX TypeInfoMutex = 0;

//#define CACHE_BLOCK  32
//#define INIT_HIGH_MARK  20
//#define THRASHING_TIME  1000*30    // 30 sec

//The constants can be tunable instead of constants.
static LONG CACHE_BLOCK=32;
static ULONG INIT_HIGH_MARK = 20;
static ULONG IDLE_TIME= 1000 * 60 *5;   // 5 minutes.

#define THRASHING_TIME  1000*30
#define DELTA_MARK 4       // shrink 25% each time.



HINSTANCE       hOleAut32 = 0;
// local to this file only
static TypeInfoCache*  g_pCache = NULL;//array of cache entries.
static LONG            g_lActiveCacheRef = 0;
static LONG            g_lCacheSize = 0;
static LONG            g_lTotalCacheRef = 0;
#ifdef DEBUGRPC
static LONG            g_lCount = 0;
#endif

#if defined(_IA64_)

#define ARGS_IN_REGISTERS 8

#elif defined(_AMD64_)

#define ARGS_IN_REGISTERS 4

#endif

//+---------------------------------------------------------------------------
//
//  Function:   CreateProxyFromTypeInfo
//
//  Synopsis:   Creates an interface proxy using the type information supplied
//              in pTypeInfo.
//
//  Arguments:
//    pTypeInfo   - Supplies the ITypeInfo * describing the interface.
//    punkOuter   - Specifies the controlling unknown.
//    riid        - Specifies the interface ID.
//    ppProxy     - Returns a pointer to the IRpcProxyBuffer interface.
//    ppv         - Returns a pointer to the specified interface.
//
//  Returns:
//    S_OK
//    E_NOINTERFACE
//    E_OUTOFMEMORY
//
//----------------------------------------------------------------------------
HRESULT STDAPICALLTYPE
CreateProxyFromTypeInfo
(
    IN  ITypeInfo *         pTypeInfo,
    IN  IUnknown *          punkOuter,
    IN  REFIID              riid,
    OUT IRpcProxyBuffer **  ppProxy,
    OUT void **             ppv
)
{
    HRESULT hr = E_FAIL;
    BOOL    fIsDual;
    void  * pVtbl;

    *ppProxy = NULL;
    *ppv = NULL;

    //Get the proxy vtable.
    hr = GetProxyVtblFromTypeInfo(pTypeInfo, riid, &fIsDual, &pVtbl);

    if(SUCCEEDED(hr))
    {
        //Create the proxy.
        CStdProxyBuffer2 *pProxyBuffer;

        pProxyBuffer = new CStdProxyBuffer2;
        if(pProxyBuffer != NULL)
        {
            memset(pProxyBuffer, 0, sizeof(CStdProxyBuffer2));
            pProxyBuffer->lpVtbl = &CStdProxyBuffer3Vtbl;
            pProxyBuffer->RefCount = 1;
            pProxyBuffer->punkOuter = punkOuter ?
                                      punkOuter : (IUnknown *) pProxyBuffer;
            pProxyBuffer->pProxyVtbl = pVtbl;
            pProxyBuffer->pRMBVtbl = &CStdProxyBuffer_ReleaseMarshalBuffersVtbl;

            if(fIsDual)
            {
                pProxyBuffer->iidBase = IID_IDispatch;

                //Create the proxy for the base interface.
                hr = NdrpCreateProxy(IID_IDispatch,
                                     (IUnknown *) pProxyBuffer,
                                     &pProxyBuffer->pBaseProxyBuffer,
                                     (void **)&pProxyBuffer->pBaseProxy);
            }
            else
            {
                hr = S_OK;
            }

            if(SUCCEEDED(hr))
            {
                *ppProxy = (IRpcProxyBuffer *) pProxyBuffer;
                pProxyBuffer->punkOuter->lpVtbl->AddRef(pProxyBuffer->punkOuter);
                *ppv = &pProxyBuffer->pProxyVtbl;
            }
            else
            {
                delete pProxyBuffer;
            }
        }
        else
        {
            hr = E_OUTOFMEMORY;
        }

        if(FAILED(hr))
        {
            ReleaseProxyVtbl(pVtbl);
        }

    }

    return hr;
}

//+---------------------------------------------------------------------------
//
//  Function:   GetProxyVtblFromTypeInfo
//
//  Synopsis: Get a pointer to the proxy vtbl. The proxy vtbl should be
//            released via ReleaseProxyVtbl.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT GetProxyVtblFromTypeInfo

(
    IN  ITypeInfo *         pTypeInfo,
    IN  REFIID              riid,
    OUT BOOL *              pfIsDual,
    OUT void **             ppVtbl
)
{
    HRESULT hr = E_FAIL;
    TypeInfoVtbl *pInfo;

    //Get the vtbl.
    hr = GetVtbl(pTypeInfo, riid, &pInfo);
    if(SUCCEEDED(hr))
    {
        *pfIsDual = pInfo->fIsDual;
        *ppVtbl = &pInfo->proxyVtbl.Vtbl;
    }
    else
    {
        *ppVtbl = NULL;
    }

    return hr;
}


//+---------------------------------------------------------------------------
//
//  Function:   CreateStubFromTypeInfo
//
//  Synopsis:   Create an interface stub from the type information
//              supplied in pTypeInfo.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT STDAPICALLTYPE
CreateStubFromTypeInfo
(
    IN  ITypeInfo *         pTypeInfo,
    IN  REFIID              riid,
    IN  IUnknown *          punkServer,
    OUT IRpcStubBuffer **   ppStub
)
{
    HRESULT hr = E_FAIL;
    BOOL    fIsDual;
    IRpcStubBufferVtbl *pVtbl;
    void ** pForwardingVtbl;


    *ppStub = NULL;

    //Get the stub vtable.
    hr = GetStubVtblFromTypeInfo(pTypeInfo, riid, &fIsDual, &pVtbl);

    if(SUCCEEDED(hr))
    {
        //Create the stub
        IUnknown *              punkForward;

        CStdStubBuffer2 *pStubBuffer = new CStdStubBuffer2;

        if(pStubBuffer != NULL)
        {
            GetTemplateForwardVtbl(&pForwardingVtbl);

            //Initialize the new stub buffer.
            pStubBuffer->lpForwardingVtbl = pForwardingVtbl;
            pStubBuffer->pBaseStubBuffer = 0;
            pStubBuffer->lpVtbl = pVtbl;
            pStubBuffer->RefCount= 1;
            pStubBuffer->pvServerObject = 0;
            pStubBuffer->pRMBVtbl = &CStdStubBuffer_ReleaseMarshalBuffersVtbl;

            *ppStub = (IRpcStubBuffer *) &pStubBuffer->lpVtbl;

             //Connect the stub to the server object.
            if(punkServer != 0)
            {
                hr = punkServer->lpVtbl->QueryInterface(
                        punkServer,
                        riid,
                        (void **) &pStubBuffer->pvServerObject);
            }
            else
            {
                hr = S_OK;
            }

            if(SUCCEEDED(hr))
            {
                if(punkServer != 0)
                    punkForward = (IUnknown *) &pStubBuffer->lpForwardingVtbl;
                else
                    punkForward = 0;

                if(fIsDual)
                {
                    //Create a stub for the base interface
                    hr = NdrpCreateStub(IID_IDispatch,
                                        punkForward,
                                        &pStubBuffer->pBaseStubBuffer);
                }

                if(FAILED(hr))
                {
                    if(pStubBuffer->pvServerObject)
                        pStubBuffer->pvServerObject->lpVtbl->Release(pStubBuffer->pvServerObject);
                    NdrOleFree(pStubBuffer);
                }
            }
            else
            {
                hr = E_OUTOFMEMORY;
            }
        }

        if(FAILED(hr))
        {
            ReleaseTemplateForwardVtbl(pForwardingVtbl);
            ReleaseStubVtbl(pVtbl);
        }
    }
    return hr;
}


//+---------------------------------------------------------------------------
//
//  Function:   GetStubVtblFromTypeInfo
//
//  Synopsis: Get a pointer to the stub vtbl. The stub vtbl should be
//            released via ReleaseStubVtbl.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT GetStubVtblFromTypeInfo(
    IN  ITypeInfo *           pTypeInfo,
    IN  REFIID                riid,
    OUT BOOL  *               pfIsDual,
    OUT IRpcStubBufferVtbl ** ppVtbl)
{
    HRESULT hr = E_FAIL;
    TypeInfoVtbl *pInfo;

    //Get the vtbl.
    hr = GetVtbl(pTypeInfo, riid, &pInfo);
    if(SUCCEEDED(hr))
    {
        *pfIsDual = pInfo->fIsDual;
        *ppVtbl = &pInfo->stubVtbl.Vtbl;
    }

    return hr;
}


HRESULT CheckTypeInfo(
    IN  ITypeInfo  *pTypeInfo,
    OUT ITypeInfo **pptinfoProxy,
    OUT USHORT     *pcMethods,
    OUT BOOL       *pfIsDual)
{
    HRESULT      hr;
    TYPEATTR   * pTypeAttr;
    HREFTYPE     hRefType;
    UINT         cbSizeVft = 0;
    ITypeInfo   *ptinfoProxy = NULL;
    USHORT       cMethods;

    *pfIsDual = FALSE;

    hr = pTypeInfo->lpVtbl->GetTypeAttr(pTypeInfo, &pTypeAttr);
    if(SUCCEEDED(hr))
    {
        if(pTypeAttr->wTypeFlags & TYPEFLAG_FDUAL)
        {
            *pfIsDual = TRUE;

            if(TKIND_DISPATCH == pTypeAttr->typekind)
            {
                //Get the TKIND_INTERFACE type info.
                hr = pTypeInfo->lpVtbl->GetRefTypeOfImplType(pTypeInfo, (UINT) -1, &hRefType);
                if(SUCCEEDED(hr))
                {
                    hr = pTypeInfo->lpVtbl->GetRefTypeInfo(pTypeInfo, hRefType, &ptinfoProxy);
                    if(SUCCEEDED(hr))
                    {
                        TYPEATTR * ptattrProxy;
                        hr = ptinfoProxy->lpVtbl->GetTypeAttr(ptinfoProxy, &ptattrProxy);
                        if(SUCCEEDED(hr))
                        {
                            cbSizeVft = ptattrProxy->cbSizeVft;
                            ptinfoProxy->lpVtbl->ReleaseTypeAttr(ptinfoProxy, ptattrProxy);
                        }
                    }
                }
            }
            else if (TKIND_INTERFACE == pTypeAttr->typekind)
            {
                pTypeInfo->lpVtbl->AddRef(pTypeInfo);
                ptinfoProxy = pTypeInfo;
                cbSizeVft = pTypeAttr->cbSizeVft;
            }
            else
            {
                hr = E_FAIL;
            }
        }
        else if((pTypeAttr->wTypeFlags & TYPEFLAG_FOLEAUTOMATION) &&
                (TKIND_INTERFACE == pTypeAttr->typekind))
        {
            pTypeInfo->lpVtbl->AddRef(pTypeInfo);
            ptinfoProxy = pTypeInfo;
            cbSizeVft = pTypeAttr->cbSizeVft;
        }
        else
        {
            hr = E_FAIL;
        }
        pTypeInfo->lpVtbl->ReleaseTypeAttr(pTypeInfo, pTypeAttr);
    }

    cMethods = (USHORT) ( (cbSizeVft - VTABLE_BASE) / sizeof(void *) );

    if(SUCCEEDED(hr))
    {
        *pptinfoProxy = ptinfoProxy;

        //Calculate the number of methods in the vtable.
        *pcMethods = cMethods;
    }
    else
    {
        *pptinfoProxy = NULL;

        if(ptinfoProxy != NULL)
        {
            ptinfoProxy->lpVtbl->Release(ptinfoProxy);
        }
    }

    return hr;
}



//+---------------------------------------------------------------------------
//
//  Function:   GetVtbl
//
//  Synopsis: Get a pointer to the vtbl structure.
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT GetVtbl(
    IN  ITypeInfo *         pTypeInfo,
    IN  REFIID              riid,
    OUT TypeInfoVtbl **     ppVtbl)
{
    HRESULT hr;
    RPC_STATUS   rc;
    USHORT       numMethods;
    MethodInfo * aMethodInfo;
    BOOL         fIsDual = FALSE;
    ITypeInfo  * ptinfoProxy = NULL;

    *ppVtbl = NULL;

    rc = NdrpPerformRpcInitialization();

    if (RPC_S_OK != rc)
        return HRESULT_FROM_WIN32(rc);

    // the other two mutexes will be initialized in NdrpInitializeStublessVtbl
    if ( TypeInfoMutex == NULL )
        {
        hr = NdrpInitializeMutex( &TypeInfoMutex );
        if ( FAILED( hr ) )
            return hr;
        }
	
    //Check the cache.
    I_RpcRequestMutex(&TypeInfoMutex);
    hr = CacheLookup(riid, ppVtbl);
    I_RpcClearMutex(TypeInfoMutex);

    if(FAILED(hr))
    {
        //We didn't find the interface in the cache.
        //Create a vtbl from the ITypeInfo.
        hr = CheckTypeInfo(pTypeInfo, &ptinfoProxy, &numMethods, &fIsDual);

        if(SUCCEEDED(hr))
        {
            //allocate space for per-method data.
            aMethodInfo = (MethodInfo *) alloca(numMethods * sizeof(MethodInfo));
            if(aMethodInfo != NULL)
            {
                memset(aMethodInfo, 0, numMethods * sizeof(MethodInfo));

                //Get the per-method data.
                hr = GetFuncDescs(ptinfoProxy, aMethodInfo);
                if(SUCCEEDED(hr))
                {
                    hr = CreateVtblFromTypeInfo(riid, fIsDual, numMethods, aMethodInfo, ppVtbl);
                    if(SUCCEEDED(hr))
                    {
                        hr = CacheRegister(riid,ppVtbl);
                    }
                }
                ReleaseFuncDescs(numMethods, aMethodInfo);
            }
            else
            {
                hr = E_OUTOFMEMORY;
            }

            ptinfoProxy->lpVtbl->Release(ptinfoProxy);
        }
    }
    return hr;
}


//+---------------------------------------------------------------------------
//
//  Function:   CreateVtblFromTypeInfo
//
//  Synopsis:   Create a vtbl structure from the type information.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT CreateVtblFromTypeInfo(
    IN  REFIID          riid,
    IN  BOOL            fIsDual,
    IN  USHORT          numMethods,
    IN  MethodInfo    * pMethodInfo,
    OUT TypeInfoVtbl ** ppVtbl)
{
    HRESULT hr = S_OK;
    USHORT  iMethod;
    ULONG   cbVtbl;
    ULONG   cbOffsetTable;
    USHORT  cbProcFormatString = 0;
    ULONG   cbSize;
    TypeInfoVtbl *pInfo = NULL;
    byte *pTemp;
    PFORMAT_STRING pTypeFormatString = NULL;
    PFORMAT_STRING pProcFormatString;
    unsigned short *pFormatStringOffsetTable;
    CTypeGen typeGen;
    CProcGen procGen;
    USHORT   cbFormat;
    USHORT offset = 0;
    ULONG cbDelegationTable;
    void **pDispatchTable = NULL;
    void **pStublessClientVtbl = NULL;
    void **pForwardingVtbl = NULL;

    *ppVtbl = NULL;

#ifdef DEBUGRPC
    InterlockedIncrement(&g_lCount);
#endif

    //Compute the size of the vtbl structure;
    cbVtbl = numMethods * sizeof(void *);

    if(fIsDual)
        cbDelegationTable = cbVtbl;
    else
        cbDelegationTable = 0;

    cbOffsetTable = numMethods * sizeof(USHORT);

    //Compute the size of the proc format string.
    for(iMethod = 3;
        iMethod < numMethods;
        iMethod++)
    {
        if(pMethodInfo[iMethod].pFuncDesc != NULL)
        {
#if !defined(__RPC_WIN64__)
            cbProcFormatString += 22;
#else
            // proc format string in 64bit is longer
            cbProcFormatString += 22 + sizeof(NDR_PROC_HEADER_EXTS64);
#endif
            cbProcFormatString += pMethodInfo[iMethod].pFuncDesc->cParams * 6;
        }
    }

    cbSize = sizeof(TypeInfoVtbl) + cbVtbl + cbDelegationTable + cbOffsetTable + cbProcFormatString;

    //Allocate the structure
    pInfo = (TypeInfoVtbl *) I_RpcAllocate(cbSize);

    if(pInfo != NULL)
    {
        memset(pInfo, 0, cbSize);

        pTemp = (byte *) pInfo->proxyVtbl.Vtbl + cbVtbl;

        if(cbDelegationTable != 0)
        {
            pDispatchTable = (void **) pTemp;
            pInfo->stubVtbl.header.pDispatchTable = (const PRPC_STUB_FUNCTION *) pDispatchTable;
            pTemp += cbDelegationTable;
        }

        pFormatStringOffsetTable = (unsigned short *) pTemp;
        pTemp += cbOffsetTable;
        pProcFormatString = (PFORMAT_STRING) pTemp;


        pInfo->proxyVtbl.Vtbl[0] = IUnknown_QueryInterface_Proxy;
        pInfo->proxyVtbl.Vtbl[1] = IUnknown_AddRef_Proxy;
        pInfo->proxyVtbl.Vtbl[2] = IUnknown_Release_Proxy;

        hr = NdrpInitializeStublessVtbl((ULONG)numMethods);
        if (FAILED(hr))
        {
            if (pInfo)
                I_RpcFree(pInfo);
            return hr;
        }

        //Get the format strings.
        //Generate -Oi2 proc format string from the ITypeInfo.
        GetTemplateVtbl(&pStublessClientVtbl);
        GetTemplateForwardVtbl(&pForwardingVtbl);
        for(iMethod = 3;
            SUCCEEDED(hr) && iMethod < numMethods;
            iMethod++)
        {
            if(pMethodInfo[iMethod].pFuncDesc != NULL)
            {
                pFormatStringOffsetTable[iMethod] = offset;
                hr = procGen.GetProcFormat(&typeGen,
                                           pMethodInfo[iMethod].pTypeInfo,
                                           pMethodInfo[iMethod].pFuncDesc,
                                           iMethod,
                                           (PFORMAT_STRING)pTemp,
                                           &cbFormat);

                if (FAILED(hr))
                {
                    ReleaseTemplateVtbl(pStublessClientVtbl);
                    ReleaseTemplateForwardVtbl(pForwardingVtbl);
                    if (pInfo)
                        I_RpcFree(pInfo);
                    return hr;
                }
                pTemp += cbFormat;
                offset += cbFormat;

                //Stubless client function.
                pInfo->proxyVtbl.Vtbl[iMethod] = pStublessClientVtbl[iMethod];
                if(pDispatchTable != NULL)
                {
                    //Interpreted server function.
                    pDispatchTable[iMethod] = NdrStubCall2;
                }
            }
            else
            {
                pFormatStringOffsetTable[iMethod] = (USHORT) -1;

                //Proxy delegation forwarding function.
                pInfo->proxyVtbl.Vtbl[iMethod] =  pForwardingVtbl[iMethod];

                if(pDispatchTable != NULL)
                {
                    //Stub delegation forwarding function.
                    pDispatchTable[iMethod] = NdrStubForwardingFunction;
                }
            }
        }

        ReleaseTemplateForwardVtbl(pForwardingVtbl);
        ReleaseTemplateVtbl(pStublessClientVtbl);

        if(SUCCEEDED(hr))
        {
            USHORT length;

            hr = typeGen.GetTypeFormatString(&pTypeFormatString, &length);
        }

        if(SUCCEEDED(hr))
        {
            //Initialize the vtbl.
            pInfo->cRefs = 1;

            //Initialize the iid.
            pInfo->iid = riid;
            pInfo->fIsDual = fIsDual;

            //Initialize the MIDL_STUB_DESC.
            pInfo->stubDesc.pfnAllocate = NdrOleAllocate;
            pInfo->stubDesc.pfnFree = NdrOleFree;
            //pInfo->stubDesc.apfnExprEval = ExprEvalRoutines;
            pInfo->stubDesc.pFormatTypes = pTypeFormatString;
#if !defined(__RPC_WIN64__)
            pInfo->stubDesc.Version = 0x20000; /* Ndr library version */
            pInfo->stubDesc.MIDLVersion = MIDL_VERSION_3_0_44;
#else
            pInfo->stubDesc.Version = 0x50002; /* Ndr library version */
            pInfo->stubDesc.MIDLVersion = MIDL_VERSION_5_2_202;
#endif
            pInfo->stubDesc.aUserMarshalQuadruple = UserMarshalRoutines;

            //Initialize the MIDL_SERVER_INFO.
            pInfo->stubInfo.pStubDesc = &pInfo->stubDesc;
            pInfo->stubInfo.ProcString = pProcFormatString;
            pInfo->stubInfo.FmtStringOffset = pFormatStringOffsetTable;

            //Initialize the stub vtbl.
            pInfo->stubVtbl.header.piid = &pInfo->iid;
            pInfo->stubVtbl.header.pServerInfo = &pInfo->stubInfo;
            pInfo->stubVtbl.header.DispatchTableCount = numMethods;

            //Initialize stub methods.
            memcpy(&pInfo->stubVtbl.Vtbl, &CStdStubBuffer2Vtbl, sizeof(CStdStubBuffer2Vtbl));
            pInfo->stubVtbl.Vtbl.Release = CStdStubBuffer3_Release;

            //Initialize the proxy info.
            pInfo->proxyInfo.pStubDesc = &pInfo->stubDesc;
            pInfo->proxyInfo.ProcFormatString = pProcFormatString;
            pInfo->proxyInfo.FormatStringOffset = pFormatStringOffsetTable;

            //Initialize the proxy vtbl.
            pInfo->proxyVtbl.header.pStublessProxyInfo = &pInfo->proxyInfo;
            pInfo->proxyVtbl.header.piid = &pInfo->iid;

            *ppVtbl = pInfo;
        }
        else
        {
            //Free the memory.
            I_RpcFree(pInfo);
        }
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }
    return hr;

}


//+---------------------------------------------------------------------------
//
//  Function:   GetFuncDescs
//
//  Synopsis:   Get the funcdesc for each method.
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT GetFuncDescs(
    IN  ITypeInfo *pTypeInfo,
    OUT MethodInfo *pMethodInfo)
{
    HRESULT hr;
    TYPEATTR *pTypeAttr;
    HREFTYPE hRefType;
    ITypeInfo *pRefTypeInfo;

    hr = pTypeInfo->lpVtbl->GetTypeAttr(pTypeInfo, &pTypeAttr);

    if(SUCCEEDED(hr))
    {
        if(IsEqualIID(IID_IUnknown, pTypeAttr->guid))
        {
            hr = S_OK;
        }
        else if(IsEqualIID(IID_IDispatch, pTypeAttr->guid))
        {
            hr = S_OK;
        }
        else
        {
            //This is an oleautomation interface.
            ULONG i, iMethod;
            FUNCDESC *pFuncDesc;

            if(pTypeAttr->cImplTypes)
            {
                //Recursively get the inherited member functions.
                hr = pTypeInfo->lpVtbl->GetRefTypeOfImplType(pTypeInfo, 0, &hRefType);
                if(SUCCEEDED(hr))
                {
                    hr = pTypeInfo->lpVtbl->GetRefTypeInfo(pTypeInfo, hRefType, &pRefTypeInfo);
                    if(SUCCEEDED(hr))
                    {
                        hr = GetFuncDescs(pRefTypeInfo, pMethodInfo);
                        pRefTypeInfo->lpVtbl->Release(pRefTypeInfo);
                    }
                }
            }

            //Get the member functions.
            for(i = 0; SUCCEEDED(hr) && i < pTypeAttr->cFuncs; i++)
            {
                hr = pTypeInfo->lpVtbl->GetFuncDesc(pTypeInfo, i, &pFuncDesc);
                if(SUCCEEDED(hr))
                {
                    iMethod = (pFuncDesc->oVft - VTABLE_BASE) / sizeof(void *);
                    pMethodInfo[iMethod].pFuncDesc = pFuncDesc;
                    pTypeInfo->lpVtbl->AddRef(pTypeInfo);
                    pMethodInfo[iMethod].pTypeInfo = pTypeInfo;
                }
            }
        }

        pTypeInfo->lpVtbl->ReleaseTypeAttr(pTypeInfo, pTypeAttr);
   }

    return hr;
}


//+---------------------------------------------------------------------------
//
//  Function:   ReleaseFuncDescs
//
//  Synopsis:   Release the funcdescs.
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT ReleaseFuncDescs(USHORT numMethods, MethodInfo *pMethodInfo)
{
    USHORT iMethod;

    //Release the funcdescs.
    if(pMethodInfo != NULL)
    {
        for(iMethod = 0;
            iMethod < numMethods;
            iMethod++)
        {
            if(pMethodInfo[iMethod].pFuncDesc != NULL)
            {
                 //Release the funcdesc.
                 pMethodInfo[iMethod].pTypeInfo->lpVtbl->ReleaseFuncDesc(
                     pMethodInfo[iMethod].pTypeInfo,
                      pMethodInfo[iMethod].pFuncDesc);

                 pMethodInfo[iMethod].pFuncDesc = NULL;

                 //release the type info
                 pMethodInfo[iMethod].pTypeInfo->lpVtbl->Release(
                     pMethodInfo[iMethod].pTypeInfo);

                 pMethodInfo[iMethod].pTypeInfo = NULL;
            }
        }
    }
    return S_OK;
}


//+---------------------------------------------------------------------------
//
//  Function:   ReleaseProxyVtbl
//
//  Synopsis:   Releases the proxy vtbl.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT ReleaseProxyVtbl(void * pVtbl)
{
    HRESULT hr = S_OK;
    byte *pTemp;
    TypeInfoVtbl *pInfo;

    pTemp = (byte *)pVtbl - offsetof(TypeInfoVtbl, proxyVtbl.Vtbl);
    pInfo = (TypeInfoVtbl *) pTemp;

    hr = ReleaseVtbl(pInfo);
    return hr;
}


//+---------------------------------------------------------------------------
//
//  Function:   ReleaseStubVtbl
//
//  Synopsis:   Releases the stub vtbl.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT ReleaseStubVtbl(void * pVtbl)
{
    HRESULT hr = S_OK;
    byte *pTemp;
    TypeInfoVtbl *pInfo;

    pTemp = (byte *)pVtbl - offsetof(TypeInfoVtbl, stubVtbl.Vtbl);
    pInfo = (TypeInfoVtbl *) pTemp;

    hr = ReleaseVtbl(pInfo);
    return hr;
}

//+---------------------------------------------------------------------------
//
//  Function:   ReleaseTypeFormatString
//
//  Synopsis:   Frees the type format string.
//
//----------------------------------------------------------------------------
HRESULT ReleaseTypeFormatString(
    PFORMAT_STRING pTypeFormat)
{
    if((pTypeFormat != 0) &&
       (pTypeFormat != __MIDL_TypeFormatString.Format))
    {
        I_RpcFree((void *)pTypeFormat);
    }

    return S_OK;
}

//+---------------------------------------------------------------------------
//
//  Function:   CStdProxyBuffer3_Release
//
//  Synopsis:   Decrement the proxy's reference count
//
//  Returns:    Reference count.
//
//----------------------------------------------------------------------------
ULONG STDMETHODCALLTYPE
CStdProxyBuffer3_Release(
    IN  IRpcProxyBuffer *   This)
{
    ULONG               count;
    IRpcProxyBuffer *   pBaseProxyBuffer;

    count = (ULONG) ((CStdProxyBuffer2 *)This)->RefCount - 1;

    if(InterlockedDecrement(&((CStdProxyBuffer2 *)This)->RefCount) == 0)
    {
        count = 0;

        ReleaseProxyVtbl((void *) ((CStdProxyBuffer2 *)This)->pProxyVtbl);

        //Delegation support.
        pBaseProxyBuffer = ((CStdProxyBuffer2 *)This)->pBaseProxyBuffer;

        if( pBaseProxyBuffer != 0)
        {
            pBaseProxyBuffer->lpVtbl->Release(pBaseProxyBuffer);
        }

        //Free the memory
        delete (CStdProxyBuffer2 *)This;
    }

    return count;
};


//+---------------------------------------------------------------------------
//
//  Function:   CStdStubBuffer3_Release
//
//  Synopsis:   Decrement the proxy's reference count
//
//  Returns:    Reference count.
//
//----------------------------------------------------------------------------
ULONG STDMETHODCALLTYPE
CStdStubBuffer3_Release(
    IN  IRpcStubBuffer *    This)
{
    ULONG       count;
    unsigned char *pTemp;
    CStdStubBuffer2 * pStubBuffer;
    IRpcStubBuffer *pBaseStubBuffer;

    pTemp = (unsigned char *)This;
    pTemp -= offsetof(CStdStubBuffer2, lpVtbl);
    pStubBuffer = (CStdStubBuffer2 *) pTemp;

    count = (ULONG) pStubBuffer->RefCount - 1;

    if(InterlockedDecrement(&pStubBuffer->RefCount) == 0)
    {
        count = 0;

        ReleaseStubVtbl((void *) This->lpVtbl);

        pBaseStubBuffer = pStubBuffer->pBaseStubBuffer;

        if(pBaseStubBuffer != 0)
            pBaseStubBuffer->lpVtbl->Release(pBaseStubBuffer);

        //Free the stub buffer
        if (pStubBuffer->lpForwardingVtbl)
        ReleaseTemplateForwardVtbl((void **)pStubBuffer->lpForwardingVtbl);
        delete pStubBuffer;

    }

    return count;
}

//+---------------------------------------------------------------------------
//
//  Function:   GrowCacheIfNecessary
//
//  Synopsis:   increase the size of cache array if it's too small.
//
//  Arguments:
//
//  Returns:
//
//----------------------------------------------------------------------------
HRESULT GrowCacheIfNecessary()
{
    TypeInfoCache *pTemp = NULL;
    DWORD *pIndex = NULL;
    if (NULL == g_pCache)
    {
        g_pCache = (TypeInfoCache *)I_RpcAllocate(CACHE_BLOCK * sizeof(TypeInfoCache));
        if (g_pCache )
        {
            memset(g_pCache,0,CACHE_BLOCK * sizeof(TypeInfoCache));
            g_lCacheSize = CACHE_BLOCK;
            return S_OK;
        }
        else
            return E_OUTOFMEMORY;

    }

    if (g_lCacheSize <= g_lTotalCacheRef)
    {
        pTemp = (TypeInfoCache *)I_RpcAllocate((g_lCacheSize + CACHE_BLOCK)* sizeof(TypeInfoCache));

        if (NULL == pTemp)
            return E_OUTOFMEMORY;

        memset(pTemp,0,(g_lCacheSize + CACHE_BLOCK)* sizeof(TypeInfoCache));
        memcpy(pTemp,g_pCache,g_lCacheSize*sizeof(TypeInfoCache));
        I_RpcFree(g_pCache);
        g_pCache = pTemp;
        g_lCacheSize += CACHE_BLOCK;
    }

    return S_OK;


}

void swapCache(ULONG src, ULONG dest)
{
    if (src == dest)
        return;

    TypeInfoCache temp;
    memcpy(&temp,&g_pCache[src],sizeof(TypeInfoCache));
    memcpy(&g_pCache[src],&g_pCache[dest],sizeof(TypeInfoCache));
    memcpy(&g_pCache[dest],&temp,sizeof(TypeInfoCache));
}

//+---------------------------------------------------------------------------
//
//  Function:   CacheRegister
//
//  Synopsis:   add a new instance of interface into the cache list. before
//                  inserting the newly generated vtbl, make sure no other thread
//                  has generated it first. If existing, discard the one this
//                  thread generated and use the one already in the list. Otherwise
//                  put it at the end of the array and adjust cache length.
//
//  Arguments:  riid,
//              pVtbl
//
//  Returns:
//
//----------------------------------------------------------------------------
HRESULT CacheRegister(
    IID riid,
    TypeInfoVtbl ** ppVtbl)
{
    HRESULT hr = E_FAIL;
    TypeInfoVtbl *pVtbl = *ppVtbl;

    // this exact same item has been registered by others while we
    // are busying building our own: use the existing one instead.
    I_RpcRequestMutex(&TypeInfoMutex);
    if (CacheLookup(riid,ppVtbl) == S_OK)
    {
        if(pVtbl->stubDesc.pFormatTypes != __MIDL_TypeFormatString.Format)
        {
            I_RpcFree((void *) pVtbl->stubDesc.pFormatTypes);
        }
        I_RpcFree(pVtbl);
        I_RpcClearMutex(TypeInfoMutex);
        return S_OK;
    }

    hr = GrowCacheIfNecessary();
    if (SUCCEEDED(hr))
    {
        g_pCache[g_lTotalCacheRef].iid = (*ppVtbl)->iid;
        g_pCache[g_lTotalCacheRef].dwTickCount = 0;
        g_pCache[g_lTotalCacheRef].pVtbl = *ppVtbl;

        swapCache(g_lTotalCacheRef,g_lActiveCacheRef);
        g_lTotalCacheRef++;
        g_lActiveCacheRef++;
    }


    I_RpcClearMutex(TypeInfoMutex);
    return hr;

}


void swap(ULONG *dwDelta,
          ULONG *dwIndex,
          ULONG src,
          ULONG dest)
{
    if (src == dest)
        return;

    ULONG temp;
    temp = dwDelta[src];
    dwDelta[src] = dwDelta[dest];
    dwDelta[dest] = temp;

    temp = dwIndex[src];
    dwIndex[src] = dwIndex[dest];
    dwIndex[dest] = temp;
}

//+---------------------------------------------------------------------------
//
//  Function:   FindEntrysToShrink
//
//  Synopsis:   find the largest ulPivot number of entrys in the array.
//              a modification of algorithm due to C.A.R. Hoare in Programming
//              Pearls in Novermber 1985 Communications of ACM.
//
//  Arguments:
//
//  Returns:
//
//----------------------------------------------------------------------------

void  FindEntrysToShrink(ULONG *dwDelta,
                         ULONG *dwIndex,
                         ULONG ulPivot)
{
    ULONG ulLow = 0 , ulUp , ulSeq;
    ULONG ulMid ,ulIndex;
    ulUp = g_lTotalCacheRef - g_lActiveCacheRef -1 ;
    while (ulLow < ulUp)
    {
        ulSeq = ulLow;
        // pick a random number and assume it's the kth largest.
        ulIndex = ulLow + ((GetTickCount() & 0xff)*(ulUp-ulLow)/0xff);
        ulMid = dwDelta[ulIndex];
        swap(dwDelta,dwIndex,ulIndex,ulLow);
        for (ULONG i = ulLow + 1; i <= ulUp; i++)
        {
            if (dwDelta[i] >= ulMid)
                swap(dwDelta,dwIndex,++ulSeq,i);
        }
        // ulSeq is the ulSeq'th largest.
        swap(dwDelta,dwIndex,ulSeq,ulLow);
        if (ulSeq == ulPivot)
            break;  // done
        if (ulSeq < ulPivot)
            ulLow = ulSeq + 1 ;
        else
            ulUp = ulSeq - 1;
    }
}

//+---------------------------------------------------------------------------
//
//  Function:   ShrinkReleasedCacheIfNecessary
//
//  Synopsis:   adjust the released cache size if necessary.
//              the algorithm:
//              find the eldest DELTA_MARK released interfaces. to avoid thrashing,
//                  increase the released cache size if eldest one is released within
//                  30 sec. fill the empty spots with active entries.
//
//  Arguments:
//
//  Returns:    S_OK. leave the HRESULT return for possible future change.
//
//----------------------------------------------------------------------------

HRESULT ShrinkReleasedCacheIfNecessary(TypeInfoVtbl*** pppVtbl, DWORD *dwLength)
{
    static ULONG dwHigh = INIT_HIGH_MARK;
    HRESULT hr = E_FAIL;
    ULONG dwShrink ;
    ULONG *dwDelta = NULL;
    ULONG *dwIndex = NULL;
    TypeInfoVtbl **ppvtbl = NULL;
    ULONG dwTime = GetTickCount(), dwMax = 0;
    ULONG i,j, dwMin=0xffffffff;
    ULONG dwReleasedCache = g_lTotalCacheRef - g_lActiveCacheRef;

    // doesn't need to shrink
    if (dwReleasedCache <= dwHigh)
        return S_FALSE;

    dwShrink = (ULONG)(dwHigh / DELTA_MARK); // number to shrink
    dwDelta = (ULONG *)I_RpcAllocate(dwReleasedCache * sizeof(ULONG));
    dwIndex = (ULONG *)I_RpcAllocate(dwReleasedCache * sizeof(ULONG));
    ppvtbl = (TypeInfoVtbl **)I_RpcAllocate(dwShrink * sizeof(TypeInfoVtbl *));

    if ( ( NULL == ppvtbl ) || ( NULL == dwDelta ) || ( NULL == dwIndex ) )
    {
        if ( NULL != dwDelta ) I_RpcFree( dwDelta );
        if ( NULL != dwIndex ) I_RpcFree( dwIndex );
        if ( NULL != ppvtbl ) I_RpcFree( ppvtbl );
        return E_OUTOFMEMORY;
    }

    for ( i = 0; i < dwReleasedCache; i++)
    {
        dwDelta[i] = dwTime - g_pCache[g_lActiveCacheRef+i].dwTickCount;
        dwIndex[i] = g_lActiveCacheRef+i;
        // basic book keeping to find the eldest and latest release.
        if (dwDelta[i] > dwMax)
            dwMax = dwDelta[i];
        if (dwDelta[i] < dwMin)
            dwMin = dwDelta[i];
    }

    // don't reclaim those released entries if it's thrashing.
    if (dwMax <= THRASHING_TIME)
    {
        dwHigh += (ULONG)(dwHigh / DELTA_MARK);
        return S_FALSE;
    }

    FindEntrysToShrink(dwDelta,dwIndex,dwShrink);


    // cleanup the entries to be removed.
    for (i = 0; i < dwShrink; i++)
    {
        ppvtbl[i] = g_pCache[dwIndex[i]].pVtbl;
        ASSERT(ppvtbl[i]->cRefs == 0);
        g_pCache[dwIndex[i]].pVtbl = 0;
    }


    // fill in the empty spots.
    j = g_lTotalCacheRef -1;
    for (i = 0; i < dwShrink; i++)
    {

        while (j > 0 && (g_pCache[j].pVtbl == 0)) j--;
        if (j > dwIndex[i])
        {
            memcpy(&g_pCache[dwIndex[i]],&g_pCache[j],sizeof(TypeInfoCache));
            memset(&g_pCache[j],0,sizeof(TypeInfoCache));
            j--;
        }
        else
            memset(&g_pCache[dwIndex[i]],0,sizeof(TypeInfoCache));

    }
    g_lTotalCacheRef -= dwShrink;

#ifdef DEBUGRPC
    for (i = 0; (LONG)i < g_lTotalCacheRef; i++)
        ASSERT(g_pCache[i].pVtbl != 0);
#endif

    if (dwMin > IDLE_TIME)
    {
        dwHigh -= (ULONG)(dwHigh / DELTA_MARK);
        if (dwHigh < INIT_HIGH_MARK)
            dwHigh = INIT_HIGH_MARK;
    }

    // don't free those vtbls: do it out of CS
    *pppVtbl = ppvtbl;
    *dwLength = dwShrink;
    return S_OK;

}



//+---------------------------------------------------------------------------
//
//  Function:   ReleaseVtbl
//
//  Synopsis:   Releases the vtbl.
//
//  Arguments:
//
//  Returns:
//    S_OK
//
//----------------------------------------------------------------------------
HRESULT ReleaseVtbl(TypeInfoVtbl *pInfo)
{
    TypeInfoVtbl **ppvtbl = NULL;
    DWORD dwLength = 0;
    LONG i;
    HRESULT hr = E_FAIL;

    I_RpcRequestMutex(&TypeInfoMutex);
    if (0 == --pInfo->cRefs)
    {
        for (i = 0 ; i < g_lTotalCacheRef; i++)
        {
            if (IsEqualIID(pInfo->iid,g_pCache[i].iid))
            {
                g_pCache[i].dwTickCount = GetTickCount();
                g_lActiveCacheRef--;
                swapCache(i,g_lActiveCacheRef);
                hr = ShrinkReleasedCacheIfNecessary(&ppvtbl,&dwLength);
                break;
             }
        }
    }
    else
    {
        I_RpcClearMutex(TypeInfoMutex);
        return S_OK;
    }
    I_RpcClearMutex(TypeInfoMutex);

    // free the vtbl outof mutex.
    if (S_OK == hr)
    {
        for (i = 0; i < (LONG)dwLength; i++)
        {

            if((ppvtbl[i])->stubDesc.pFormatTypes != __MIDL_TypeFormatString.Format)
            {
                I_RpcFree((void *) (ppvtbl[i])->stubDesc.pFormatTypes);
            }

            I_RpcFree((void *) ppvtbl[i]);
        }
        I_RpcFree(ppvtbl);
    }

    return hr;
}


//+---------------------------------------------------------------------------
//
//  Function:   CacheLookup
//
//  Synopsis:   look up a TypeInfoVtbl from cache array using IID.
//              adjust the cache state if the entry is released.
//
//  Arguments:  riid
//              ppInfo
//
//  Returns:    S_OK if an entry is found in cache. E_NOINTERFACE is not.
//
//----------------------------------------------------------------------------
HRESULT CacheLookup(
    REFIID riid,
    TypeInfoVtbl **ppInfo)
{
    HRESULT hr = E_NOINTERFACE;
    LONG i;

    if (NULL == g_pCache)
        goto Exit;

    for ( i = 0; i < g_lTotalCacheRef; i++)
    {
        if (IsEqualIID(riid,g_pCache[i].iid))
        {
            *ppInfo = g_pCache[i].pVtbl;
            if (0 == (*ppInfo)->cRefs++)
            {
                g_pCache[i].dwTickCount = 0;
                swapCache(i,g_lActiveCacheRef++);
            }
            hr = S_OK;
            goto Exit;
        }
    }

Exit:
    return hr;

}


//+---------------------------------------------------------------------------
//
//  Function:   NdrpGetTypeGenCookie
//
//  Synopsis:   Allocate a cookie that can be used in subsequent calls to
//              NdrpGetProcFormatString, NdrpGetTypeFormatString, and
//              NdrpReleaseTypeGenCookie.
//
//  Parameters:
//
//  Returns:    ppvTypeGenCookie: A type gen cookie
//
//----------------------------------------------------------------------------
EXTERN_C
HRESULT NdrpGetTypeGenCookie(void **ppvTypeGenCookie)
{
	HRESULT hr = S_OK;
	CTypeGen *pTypeGen;

	if (ppvTypeGenCookie == NULL)
		return E_POINTER;

    // Ensure that everything is initialized.
    RPC_STATUS sc = NdrpPerformRpcInitialization();
    if (sc != RPC_S_OK)
        return HRESULT_FROM_WIN32(sc);

	pTypeGen = new CTypeGen;
	if (pTypeGen)
		*ppvTypeGenCookie = pTypeGen;
	else
		hr = E_OUTOFMEMORY;

	return hr;
}

//+---------------------------------------------------------------------------
//
//  Function:   NdrpReleaseTypeGenCookie
//
//  Synopsis:   Free memory associeated with type gen cookie allocated with
//              NdrpGetTypeGenCookie
//
//  Parameters: pvTypeGenCookie: cookie returned from NdrpGetTypeGenCookie
//
//  Returns:
//
//----------------------------------------------------------------------------
EXTERN_C
HRESULT NdrpReleaseTypeGenCookie(void *pvTypeGenCookie)
{
	CTypeGen *pTypeGen = (CTypeGen *)pvTypeGenCookie;

	delete pTypeGen;

	return S_OK;
}

//+---------------------------------------------------------------------------
//
//  Function:   NdrpGetProcFormatString
//
//  Synopsis:   Generate proc format string and type format string for specified
//              function.
//
//  Parameters: pTypeGenCookie: Type format generator object
//              pTypeInfo:      ITypeInfo interface.
//              pFuncDesc:      Function descriptor.
//              iMethod:        # of methods.
//
//  Returns:    pProcFormatString: Address for proc format string.
//              pcbFormat: Size of format string.
//
//----------------------------------------------------------------------------
EXTERN_C
HRESULT NdrpGetProcFormatString(IN  void      *pvTypeGenCookie,
								IN  ITypeInfo *pTypeInfo,
								IN  FUNCDESC  *pFuncDesc,
								IN  USHORT     iMethod,
								OUT PFORMAT_STRING pProcFormatString,
								OUT USHORT    *pcbFormat)
{
	CProcGen proc;
	CTypeGen *pTypeGen = (CTypeGen *)pvTypeGenCookie;

	if (pTypeGen == NULL)
		return E_INVALIDARG;
	else
		return proc.GetProcFormat(pTypeGen, pTypeInfo, pFuncDesc, iMethod,
								  pProcFormatString, pcbFormat);
}

//+---------------------------------------------------------------------------
//
//  Function:   NdrpGetTypeFormatString
//
//  Synopsis:   Get the MIDL_TYPE_FORMAT_STRING.
//
//  Arguments:  pvTypeGenCookie - cookie allocated with NdrpGetTypeGenCookie
//                                and used in subsequent NdrpGetProcFormatString
//                                calls.
//
//              ppTypeFormatString - Returns a pointer to the type format
//                                   string.
//
//              pLength - Returns the length of the format string.
//
//----------------------------------------------------------------------------
EXTERN_C
HRESULT NdrpGetTypeFormatString(IN void *pvTypeGenCookie,
								OUT PFORMAT_STRING * pTypeFormatString,
								OUT USHORT *         pLength)
{
	CTypeGen *pTypeGen = (CTypeGen *)pvTypeGenCookie;

	if (pTypeGen == NULL)
		return E_INVALIDARG;
	else
		return pTypeGen->GetTypeFormatString(pTypeFormatString, pLength);
}

//+---------------------------------------------------------------------------
//
//  Function:   NdrpReleaseTypeFormatString
//
//  Synopsis:   Free the memory returned from NdrpGetTypeFormatString function.
//
//  Parameters: pTypeFormatString: Address of format string returned from
//                                 NdrpGetTypeFormatString.
//
//----------------------------------------------------------------------------
EXTERN_C
HRESULT NdrpReleaseTypeFormatString(PFORMAT_STRING pTypeFormatString)
{
	if (pTypeFormatString)
	{
		if(pTypeFormatString != __MIDL_TypeFormatString.Format)
		{
			I_RpcFree((void *)pTypeFormatString);
		}	
	}

	return S_OK;
}

//+---------------------------------------------------------------------------
//
//  Function:   GetProcFormat
//
//  Synopsis:   Generate proc format string and type format string for specified
//              function.
//
//  Parameters: pTeypGen: Type format generator object
//              pTypeInfo: ITypeInfo interface.
//              pFuncDesc: Function descriptor.
//              iMethod:   # of methods.
//
//  Returns:    pProcFormatString: Address for proc format string.
//
//----------------------------------------------------------------------------
HRESULT CProcGen::GetProcFormat(
    IN  CTypeGen     * pTypeGen,
    IN  ITypeInfo    * pTypeInfo,
    IN  FUNCDESC     * pFuncDesc,
    IN  USHORT         iMethod,
    OUT PFORMAT_STRING pProcFormatString,
    OUT USHORT       * pcbFormat)
{
    HRESULT hr = S_OK;
    USHORT  iParam;
    INTERPRETER_FLAGS OiFlags ;
    INTERPRETER_OPT_FLAGS Oi2Flags ;
    INTERPRETER_OPT_FLAGS2 Oi2Flags2;
    PARAMINFO *aParamInfo;
    BOOLEAN fChangeSize,fNeedChange = FALSE;
    USHORT offset;

    aParamInfo = new PARAMINFO[pFuncDesc->cParams];

    if(0 == aParamInfo)
    {
        return E_OUTOFMEMORY;
    }

    for(iParam = 0;
        iParam < pFuncDesc->cParams;
        iParam++)
    {
        hr = VarVtOfTypeDesc(pTypeInfo,
                             &pFuncDesc->lprgelemdescParam[iParam].tdesc,
                             &aParamInfo[iParam]);

        if(SUCCEEDED(hr))
        {
            // PARAMFlags should give us enough information.
            DWORD wIDLFlags = pFuncDesc->lprgelemdescParam[iParam].idldesc.wIDLFlags;

            if(wIDLFlags & IDLFLAG_FRETVAL)
            {
                wIDLFlags |= IDLFLAG_FOUT;
            }

            if(!(wIDLFlags & (IDLFLAG_FIN | IDLFLAG_FOUT)))
            {
                //Set the direction flags.
                if(aParamInfo[iParam].vt & (VT_BYREF | VT_ARRAY))
                {
                    wIDLFlags |= IDLFLAG_FIN | IDLFLAG_FOUT;
                }
                else
                {
                    wIDLFlags |= IDLFLAG_FIN;
                }
            }

            aParamInfo[iParam].wIDLFlags = wIDLFlags;
        }
        else
        {
            goto Error;
        }
    }


    _pTypeGen = pTypeGen;
    _offset = 0;
    _pProcFormatString = pProcFormatString;
    _fClientMustSize  = FALSE;
    _fServerMustSize  = FALSE;
    _fClientCorrCheck = FALSE;
    _fServerCorrCheck = FALSE;
    _clientBufferSize = 0;
    _serverBufferSize = 0;
    _usFloatArgMask   = 0;
    _usFloatSlots     = 0;

    // The "this" pointer uses 8 bytes of stack on Alpha and 64bit platforms
    // and 4 bytes of stack on other platforms.
    _stackSize = sizeof(REGISTER_TYPE);


    //Compute the size of the parameters. leave out structures. We can only determine
    // the size later.
    // Also, we calculate the Oi2 extension flags / FloatMask for 64bit
    for(iParam = 0;
        SUCCEEDED(hr) && iParam < pFuncDesc->cParams;
        iParam++)
    {
        hr = CalcSize(aParamInfo[iParam].vt,
                      aParamInfo[iParam].wIDLFlags,
                      iParam);
    }

    if(SUCCEEDED(hr))
    {
        //Compute the size of the HRESULT return value.
        _stackSize += sizeof(REGISTER_TYPE);

        LENGTH_ALIGN(_serverBufferSize, 3);
        _serverBufferSize += 8;     // HRESULT is simple type, overestimate it also

        //Handle type
        PushByte(FC_AUTO_HANDLE);

        //Oi interpreter flags
        OiFlags.FullPtrUsed           = FALSE;
        OiFlags.RpcSsAllocUsed        = FALSE;
        OiFlags.ObjectProc            = TRUE;
        OiFlags.HasRpcFlags           = TRUE;
        OiFlags.IgnoreObjectException = FALSE;
        OiFlags.HasCommOrFault        = TRUE;
        OiFlags.UseNewInitRoutines    = TRUE;
        OiFlags.Unused                = FALSE;
        PushByte(*((byte *) &OiFlags));

        PushLong(0);       // RpcFlags

        //Method number
        PushShort(iMethod);

        offset = _offset;   // _stackSize,_clientBufferSize and _serverBufferSize could be
        //Stack size
        PushShort(_stackSize);

        //Size of client RPC message buffer.
        // place holder if there is a UDT parameter
        if(_clientBufferSize <= 65535)
            PushShort((USHORT) _clientBufferSize);
        else
        {
            hr = E_FAIL;
            goto Error;
        }

        //Size of server RPC message buffer.
        if(_serverBufferSize <= 65535)
            PushShort((USHORT) _serverBufferSize);
        else
        {
            hr = E_FAIL;
            goto Error;
        }

        //Oi2 interpreter flags
        *(byte*)&Oi2Flags = 0;
        Oi2Flags.ServerMustSize = _fServerMustSize;
        Oi2Flags.ClientMustSize = _fClientMustSize;
        Oi2Flags.HasReturn = TRUE;
        Oi2Flags.HasPipes = FALSE;
        Oi2Flags.Unused = 0;

#if defined(__RPC_WIN64__)
//      robust is only availble in 64bit tlb
        Oi2Flags.HasExtensions = TRUE;
#endif
        PushByte(*((byte *) &Oi2Flags));

        //Number of parameters + return value.
        PushByte(pFuncDesc->cParams + 1);

#if defined(__RPC_WIN64__)
//      robust is only availble in 64bit tlb
        if ( Oi2Flags.HasExtensions )
            {
            *(byte*)&Oi2Flags2 = 0;
            Oi2Flags2.HasNewCorrDesc = _fClientCorrCheck | _fServerCorrCheck ;
            Oi2Flags2.ClientCorrCheck = _fClientCorrCheck;
            Oi2Flags2.ServerCorrCheck = _fServerCorrCheck;
            PushByte( sizeof(NDR_PROC_HEADER_EXTS64) );   // header extension size
            PushByte( *( (byte *) &Oi2Flags2 ) ); // extension flags
            PushShort(0);   // client correlation count
            PushShort(0);   // server collrelation count
            PushShort(0);   // notify index
            PushShort(0);   // placeholder for WIN64 float mask
            }
#endif

        // only a place holder for now if there are struct parameters.
        //Generate the parameter info.

        //The "this" pointer uses 8 bytes of stack on Alpha
        //and 4 bytes of stack on other platforms.
        _stackSize = sizeof(REGISTER_TYPE);

        for(iParam = 0;
            SUCCEEDED(hr) && iParam < pFuncDesc->cParams;
            iParam++)
        {
            hr = GenParamDescriptor(&aParamInfo[iParam],&fChangeSize);
            if (fChangeSize)    // there are structs as parameters.
            // _stackSize etc. might need to be changed.
            {
                fNeedChange = TRUE;
            }
        }


        if(SUCCEEDED(hr))
        {
            //Generate the HRESULT return value.
            PushShort( 0x70); //IsOut, IsReturn, IsBaseType
            PushShort(_stackSize);
            PushByte(FC_LONG);
            PushByte(0);
            *pcbFormat = _offset;
        }

         if (fNeedChange)
         {
            //Compute the size of the HRESULT return value.
            LENGTH_ALIGN(_serverBufferSize, 3);
            _serverBufferSize += 4;
            _stackSize += sizeof(REGISTER_TYPE);
            SetShort(offset,_stackSize);
            SetShort(offset + sizeof(SHORT),(USHORT)_clientBufferSize);
            SetShort(offset + 2*sizeof(SHORT),(USHORT)_serverBufferSize);
            if (_clientBufferSize > _UI16_MAX || _serverBufferSize > _UI16_MAX)
            {
                hr = E_FAIL;
                goto Error;
            }
         }

#if defined(__RPC_WIN64__)
         // Set the WIN64 floatarg mask
         SetShort(24, _usFloatArgMask);
#endif
   }
Error:
    delete [] aParamInfo;
    return hr;
}



//+---------------------------------------------------------------------------
//
//  Function:   CProcGen::CalcSize
//
//  Synopsis:   calculate the stack size of current parameter in a method, also
//              calculate Oi2 extension flags for 64bit
//
//
//  Arguments:
//    IN VARTYPE vt       - vt type of current parameter
//    IN DWORD wIDLFlags  - IDL flags of this paramter
//    IN ULONG nParam     - parameter number
//
//  Returns:
//    S_OK
//    DISP_E_BADVARTYPE
//
//----------------------------------------------------------------------------
HRESULT CProcGen::CalcSize(
        IN  VARTYPE vt,
        IN  DWORD   wIDLFlags,
        IN  ULONG   nParam)
{
    HRESULT    hr = S_OK;

    switch(vt & (~VT_BYREF))
    {
    case VT_I1:
    case VT_UI1:
        _stackSize += sizeof(REGISTER_TYPE);

         if(wIDLFlags & IDLFLAG_FIN)
         {
            _clientBufferSize += 1;
         }

         if(wIDLFlags & IDLFLAG_FOUT)
         {
            _serverBufferSize += 1;
         }
        break;

    case VT_I2:
    case VT_UI2:
    case VT_BOOL:
        _stackSize += sizeof(REGISTER_TYPE);

         if(wIDLFlags & IDLFLAG_FIN)
         {
            _clientBufferSize = (_clientBufferSize + 1) & ~1;
            _clientBufferSize += 4;
         }

         if(wIDLFlags & IDLFLAG_FOUT)
         {
            _serverBufferSize = (_serverBufferSize + 1) & ~1;
            _serverBufferSize += 4;
         }
        break;

    case VT_R4:
#if defined(_WIN64)
        // setup float mask for WIN64
        if ( !( vt & VT_BYREF ) && ( _stackSize/sizeof(REGISTER_TYPE)  < ARGS_IN_REGISTERS ) ) 
            _usFloatArgMask |= 1 << ( 2 * _stackSize/sizeof(REGISTER_TYPE)  );
#endif
    case VT_I4:
    case VT_UI4:
    case VT_INT:
    case VT_UINT:
    case VT_ERROR:
    case VT_HRESULT:
        _stackSize += sizeof(REGISTER_TYPE);

         if(wIDLFlags & IDLFLAG_FIN)
         {
            _clientBufferSize = (_clientBufferSize + 3) & ~3;
            _clientBufferSize += 8;
         }

         if(wIDLFlags & IDLFLAG_FOUT)
         {
            _serverBufferSize = (_serverBufferSize + 3) & ~3;
            _serverBufferSize += 8;
         }
        break;

    case VT_R8:
    case VT_DATE:
#if defined(_WIN64)
        // setup float mask for WIN64
        if ( !( vt & VT_BYREF )  && ( _stackSize/sizeof(REGISTER_TYPE)  < ARGS_IN_REGISTERS ) ) 
            _usFloatArgMask |= 1 << ( 2 * _stackSize/sizeof(REGISTER_TYPE) + 1 );
#endif
    case VT_CY:
    case VT_I8:
    case VT_UI8:
        if(vt & VT_BYREF)
        {
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            _stackSize += 8;
        }

        if(wIDLFlags & IDLFLAG_FIN)
        {
            _clientBufferSize = (_clientBufferSize + 7) & ~7;
            _clientBufferSize += 16;
        }

        if(wIDLFlags & IDLFLAG_FOUT)
        {
            _serverBufferSize = (_serverBufferSize + 7) & ~7;
            _serverBufferSize += 16;
        }
        break;

    case VT_DECIMAL:
        if(vt & VT_BYREF)
        {
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            // On WIN64 no need to align when size is up to 8 bytes
#if defined(_AMD64_)
            _stackSize += sizeof(REGISTER_TYPE);
#else
            _stackSize += sizeof(DECIMAL);
#endif
        }

        // overestimate the alignment to avoid buffer underrun during marshalling
        if(wIDLFlags & IDLFLAG_FIN)
        {
            _clientBufferSize = (_clientBufferSize + 7) & ~7;
            _clientBufferSize += sizeof(DECIMAL)  + 8;
        }

        if(wIDLFlags & IDLFLAG_FOUT)
        {
            _serverBufferSize = (_serverBufferSize + 7) & ~7;
            _serverBufferSize += sizeof(DECIMAL)  + 8;
        }
        break;

    case VT_VARIANT:
        if(vt & VT_BYREF)
        {
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
// in new spec, VARIANT is aligned to 8 again.
#if defined(_WIN64)
            LENGTH_ALIGN(_stackSize, 7);
#endif
#if defined(_AMD64_)
            _stackSize += sizeof(REGISTER_TYPE );
#else
            _stackSize += sizeof(VARIANT);
#endif
        }

        if(wIDLFlags & IDLFLAG_FIN)
        {
            _fClientMustSize = TRUE;
            _fServerCorrCheck= TRUE;
        }

        if(wIDLFlags & IDLFLAG_FOUT)
        {
            _fServerMustSize = TRUE;
            _fClientCorrCheck= TRUE;
        }
        break;

    case VT_INTERFACE:
    case VT_UNKNOWN:
    case VT_DISPATCH:
    case VT_STREAM:
    case VT_STORAGE:
    // structure
    case VT_USERDEFINED:
    case VT_BSTR:
    case VT_MULTIINDIRECTIONS:
    case VT_CARRAY:
        // set robust check for interfaces
        if(wIDLFlags & IDLFLAG_FIN)
        {
            _fServerCorrCheck= TRUE;
        }

        if(wIDLFlags & IDLFLAG_FOUT)
        {
            _fClientCorrCheck= TRUE;
        }
        // fall through for sizing

    case VT_LPSTR:
    case VT_LPWSTR:
        _stackSize += sizeof(REGISTER_TYPE);

        if(wIDLFlags & IDLFLAG_FIN)
        {
            _fClientMustSize = TRUE;
        }

        if(wIDLFlags & IDLFLAG_FOUT)
        {
            _fServerMustSize = TRUE;
        }
        break;

    case VT_FILETIME:
        if(vt & VT_BYREF)
        {
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            _stackSize += sizeof(FILETIME);
        }

        if(wIDLFlags & IDLFLAG_FIN)
        {
            _clientBufferSize = (_clientBufferSize + 3) & ~3;
            _clientBufferSize += sizeof(FILETIME) + 4;
        }

        if(wIDLFlags & IDLFLAG_FOUT)
        {
            _serverBufferSize = (_serverBufferSize + 3) & ~3;
            _serverBufferSize += sizeof(FILETIME) + 4;
        }
        break;

    default:
        if(vt & VT_ARRAY)
        {
            _stackSize += sizeof(REGISTER_TYPE);

            if(wIDLFlags & IDLFLAG_FIN)
            {
                _fClientMustSize = TRUE;
                _fServerCorrCheck= TRUE;
            }

            if(wIDLFlags & IDLFLAG_FOUT)
            {
                _fServerMustSize = TRUE;
                _fClientCorrCheck= TRUE;
            }
        }
        else
        {
            hr = DISP_E_BADVARTYPE;
        }
        break;
    }

    return hr;
}



//+---------------------------------------------------------------------------
//
//  Function:   GenParamDescriptor
//
//  Synopsis:   generate proc string and format string for one parameter.
//
//  Parameter:  parainfo. the parameter information. vt for VARTYPE,
//                  IID if VARTYPE is interface,
//                  ITypeInfo if VARTYPE is UDT
//
//  Returns:    fChangeSize. TRUE if the parameter is a UDT. member variable of
//              CProcGen (_stackSize,_clientBufferSize and _serverBufferSize need to
//              be written back)
//
//----------------------------------------------------------------------------
HRESULT
CProcGen::GenParamDescriptor(
    IN  PARAMINFO  *parainfo,
    OUT BOOLEAN  * fChangeSize)
{
    HRESULT hr = S_OK;
    PARAM_ATTRIBUTES attr;
    USHORT     offset;
    *fChangeSize = FALSE;
    VARTYPE vt = parainfo->vt;
    VARTYPE vtnoref = vt & (~VT_BYREF) ;
    MemoryInfo ProcMemInfo;
    USHORT ParamOffset = _stackSize;

    memset( &attr, 0, sizeof(attr) );
    memset( &ProcMemInfo, 0, sizeof( ProcMemInfo ) );
    attr.IsIn = (parainfo->wIDLFlags & IDLFLAG_FIN) ? 1 : 0;
    attr.IsOut = (parainfo->wIDLFlags & IDLFLAG_FOUT) ? 1 : 0;

    switch(vtnoref)
    {
    case VT_I1:
    case VT_UI1:
    case VT_I2:
    case VT_BOOL:
    case VT_UI2:
    case VT_I4:
    case VT_INT:
    case VT_ERROR:
    case VT_HRESULT:
    case VT_UINT:
    case VT_UI4:
    case VT_R4:
        attr.IsBasetype = TRUE;
        if(vt & VT_BYREF)
        {
            attr.IsSimpleRef = TRUE;
            if(!attr.IsIn)
            {
                attr.ServerAllocSize = 1;
            }
        }
        PushShort(*((short *) &attr));

        PushShort(_stackSize);
        _stackSize += sizeof(REGISTER_TYPE);

        PushShort(VT_FC_MAP[vtnoref] );
        break;

    case VT_I8:
    case VT_UI8:
    case VT_CY:
    case VT_R8:
    case VT_DATE:
        attr.IsBasetype = TRUE;
        if(vt & VT_BYREF)
        {
            attr.IsSimpleRef = TRUE;
            if(!attr.IsIn)
            {
                attr.ServerAllocSize = 1;
            }
        }
        PushShort(*((short *) &attr));

        if(vt & VT_BYREF)
        {
            PushShort(_stackSize);
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            PushShort(_stackSize);
            _stackSize += 8;
        }

        PushShort(VT_FC_MAP[vtnoref] );
        break;
    case VT_DISPATCH:
    case VT_UNKNOWN:
    case VT_INTERFACE:
    case VT_STREAM:
    case VT_STORAGE:
        attr.MustSize = TRUE;
        attr.MustFree = TRUE;
        PushShort(*((short *) &attr));

        PushShort(_stackSize);
        _stackSize += sizeof(REGISTER_TYPE);

        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
        PushShort(offset);
        break;

    case VT_BSTR:
        attr.MustSize = TRUE;
        attr.MustFree = TRUE;

        if(vt & VT_BYREF)
        {
            attr.IsSimpleRef = TRUE;
            if(!attr.IsIn)
            {
                attr.ServerAllocSize = 1;
            }
        }
        else
        {
            attr.IsByValue = TRUE;
        }

        PushShort(*((short *) &attr));

        PushShort(_stackSize);
        _stackSize += sizeof(REGISTER_TYPE);

        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
        PushShort(offset);
        break;

    case VT_VARIANT:
        attr.MustSize = TRUE;
        attr.MustFree = TRUE;

#if defined(_AMD64_)
        // in amd64, variant is always passed by reference. 
        vt |= VT_BYREF;
        parainfo->vt  |= VT_BYREF;
#endif
        if(vt & VT_BYREF)
        {
            attr.IsSimpleRef = TRUE;
            if(!attr.IsIn)
            {
                // size of VARIANT in 64bit is larger.
                attr.ServerAllocSize = (sizeof(VARIANT) +7 ) >> 3;
            }
        }
        else
        {
            attr.IsByValue = TRUE;
        }

        PushShort(*((short *) &attr));

        if(vt & VT_BYREF)
        {
            PushShort(_stackSize);
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
#if defined(_WIN64)
            LENGTH_ALIGN(_stackSize, 7);
#endif 
            PushShort(_stackSize);
            _stackSize += sizeof(VARIANT);
        }

        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
        PushShort(offset);
        break;

    case VT_LPSTR:
    case VT_LPWSTR:
        attr.MustSize = TRUE;
        attr.MustFree = TRUE;

        if(vt & VT_BYREF)
        {
            if(!attr.IsIn)
            {
                attr.ServerAllocSize = 1;
            }
        }
        else
        {
            attr.IsSimpleRef = TRUE;
        }

        PushShort(*((short *) &attr));

        PushShort(_stackSize);
        _stackSize += sizeof(REGISTER_TYPE);

        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
        PushShort(offset);
        break;

    case VT_DECIMAL:
        attr.MustFree = TRUE;
#if defined(_AMD64_)
        // in amd64, decimal is always passed by reference.
        vt |= VT_BYREF;
        parainfo->vt |= VT_BYREF;
#endif

        if(vt & VT_BYREF)
        {
            attr.IsSimpleRef = TRUE;
            if(!attr.IsIn)
            {
                attr.ServerAllocSize = (sizeof( DECIMAL ) + 7 ) >> 3;
            }
        }
        else
        {
            attr.IsByValue = TRUE;
        }

        PushShort(*((short *) &attr));

        if(vt & VT_BYREF)
        {
            PushShort(_stackSize);
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            // On WIN64 no need to align when size is up to 8 bytes
            PushShort(_stackSize);
            _stackSize += sizeof(DECIMAL);
        }

        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
        PushShort(offset);
        break;

    case VT_FILETIME:
        attr.MustFree = TRUE;

        if(vt & VT_BYREF)
        {
            attr.IsSimpleRef = TRUE;
            if(!attr.IsIn)
            {
                attr.ServerAllocSize = 1;
            }
        }
        else
        {
            attr.IsByValue = TRUE;
        }

        PushShort(*((short *) &attr));

        if(vt & VT_BYREF)
        {
            PushShort(_stackSize);
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            // On WIN64 no need to align when size is up to 8 bytes
            PushShort(_stackSize);
            _stackSize += sizeof(FILETIME);
        }

        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
        PushShort(offset);
        break;

    case VT_CARRAY:
    case VT_USERDEFINED:
        {
        SHORT WireAlignment,length;
        BOOL   IsByRef = FALSE;
#if defined(_AMD64_)        
        IsByRef = vt & VT_BYREF;

        if ( vt & VT_USERDEFINED )
            {
            parainfo->vt |= VT_BYREF;
            }
#endif        
        hr = _pTypeGen->RegisterType(parainfo, &offset,&ProcMemInfo);
        if (FAILED(hr))
            break;
        WireAlignment = ProcMemInfo.WireAlignment;
        length = ProcMemInfo.MemorySize;

#if defined(_AMD64_)
        // we need to roll back if the struct size is 1,2,4,8
        if ( !IsByRef && length <= 8 &&  !(length & (length-1) ) )
            {
            parainfo->vt &= ~VT_BYREF;
            hr = _pTypeGen->GetOffset(offset + 2 * sizeof (BYTE), &offset);
            if(FAILED(hr))
                    break;
            }
        else    // otherwise, it's effecitvely pass by reference
            vt |= VT_BYREF;
            
#endif
        attr.MustSize = TRUE;
        attr.MustFree = TRUE;
        if ((vt & (~VT_BYREF)) != VT_CARRAY)
        {
            // this is code for server alloc field in proc format string. 
            // optimization to reduce the allocation for small top level struct
            if(vt & VT_BYREF)
            {
                USHORT serverSize = length + 7;
                serverSize = serverSize >> 3;
                if (serverSize < 8 && !attr.IsIn)
                    attr.ServerAllocSize = serverSize;
                attr.IsSimpleRef = TRUE;
                hr = _pTypeGen->GetOffset(offset + 2 * sizeof (BYTE), &offset);
                if(FAILED(hr))
                    break;
            }
            else
            {
                attr.IsByValue = TRUE;
            }
        }

        PushShort(*((short *) &attr));
        PushShort(_stackSize);
        PushShort(offset);

        if(vt & VT_BYREF)
        {
            _stackSize += sizeof(REGISTER_TYPE);
        }
        else
        {
            if (vt == VT_CARRAY)
                _stackSize += sizeof(REGISTER_TYPE);
            else
            {
                USHORT ualign = sizeof(REGISTER_TYPE) -1;
#if defined(_AMD64_)
                _stackSize += sizeof(REGISTER_TYPE);
#else                
                _stackSize += length;
#endif
                _stackSize = (_stackSize + ualign) & ~ualign;
            #if defined(__RPC_WIN64__)
                // only check for floatmask if it UDT. top level float/double is handled already
                AnalyzeFloatTypes(ParamOffset, offset);
            #endif
            }
        }

        // over estimate the buffersize to avoid buffer underrun during marshalling
        if (parainfo->wIDLFlags & IDLFLAG_FIN)
        {
            _clientBufferSize += WireAlignment+ 1;
        }
        if (parainfo->wIDLFlags & IDLFLAG_FOUT)
        {
            _serverBufferSize += WireAlignment + 1;
        }


        *fChangeSize = TRUE;
        }
        break;

    case VT_MULTIINDIRECTIONS:
        attr.MustSize = TRUE;
        attr.MustFree = TRUE;
        PushShort(*((short *) &attr));

        PushShort(_stackSize);
        _stackSize += sizeof(REGISTER_TYPE);

        // top level double pointers should be FC_RP FC_ALLOCED_ON_STACK|DEREF
        // we have to change the type format string here because RegisterType
        // doesn't know if the ** is in top level or embedded
        hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo);
        if (SUCCEEDED(hr))
            {
            hr = _pTypeGen->AdjustTopLevelRef(offset);
            }
        if (SUCCEEDED(hr))
            {
            PushShort(offset);
            }
        break;

    default:
        {
        if(vt & VT_ARRAY)
            {
            attr.MustSize = TRUE;
            attr.MustFree = TRUE;

            if(vt & VT_BYREF)
                {
                attr.IsSimpleRef = TRUE;
                if(!attr.IsIn)
                    {
                    attr.ServerAllocSize = 1;
                    }
                }
            else
                {
                attr.IsByValue = TRUE;
                }

            PushShort(*((short *) &attr));

            PushShort(_stackSize);
            _stackSize += sizeof(REGISTER_TYPE);

            hr = _pTypeGen->RegisterType(parainfo, &offset, &ProcMemInfo );
            PushShort(offset);
            }
        else
            {
            if (vt & VT_VECTOR)
                {
                NDR_ASSERT(0, "VT_VECTOR is not supported");
                }
            hr = DISP_E_BADVARTYPE;
            }
            break;
        }// end default
    }


    return hr;
}



//+---------------------------------------------------------------------------
//
//  Method:     CProcGen::IsHomogeneous
//
//  Synopsis:   Determine if a type is a homogeneous floating point type
//
//  Parameter:  pFormat     -- The type info string
//              fc          -- FC_FLOAT or FC_DOUBLE
//
//  Returns:    true if the type is a homogeneous type of the specified
//              FC_FLOAT or FC_DOUBLE
//
//  Notes:      Base types are homogeneous.  Structs are homogeneous if each
//              member is homogeneous.
//
//----------------------------------------------------------------------------
#if defined(__RPC_WIN64__)
bool CProcGen::IsHomogeneous(PFORMAT_STRING pFormat, FORMAT_CHARACTER fc)
{
    switch (*pFormat)
    {
    case FC_FLOAT:
    case FC_DOUBLE:
        return ( *pFormat == fc );

    case FC_STRUCT:
        return IsHomogeneousMemberLayout(pFormat + 4, fc);

    case FC_BOGUS_STRUCT:
        return IsHomogeneousMemberLayout(pFormat + 8, fc);

    }

    return false;
}
#endif // __RPC_WIN64__


//+---------------------------------------------------------------------------
//
//  Method:     CProcGen::IsHomogeneousMemberLayout
//
//  Synopsis:   Determine if a member layout is homogeneous
//
//  Parameter:  pFormat     -- The type info string
//              fc          -- FC_FLOAT or FC_DOUBLE
//
//  Returns:    true if the type is a homogeneous layout of the specified
//              FC_FLOAT or FC_DOUBLE
//
//  Notes:      Base types are homogeneous.  Structs are homogeneous if each
//              member is homogeneous.
//
//----------------------------------------------------------------------------
#if defined(__RPC_WIN64__)
bool CProcGen::IsHomogeneousMemberLayout(PFORMAT_STRING pFormat, FORMAT_CHARACTER fc)
{
    while (*pFormat != FC_END)
    {
        switch (*pFormat)
        {           
        case FC_FLOAT:
        case FC_DOUBLE:
            if ( *pFormat != fc )
                return false;

            ++pFormat;
            break;

        case FC_EMBEDDED_COMPLEX:
            pFormat += 2;
            if ( !IsHomogeneous( pFormat + *(short UNALIGNED  *)pFormat, fc ) )
                return false;

            pFormat += 2;
            break;

        case FC_PAD:
            pFormat += 1;
            break;

        default:
            return false;
        }
    }

    return true;
}
#endif __RPC_WIN64__



//+---------------------------------------------------------------------------
//
//  Method:     CProcGen::AnalyzeFloatTypes
//
//  Synopsis:   Analyse a type to see if it is a homogeneous floating point
//              type and adjust the floating point mask accordingly.
//
//  Parameter:  ParamOffset -- Offset of the param from the stack top
//              offset      -- The offset into the type string of the type
//
//----------------------------------------------------------------------------
#if defined(__RPC_WIN64__)
void CProcGen::AnalyzeFloatTypes(USHORT ParamOffset, USHORT offset)
{
    enum FloatType
    {
        NonFloat    = 0,
        Single      = 1,
        Double      = 2,
        DualSingle  = 3
    }
    type;

    PFORMAT_STRING pFormat = _pTypeGen->GetFormatString() + offset;

    bool issingle = IsHomogeneous(pFormat, FC_FLOAT);
    bool isdouble = IsHomogeneous(pFormat, FC_DOUBLE);

    if ( issingle || isdouble )
    {
        ULONG   paramSlot = ParamOffset /= sizeof(REGISTER_TYPE);
        long    members;

        if ( FC_FLOAT == *pFormat || FC_DOUBLE == *pFormat )
            members = 1;
        else
            members = _pTypeGen->GetStructSize() / ( isdouble ? 8 : 4 );
        
        while (members > 0 && _usFloatSlots < 8 && paramSlot < 8)
        {
            if ( isdouble )
                type = Double;
            else if ( members > 1 && _usFloatSlots < 7 )
                type = DualSingle;
            else
                type = Single;
            
            _usFloatArgMask |= type << (paramSlot * 2);

            paramSlot     += 1;
            members       -= 1 + (DualSingle == type);
            _usFloatSlots += 1 + (DualSingle == type);
        }
    }
}
#endif // __RPC_WIN64__


HRESULT CProcGen::PushByte(
    IN  byte b)
{
    BYTE *pb = (BYTE *) &_pProcFormatString[_offset];

    *pb = b;

    _offset += sizeof(b);

    return S_OK;
}

HRESULT CProcGen::PushShort(
    IN  USHORT s)
{
    short UNALIGNED *ps = (UNALIGNED short*)&_pProcFormatString[_offset];

    *ps = s;

    _offset += sizeof(s);

    return S_OK;
}

HRESULT CProcGen::PushLong(
    IN  ULONG s)
{
    long UNALIGNED *ps = (UNALIGNED long*)&_pProcFormatString[_offset];

    *ps = s;

    _offset += sizeof(s);

    return S_OK;
}


HRESULT CProcGen::SetShort(
    IN  USHORT offset,
    IN  USHORT data)
{
    if (offset >=  _offset)
        return E_INVALIDARG;

    *((UNALIGNED short *) &_pProcFormatString[offset]) = data;
    return S_OK;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::CTypeGen
//
//  Synopsis:   Constructor for type generator.
//
//----------------------------------------------------------------------------
CTypeGen::CTypeGen()
{
    Init();
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::Init
//
//  Synopsis:   Initialize the type generator.
//
//----------------------------------------------------------------------------
void CTypeGen::Init()
{
    _pTypeFormat = __MIDL_TypeFormatString.Format;

    _cbTypeFormat = TYPE_FORMAT_STRING_SIZE;

    //The _offset must be aligned on a 4 byte boundary.
    //Note that this may result in _offset > _cbTypeFormat.
    _offset = (TYPE_FORMAT_STRING_SIZE + 3) & ~3;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::~CTypeGen
//
//  Synopsis:   Destructor for type generator.
//
//----------------------------------------------------------------------------
CTypeGen::~CTypeGen()
{
    ReleaseTypeFormatString(_pTypeFormat);
}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::GetTypeFormatString
//
//  Synopsis:   Get the MIDL_TYPE_FORMAT_STRING.
//
//  Arguments:  ppTypeFormatString - Returns a pointer to the type format
//                                   string.
//
//              pLength - Returns the length of the format string.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::GetTypeFormatString(
    OUT PFORMAT_STRING * ppTypeFormatString,
    OUT USHORT         * pLength)
{
    HRESULT hr = S_OK;

    *ppTypeFormatString = _pTypeFormat;

    if(_offset < _cbTypeFormat)
    {
        *pLength = _offset;
    }
    else
    {
        *pLength = _cbTypeFormat;
    }

    //Clear the type format string.
    Init();

    return hr;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::UpdateStructInfo
//
//  Synopsis:   Update the memory information of the specified vt type. 
//
//  Arguments:  uPackingLevel: packing level specified by tlb. Same as /Zp setting in MIDL
//                  packing level is the maximum alignment  in memory. It doesn't affect wire format but
//                  it could force a structure to be bogus.
//
//
//----------------------------------------------------------------------------
void
CTypeGen::UpdateStructInfo( MemoryInfo *pStructInfo, 
                                                          VARTYPE vtnoref,
                                                          long IsRef,
                                                          ushort uPackingLevel )
{
    MemoryInfo const*pInfo;
    if ( IsRef )
        pInfo = &VarMemInfo[VT_PTR];
    else
        pInfo = &VarMemInfo[vtnoref];

    pStructInfo->MemorySize = pInfo->MemorySize;
    if ( pInfo->MemoryAlignment > uPackingLevel )
        pStructInfo->MemoryAlignment = uPackingLevel;
    else
        pStructInfo->MemoryAlignment = pInfo->MemoryAlignment;
    pStructInfo->WireAlignment = pInfo->WireAlignment;
    pStructInfo->WireSize = pInfo->WireSize;    
}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::RegisterType
//
//  Synopsis:   Registers a top-level type in the type format string.
//
//  Arguments:  pTypeDesc - Supplies the type descriptor.
//              pOffset - Returns the offset in the type format string.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::RegisterType(
    IN  PARAMINFO * parainfo,
    OUT USHORT    * pOffset,
    OUT MemoryInfo     * pStructInfo)
{
    HRESULT hr = S_OK;
    VARTYPE vt = parainfo->vt;
    VARTYPE vtnoref = vt & (~VT_BYREF);
    long IsRef = (vt & VT_BYREF) ? 1 : 0;
    
    IID *piid = &(parainfo->iid);
    USHORT uPackingLevel = parainfo->cbAlignment;

    // we don't support vector; vt_array is safearray, treated separately
    if ( vt & (VT_ARRAY | VT_VECTOR | VT_RESERVED ) )
    {
        if(vt & VT_ARRAY)
        {
            hr = RegisterSafeArray(parainfo, pOffset);
        }
        else
        {
            hr = DISP_E_BADVARTYPE;
        }
        vtnoref = VT_SAFEARRAY;
        if ( SUCCEEDED(hr ) )
            {
            // safearray is pointer.
            UpdateStructInfo( pStructInfo, VT_SAFEARRAY,  FALSE , uPackingLevel );
            }
        return hr;
     }

    // it's at the end and we can't folk this into the offset array.
    if ( vt == VT_MULTIINDIRECTIONS )
        {
        // realvt is already VT_BYREF | something.
        // in multiple indirections case, we generate multiple
        // FC_UP FC_POINTER_DEREF. It'll be fixed up if it's
        // top level parameter.
        parainfo->vt = parainfo->realvt;
        ASSERT(parainfo->vt != VT_MULTIINDIRECTIONS);
        hr = RegisterType( parainfo,pOffset, pStructInfo );
        parainfo->vt = VT_MULTIINDIRECTIONS;
        vtnoref = parainfo->realvt;

        if (SUCCEEDED(hr))
            {
            USHORT tmpOffset = *pOffset;
            USHORT prevOffset;
            ASSERT(parainfo->lLevelCount > 0);

            for (LONG i = 0; i < parainfo->lLevelCount; i++ )
                {
                prevOffset = _offset;
                PushByte(FC_UP);
                PushByte(FC_POINTER_DEREF);
                PushOffset(tmpOffset);
                tmpOffset = prevOffset;
                }
            *pOffset = prevOffset;
            //  this is multiple indirection case, it's a pointer.
            UpdateStructInfo( pStructInfo, VT_PTR, TRUE, uPackingLevel );
            }
        }
    else
        {
        *pOffset = OffsetArray[vtnoref][IsRef];   
        UpdateStructInfo( pStructInfo, vtnoref, IsRef, uPackingLevel );

        // take care of special cases and invalid cases.
        if ( *pOffset == 0xffff )
            {
            switch ( vtnoref )
                {
                case VT_INTERFACE:
                    hr = RegisterInterfacePointer(parainfo, pOffset);
                    break;

                case VT_USERDEFINED:
                    hr = RegisterUDT(parainfo, pOffset, pStructInfo);
                    break;

                case VT_I8:
                case VT_UI8:
                case VT_CY:
                    if ( IsRef )
                    {
                        *pOffset = _offset;
                        PushByte(FC_UP);
                        PushByte(FC_SIMPLE_POINTER);
                        PushByte(FC_HYPER);
                        PushByte(FC_PAD);
                    }
                    break;

                case VT_CARRAY:
                    hr = RegisterCArray(parainfo,pOffset,pStructInfo);
                    break;
                default:
                    NDR_ASSERT(0, "invalid vartype");
                    hr = DISP_E_BADVARTYPE;
                }
            }        
        }

    return hr;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::RegisterInterfacePointer
//
//  Synopsis:   Register an interface pointer in the type format string.
//
//  Arguments:  riid - Supplies the IID of the interface.
//              pOffset - Returns the type offset of the interface pointer.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::RegisterInterfacePointer(
    IN  PARAMINFO* parainfo,
    OUT USHORT    *pOffset)
{
    HRESULT hr = E_FAIL;
    USHORT  offset;
    VARTYPE vt = parainfo->vt;
    IID* piid = &(parainfo->iid);

    offset = _offset;
    hr = PushByte(FC_IP);
    if(FAILED(hr))
        return hr;

    hr = PushByte(FC_CONSTANT_IID);
    if(FAILED(hr))
        return hr;

    hr = PushIID(*piid);
    if(FAILED(hr))
        return hr;

    if(vt & VT_BYREF)
    {
        *pOffset = _offset;
        hr = PushByte(FC_RP);
        if(FAILED(hr))
            return hr;

        hr = PushByte(FC_POINTER_DEREF);
        if(FAILED(hr))
            return hr;

        hr = PushOffset(offset);
    }
    else
    {
        *pOffset = offset;
    }
    return hr;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::RegisterCArray
//
//  Synopsis:   generate format string for a fixed size array. 
//
//  Arguments:  pTypeDesc - Supplies the type descriptor.
//              pOffset - Returns the offset in the type format string.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::RegisterCArray(
    IN  PARAMINFO* parainfo,
    OUT USHORT   *pOffset,
    OUT MemoryInfo *pStructInfo)
{
    HRESULT hr = S_OK;
    ARRAYDESC *padesc = parainfo->pArray;
    VARTYPE vt = padesc->tdescElem.vt;
    ULONG ulCount=1;
    USHORT WireAlignment = 0,ussize = 0,offset;
    BYTE fcElem,fcStruct;
    PARAMINFO iteminfo;
    USHORT uPackingLevel = parainfo->cbAlignment;
    MemoryInfo MemInfo;

    for ( int i = 0; i < padesc->cDims; i++)
        ulCount *= padesc->rgbounds[i].cElements ;

    memset( &MemInfo, 0, sizeof( MemoryInfo ) );
    if ( vt > VT_VERSIONED_STREAM )
        {
        NDR_ASSERT(0, "invalid c-array type");
        hr = DISP_E_BADVARTYPE;
        goto Error;
        }

    
    fcElem = VT_FC_MAP[vt];
    MemInfo = VarMemInfo[vt];
    // for simple types, wire size is the same as memory size;
    // for complex data, it's bogus array and the value here is irrelavant.

    // take care of special case here.
    if ( fcElem == FC_ZERO )
        {
        switch (vt)
            {
            case VT_USERDEFINED:
            case VT_INTERFACE:
            case VT_PTR:
            case VT_CARRAY:
            // we need some more information other than vt type.
                {
                ITypeInfo* pTempInfo = parainfo->pTypeInfo;
                fcElem = FC_EMBEDDED_COMPLEX;
                hr = VarVtOfTypeDesc(parainfo->pTypeInfo,&(padesc->tdescElem),&iteminfo);
                if (SUCCEEDED(hr))
                {
                    hr = RegisterType(&iteminfo,&offset,&MemInfo);
                     // it's a bogus array if wire size is diffrent from memorysize
                }
                if (FAILED(hr))
                    goto Error;
                }
                if ( offset == 0 )
                    {
                    fcElem = VT_FC_MAP[iteminfo.vt];
                    }
                break;
            default:
                hr = DISP_E_BADVARTYPE;
                goto Error;
            }
        }
    else
        if ( fcElem == FC_EMBEDDED_COMPLEX )
            {
            iteminfo.vt = vt;
            hr = RegisterType(&iteminfo,&offset, &MemInfo);
            if (FAILED(hr))
                goto Error;
            }

    ussize = MemInfo.MemorySize;
    WireAlignment = MemInfo.WireAlignment;
    *pOffset = _offset;
    // simple array. ulCount is the total size(same for wire & mem)
    if (fcElem != FC_EMBEDDED_COMPLEX)
    {
        ulCount *= ussize;
        if (ulCount <= _UI16_MAX)
        {
            PushByte(FC_SMFARRAY);
            PushByte((BYTE)WireAlignment);
            PushShort((SHORT)ulCount);
            PushByte(fcElem);
        }
        else
        {
            PushByte(FC_LGFARRAY);
            PushByte((BYTE)WireAlignment);
            PushLong(ulCount);
            PushByte(fcElem);
        }
    }
    else
    {
        hr = GetByte(offset,&fcStruct);
        if (FAILED(hr))
            goto Error;
        switch (fcStruct)
        {
        case FC_STRUCT:
            ulCount *= ussize;
            if ( ulCount <= _UI16_MAX )
                {
                PushByte(FC_SMFARRAY);
                PushByte((BYTE)WireAlignment);
                PushShort((SHORT)ulCount);     // total size
                PushByte(FC_EMBEDDED_COMPLEX);
                PushByte(0);
                PushOffset(offset);
                PushByte(FC_PAD);
                }
            else
                {
                PushByte(FC_LGFARRAY);
                PushByte((BYTE)WireAlignment);
                PushLong(ulCount);     // total size
                PushByte(FC_EMBEDDED_COMPLEX);
                PushByte(0);
                PushOffset(offset);
                PushByte(FC_PAD);
                }
            break;

        // FC_EMBEDDED_COMPLEX-FC_UP within a complex array
        // should be the same as FC_BOGUSY_ARRAY-FC_UP directly
        case FC_UP:
        case FC_RP:
            {
            byte fctmp,bflag;
            USHORT tmpoffset;

            PushByte(FC_BOGUS_ARRAY);
            PushByte((BYTE)WireAlignment);
            PushShort((SHORT)ulCount); // this is the count
            PushLong(0xffffffff);   // no conformance description
#if defined(__RPC_WIN64__)
            PushShort(0);           // 6 bytes description in /robust
#endif
            PushLong(0xffffffff);   // no variance description
#if defined(__RPC_WIN64__)
            PushShort(0);
#endif
            GetByte(offset, &fctmp);
            PushByte(fctmp);
            GetByte(offset+1, &bflag);
            PushByte(bflag);
            GetOffset(offset+2, &tmpoffset);
            PushOffset(tmpoffset);
            PushByte(FC_PAD);
            ulCount *= PTR_MEM_SIZE;
            break;
            }

        case FC_BOGUS_STRUCT:
        case FC_USER_MARSHAL:
        case FC_IP:
            PushByte(FC_BOGUS_ARRAY);
            PushByte((BYTE)WireAlignment);
            PushShort((SHORT)ulCount); // this is the count
            PushLong(0xffffffff);   // no conformance description
#if defined(__RPC_WIN64__)
            PushShort(0);
#endif
            PushLong(0xffffffff);   // no variance description
#if defined(__RPC_WIN64__)
            PushShort(0);       // correlation description
#endif
            PushByte(FC_EMBEDDED_COMPLEX);
            PushByte(0);        // the first element
            PushOffset(offset);
            PushByte(FC_PAD);
            ulCount *= ussize;
            break;
        default:
            hr = DISP_E_BADVARTYPE;
        }

    }
    PushByte(FC_END);

    if (parainfo->vt & VT_BYREF)
    {
        USHORT uTemp = _offset;
        PushByte(FC_UP);
        PushByte(0);
        PushOffset(*pOffset);
        *pOffset = uTemp;
        UpdateStructInfo(pStructInfo, vt, TRUE, uPackingLevel);  // isbyref
    }
    else
    {
        UpdateStructInfo( pStructInfo, vt, FALSE,  uPackingLevel);   // not byref
        // special case for larger array. make sure structures
        // embedding a big array would be rejected
        if (ulCount > _UI16_MAX)
            ulCount = _UI16_MAX;
        pStructInfo->MemorySize = (USHORT)ulCount;
        pStructInfo->WireSize = (USHORT)ulCount;
    }

Error:
    return hr;
}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::RegisterUDT
//
//  Synopsis:   Register a user defined type in the type format string.
//
//  Arguments:  pParamInfo  - Supplies the user defined type.
//              pOffset     - Returns the type offset of the struct.
//              pStructInfo - HIWORD is alignment, LOWORD is size.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::RegisterUDT(
    IN  PARAMINFO * pParamInfo,
    OUT USHORT    * pOffset,
    OUT MemoryInfo     * pStructInfo)
{
    HRESULT     hr;
    TYPEATTR  * pTypeAttr;
    PARAMINFO   paramInfo;
    ITypeInfo * pTypeInfo = pParamInfo->pTypeInfo;

    *pOffset = 0;

    hr = pTypeInfo->lpVtbl->GetTypeAttr(pTypeInfo,
                                        &pTypeAttr);
    if(SUCCEEDED(hr))
    {
        pParamInfo->cbAlignment = pTypeAttr->cbAlignment -1;

        switch(pTypeAttr->typekind)
        {
        case TKIND_RECORD:
            hr = RegisterStruct(pParamInfo, pOffset, pStructInfo);
            NDR_ASSERT( pStructInfo->MemorySize != 0, "struct size can't be 0");
            break;

        case TKIND_ALIAS:
            hr = VarVtOfTypeDesc(pTypeInfo, &pTypeAttr->tdescAlias, &paramInfo);
            if(SUCCEEDED(hr))
            {
                hr = RegisterType(&paramInfo, pOffset, pStructInfo);
                NDR_ASSERT( pStructInfo->MemorySize != 0, "data size can't be 0");
            }
            if (FAILED(hr))
                break;
            if (0 == *pOffset)
            // this is an aliases to a simple type. We should just pass the vt type back
            {
                pParamInfo->vt = paramInfo.vt;
            }
            break;

        case TKIND_DISPATCH:
        case TKIND_INTERFACE:
            hr = VarVtOfIface(pTypeInfo, pTypeAttr, &paramInfo);
            if(SUCCEEDED(hr))
            {
                hr = RegisterType(&paramInfo, pOffset, pStructInfo);
            }
            break;

        case TKIND_ENUM:
            pParamInfo->vt = VT_I4;
            *pOffset = 0;
            UpdateStructInfo( pStructInfo, VT_I4, FALSE , pParamInfo->cbAlignment);
            break;

        case TKIND_MODULE:
        case TKIND_COCLASS:
        case TKIND_UNION:
        default:
            hr = DISP_E_BADVARTYPE;
            break;
        }
    }
    pTypeInfo->lpVtbl->ReleaseTypeAttr(pTypeInfo,pTypeAttr);
    return hr;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::RegisterStruct
//
//  Synopsis:   Register an user defined struct in the type format string.
//
//  Arguments:  riid - Supplies the ITypeInfo for that struct.
//              pOffset - Returns the type offset of the struct.
//              pStructInfo: HIWORD is alignment, LOWORD is size.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::RegisterStruct(
    IN PARAMINFO * parainfo,
    OUT USHORT   * pOffset,
    OUT MemoryInfo    * pStructInfo)
{
    HRESULT hr = S_OK;
    TYPEATTR *pTypeAttr;
    VARDESC  **ppVarDesc;
    ITypeInfo* pInfo = parainfo->pTypeInfo;
    VARTYPE vt = parainfo->vt;
    FORMAT_CHARACTER fcStruct = FC_STRUCT;
    BYTE fcTemp;
    USHORT *poffsets;
    MemoryInfo *pFieldInfos;
    USHORT uPacklingLevel;
    int i = 0;

    hr = pInfo->lpVtbl->GetTypeAttr(pInfo,&pTypeAttr);
    if (FAILED(hr))
        return hr;

    *pOffset = 0;

    uPacklingLevel = parainfo->cbAlignment;
    poffsets = (USHORT *)alloca(pTypeAttr->cVars * sizeof(USHORT));
    memset(poffsets,0,pTypeAttr->cVars * sizeof(USHORT));
    pFieldInfos = (MemoryInfo *)alloca(pTypeAttr->cVars * sizeof(MemoryInfo));
    memset(pFieldInfos,0,pTypeAttr->cVars * sizeof(MemoryInfo));
    ppVarDesc = (VARDESC **)alloca(pTypeAttr->cVars * sizeof(VARDESC *));
    memset(ppVarDesc,0,pTypeAttr->cVars * sizeof(VARDESC *));


    for(i = 0; SUCCEEDED(hr) && i < pTypeAttr->cVars; i++)
    {
        hr = pInfo->lpVtbl->GetVarDesc(pInfo,i, &ppVarDesc[i]);
        if(SUCCEEDED(hr))
        {
            VARKIND varkind = ppVarDesc[i]->varkind;
            PARAMINFO iteminfo;
            iteminfo.cbAlignment = pTypeAttr->cbAlignment - 1;
            BOOL IsRef;  

            switch (varkind)
            {
                case VAR_PERINSTANCE:
                    iteminfo.wIDLFlags = parainfo->wIDLFlags;
                    iteminfo.vt = ppVarDesc[i]->elemdescVar.tdesc.vt;
                    IsRef = iteminfo.vt & VT_BYREF;

                    switch (iteminfo.vt  & ~VT_BYREF)
                    {
                    case VT_USERDEFINED:
                        ITypeInfo *pTempTI;
                        hr = pInfo->lpVtbl->GetRefTypeInfo(pInfo,ppVarDesc[i]->elemdescVar.tdesc.hreftype, &pTempTI);
                        if (FAILED(hr))
                            goto Error;
                        iteminfo.pTypeInfo = pTempTI;
                        hr = RegisterUDT(&iteminfo,&poffsets[i],&pFieldInfos[i]);
                        if (FAILED(hr))
                            goto Error;

                        if (0 == poffsets[i])
                        // the UDT in fact is a simple type (alias or TKIND_ENUM). pass the type back
                        // this is a hack. If the member is typedef->typedef<>...tkind_enum, it's effectively a 
                        // simple type, and we bring the real type up here to be used in Parse & PushStruct.
                        {
                            ppVarDesc[i]->elemdescVar.tdesc.vt = iteminfo.vt;
                            UpdateStructInfo( &pFieldInfos[i], iteminfo.vt,  IsRef, uPacklingLevel);
                        }
                        break;

                    // special case: the top level parameter case is treated differently
                    case VT_DECIMAL:
                        if ( IsRef )
                        {
                            poffsets[i] = BYREF_DECIMAL_TYPE_FS_OFFSET ;
                        }                            
                        else
                        {
                            poffsets[i] = DECIMAL_TYPE_FS_OFFSET;
                        }
                        UpdateStructInfo(&pFieldInfos[i], VT_DECIMAL, IsRef, uPacklingLevel);
                        break;
                     case VT_LPSTR:
                        if (IsRef)
                        {
                            poffsets[i] = BYREF_LPSTR_TYPE_FS_OFFSET ;
                        }
                        else
                        {
                            poffsets[i] = EMBEDDED_LPSTR_TYPE_FS_OFFSET ;
                        }
                        UpdateStructInfo(&pFieldInfos[i], VT_LPSTR, IsRef, uPacklingLevel);
                        break;
                     case VT_LPWSTR:
                        if ( IsRef )
                        {
                            poffsets[i] = BYREF_LPWSTR_TYPE_FS_OFFSET ;
                         }
                        else
                        {
                            poffsets[i] = EMBEDDED_LPWSTR_TYPE_FS_OFFSET ;
                        }
                        UpdateStructInfo(&pFieldInfos[i], VT_LPWSTR, IsRef, uPacklingLevel);
                        break;

                    // doesn't need special case
                    case VT_FILETIME:
                    //  all the following are user marshalls.

                     case VT_DISPATCH:
                     case VT_UNKNOWN:
                     case VT_INTERFACE:
                     case VT_STREAM:
                     case VT_STORAGE:
                     case VT_BSTR:
                     case VT_VARIANT:
                        hr = RegisterType(&iteminfo,&(poffsets[i]),&(pFieldInfos[i]));
                        if (FAILED(hr))
                            goto Error;
                        break;


                     case VT_PTR:
                     case VT_SAFEARRAY:     // 13129
                     {
                        PARAMINFO ptrInfo;
                        hr = VarVtOfTypeDesc(pInfo,&(ppVarDesc[i]->elemdescVar.tdesc),&ptrInfo);
                        if (SUCCEEDED(hr))
                            hr = RegisterType(&ptrInfo,&(poffsets[i]),&(pFieldInfos[i]));
                        if (FAILED(hr))
                            goto Error;
                        // these are really pointers.
                        UpdateStructInfo(&pFieldInfos[i], iteminfo.vt, IsRef, uPacklingLevel);
                     }
                        break;


                     case VT_CARRAY:
                        iteminfo.pArray = ppVarDesc[i]->elemdescVar.tdesc.lpadesc;
                        iteminfo.pTypeInfo = parainfo->pTypeInfo;
                        iteminfo.pTypeInfo->lpVtbl->AddRef(iteminfo.pTypeInfo);
                        iteminfo.vt = ppVarDesc[i]->elemdescVar.tdesc.vt;
                        hr = RegisterCArray(&iteminfo,&poffsets[i],&pFieldInfos[i]);
                        if (FAILED(hr))
                            goto Error;
                        break;

                    }

                    // this member is not a simple type.
                    // we could have some optimization here but for now we just 
                    // generate complex struct.
                    if (poffsets[i])
                    {
                        hr = GetByte(poffsets[i],&fcTemp);
                        if (FAILED(hr))
                            return hr;

                        switch (fcTemp)
                        {
                        case FC_PSTRUCT:
                        case FC_CSTRUCT:
                        case FC_CVSTRUCT:
                            hr = DISP_E_BADVARTYPE;
                            break;
                        case FC_STRUCT:
                        case FC_BOGUS_STRUCT:
                        case FC_USER_MARSHAL:
                        case FC_LGFARRAY:
                        case FC_SMFARRAY:
                        case FC_BOGUS_ARRAY:
                        case FC_IP:
                        case FC_UP:
                        case FC_OP:
                        case FC_RP:
                            fcStruct = FC_BOGUS_STRUCT;
                            break;
                        default:    
                            NDR_ASSERT(0, "invalid struct type");
                            fcStruct = FC_BOGUS_STRUCT;
                        break;
                        }
                    }
                break;

                default:    // all other types shouldn't happen .

                    hr = DISP_E_BADVARTYPE;
                break;
            }
        }
        else
            goto Error;
    }
    if (FAILED(hr))
        goto Error;

    hr = PushStruct(parainfo,fcStruct,ppVarDesc,poffsets,pFieldInfos,pTypeAttr->cVars,pOffset,pStructInfo);

Error:
    for (int j = 0; j < pTypeAttr->cVars ; j++)
        if (ppVarDesc[j])
            pInfo->lpVtbl->ReleaseVarDesc(pInfo,ppVarDesc[j]);

    pInfo->lpVtbl->ReleaseTypeAttr(pInfo,pTypeAttr);
    return hr;
}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::PushStruct
//
//  Synopsis:   This should be part of RegisterStruct, seperate them just because
//              the function is too long.
//
//  Arguments:  parainfo - parameter information.
//              fcStruct - type of struct.
//              ppVarDesc - variable description, if applicable.
//              poffsets  - offset of embedded complex members.
//              pdwStructInfo - size/pad of embedded complex members.
//              uNumElement     - number of members in the struct.
//              pOffset - Returns the type offset of the struct.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::PushStruct(
    IN PARAMINFO *parainfo,
    IN FORMAT_CHARACTER fcStruct,
    IN VARDESC **ppVarDesc,
    IN USHORT *poffsets,
    IN MemoryInfo * pFieldInfos,
    IN USHORT uNumElements,
    OUT USHORT *pOffset,
    OUT MemoryInfo *pStructInfo)
{
    HRESULT hr = S_OK;
    USHORT  uStartoffset;
    USHORT uMemorySize = 0,maxMemoryAlignment=0;
    USHORT FieldMemoryAlignment = 0;
    USHORT structpad;
    int i;
    VARTYPE vt = parainfo->vt;
    boolean fHasPointer = FALSE;
    USHORT uPackingLevel = parainfo->cbAlignment;

    _uStructSize = 0;

    // sizing pass, find out the size of the struct and check if we need to convert struct into bogus struct.
    hr = ParseStructMembers(parainfo,&fcStruct,ppVarDesc,poffsets,pFieldInfos,uNumElements,pStructInfo);

    if (FAILED(hr))
        return hr;

    uStartoffset = _offset;   // the starting point of this struct.
    _uStructSize = 0;

    PushByte(fcStruct);
    PushByte((BYTE)pStructInfo->WireAlignment);
    PushShort(pStructInfo->MemorySize);

    switch (fcStruct)
    {
    case FC_STRUCT:
    break;

    case FC_CSTRUCT:
    case FC_PSTRUCT:
    case FC_CVSTRUCT:
        hr = E_NOTIMPL;
    break;

    case FC_BOGUS_STRUCT:
        PushShort(0);   // offset to array. E_NOTIMPLE;
        PushShort(0);   // offset to pointer layout.
    break;

    default:
    hr = DISP_E_BADVARTYPE;
    break;

    }
    if (FAILED(hr))
        return hr;

    for (i = 0; i < uNumElements; i++)
    {
        if (poffsets[i] > 0)
        {
            // The struct member is an embedded complex, the descsriptor of which
            // is already generated.
            NDR_ASSERT( pFieldInfos[i].MemorySize > 0, "MemoryInfo should have been initialized");
            USHORT uPrevSize;
            BYTE fcTemp;

            // we need to find the offset to know if we have pointers. 
            // we could do that through the typelib directly though.
            hr = GetByte(poffsets[i],&fcTemp);
            if (FAILED(hr))
                return hr;

            if ( pFieldInfos[i].MemoryAlignment > uPackingLevel )
                FieldMemoryAlignment = uPackingLevel;
            else
                FieldMemoryAlignment = pFieldInfos[i].MemoryAlignment;
            uMemorySize = pFieldInfos[i].MemorySize;

            if (fcTemp == FC_UP || fcTemp == FC_RP || fcTemp == FC_OP )
            {
                FieldMemoryAlignment = Alignment( PTR_MEM_ALIGN, uPackingLevel );
                PushByte(FC_POINTER);
                fHasPointer = TRUE;
                _uStructSize += PTR_MEM_SIZE;
                NDR_ASSERT( uMemorySize == PTR_MEM_SIZE, "invalid pointer size");
            }
            else
            {
                PushByte(FC_EMBEDDED_COMPLEX);
                // push the padding required the previous field and
                // following FC_EMBEDDED_COMPLEX
                uPrevSize = (SHORT )_uStructSize;
                LENGTH_ALIGN( _uStructSize, FieldMemoryAlignment);
                PushByte( (BYTE)( _uStructSize - uPrevSize ) );
                PushOffset(poffsets[i]);
                _uStructSize += uMemorySize;  // size of the struct
            }
        }
        else
        {
            hr = GenStructSimpleTypesFormatString(parainfo,ppVarDesc[i],&FieldMemoryAlignment);
            if (FAILED(hr))
                return hr;
        }
        if ( FieldMemoryAlignment > maxMemoryAlignment  )
            maxMemoryAlignment = FieldMemoryAlignment;

    }
    structpad = (USHORT)_uStructSize & maxMemoryAlignment;
    if (structpad )
    {
        structpad = maxMemoryAlignment - structpad;
        hr = PushByte((BYTE)FC_STRUCTPAD1 + structpad) ;
        if (FAILED(hr))
            return hr;
    }

    if (!((_offset - uStartoffset) & 1))
    {
        hr = PushByte(FC_PAD);
        if (FAILED(hr))
            return hr;
    }

    PushByte(FC_END);

    // one level of indirection if it's a pointer
    if (fHasPointer)
    {
        USHORT tempOffset, tempAddr;
        BYTE fcTemp,fcType;
        tempOffset = uStartoffset + 2*sizeof(BYTE) + 2*sizeof(SHORT);

        SetShort(tempOffset,_offset-tempOffset);
        for (i = 0 ; i < uNumElements; i++)
        {
            if (poffsets[i] > 0)
            {
                hr = GetByte(poffsets[i],&fcTemp);
                hr = GetByte(poffsets[i] + 1, &fcType);
                if (FAILED(hr))
                    return hr;
                if (fcTemp == FC_UP || fcTemp == FC_RP || fcTemp == FC_OP )
                {
                   // should really be FC_OP here!!!
                   // MIDL generate FC_OP for [out] and [in,out] param.
                   // We can always generate FC_OP here: the engine behave differently
                   // only on unmarshaling in client side, and force no buffer reuse on
                   // server side, but even we set the flag on [in] only parameter,
                   // unmarshall routine is not called on the client side so it doesn't matter.
                   PushByte(FC_UP);
                   PushByte(fcType);
                   if (FC_SIMPLE_POINTER == fcType)
                   {
                       GetByte(poffsets[i] + 2 * sizeof(BYTE), &fcTemp);
                       PushByte(fcTemp);
                       PushByte(FC_PAD);
                   }
                   else
                   {
                       GetOffset(poffsets[i] + 2*sizeof(BYTE) , &tempAddr);
                       PushOffset(tempAddr);
                   }
                }
            }
        }
    }
    if (vt & VT_BYREF)
    {
        *pOffset = _offset;
        PushByte(FC_UP);
        PushByte(0);
        PushOffset(uStartoffset);
    }
    else
        *pOffset = uStartoffset;

    if (_offset & 2)    // align the starting point of next struct to 4 byte boundary.
        PushShort(0);

    return hr;

}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::ParseStructMembers
//
//  Synopsis:   basically sizing pass of the struct, check if we need to convert
//              struct into bogus struct when we can't memcpy the buffer.
//
//  Arguments:  parainfo - parameter information.
//              pfcStruct - type of struct.
//              ppVarDesc - variable description, if applicable.
//              poffsets  - offset of embedded complex members.
//              pdwStructInfo - size/pad of embedded complex members.
//              uNumElement     - number of members in the struct.
//              pStructInfo - return the struct info.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::ParseStructMembers(
    IN PARAMINFO *parainfo,
    IN OUT FORMAT_CHARACTER *pfcStruct,
    IN VARDESC **ppVarDesc,
    IN USHORT *poffsets,
    IN MemoryInfo *pFieldInfos,
    IN USHORT uNumElements,
    OUT MemoryInfo *pStructInfo)
{
    USHORT uStartoffset = 0;
    BOOL fChangeToBogus = FALSE;
    int i;
    USHORT FieldMemoryAlignment = 0, MaxWireAlignment = 0, MaxMemoryAlignment = 0;
    HRESULT hr = S_OK ;
    USHORT uPacklingLevel = parainfo->cbAlignment;

    memset( pStructInfo, 0, sizeof( MemoryInfo ) );

    for (i = 0; i < uNumElements; i++)
    {
        // this is simple type. Let's fill in the memory/size info.
        if ( pFieldInfos[i].MemorySize == 0 )
            {
            NDR_ASSERT( poffsets[i] == 0 , "invalid simple type format string offset");
            hr = GetMemoryInfoForSimpleType(ppVarDesc[i]->elemdescVar.tdesc.vt,
                                                      &pFieldInfos[i]);
            if (FAILED(hr))
                return hr;
            }

        NDR_ASSERT( pFieldInfos[i].MemorySize != 0, "invalid field information");
        if ( pFieldInfos[i].MemoryAlignment > uPacklingLevel )
            {
            // need to change to bogus struct if the struct is not naturally aligned.
            FieldMemoryAlignment = uPacklingLevel;
            fChangeToBogus = TRUE;
            }
        else
            FieldMemoryAlignment = pFieldInfos[i].MemoryAlignment;

        // wire alignment is put on the format string only.
        if ( pFieldInfos[i].WireAlignment > MaxWireAlignment )
            MaxWireAlignment = pFieldInfos[i].WireAlignment ;

        if ( FieldMemoryAlignment > MaxMemoryAlignment )
            MaxMemoryAlignment = FieldMemoryAlignment;

        LENGTH_ALIGN( _uStructSize, FieldMemoryAlignment );
            _uStructSize += pFieldInfos[i].MemorySize;  // size of the struct
    }

    if ((USHORT)_uStructSize & MaxMemoryAlignment )
    {
        // it's a bogus struct if we have trailing padding.
        fChangeToBogus = TRUE;
        *pfcStruct = FC_BOGUS_STRUCT;
    }

    LENGTH_ALIGN( _uStructSize, MaxMemoryAlignment );

    // we are returning the real size of struct up. In the type format generating code,
    // embedded pointer size is always PTR_MEM_SIZE and the sizing info returned from here
    // is ignored.
    pStructInfo->MemorySize = (USHORT)_uStructSize;
    pStructInfo->MemoryAlignment = MaxMemoryAlignment;
    pStructInfo->WireAlignment = MaxWireAlignment;
    NDR_ASSERT( _uStructSize != 0, "invalid struct size");

    if (_uStructSize > _UI16_MAX)
    {
        hr = DISP_E_BADVARTYPE;
    }

    return hr;

}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::GetMemoryInfoForSimpleType
//
//  Synopsis:   Fill in the memory and wire size information for simple types
//
//  Arguments:  vt - what's the simple type.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::GetMemoryInfoForSimpleType(VARTYPE vt,
        MemoryInfo *pSimpleTypeInfo)
{
    HRESULT hr = S_OK;
    VARTYPE vtnoref = vt & ~VT_BYREF;
    if ( vt > VT_VERSIONED_STREAM )
        {
        NDR_ASSERT( 0, "invalid vartype" );
        hr = DISP_E_BADVARTYPE;
        }

    *pSimpleTypeInfo = VarMemInfo[vtnoref];
    if ( pSimpleTypeInfo->WireSize == 0 )
        {
        NDR_ASSERT(0, "invalid vartype" );
        hr = DISP_E_BADVARTYPE;
        }
    return hr;
}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::RegisterSafeArray
//
//  Synopsis:   Register a safe array in the type format string.
//
//  Arguments:  riid - Supplies the IID of the interface.
//              pOffset - Returns the type offset of the safe array.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::RegisterSafeArray(
          IN PARAMINFO* pParainfo,
          OUT USHORT    *pOffset)
{
    HRESULT hr = S_OK;
    USHORT  offset;
    VARTYPE vt = pParainfo->vt;
    IID *piid = &(pParainfo->iid);

    switch(vt & ~(VT_ARRAY | VT_BYREF))
    {
    case VT_I2:
    case VT_I4:
    case VT_R4:
    case VT_R8:
    case VT_CY:
    case VT_DATE:
    case VT_BSTR:
    case VT_DISPATCH:
    case VT_ERROR:
    case VT_BOOL:
    case VT_VARIANT:
    case VT_UNKNOWN:
    case VT_DECIMAL:
    case VT_I1:
    case VT_UI1:
    case VT_UI2:
    case VT_UI4:
    case VT_I8:
    case VT_UI8:
    case VT_INT:
    case VT_UINT:
    case VT_USERDEFINED:    // 13129
    case VT_CARRAY:
        //This is actually an LPSAFEARRAY pSafeArray.
        if(vt & VT_BYREF)
        {
            *pOffset = BYREF_SAFEARRAY_TYPE_FS_OFFSET ;
        }
        else
        {
            *pOffset = SAFEARRAY_TYPE_FS_OFFSET ;
        }
        break;

    case VT_INTERFACE:
        offset = _offset;
        hr = PushByte(FC_USER_MARSHAL);
        if(FAILED(hr))
            return hr;

        hr = PushByte(USER_MARSHAL_UNIQUE | USER_MARSHAL_IID | 3);
        if(FAILED(hr))
            return hr;

        hr = PushShort(2);
        if(FAILED(hr))
            return hr;

        hr = PushShort(4);
        if(FAILED(hr))
            return hr;

        hr = PushShort(0);
        if(FAILED(hr))
            return hr;

        // yongqu: this offset doesn't really matter. In NdrpInitUserMarshalCB,
        // pReserve is set to offset+10, pointing to the iid directly. In safearrayunmarshal,
        // the iid is read from usermarshalinfo->pReserve. So this short value is never read
        hr = PushOffset(SAFEARRAY_TYPE_FS_OFFSET); //LPSAFEARRAY * type offset
        if(FAILED(hr))
            return hr;

        hr = PushIID(*piid);
        if(FAILED(hr))
            return hr;

        *pOffset = offset;
        break;

    default:
        hr = DISP_E_BADVARTYPE;
        break;
    }


    return hr;
}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::GenStructFormatString
//
//  Synopsis:   generate type format string for a simple type member of a struct.
//
//  Arguments:  parainfo - parameter information
//                              pVarDesc - variable description.
//              pOffset - Returns the type offset of the safe array.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::GenStructSimpleTypesFormatString(
        IN PARAMINFO *parainfo,
        IN VARDESC *pVarDesc,
        OUT USHORT *TypeMemoryAlignment)
{
    VARTYPE vtnoref = pVarDesc->elemdescVar.tdesc.vt & ~VT_BYREF;
    HRESULT hr = S_OK;
    NDR_ASSERT( vtnoref <= VT_VERSIONED_STREAM, "invalid vt");
    if ( vtnoref > VT_VERSIONED_STREAM )
        return DISP_E_BADVARTYPE;

    BYTE FcElement = VT_FC_MAP[vtnoref];
    *TypeMemoryAlignment = Alignment( VarMemInfo[vtnoref].MemoryAlignment, parainfo->cbAlignment );
    PushByte( FcElement);
    _uStructSize += VarMemInfo[vtnoref].MemorySize;

    NDR_ASSERT( VarMemInfo[vtnoref].WireSize != 0, "invalid simple vartype");
    if ( VarMemInfo[vtnoref].WireSize == 0 )
        hr = DISP_E_BADVARTYPE;

    return hr;

}


//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::AdjustTopLevelRef
//
//  Synopsis:   Adjust the top level multiple level pointer to
//              FC_RP FC_ALLOCED_ON_STACK|FC_POINTER_DEREF
//
//  Arguments:  dwReq - basic alignment requirement of the member.
//              dwMax - the alignment requirement of the struct.
//
//----------------------------------------------------------------------------
HRESULT CTypeGen::AdjustTopLevelRef(USHORT offset)
{
    HRESULT hr;
    hr = SetByte(offset,FC_RP);
    if (SUCCEEDED(hr))
        hr = SetByte(offset+1, FC_ALLOCED_ON_STACK|FC_POINTER_DEREF);
    return hr;
}

//+---------------------------------------------------------------------------
//
//  Method:     CTypeGen::Alignment
//
//  Synopsis:   get the alignment info of a type and push FC_ALIGN when required.
//
//  Arguments:  dwReq - basic alignment requirement of the member.
//              dwMax - the alignment requirement of the struct.
//
//----------------------------------------------------------------------------
USHORT CTypeGen::Alignment(DWORD dwReq,DWORD dwMax)
{
    USHORT TypeAlignment = (USHORT)(dwReq > dwMax ? dwMax : dwReq);

    if (_uStructSize & TypeAlignment)
    {
        LENGTH_ALIGN( _uStructSize, TypeAlignment );
        switch (TypeAlignment)
        {
        case 1:
            PushByte(FC_ALIGNM2);
            break;
        case 3:
            PushByte(FC_ALIGNM4);
            break;
        case 7:
            PushByte(FC_ALIGNM8);
            break;
        }
    }
    return TypeAlignment;
}

HRESULT CTypeGen::GrowTypeFormat(
    USHORT cb)
{
    HRESULT hr = S_OK;

    //Check if we need to grow the type format string.
    if((_offset + cb) >= _cbTypeFormat)
    {
        void  *pTemp;
        USHORT cbTemp;

        cbTemp = _cbTypeFormat * 2;
        pTemp = I_RpcAllocate(cbTemp);
        if(pTemp != NULL)
        {
            //copy the memory
            memcpy(pTemp, _pTypeFormat, _cbTypeFormat);

            //free the old memory
            if(_pTypeFormat != __MIDL_TypeFormatString.Format)
            {
                I_RpcFree((void *) _pTypeFormat);
            }

            _pTypeFormat = (PFORMAT_STRING) pTemp;
            _cbTypeFormat = cbTemp;
        }
        else
        {
            hr = E_OUTOFMEMORY;
        }
    }
    return hr;
}

HRESULT CTypeGen::PushByte(
    IN  byte b)
{
    HRESULT hr;

    hr = GrowTypeFormat(sizeof(b));
    if(SUCCEEDED(hr))
    {
        *((BYTE *) &_pTypeFormat[_offset]) = b;
        _offset += sizeof(b);
    }

    return hr;
}

HRESULT CTypeGen::PushShort(
    IN  USHORT s)
{
    HRESULT hr;

    hr = GrowTypeFormat(sizeof(s));
    if(SUCCEEDED(hr))
    {
        *((UNALIGNED short *) &_pTypeFormat[_offset]) = s;
        _offset += sizeof(s);
    }

    return hr;
}

HRESULT CTypeGen::PushLong(
    IN  ULONG s)
{
    HRESULT hr;

    hr = GrowTypeFormat(sizeof(s));
    if(SUCCEEDED(hr))
    {
        *((UNALIGNED long *) &_pTypeFormat[_offset]) = s;
        _offset += sizeof(s);
    }

    return hr;
}

HRESULT CTypeGen::PushOffset(
    IN  USHORT offset)
{
    HRESULT hr;

    hr = PushShort(offset - _offset);
    return hr;
}


HRESULT CTypeGen::PushIID(
    IN  IID iid)
{
    HRESULT hr;

    hr = GrowTypeFormat(sizeof(IID));
    if(SUCCEEDED(hr))
    {
        memcpy((void *)&_pTypeFormat[_offset], &iid, sizeof(IID));
        _offset += sizeof(IID);
    }

    return hr;
}

HRESULT CTypeGen::SetShort(
    IN  USHORT offset,
    IN  USHORT data)
{
    if (offset >=  _offset)
        return E_INVALIDARG;

    *((UNALIGNED short *) &_pTypeFormat[offset]) = data;
    return S_OK;
}

HRESULT CTypeGen::SetByte(
    IN  USHORT offset,
    IN  BYTE data)
{
    if (offset >=  _offset)
        return E_INVALIDARG;

    *((BYTE *) &_pTypeFormat[offset]) = data;
    return S_OK;
}

HRESULT CTypeGen::GetShort(
    IN  USHORT offset,
    OUT  USHORT* data)
{
    if (offset >=  _offset)
        return E_INVALIDARG;

    *data = *((UNALIGNED short*)&_pTypeFormat[offset]);
    return S_OK;
}

HRESULT CTypeGen::GetOffset(
    IN USHORT addr,
    OUT USHORT* poffset)
{
    USHORT delta;
    HRESULT hr;

    hr = GetShort(addr,&delta);
    if (FAILED(hr))
        return hr;
    *poffset = addr + (SHORT) delta;
//  hr = GetShort(addr + (SHORT)delta,poffset);
    return hr;
}


HRESULT CTypeGen::GetByte(
    IN  USHORT offset,
    OUT  BYTE* data)
{
    if (offset >=  _offset)
        return E_INVALIDARG;

    *data = *((BYTE *) &_pTypeFormat[offset]);
    return S_OK;
}


ULONG __RPC_USER
BSTR_UserSize(ULONG * pFlags, ULONG Offset, BSTR * pBstr)

{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (UserMarshalRoutines[0].pfnBufferSize)(pFlags, Offset, pBstr);
}

BYTE * __RPC_USER
BSTR_UserMarshal (ULONG * pFlags, BYTE * pBuffer, BSTR * pBstr)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (UserMarshalRoutines[0].pfnMarshall)(pFlags, pBuffer, pBstr);
}

BYTE * __RPC_USER
BSTR_UserUnmarshal(ULONG * pFlags, BYTE * pBuffer, BSTR * pBstr)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (UserMarshalRoutines[0].pfnUnmarshall)(pFlags, pBuffer, pBstr);
}

void __RPC_USER
BSTR_UserFree(ULONG * pFlags, BSTR * pBstr)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    (UserMarshalRoutines[0].pfnFree)(pFlags, pBstr);
}

ULONG __RPC_USER
VARIANT_UserSize(ULONG * pFlags, ULONG Offset, VARIANT * pVariant)

{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (UserMarshalRoutines[1].pfnBufferSize)(pFlags, Offset, pVariant);
}

BYTE * __RPC_USER
VARIANT_UserMarshal (ULONG * pFlags, BYTE * pBuffer, VARIANT * pVariant)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (UserMarshalRoutines[1].pfnMarshall)(pFlags, pBuffer, pVariant);
}

BYTE * __RPC_USER
VARIANT_UserUnmarshal(ULONG * pFlags, BYTE * pBuffer, VARIANT * pVariant)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (UserMarshalRoutines[1].pfnUnmarshall)(pFlags, pBuffer, pVariant);
}

void __RPC_USER
VARIANT_UserFree(ULONG * pFlags, VARIANT * pVariant)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    (UserMarshalRoutines[1].pfnFree)(pFlags, pVariant);
}

ULONG __RPC_USER
LPSAFEARRAY_UserSize(ULONG * pFlags, ULONG Offset, LPSAFEARRAY * ppSafeArray)

{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (pfnLPSAFEARRAY_UserSize)(pFlags, Offset, ppSafeArray);
}

BYTE * __RPC_USER
LPSAFEARRAY_UserMarshal (ULONG * pFlags, BYTE * pBuffer, LPSAFEARRAY * ppSafeArray)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (pfnLPSAFEARRAY_UserMarshal)(pFlags, pBuffer, ppSafeArray);
}

BYTE * __RPC_USER
LPSAFEARRAY_UserUnmarshal(ULONG * pFlags, BYTE * pBuffer, LPSAFEARRAY * ppSafeArray)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    return (pfnLPSAFEARRAY_UserUnmarshal)(pFlags, pBuffer, ppSafeArray);
}

void __RPC_USER
LPSAFEARRAY_UserFree(ULONG * pFlags, LPSAFEARRAY * ppSafeArray)
{
    HRESULT hr;

    hr = NdrLoadOleAutomationRoutines();

    if(FAILED(hr))
        RpcRaiseException(hr);

    (UserMarshalRoutines[2].pfnFree)(pFlags, ppSafeArray);
}


ULONG __RPC_USER
LPSAFEARRAY_Size(ULONG * pFlags, ULONG Offset, LPSAFEARRAY * ppSafeArray, const IID *piid)
{
    HINSTANCE h;
    void * pfnTemp;

    //Load oleaut32.dll
    if(0 == hOleAut32)
    {
        h = LoadLibraryA("OLEAUT32");

        if(h != 0)
        {
            hOleAut32 = h;
        }
        else
        {
            RpcRaiseException(HRESULT_FROM_WIN32(GetLastError()));
        }
    }

    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_Size");
    if(pfnTemp != 0)
    {
        pfnLPSAFEARRAY_Size = (PFNSAFEARRAY_SIZE) pfnTemp;
    }
    else
    {
        RpcRaiseException(HRESULT_FROM_WIN32(GetLastError()));
    }

    return (pfnLPSAFEARRAY_Size)(pFlags, Offset, ppSafeArray, piid);
}

BYTE * __RPC_USER
LPSAFEARRAY_Marshal (ULONG * pFlags, BYTE * pBuffer, LPSAFEARRAY * ppSafeArray, const IID *piid)
{
    HINSTANCE h;
    void * pfnTemp;

    //Load oleaut32.dll
    if(0 == hOleAut32)
    {
        h = LoadLibraryA("OLEAUT32");

        if(h != 0)
        {
            hOleAut32 = h;
        }
        else
        {
            RpcRaiseException(HRESULT_FROM_WIN32(GetLastError()));
        }
    }

    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_Marshal");
    if(pfnTemp != 0)
    {
        pfnLPSAFEARRAY_Marshal = (PFNSAFEARRAY_MARSHAL) pfnTemp;
    }
    else
    {
        RpcRaiseException(HRESULT_FROM_WIN32(GetLastError()));
    }

    return (pfnLPSAFEARRAY_Marshal)(pFlags, pBuffer, ppSafeArray, piid);
}

BYTE * __RPC_USER
LPSAFEARRAY_Unmarshal(ULONG * pFlags, BYTE * pBuffer, LPSAFEARRAY * ppSafeArray, const IID *piid)
{
    HINSTANCE h;
    void * pfnTemp;

    //Load oleaut32.dll
    if(0 == hOleAut32)
    {
        h = LoadLibraryA("OLEAUT32");

        if(h != 0)
        {
            hOleAut32 = h;
        }
        else
        {
            RpcRaiseException(HRESULT_FROM_WIN32(GetLastError()));
        }
    }

    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_Unmarshal");
    if(pfnTemp != 0)
    {
        pfnLPSAFEARRAY_Unmarshal = (PFNSAFEARRAY_UNMARSHAL) pfnTemp;
    }
    else
    {
        RpcRaiseException(HRESULT_FROM_WIN32(GetLastError()));
    }
    return (pfnLPSAFEARRAY_Unmarshal)(pFlags, pBuffer, ppSafeArray, piid);
}

PFNSAFEARRAY_SIZE      pfnLPSAFEARRAY_Size      = LPSAFEARRAY_Size;
PFNSAFEARRAY_MARSHAL   pfnLPSAFEARRAY_Marshal   = LPSAFEARRAY_Marshal;
PFNSAFEARRAY_UNMARSHAL pfnLPSAFEARRAY_Unmarshal = LPSAFEARRAY_Unmarshal;

ULONG __RPC_USER
SafeArraySize(ULONG * pFlags, ULONG Offset, LPSAFEARRAY * ppSafeArray)

{
    USER_MARSHAL_CB *pUserMarshal = (USER_MARSHAL_CB *) pFlags;

    if(pUserMarshal->pReserve != 0)
    {
        IID iid;
        memcpy(&iid, pUserMarshal->pReserve, sizeof(IID));
        return (pfnLPSAFEARRAY_Size)(pFlags, Offset, ppSafeArray, &iid);
    }
    else
    {
        return (pfnLPSAFEARRAY_UserSize)(pFlags, Offset, ppSafeArray);
    }
}

BYTE * __RPC_USER
SafeArrayMarshal (ULONG * pFlags, BYTE * pBuffer, LPSAFEARRAY * ppSafeArray)
{
    USER_MARSHAL_CB *pUserMarshal = (USER_MARSHAL_CB *) pFlags;

    if(pUserMarshal->pReserve != 0)
    {
        IID iid;
        memcpy(&iid, pUserMarshal->pReserve, sizeof(IID));
        return (pfnLPSAFEARRAY_Marshal)(pFlags, pBuffer, ppSafeArray, &iid);
    }
    else
    {
        return (pfnLPSAFEARRAY_UserMarshal)(pFlags, pBuffer, ppSafeArray);
    }
}

BYTE * __RPC_USER
SafeArrayUnmarshal(ULONG * pFlags, BYTE * pBuffer, LPSAFEARRAY * ppSafeArray)
{
    USER_MARSHAL_CB *pUserMarshal = (USER_MARSHAL_CB *) pFlags;

    if(pUserMarshal->pReserve != 0)
    {
        IID iid;
        memcpy(&iid, pUserMarshal->pReserve, sizeof(IID));
        return (pfnLPSAFEARRAY_Unmarshal)(pFlags, pBuffer, ppSafeArray, &iid);
    }
    else
    {
        return (pfnLPSAFEARRAY_UserUnmarshal)(pFlags, pBuffer, ppSafeArray);
    }
}


USER_MARSHAL_SIZING_ROUTINE
pfnLPSAFEARRAY_UserSize = (USER_MARSHAL_SIZING_ROUTINE) LPSAFEARRAY_UserSize;

USER_MARSHAL_MARSHALLING_ROUTINE
pfnLPSAFEARRAY_UserMarshal = (USER_MARSHAL_MARSHALLING_ROUTINE) LPSAFEARRAY_UserMarshal;

USER_MARSHAL_UNMARSHALLING_ROUTINE
pfnLPSAFEARRAY_UserUnmarshal = (USER_MARSHAL_UNMARSHALLING_ROUTINE) LPSAFEARRAY_UserUnmarshal;

USER_MARSHAL_ROUTINE_QUADRUPLE UserMarshalRoutines[3] =
{
    {
        (USER_MARSHAL_SIZING_ROUTINE) BSTR_UserSize,
        (USER_MARSHAL_MARSHALLING_ROUTINE) BSTR_UserMarshal,
        (USER_MARSHAL_UNMARSHALLING_ROUTINE) BSTR_UserUnmarshal,
        (USER_MARSHAL_FREEING_ROUTINE) BSTR_UserFree
    },
    {
        (USER_MARSHAL_SIZING_ROUTINE) VARIANT_UserSize,
        (USER_MARSHAL_MARSHALLING_ROUTINE) VARIANT_UserMarshal,
        (USER_MARSHAL_UNMARSHALLING_ROUTINE) VARIANT_UserUnmarshal,
        (USER_MARSHAL_FREEING_ROUTINE) VARIANT_UserFree
    },
    {
        (USER_MARSHAL_SIZING_ROUTINE) SafeArraySize,
        (USER_MARSHAL_MARSHALLING_ROUTINE) SafeArrayMarshal,
        (USER_MARSHAL_UNMARSHALLING_ROUTINE) SafeArrayUnmarshal,
        (USER_MARSHAL_FREEING_ROUTINE) LPSAFEARRAY_UserFree
    }
};

HRESULT NdrLoadOleAutomationRoutines()
{

    void * pfnTemp;

    //Load oleaut32.dll
    if(hOleAut32 == 0)
    {
        hOleAut32 = LoadLibraryA("OLEAUT32");

        if(0 == hOleAut32)
            return HRESULT_FROM_WIN32(GetLastError());
    }

    pfnTemp = GetProcAddress(hOleAut32, "BSTR_UserSize");
    if(pfnTemp != 0)
        UserMarshalRoutines[0].pfnBufferSize = (USER_MARSHAL_SIZING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());

    pfnTemp = GetProcAddress(hOleAut32, "BSTR_UserMarshal");
    if(pfnTemp != 0)
        UserMarshalRoutines[0].pfnMarshall = (USER_MARSHAL_MARSHALLING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());


    pfnTemp = GetProcAddress(hOleAut32, "BSTR_UserUnmarshal");
    if(pfnTemp != 0)
        UserMarshalRoutines[0].pfnUnmarshall = (USER_MARSHAL_UNMARSHALLING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());


    pfnTemp = GetProcAddress(hOleAut32, "BSTR_UserFree");
    if(pfnTemp != 0)
        UserMarshalRoutines[0].pfnFree = (USER_MARSHAL_FREEING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());

    pfnTemp = GetProcAddress(hOleAut32, "VARIANT_UserSize");
    if(pfnTemp != 0)
        UserMarshalRoutines[1].pfnBufferSize = (USER_MARSHAL_SIZING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());

    pfnTemp = GetProcAddress(hOleAut32, "VARIANT_UserMarshal");
    if(pfnTemp != 0)
        UserMarshalRoutines[1].pfnMarshall = (USER_MARSHAL_MARSHALLING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());


    pfnTemp = GetProcAddress(hOleAut32, "VARIANT_UserUnmarshal");
    if(pfnTemp != 0)
        UserMarshalRoutines[1].pfnUnmarshall = (USER_MARSHAL_UNMARSHALLING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());


    pfnTemp = GetProcAddress(hOleAut32, "VARIANT_UserFree");
    if(pfnTemp != 0)
        UserMarshalRoutines[1].pfnFree = (USER_MARSHAL_FREEING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());

    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_UserSize");
    if(pfnTemp != 0)
        pfnLPSAFEARRAY_UserSize = (USER_MARSHAL_SIZING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());

    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_UserMarshal");
    if(pfnTemp != 0)
        pfnLPSAFEARRAY_UserMarshal = (USER_MARSHAL_MARSHALLING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());


    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_UserUnmarshal");
    if(pfnTemp != 0)
        pfnLPSAFEARRAY_UserUnmarshal = (USER_MARSHAL_UNMARSHALLING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());


    pfnTemp = GetProcAddress(hOleAut32, "LPSAFEARRAY_UserFree");
    if(pfnTemp != 0)
        UserMarshalRoutines[2].pfnFree = (USER_MARSHAL_FREEING_ROUTINE) pfnTemp;
    else
        return HRESULT_FROM_WIN32(GetLastError());

    return S_OK;
}
