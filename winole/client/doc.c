/****************************** Module Header ******************************\
* Module Name: doc.c
*
* PURPOSE: Contains client document maipulation routines.
*
* Created: Jan 1991
*
* Copyright (c) 1991  Microsoft Corporation
*
* History:
*   Srinik  01/11/1191  Orginal
*   curts created portable version for WIN16/32
*
\***************************************************************************/

#include <windows.h>
#include "dll.h"


LPCLIENTDOC lpHeadDoc = NULL;
LPCLIENTDOC lpTailDoc  = NULL;

extern ATOM aClipDoc;
extern int  iUnloadableDll;

OLESTATUS FAR PASCAL OleRegisterClientDoc(
    LPCSTR               lpClassName,
    LPCSTR               lpDocName,
    LONG                future,
    LHCLIENTDOC FAR *   lplhclientdoc
){
    HANDLE      hdoc = NULL;
    LPCLIENTDOC lpdoc;
    OLESTATUS   retVal;
    ATOM        aClass, aDoc;

    UNREFERENCED_PARAMETER(future);


    Puts ("OleRegisterClientDoc");

    PROBE_MODE(bProtMode);
    FARPROBE_WRITE(lplhclientdoc);
    *lplhclientdoc = 0;
    FARPROBE_READ(lpClassName);
    FARPROBE_READ(lpDocName);
    if (!lpDocName[0])
        return OLE_ERROR_NAME;

    aDoc = GlobalAddAtom (lpDocName);
    aClass = GlobalAddAtom (lpClassName);

    if (!(hdoc = GlobalAlloc (GMEM_MOVEABLE | GMEM_ZEROINIT | GMEM_SHARE,
                        sizeof(CLIENTDOC)))
            || !(lpdoc = (LPCLIENTDOC) GlobalLock (hdoc))) {
        retVal =  OLE_ERROR_MEMORY;
        goto err;
    }

    lpdoc->docId[0] = 'C';
    lpdoc->docId[1] = 'D';
    lpdoc->aClass   = aClass;
    lpdoc->aDoc     = aDoc;
    lpdoc->hdoc     = hdoc;
    lpdoc->dwFileVer= (DWORD)MAKELONG(wReleaseVer,OS_WIN16);

    // Documents are doubly linked

    if (!lpHeadDoc) {
        lpHeadDoc = lpTailDoc = lpdoc;
    }
    else {
        lpTailDoc->lpNextDoc = lpdoc;
        lpdoc->lpPrevDoc = lpTailDoc;
        lpTailDoc = lpdoc;
    }

    *lplhclientdoc = (LHCLIENTDOC) lpdoc;

    // inform the link manager;
    return OLE_OK;

err:
    if (aClass)
        GlobalDeleteAtom (aClass);

    if (aDoc)
        GlobalDeleteAtom (aDoc);

    if (hdoc)
        GlobalFree (hdoc);

    return retVal;
}


OLESTATUS FAR PASCAL OleRevokeClientDoc (
   LHCLIENTDOC lhclientdoc
){
    LPCLIENTDOC lpdoc;

    Puts ("OleRevokeClientDoc");

    // if there is any handler dll that can be freed up, free it now.
    // Otherwise it might become too late if the app quits.
    if (iUnloadableDll)
        UnloadDll ();

    if (!CheckClientDoc (lpdoc = (LPCLIENTDOC) lhclientdoc))
        return OLE_ERROR_HANDLE;

    if (lpdoc->lpHeadObj) {
        ASSERT (0, "OleRevokeClientDoc() called without freeing all objects");
        return OLE_ERROR_NOT_EMPTY;
    }

    if (lpdoc->aClass)
        GlobalDeleteAtom (lpdoc->aClass);

    if (lpdoc->aDoc)
        GlobalDeleteAtom (lpdoc->aDoc);

    // if only one doc is in the list then it's prev and next docs are NULL

    if (lpdoc == lpHeadDoc)
        lpHeadDoc = lpdoc->lpNextDoc;

    if (lpdoc == lpTailDoc)
        lpTailDoc = lpdoc->lpPrevDoc;

    if (lpdoc->lpPrevDoc)
        lpdoc->lpPrevDoc->lpNextDoc = lpdoc->lpNextDoc;

    if (lpdoc->lpNextDoc)
        lpdoc->lpNextDoc->lpPrevDoc = lpdoc->lpPrevDoc;

    GlobalUnlock (lpdoc->hdoc);
    GlobalFree (lpdoc->hdoc);

    // inform link manager
    return OLE_OK;
}


