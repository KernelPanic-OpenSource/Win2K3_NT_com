//+--------------------------------------------------------------------------
//
// Microsoft Windows
// Copyright (C) Microsoft Corporation, 1992 - 1996
//
// File:        propstm.hxx
//
// Contents:    property set definitions
//
//---------------------------------------------------------------------------

#ifndef _PROPSTM_HXX_
#define _PROPSTM_HXX_
#include "props.h"
#include "../props/h/propapi.h"
#include "../props/h/propset.hxx"

class PMemoryAllocator
{
public:
    virtual void *Allocate(ULONG cbSize) = 0;
    virtual void Free(void *pv) = 0;
};

#define DEB_USER1 0x00010000
#define DEB_USER2 0x00020000

#define IsBadReadPtr(pv,cb) (pv==NULL)
#define IsBadWritePtr(pv,cb) (pv==NULL)
#define IsValidReadPtrIn(pv,cb)  (!IsBadReadPtr ((pv),(cb)))
#define IsValidPtrOut(pv,cb) (!IsBadWritePtr((pv),(cb)))
#define IsValidPtrIn(pv,cb) (TRUE)  // nothing we can check here

#define VDATESIZEREADPTRIN_LABEL(pv, cb, label, retVar) \
        if (!IsValidReadPtrIn((pv), cb)) \
        { \
            retVar = ResultFromScode(E_INVALIDARG); \
            goto label; \
        }

#define VDATEREADPTRIN_LABEL(pv, TYPE, label, retVar) \
        if (!IsValidReadPtrIn((pv), sizeof(TYPE)))  { \
            retVar = ResultFromScode(E_INVALIDARG); \
            goto label; }

#define VDATEREADPTRIN( pv, TYPE ) \
    if (!IsValidReadPtrIn( (pv), sizeof(TYPE))) \
       return (ResultFromScode(E_INVALIDARG))

#define VDATESIZEPTROUT_LABEL(pv, cb, label, retVar) \
    if (!IsValidPtrOut((pv), cb)) \
    { retVar = ResultFromScode(E_INVALIDARG); \
         goto label; }

#define GEN_VDATEPTROUT_LABEL( pv, TYPE, retval, label, retVar ) \
        if (!IsValidPtrOut( (pv), sizeof(TYPE))) \
        { retVar = (retval); \
         goto label; }

#define VDATEPTROUT( pv, TYPE ) \
    if (!IsValidPtrOut( (pv), sizeof(TYPE))) \
        return ResultFromScode(E_INVALIDARG);

#define VDATEPTROUT_LABEL( pv, TYPE, label, retVar ) \
        if (!IsValidPtrOut( (pv), sizeof(TYPE))) \
        { retVar = ResultFromScode(E_INVALIDARG); \
         goto label; }

#define VDATEPTRIN_LABEL(pv, TYPE, label, retVar) \
        if (!IsValidPtrIn((pv), sizeof(TYPE))) \
        { retVar = ResultFromScode(E_INVALIDARG); \
         goto label; }

#define GEN_VDATEREADPTRIN_LABEL(pv, TYPE, retval, label, retVar) \
        if (!IsValidReadPtrIn((pv), sizeof(TYPE))) \
        { retVar = retval; \
         goto label; }

#define GEN_VDATEPTRIN_LABEL(pv, TYPE, retval, label, retVar) \
        if (!IsValidPtrIn((pv), sizeof(TYPE))) \
        { retVar = retval; \
         goto label; }

#include <memory.h>
#define  ZeroMemory(pb,cb)  memset((pb),0,(cb))

typedef enum _PROPOP
{
    PROPOP_IGNORE = 1,
    PROPOP_DELETE = 2,
    PROPOP_INSERT = 3,
    PROPOP_MOVE   = 4,
    PROPOP_UPDATE = 5,
} PROPOP;

typedef struct tagPROPERTY_INFORMATION  // pinfo
{
    PROPID                   pid;

    ULONG                    cbprop;
    PROPOP                   operation;

} PROPERTY_INFORMATION;

#define VT_DICTIONARY	(VT_VECTOR | VT_ILLEGALMASKED)



// Validate the endian flags.  Either BIGENDIAN or LITTLEENDIAN, but not
// both, must be defined as 1.

