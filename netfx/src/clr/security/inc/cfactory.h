// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
#ifndef __CFactory_h__
#define __CFactory_h__

#include "CUnknown.h"
///////////////////////////////////////////////////////////

// Forward reference
class CFactoryData ;

// Global data used by CFactory
extern CFactoryData g_FactoryDataArray[] ;
extern int g_cFactoryDataEntries ;

//////////////////////////////////////////////////////////
//
//  Component creation function
//
class CUnknown ;

typedef HRESULT (*FPCREATEINSTANCE)(IUnknown*, CUnknown**) ;

///////////////////////////////////////////////////////////
//
// CFactoryData
//   - Information CFactory needs to create a component
//     supported by the DLL
//
class CFactoryData
{
public:
    // The class ID for the component
    const CLSID* m_pCLSID ;

    // Pointer to the function that creates it
    FPCREATEINSTANCE CreateInstance ;

    // Name of the component to register in the registry
    LPCWSTR m_RegistryName ;

    // ProgID
    LPCWSTR m_wszProgID ;

    // Class id
    LPCWSTR m_wszClassID;
    
    // Version-independent ProgID
    int m_version ;

    // Helper function for finding the class ID
    BOOL IsClassID(const CLSID& clsid) const
        { return (*m_pCLSID == clsid) ;}

    //
    // Out of process server support
    //

    // Pointer to running class factory for this component
    IClassFactory* m_pIClassFactory ;

    // Magic cookie to identify running object
    DWORD m_dwRegister ;
} ;


///////////////////////////////////////////////////////////
//
// Class Factory
//
class CFactory : public IClassFactory
{
public:
    // IUnknown
    virtual HRESULT __stdcall QueryInterface(const IID& iid, void** ppv) ;
    virtual ULONG   __stdcall AddRef() ;
    virtual ULONG   __stdcall Release() ;
    
    // IClassFactory
    virtual HRESULT __stdcall CreateInstance(IUnknown* pUnknownOuter,
                                             const IID& iid,
                                             void** ppv) ;
    virtual HRESULT __stdcall LockServer(BOOL bLock) ; 

    // Constructor - Pass pointer to data of component to create.
    CFactory(const CFactoryData* pFactoryData) ;

    // Destructor
    ~CFactory() { }

    //
    // Static FactoryData support functions
    //

    // DllGetClassObject support
    static HRESULT GetClassObject(const CLSID& clsid, 
                                  const IID& iid, 
                                  void** ppv) ;

    // Helper function for DllCanUnloadNow 
    static BOOL IsLocked()
        { return (s_cServerLocks > 0) ;}

    // Functions to [un]register all components
    static HRESULT RegisterAll(HINSTANCE hInst) ;
    static HRESULT UnregisterAll() ;

    // Function to determine if component can be unloaded
    static HRESULT CanUnloadNow() ;

    // CloseExe doesn't do anything if we are in process.
    static void CloseExe() { /*Empty*/ } 

public:
    // Reference Count
    LONG m_cRef ;

    // Pointer to information about class this factory creates
    const CFactoryData* m_pFactoryData ;

    // Count of locks
    static LONG s_cServerLocks ;   

    // Module handle
    //static HMODULE s_hModule ;
} ;

#endif