OLESTATUS FAR PASCAL OleRenameClientDoc (
    LHCLIENTDOC lhclientdoc,
    LPCSTR       lpNewDocName
){
    LPCLIENTDOC lpdoc;
    ATOM        aNewDoc;
    LPOLEOBJECT lpobj = NULL;

    if (!CheckClientDoc (lpdoc = (LPCLIENTDOC) lhclientdoc))
        return OLE_ERROR_HANDLE;

    FARPROBE_READ(lpNewDocName);

    aNewDoc = GlobalAddAtom (lpNewDocName);
    if (aNewDoc == lpdoc->aDoc) {
        if (aNewDoc)
            GlobalDeleteAtom (aNewDoc);
        return OLE_OK;
    }

    // Document name has changed. So, change the topic of all embedded objects
    if (lpdoc->aDoc)
        GlobalDeleteAtom (lpdoc->aDoc);
    lpdoc->aDoc = aNewDoc;

    while (lpobj = DocGetNextObject (lpdoc, lpobj)) {
        if (lpobj->ctype == CT_EMBEDDED)
            if (OleQueryReleaseStatus (lpobj) != OLE_BUSY)
                SetEmbeddedTopic ((LPOBJECT_LE) lpobj);
    }

    return OLE_OK;
}


OLESTATUS FAR PASCAL OleRevertClientDoc (
    LHCLIENTDOC lhclientdoc
){
    // if there is any handler dll that can be freed up, free it now.
    // Otherwise it might become too late if the app quits.
    if (iUnloadableDll)
        UnloadDll ();

    if (!CheckClientDoc ((LPCLIENTDOC) lhclientdoc))
        return OLE_ERROR_HANDLE;

    return OLE_OK;
}


OLESTATUS FAR PASCAL OleSavedClientDoc (
    LHCLIENTDOC lhclientdoc
){
    if (!CheckClientDoc ((LPCLIENTDOC) lhclientdoc))
        return OLE_ERROR_HANDLE;

    return OLE_OK;
}

OLESTATUS FAR PASCAL OleEnumObjects (
    LHCLIENTDOC         lhclientdoc,
    LPOLEOBJECT FAR *   lplpobj
){
    if (!CheckClientDoc ((LPCLIENTDOC) lhclientdoc))
        return OLE_ERROR_HANDLE;

    FARPROBE_WRITE(lplpobj);

    if (*lplpobj) {
        // we are making lhclientdoc field of the object NULL at deletion
        // time. The check (*lpobj->lhclientdoc != lhclientdoc) will take care
        // of the case where same chunk of memory is allocated again for the
        // same pointer and old contents are not erased yet.
        if (!FarCheckObject (*lplpobj)
                || ((*lplpobj)->lhclientdoc != lhclientdoc))
            return OLE_ERROR_OBJECT;
    }

    *lplpobj = DocGetNextObject ((LPCLIENTDOC) lhclientdoc, *lplpobj);
    return OLE_OK;
}



LPOLEOBJECT INTERNAL DocGetNextObject (
    LPCLIENTDOC lpdoc,
    LPOLEOBJECT lpobj
){
    if (!lpobj)
        return lpdoc->lpHeadObj;

    return lpobj->lpNextObj;
}


BOOL FARINTERNAL CheckClientDoc (
    LPCLIENTDOC lpdoc
){
    if (!CheckPointer(lpdoc, WRITE_ACCESS))
        return FALSE;

    if ((lpdoc->docId[0] == 'C') && (lpdoc->docId[1] == 'D'))
        return TRUE;
    return FALSE;
}


void FARINTERNAL DocAddObject (
    LPCLIENTDOC lpdoc,
    LPOLEOBJECT lpobj,
    LPCSTR      lpobjname
){
    if (lpobjname)
        lpobj->aObjName = GlobalAddAtom (lpobjname);
    else
        lpobj->aObjName = (ATOM)0;

    // Objects of a doc are doubly linked

    if (!lpdoc->lpHeadObj)
        lpdoc->lpHeadObj = lpdoc->lpTailObj = lpobj;
    else {
        lpdoc->lpTailObj->lpNextObj = lpobj;
        lpobj->lpPrevObj = lpdoc->lpTailObj;
        lpdoc->lpTailObj = lpobj;
    }
    lpobj->lhclientdoc = (LHCLIENTDOC)lpdoc;
}


void FARINTERNAL DocDeleteObject (
    LPOLEOBJECT lpobj
){
    LPCLIENTDOC lpdoc;

    if (!(lpdoc = (LPCLIENTDOC) lpobj->lhclientdoc))
        return;

    if (lpobj->aObjName) {
        GlobalDeleteAtom (lpobj->aObjName);
        lpobj->aObjName = (ATOM)0;
    }

    // Remove this obj from object chain of the relevant client doc.
    // The objects of a doc are doubly linked.

    if (lpdoc->lpHeadObj == lpobj)
        lpdoc->lpHeadObj = lpobj->lpNextObj;

    if (lpdoc->lpTailObj == lpobj)
        lpdoc->lpTailObj = lpobj->lpPrevObj;

    if (lpobj->lpPrevObj)
        lpobj->lpPrevObj->lpNextObj = lpobj->lpNextObj;

    if (lpobj->lpNextObj)
        lpobj->lpNextObj->lpPrevObj = lpobj->lpPrevObj;

    lpobj->lhclientdoc = 0;
}