#if defined(BIGENDIAN)
#   ifdef LITTLEENDIAN
#       error Both BIGENDIAN & LITTLEENDIAN are defined
#   endif
#   if BIGENDIAN != 1
#       error BIGENDIAN must be set to 1 (not just defined)
#   endif
#elif defined(LITTLEENDIAN)
#   ifdef BIGENDIAN
#       error Both BIGENDIAN & LITTLEENDIAN are defined
#   endif
#   if LITTLEENDIAN != 1
#       error LITTLEENDIAN must be set to 1 (not just defined)
#   endif
#else
#   error Either BIGENDIAN or LITTLEENDIAN must be defined as 1
#endif




//+-------------------------------------------------------------------------
//
//         Class:       CMappedStream (user mode implementation)
//
//--------------------------------------------------------------------------

class CMappedStream
{
public:
    virtual VOID Open(IN NTPROP np, OUT LONG *phr) = 0;
    virtual VOID Close(OUT LONG *phr) = 0;
    virtual VOID ReOpen(IN OUT VOID **ppv, OUT LONG *phr) = 0;
    virtual VOID Quiesce(VOID) = 0;
    virtual VOID Map(IN BOOLEAN fCreate, OUT VOID **ppv) = 0;
    virtual VOID Unmap(IN BOOLEAN fFlush, IN OUT VOID **ppv) = 0;
    virtual VOID Flush(OUT LONG *phr) = 0;

    virtual ULONG GetSize(OUT LONG *phr) = 0;
    virtual VOID SetSize(IN ULONG cb, IN BOOLEAN fPersistent, IN OUT VOID **ppv, OUT LONG *phr) = 0;
    virtual VOID QueryTimeStamps(OUT STATPROPSETSTG *pspss, BOOLEAN fNonSimple) const = 0;
    virtual BOOLEAN QueryModifyTime(OUT LONGLONG *pll) const = 0;
    virtual BOOLEAN QuerySecurity(OUT ULONG *pul) const = 0;

    virtual BOOLEAN IsWriteable(VOID) const = 0;
    virtual BOOLEAN IsModified(VOID) const = 0;
    virtual VOID SetModified(VOID) = 0;
    virtual HANDLE GetHandle(VOID) const = 0;
#if DBGPROP
    virtual BOOLEAN SetChangePending(BOOLEAN fChangePending) = 0;
    virtual BOOLEAN IsNtMappedStream(VOID) const = 0;
#endif
};

//+-------------------------------------------------------------------------
// Class:       CStreamChunkList
//
// Purpose:     Used to support efficient in-place compaction/expansion of
//              property set streams for CPropertySetStorage.
//--------------------------------------------------------------------------

struct CStreamChunk             // scnk
{
    ULONG       oOld;
    LONG        cbChange;
};

class CStreamChunkList          // scl
{
public:
    CStreamChunkList(ULONG cMaxChunks, CStreamChunk *ascnk);

    CStreamChunk *GetFreeChunk(OUT NTSTATUS *pstatus);
    CStreamChunk const *GetChunk(ULONG i) const;
    ULONG Count(VOID) const;
    VOID SortByStartAddress(VOID);
    VOID Delete(VOID);

#if DBGPROP
    VOID AssertCbChangeTotal(
            CStreamChunk const *pscnk,
            ULONG cbChangeTotal) const;
#endif

private:
    ULONG         _cMaxChunks;  // elements in _ascnk
    ULONG         _cChunks;     // elements allocated from _ascnk
    CStreamChunk *_ascnk;
    BOOLEAN       _fDelete;     // TRUE if _ascnk allocated from heap
};

#define _MSTM(fn) _pmstm->fn

#define CPSS_DOWNLEVEL              0x02  // Downlevel property set.
#define CPSS_PACKEDPROPERTIES       0x04  // Packed property values
#define CPSS_DOCUMENTSUMMARYINFO    0x08  // DocumentSummaryInfo propset
#define CPSS_USERDEFINEDPROPERTIES  0x10  // DocumentSummaryInfo: second section
#define CPSS_MULTIPLESECTIONS       0x20  // Multiple property sections
#define CPSS_USERDEFINEDDELETED	    0x40  // second section deleted
#define CPSS_VARIANTVECTOR          0x80  // Only for recursion


typedef enum tagLOADSTATE
{
    LOADSTATE_FAIL			= 0,	// load failed
    LOADSTATE_DONE			= 1,	// load succeeded
    LOADSTATE_BADFMTID			= 2,	// fmtid mismatch
    LOADSTATE_USERDEFINEDNOTFOUND	= 3,	// user defined section missing
    LOADSTATE_USERDEFINEDDELETE		= 4,	// delete user defined section
} LOADSTATE;


