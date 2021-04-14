//+---------------------------------------------------------------------------
//
//  Microsoft Windows
//  Copyright (C) Microsoft Corporation, 1992 - 1996.
//
//  File:	refilb.cxx
//
//  Contents:	Reference ILockBytes class
//
//  Classes:	CFileILB
//
//  Notes:      This Class always call single byte I/O routines
//              because most systems only have support for single byte
//              I/O. This makes the code more portable.
//
//----------------------------------------------------------------------------


#include "msfhead.cxx"
#include <errno.h>
#include "h/refilb.hxx"
#include <stdio.h>
#include <stdlib.h>

#include <sys/stat.h>
#include "time.hxx"
#include "h/tchar.h"

#ifdef _WIN32
#include <io.h> // to get definition of wunlink
#else
#include <unistd.h>
// get the correct stat structure
#endif

#include <stdlib.h>
#include "h/ole.hxx"

DECLARE_INFOLEVEL(ol, DEB_ERROR)

static int filenum = 0;

char * GetTempFileName(void)
{
    char *psz = new char[_MAX_PATH +1];
    strcpy(psz, "dft");

    _itoa(filenum, psz + 3, 10);
    filenum++;
    return psz;
}

CFileILB::CFileILB(const TCHAR *pszName,
                   DWORD grfMode,
                   BOOL fOpenFile/* =TRUE */)
{
    _pszName = NULL;

    _fDelete = FALSE;
    if (pszName == NULL)
    {
         _pszName = GetTempFileName();
         _unlink(_pszName);            // make sure file is over written
         _fDelete |= ILB_DELETEONERR;  // don't want to keep scratch files
    }
    else
    {
        _pszName = new char[_MAX_PATH + 1];
        TTOS(pszName, _pszName, _tcslen(pszName)+1);
    }

    if (grfMode & STGM_DELETEONRELEASE)
        _fDelete |= ILB_DELETEONRELEASE;
    _f = NULL;
    if (fOpenFile)
    {
        // disregard if file is already there
	Create(STGM_CREATE|STGM_READWRITE);
        // got to open the file with this option
        olAssert(_f && "CFileILB could not open the file!");
    }
    _ulRef = 1;
}

static const char pcszReadOnly[] ="rb";
static const char pcszReadWrite[] = "r+b";
static const char pcszWrite[] = "w+b";

SCODE CFileILB::Create(DWORD grfMode)
{
    char const *pszMode = pcszReadOnly; // default

    if (grfMode & STGM_READWRITE)
        pszMode = pcszReadWrite;
    else
        olDebugOut((DEB_ITRACE, "CFileILB::Create called with Read Only!!\n"));

    _f = fopen(_pszName, pszMode);
    if (_f)                     // open succeeded
    {
        if ((grfMode & (STGM_CREATE|STGM_CONVERT) ) == STGM_FAILIFTHERE)
            return STG_E_FILEALREADYEXISTS;
    }
    else if (errno==EACCES && (grfMode & STGM_CONVERT))
    {
        olDebugOut((DEB_ERROR,"Access Denied in CFileILB::Create\n"));
        return STG_E_ACCESSDENIED;
    }
    else
    {
        // the file does not exists, create the file
        _f = fopen(_pszName, pcszWrite);
        if (_f==NULL)
        {
            // we could not create the file for some reason
            // return the appropriate error code
            if (errno== EACCES)
                return STG_E_ACCESSDENIED;
            else
            {
                return STG_E_INVALIDNAME; // assume it is an invalid name
            }
        }
        else
        {
            // the newly create file should be deleted on error
            _fDelete |= ILB_DELETEONERR;
        }
    }
    return S_OK;
}

SCODE CFileILB::Open(DWORD grfMode)
{
    char const *pszMode = pcszReadOnly; // default

    // this means an null named file
    olAssert( (_fDelete & ILB_DELETEONERR)==0 );
                                                  // has been opened
    if (grfMode & STGM_READWRITE)
        pszMode = pcszReadWrite;
    else
        olDebugOut((DEB_ITRACE, "CFileILB::Open called with Read Only!!\n"));

    _f = fopen(_pszName, pszMode);
    if (_f == NULL)
    {
        if (errno==EACCES) return STG_E_ACCESSDENIED;
        else if (errno==ENOENT) return STG_E_FILENOTFOUND;
        else return STG_E_INVALIDNAME; // we assume that the name is invalid
    }

    return S_OK;
}

CFileILB::~CFileILB()
{
    if (_f)
        fclose(_f);
    if (_fDelete & ILB_DELETEONRELEASE)
    {
         // nothing we can do if the file cannot be deleted somehow
         // since the ref impl. is not multi-thread safe
        _unlink(_pszName);
    }
    delete _pszName;
}

STDMETHODIMP CFileILB::QueryInterface(REFIID riid, LPVOID FAR* ppvObj)
{
    *ppvObj = NULL;
    UNREFERENCED_PARM(riid);
    olAssert(FALSE && aMsg("function not implemented!"));
    return ResultFromScode(STG_E_INVALIDFUNCTION);
}

STDMETHODIMP_(ULONG) CFileILB::AddRef(void)
{
    AtomicInc(&_ulRef);
    return(_ulRef);
}