typedef enum tagPATCHOP
{
    PATCHOP_COMPUTESIZE			= 0,	// just compute expanded size
    PATCHOP_ALIGNLENGTHS		= 1,	// just align string lengths
    PATCHOP_EXPAND			= 2,	// expand property in-place
} PATCHOP;

// Create a macro that tells us the type
// of OLECHARs.
 
#ifdef OLE2ANSI
#define OLECHAR_IS_UNICODE FALSE
#else
#define OLECHAR_IS_UNICODE TRUE
#endif

#if DBGPROP
BOOLEAN IsUnicodeString(WCHAR const *pwszname, ULONG cb);
BOOLEAN IsAnsiString(CHAR const *pszname, ULONG cb);
#endif


// IsOLECHARString calls IsUnicodeString or IsAnsiString,
// whichever is appropriate for this compilation.

inline BOOLEAN IsOLECHARString( OLECHAR const *posz, ULONG cb )
{
#if DBGPROP

#if OLECHAR_IS_UNICODE
    return( IsUnicodeString( posz, cb ));
#else
    return( IsAnsiString( posz, cb ));
#endif

#else // DBGPROP
    posz;  // remove compiler warnings
    cb;    // remove compiler warnings
    return TRUE;
#endif
}

class CPropertySetStream
{
public:
    CPropertySetStream(
        IN USHORT Flags, // NONSIMPLE|*1* of READ/WRITE/CREATE/CREATEIF/DELETE
        IN CMappedStream *pmstm,    // mapped stream implementation
        IN PMemoryAllocator *pma    // caller's memory allocator
        );


    VOID Close(OUT NTSTATUS *pstatus);
    VOID SetValue(
                IN ULONG cprop,
                IN PROPVARIANT const avar[],
                IN PROPERTY_INFORMATION *ppinfo,
                OUT NTSTATUS *pstatus);

    SERIALIZEDPROPERTYVALUE const *GetValue(IN PROPID pid, OUT ULONG *pcbprop,
                                            OUT NTSTATUS *pstatus);

    PROPID QueryPropid(IN OLECHAR const *poszName, OUT NTSTATUS *pstatus);
    USHORT GetCodePage(VOID) const { return(_CodePage); }

#if DBGPROP
    VOID Validate(OUT NTSTATUS *pstatus);
#else
    VOID Validate(OUT NTSTATUS *pstatus) const { *pstatus = STATUS_SUCCESS; }
#endif

    BOOLEAN IsModified(VOID) const { return(_MSTM(IsModified)()); }
    VOID Flush(OUT NTSTATUS *pstatus)
    {
        PROPASSERT(PROPSET_BYTEORDER == _pph->wByteOrder);
        _MSTM(Flush)(pstatus);
        PROPASSERT(PROPSET_BYTEORDER == _pph->wByteOrder);
    }


    VOID Open(
        OPTIONAL IN GUID const *pfmtid,	// property set fmtid (create or Word95)
        OPTIONAL IN GUID const *pclsid, // CLASSID of propset code (create only)
	IN ULONG LocaleId,		// Locale Id (create only)
        OPTIONAL OUT ULONG *pOSVersion, // OS Version field in the propset header
        IN USHORT CodePage,             // CodePage of propset (create only)
        OUT NTSTATUS *pstatus);         // Return code.
    ULONG ReOpen(OUT NTSTATUS *pstatus);

    // Property Dictionary Lookup and Manipulation.
    BOOLEAN EnumeratePropids(
                IN OUT ULONG *pkey,
                IN OUT ULONG *pcprop,
                OUT PROPID *apid,
                OUT NTSTATUS *pstatus);

    OLECHAR *DuplicatePropertyName(
                IN OLECHAR const *poszName,
                IN ULONG cbName,
                OUT NTSTATUS *pstatus) const;

    BOOLEAN QueryPropertyNameBuf(
                IN PROPID pid,
                OUT OLECHAR *aocName,
                IN OUT ULONG *pcbName,
                OUT NTSTATUS *pstatus);

    BOOLEAN QueryPropertyNames(
                IN ULONG cprop,
                IN PROPID const *apid,
                OUT OLECHAR *aposz[],
                OUT NTSTATUS *pstatus);

    VOID SetPropertyNames(
                IN ULONG cprop,
                IN PROPID const *apid,
                IN OPTIONAL OLECHAR const * const aposz[],
                OUT NTSTATUS *pstatus);

    VOID QueryPropertySet(OUT STATPROPSETSTG *pspss, OUT NTSTATUS *pstatus) const;
    VOID SetClassId(IN GUID const *pclsid, OUT NTSTATUS *pstatus);
    HANDLE GetHandle(VOID) const { return(_MSTM(GetHandle)()); }
    PMemoryAllocator *GetAllocator(VOID) const { return(_pma); }

    VOID ByteSwapHeaders( IN PROPERTYSETHEADER *psh,
                          IN ULONG cbstm,
                          OUT NTSTATUS *pstatus );

    // Convert a character count to a byte count based on the _CodePage

    ULONG     CCh2CB( ULONG cch ) const
                {
                    return( _CodePage == CP_WINUNICODE
                            ? cch * sizeof( WCHAR )
                            : cch * sizeof( CHAR )
                          );
                }

    // Convert a byte count to a character count based on the _CodePage
    // If for some reason the given byte-count is odd, the return
    // value won't include that last half-character.

    ULONG     CB2CCh( ULONG cb ) const
    {
        return( _CodePage == CP_WINUNICODE
                ? cb / sizeof( WCHAR )
                : cb / sizeof( CHAR )
            );
    }
    
    
private:

    // Private methods.

    VOID      _Create(
        IN GUID const *pfmtid,
        OPTIONAL IN GUID const *pclsid,
        IN ULONG LocaleId,
        IN USHORT CodePage,
        IN LOADSTATE LoadState,
        OUT NTSTATUS *pstatus);
    BOOLEAN   _HasPropHeader(VOID) const { return(TRUE); }
    BOOLEAN   _CreateUserDefinedSection(
        IN LOADSTATE LoadState,
        IN ULONG LocaleId,
        OUT NTSTATUS *pstatus);
    VOID      _InitSection(
        IN FORMATIDOFFSET *pfo,
        IN ULONG LocaleId,
        IN BOOL  fCreateDictionary );
    
    VOID      _FixSummaryInformation(IN OUT ULONG *pcbstm, OUT NTSTATUS *pstatus);
    VOID      _FixPackedPropertySet(OUT NTSTATUS *pstatus);
    
    BOOLEAN   _FixDocPartsVector(
        IN PATCHOP PatchOp,
        IN OUT SERIALIZEDPROPERTYVALUE *pprop,
        OUT ULONG *pcbprop);
    BOOLEAN   _FixDocPartsElements(
        IN PATCHOP PatchOp,
        IN ULONG cString,
        OUT VOID *pvDst,
        IN VOID UNALIGNED const *pvSrc,
        OUT ULONG *pcbprop);
    
    BOOLEAN   _FixHeadingPairVector(
        IN PATCHOP PatchOp,
        IN OUT SERIALIZEDPROPERTYVALUE *pprop,
        OUT ULONG *pcbprop);
    BOOLEAN   _FixHeadingPairElements(
        IN PATCHOP PatchOp,
        IN ULONG cPairs,
        OUT SERIALIZEDPROPERTYVALUE *ppropDst,
        IN SERIALIZEDPROPERTYVALUE UNALIGNED const *ppropSrc,
        OUT ULONG *pcbprop);
    
    BOOLEAN   _IsMapped(VOID) const { return(_pph != NULL); }
    LOADSTATE _LoadHeader(OPTIONAL IN GUID const *pfmtid, IN BYTE Mode,
                          OUT NTSTATUS *pstatus);

    PROPERTYSECTIONHEADER *_LoadPropertyOffsetPointers(
        OUT PROPERTYIDOFFSET **pppo,
        OUT PROPERTYIDOFFSET **pppoMax,
        OUT NTSTATUS *pstatus);
    
    SERIALIZEDPROPERTYVALUE *_LoadProperty(IN PROPID pid, OUT ULONG *pcbprop,
                                           OUT NTSTATUS *pstatus);
    inline PROPERTYSECTIONHEADER *_GetSectionHeader(VOID) const;
    VOID _SearchForCodePage( OUT NTSTATUS *pstatus);
    PROPERTYSECTIONHEADER *_GetSectionHeader(IN ULONG iSection, OUT NTSTATUS *pstatus);
    FORMATIDOFFSET *_GetFormatidOffset(IN ULONG iSection) const;

    VOID     *_MapOffsetToAddress(IN ULONG oOffset) const;
    ULONG     _MapAddressToOffset(IN VOID const *pvAddr) const;