STDMETHODIMP_(ULONG) CFileILB::Release(void)
{
    AtomicDec(&_ulRef);
    olDebugOut( (DEB_ITRACE, "CFileILB::Release => %lx\n", _ulRef) );
    if (_ulRef > 0)
        return(_ulRef);
    delete this;

    return(0);
}

ULONG CFileILB::ReleaseOnError(void)
{
    // this function should be not used otherwise
    olAssert(_ulRef == 1);

    // Delete the file if it is a file we just created
    if (_fDelete & ILB_DELETEONERR)
        _fDelete |= ILB_DELETEONRELEASE;
    return( Release() );
}

ULONG CFileILB::Seek (ULONGLONG ulPos)
{
    ULONG ulRet = 0;

    if (ulPos > MAX_ULONG)
    {
        fseek(_f, MAX_ULONG, SEEK_SET);
        while (ulPos > MAX_ULONG)
        {
            ulPos -= MAX_ULONG;
            if (ulPos > MAX_ULONG)
                ulRet = fseek(_f, MAX_ULONG, SEEK_CUR);
            else
                ulRet = fseek(_f, (long) ulPos, SEEK_CUR);
        }
    }
    else ulRet = fseek(_f, (long) ulPos, SEEK_SET);
    
    return ulRet;
}

STDMETHODIMP CFileILB::ReadAt(ULARGE_INTEGER ulPosition,

        VOID HUGEP *pb,
        ULONG cb,
        ULONG *pcbRead)
{
    Seek (ulPosition.QuadPart);

    *pcbRead = fread(pb, 1, cb, _f);
    return NOERROR;
}

STDMETHODIMP CFileILB::WriteAt(ULARGE_INTEGER ulPosition,
        VOID const HUGEP *pb,
        ULONG cb,
        ULONG FAR *pcbWritten)
{
    Seek (ulPosition.QuadPart);

    *pcbWritten = fwrite(pb, 1, cb, _f);
    return NOERROR;
}

STDMETHODIMP CFileILB::Flush(void)
{
    fflush(_f);
    return NOERROR;
}

STDMETHODIMP CFileILB::SetSize(ULARGE_INTEGER ulNewSize)
{
    LONGLONG cbNewSize = ulNewSize.QuadPart;
    LONGLONG cbCurrentSize = _filelengthi64 (_fileno(_f));
    if(-1 == cbCurrentSize)
        return STG_E_SEEKERROR;

    if(cbCurrentSize < cbNewSize)
    {                                       // Increase the Size
        Seek (cbNewSize-1);

        if(1 != fwrite("", 1, 1, _f))
            return STG_E_WRITEFAULT;
    }
    else if(cbCurrentSize > cbNewSize)
    {                                       // Decrease the Size
        //  OS specific: file truncation.
    }
    return NOERROR;
}

STDMETHODIMP CFileILB::LockRegion(ULARGE_INTEGER libOffset,
                                  ULARGE_INTEGER cb,
                                  DWORD dwLockType)
{
    UNREFERENCED_PARM(dwLockType);
    UNREFERENCED_PARM(cb);
    UNREFERENCED_PARM(libOffset);
    olAssert(FALSE && aMsg("function not implemented!"));
    return ResultFromScode(STG_E_INVALIDFUNCTION);
}


STDMETHODIMP CFileILB::UnlockRegion(ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD dwLockType)
{
    UNREFERENCED_PARM(dwLockType);
    UNREFERENCED_PARM(cb);
    UNREFERENCED_PARM(libOffset);
    olAssert(FALSE && aMsg("function not implemented!"));
    return ResultFromScode(STG_E_INVALIDFUNCTION);
}


STDMETHODIMP CFileILB::Stat(STATSTG FAR *pstatstg, DWORD grfStatFlag)
{
    memset(pstatstg, 0, sizeof(STATSTG));

    if ((grfStatFlag & STATFLAG_NONAME) == 0)
    {
         char pchTemp[_MAX_PATH+1];
        _fullpath(pchTemp, _pszName, _MAX_PATH+1);
        pstatstg->pwcsName = new TCHAR[strlen(pchTemp)+1];
        STOT(pchTemp, pstatstg->pwcsName, strlen(pchTemp)+1);
    }

    pstatstg->type = STGTY_LOCKBYTES;

    fseek(_f, 0, SEEK_END);
    pstatstg->cbSize.QuadPart = ftell(_f);

    // just return a default, the function that calls this should fill in
    // the structure.
    pstatstg->grfMode = STGM_READWRITE | STGM_DIRECT | STGM_SHARE_EXCLUSIVE;

    struct _stat buf;
    int result = _stat(_pszName, &buf);
    if (!result)  // fill in zeros
    {
        pstatstg->atime.dwLowDateTime = pstatstg->atime.dwHighDateTime = 0;
        pstatstg->mtime.dwLowDateTime = pstatstg->mtime.dwHighDateTime = 0;
        pstatstg->ctime.dwLowDateTime = pstatstg->ctime.dwHighDateTime = 0;
    }
    else
    {
        TimeTToFileTime(&buf.st_atime, &pstatstg->atime);
        TimeTToFileTime(&buf.st_mtime, &pstatstg->mtime);
        TimeTToFileTime(&buf.st_ctime, &pstatstg->ctime);
    }
    return NOERROR;
}

EXTERN_C STDAPI_(BOOL) IsEqualGUID(REFGUID rguid1, REFGUID rguid2)
{
    return (memcmp(&rguid1, &rguid2, sizeof(GUID)) == 0);
}