    VOID     *_MapAbsOffsetToAddress(IN ULONG oAbsolute) const;
    ULONG     _MapAddressToAbsOffset(IN VOID const *pvAddr) const;

    ULONG     _GetNewOffset(
        IN CStreamChunkList const *pscl,
        IN ULONG oOld) const;
    
    ULONG     _DictionaryEntryLength(
        IN ENTRY UNALIGNED const * pent ) const;
    

    ULONG     _DictionaryLength(
        IN DICTIONARY const *pdy,
        IN ULONG cbbuf,
        OUT NTSTATUS *pstatus) const;
    
    ENTRY UNALIGNED
    *_NextDictionaryEntry(
        IN ENTRY UNALIGNED const * pent ) const;
    
    ULONG     _CountFreePropertyOffsets(OUT NTSTATUS *pstatus);

    VOID      _DeleteMovePropertyOffsets(
        IN PROPERTY_INFORMATION const *apinfo,
        IN ULONG cprop,
        OUT NTSTATUS *pstatus);
    
    VOID      _UpdatePropertyOffsets(
        IN CStreamChunkList const *pscl,
        OUT NTSTATUS *pstatus);
    
    VOID      _InsertMovePropertyOffsets(
        IN PROPERTY_INFORMATION const *apinfo,
        IN ULONG cprop,
        IN ULONG oInsert,
        IN ULONG cpoReserve,
        OUT NTSTATUS *pstatus);
    
    VOID      _CompactStream(IN CStreamChunkList const *pscl);

    VOID      _CompactChunk(
        IN CStreamChunk const *pscnk,
        IN LONG cbChangeTotal,
        IN ULONG oOldNext);

    VOID      _PatchSectionOffsets(LONG cbChange);

    ULONG     _ComputeMinimumSize(ULONG cbstm, OUT NTSTATUS *pstatus);
#if DBGPROP
    VOID      _ValidateStructure(OUT NTSTATUS *pstatus);
    VOID      _ValidateProperties(OUT NTSTATUS *pstatus) const;
    VOID      _ValidateDictionary(OUT NTSTATUS *pstatus);
    VOID      _StatusCorruption(char *szReason, OUT NTSTATUS *pstatus) const;
#else
    VOID      _StatusCorruption(OUT NTSTATUS *pstatus) const;
#endif
    VOID      _SetModified(VOID) { _MSTM(SetModified)(); }

#if DBGPROP
public:		// public for fnEntryNameCompare only!
#endif
    BOOLEAN   _ComparePropertyNames(
		    IN VOID const *pvName1,
		    IN VOID const *pvName2,
                    IN BOOL fSameByteOrder,
		    IN ULONG cbName) const;
#if DBGPROP
private:
#endif

    BOOLEAN   _PropertyNameLength(
                    IN VOID const *pvName,
                    OUT ULONG *pcbName) const;


    VOID      _MultiByteToWideChar(
                    IN CHAR const *pch,
                    IN ULONG cb,
                    IN USHORT CodePage,
                    OUT WCHAR **ppwc,
                    OUT ULONG *pcb,
                    OUT NTSTATUS *pstatus);

    VOID      _OLECHARToWideChar( IN OLECHAR const *poc,
                                  IN ULONG cb,
                                  IN USHORT CodePage,
                                  OUT WCHAR **ppwc,
                                  OUT ULONG *pcb,
                                  OUT NTSTATUS *pstatus)
    {        PROPASSERT( sizeof(OLECHAR) == sizeof(CHAR) );
        
        // Since OLECHAR may be MultiByte or WideChar for this
        // compilation, explicitely cast 'poc' to a CHAR* to prevent
        // a compilation error.

        _MultiByteToWideChar( (CHAR*) poc, cb, CodePage, ppwc, pcb, pstatus );
    }

    VOID      _MultiByteToOLECHAR( IN CHAR const *pch,
                                   IN ULONG cb,
                                   IN USHORT CodePage,
                                   OUT OLECHAR **ppoc,
                                   OUT ULONG *pcb,
                                   OUT NTSTATUS *pstatus)
    {
        PROPASSERT( sizeof(OLECHAR) == sizeof(WCHAR) );

        // Since OLECHAR may be MultiByte or WideChar for this
        // compilation, explicitely cast 'ppoc' to a WCHAR** to prevent
        // a compilation error.

        _MultiByteToWideChar( pch, cb, CodePage, (WCHAR**)ppoc, pcb, pstatus );
    }

    VOID      _WideCharToMultiByte( IN WCHAR const *pwc,
                                    IN ULONG cch,
                                    IN USHORT CodePage,
                                    OUT CHAR **ppch,
                                    OUT ULONG *pcb,
                                    OUT NTSTATUS *pstatus);

    VOID      _OLECHARToMultiByte( IN OLECHAR const *poc,
                                   IN ULONG cch,
                                   IN USHORT CodePage,
                                   OUT CHAR **ppch,
                                   OUT ULONG *pcb,
                                   OUT NTSTATUS *pstatus)
    {
        PROPASSERT( sizeof(OLECHAR) == sizeof(WCHAR) );

        // Since OLECHAR may be MultiByte or WideChar for this
        // compilation, explicitely cast 'poc' to a WCHAR* to prevent
        // a compilation error.

        _WideCharToMultiByte( (WCHAR*) poc, cch, CodePage, ppch, pcb, pstatus );
    }

    VOID      _WideCharToOLECHAR( IN WCHAR const *pwc,
                                  IN ULONG cch,
                                  IN USHORT CodePage,
                                  OUT OLECHAR **ppoc,
                                  OUT ULONG *pcb,
                                  OUT NTSTATUS *pstatus)
    {
        PROPASSERT( sizeof(OLECHAR) == sizeof(CHAR) );

        // Since OLECHAR may be MultiByte or WideChar for this
        // compilation, explicitely cast 'ppoc' to a CHAR** to prevent
        // a compilation error.

        _WideCharToMultiByte( pwc, cch, CodePage, (CHAR**) ppoc, pcb, pstatus );
    }


    PROPERTYSETHEADER     *_pph;
    ULONG                  _oSection;
    ULONG                  _cSection;
    USHORT                 _CodePage;
    BYTE                   _Flags;
    BYTE                   _State;
    ULONG		   _cbTail;
    PMemoryAllocator      *_pma;
    CMappedStream         *_pmstm;        // user mode: replacable virtual class
};


#ifdef WINNT
VOID CopyPropertyValue(
        IN OPTIONAL SERIALIZEDPROPERTYVALUE const *pprop,
        IN ULONG cb,
        OUT SERIALIZEDPROPERTYVALUE *ppropDst,
        OUT ULONG *pcb);

EXTERN_C ULONG __stdcall PropertyLengthAsVariant(
        IN SERIALIZEDPROPERTYVALUE const *pprop,
        IN ULONG cbprop,
	IN USHORT CodePage,
        IN BYTE flags,
        OUT NTSTATUS *pstatus);
#endif

ULONG PropertyLength(
        IN SERIALIZEDPROPERTYVALUE const *pprop,
        IN ULONG cbbuf,
        IN BYTE flags,
        OUT NTSTATUS *pstatus);

//  ------------
//  PBS Routines
//  ------------

// PBS routines (Property Byte Swap) perform specialized byte-swapping
// in the big-endian build, and do nothing in the little-endian
// build (in which case they are inlined).

#ifdef BIGENDIAN

VOID PBSCopy( OUT VOID *pvDest,
              IN VOID const *pvSource,
              IN ULONG cbCopy,
              IN LONG cbByteSwap );

VOID PBSAllocAndCopy( OUT VOID **ppvDest,
                      IN VOID const *pvSource,
                      ULONG cbSize,
                      LONG cbByteSwap,
                      OUT NTSTATUS *pstatus);

VOID PBSInPlaceAlloc( IN OUT WCHAR** ppwszResult,
                      OUT WCHAR** ppwszBuffer,
                      OUT NTSTATUS *pstatus );

VOID PBSBuffer( IN OUT VOID *pv,
                IN ULONG cbSize,
                IN ULONG cbByteSwap );

#else // Little-endian build

inline VOID PBSCopy( OUT VOID *,
                     IN VOID const *,
                     IN ULONG,
                     IN LONG )
{
}

inline VOID PBSAllocAndCopy( OUT VOID **,
                             IN VOID const *,
                             ULONG,
                             LONG,
                             OUT NTSTATUS *pstatus)
{
    *pstatus = STATUS_SUCCESS;
}

inline VOID PBSInPlaceAlloc( IN OUT WCHAR**,
                             OUT WCHAR**,
                             OUT NTSTATUS *pstatus )
{
    *pstatus = STATUS_SUCCESS;
}

inline VOID PBSBuffer( IN OUT VOID *,
                       IN ULONG,
                       IN ULONG)
{
}

#endif // #ifdef BIGENDIAN ... #else


#endif // _PROPSTM_HXX_
