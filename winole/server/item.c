/****************************** Module Header ******************************\
* Module Name: Item.c Object(item) main module
*
* Purpose: Includes All the object releated routiens.
*
* Created: Oct 1990.
*
* Copyright (c) 1990 - 1992  Microsoft Corporation
*
* History:
*    Raor (../10/1990)    Designed, coded
*    curts created portable version for WIN16/32
*
\***************************************************************************/


#include "windows.h"
#include "cmacs.h"
#include "ole.h"
#include "dde.h"
#include "srvr.h"

extern HANDLE   hdllInst;
extern FARPROC  lpFindItemWnd;
extern FARPROC  lpItemCallBack;
extern FARPROC  lpSendDataMsg;
extern FARPROC  lpSendRenameMsg;
extern FARPROC  lpDeleteClientInfo;
extern FARPROC  lpEnumForTerminate;


extern  ATOM    cfNative;
extern  ATOM    cfBinary;
extern  ATOM    aClose;
extern  ATOM    aChange;
extern  ATOM    aSave;
extern  ATOM    aEditItems;
extern  ATOM    aStdDocName;

extern  WORD    cfLink;
extern  WORD    cfOwnerLink;
HWND            hwndItem;
HANDLE          hddeRename;
HWND            hwndRename;

UINT            enummsg;
UINT            enuminfo;
LPOLEOBJECT     enumlpoleobject;
OLECLIENTVTBL   clVtbl;
BOOL            bClientUnlink;

BOOL            fAdviseSaveDoc;
BOOL            fAdviseSaveItem;

char *  stdStrTable[STDHOSTNAMES+1] =
        {
            NULL,
            "StdTargetDevice",
            "StdDocDimensions",
            "StdColorScheme",
            "StdHostNames"
        };

void ChangeOwner (HANDLE hmfp);

// !!!change child enumeration.
// !!!No consistency in errors (Sometimes Bools and sometimes OLESTATUS).


//SearchItem: Searches for a given item in a document tree.
//If found, returns the corresponding child windows handle.

HWND  INTERNAL SearchItem (
    LPDOC lpdoc,
    LPSTR lpitemname
){
    ATOM  aItem;

    Puts ("SearchItem");

    // If the item passed is an atom, get its name.
    if (!HIWORD(lpitemname))
        aItem = (ATOM) (LOWORD(lpitemname));
    else if (!lpitemname[0])
        aItem = (ATOM)0;
    else
        aItem = GlobalFindAtom (lpitemname);

    hwndItem = NULL;

    // !!! We should avoid hwndItem static. It should not cause
    // any problems since while enumerating we will not be calling
    // any window procs  or no PostMessages are entertained.

    EnumChildWindows (lpdoc->hwnd, (WNDENUMPROC)lpFindItemWnd,
        MAKELONG (aItem, ITEM_FIND));

    return hwndItem;

}

// FindItem: Given the itemname and the document handle,
// searches for the the item (object) in the document tree.
// Items are child windows for the document window.

// !!! change the child windows to somekind of
// linked lists at the item level. This will free up
// the space taken by the item windows.

int  INTERNAL FindItem (
    LPDOC          lpdoc,
    LPSTR          lpitemname,
    LPCLIENT FAR * lplpclient
){
    LPCLIENT    lpclient;
    HWND        hwnd;
    char        buf[MAX_STR];

    Puts ("FindItem");

    hwnd = SearchItem (lpdoc, lpitemname);

    if (!HIWORD(lpitemname)){
        if (LOWORD(lpitemname)){
            if (!GlobalGetAtomName ((ATOM)LOWORD(lpitemname), (LPSTR)buf, MAX_STR))
                 return OLE_ERROR_BLANK;
        }
        else
            buf[0] = '\0';

        lpitemname = (LPSTR)buf;
    }

    if (hwnd) {
        // we found the item window
        lpclient = (LPCLIENT)GetWindowLongPtr (hwnd, 0);

            *lplpclient = lpclient;
            return OLE_OK;

    }

    // Item (object)window is not create yet. Let us create one.
    return RegisterItem ((LHDOC)lpdoc, lpitemname, lplpclient, TRUE);
}



//RegisterItem: Given the document handle and the item string
//creates item with the given document.

int  INTERNAL RegisterItem (
    LHDOC          lhdoc,
    LPSTR          lpitemname,
    LPCLIENT FAR * lplpclient,
    BOOL           bSrvr
){
    LPDOC           lpdoc;
    HANDLE          hclient  = NULL;
    LPCLIENT        lpclient = NULL;
    OLESTATUS        retval   = OLE_ERROR_MEMORY;
    LPOLESERVERDOC  lpoledoc;
    LPOLEOBJECT     lpoleobject = NULL;


    Puts ("CreateItem");

    lpdoc = (LPDOC)lhdoc;

    // First create the callback client structure.

    hclient = GlobalAlloc (GMEM_MOVEABLE | GMEM_ZEROINIT | GMEM_DDESHARE, sizeof (CLIENT));
    if(!(hclient && (lpclient = (LPCLIENT)GlobalLock (hclient))))
        goto errRtn;

    lpclient->hclient       = hclient;
    hclient                 = NULL;

    if (!HIWORD(lpitemname)) {
        ASSERT (!bSrvr, "invalid lpitemname in RegisterItem\n");
        lpclient->aItem = LOWORD(lpitemname);
    }
    else if (!lpitemname[0])
        lpclient->aItem = (ATOM)0;
    else
        lpclient->aItem = GlobalAddAtom (lpitemname);

    lpclient->oleClient.lpvtbl = &clVtbl;
    lpclient->oleClient.lpvtbl->CallBack = ItemCallBack;

    lpoledoc = lpdoc->lpoledoc;

    // Call the server app to create its own object structure and link
    // it to the given document.

    // Call the server if the item is not one of the standard items.

    if (bSrvr) {
        retval = (*lpoledoc->lpvtbl->GetObject)(lpoledoc, lpitemname,
                    (LPOLEOBJECT FAR *)&lpoleobject, (LPOLECLIENT)lpclient);
        if (retval != OLE_OK)
            goto errRtn;
    }

    lpclient->lpoleobject   = lpoleobject;

    lpclient->hwnd = CreateWindowEx (WS_EX_NOPARENTNOTIFY,"ItemWndClass", "ITEM",
                        WS_CHILD,0,0,0,0,lpdoc->hwnd,NULL, hdllInst, NULL);

    if (lpclient->hwnd == NULL)
        goto errRtn;

    // save the ptr to the item in the window.
    SetWindowLongPtr (lpclient->hwnd, 0, (LONG_PTR)lpclient);
    *lplpclient = lpclient;
    return OLE_OK;

errRtn:

    if (lpclient)
        RevokeObject ((LPOLECLIENT)lpclient, FALSE);

    else {
        if(hclient)
            GlobalFree (hclient);
    }

    return retval;

}


OLESTATUS  FAR PASCAL OleRevokeObject (
    LPOLECLIENT    lpoleclient
){
    return RevokeObject (lpoleclient, TRUE);

}

// OleRevokeObject: Revokes an object (unregisres an object
// from the document tree.

OLESTATUS  INTERNAL RevokeObject (
    LPOLECLIENT    lpoleclient,
    BOOL           bUnlink
){
    HANDLE      hclient;
    LPCLIENT    lpclient;

    lpclient = (LPCLIENT)lpoleclient;

    PROBE_WRITE(lpoleclient);
    if (lpclient->lpoleobject) {
       // first call the object for deletetion.

        (*lpclient->lpoleobject->lpvtbl->Release)(lpclient->lpoleobject);

    }

    if (ISATOM(lpclient->aItem)) {
        GlobalDeleteAtom (lpclient->aItem);
        lpclient->aItem = (ATOM)0;
    }

    if (lpclient->hwnd) {
        SetWindowLongPtr (lpclient->hwnd, 0, (LONG_PTR)NULL);

        // another static for enumerating the properties.
        // we need to change these .
        bClientUnlink = bUnlink;

        EnumProps(lpclient->hwnd, (PROPENUMPROC)lpDeleteClientInfo);
        // post all the messages with yield which have been collected in enum
        // UnblockPostMsgs (lpclient->hwnd, FALSE);
        DestroyWindow (lpclient->hwnd);
    }

    GlobalUnlock (hclient = lpclient->hclient);
    GlobalFree (hclient);
    return OLE_OK;

}

BOOL    FAR PASCAL  DeleteClientInfo (
    HWND    hwnd,
    LPSTR   lpstr,
    HANDLE  hclinfo
){
    PCLINFO     pclinfo = NULL;
    HWND        hwndDoc;
    LPDOC       lpdoc;

    // delete the printer dev info block
    if(pclinfo = (PCLINFO)LocalLock (hclinfo)){
        if(pclinfo->hdevInfo)
            GlobalFree (pclinfo->hdevInfo);


        if (bClientUnlink) {
            // terminate the conversation for the client.
            TerminateDocClients ((hwndDoc = GetParent(hwnd)), NULL, pclinfo->hwnd);
            lpdoc = (LPDOC)GetWindowLongPtr (hwndDoc, 0);
            // for some reason this delete is gving circular lists for properties

            //DeleteClient (hwndDoc, pclinfo->hwnd);
            //lpdoc->cClients--;
        }
        LocalUnlock (hclinfo);
    }
    LocalFree (hclinfo);

    RemoveProp(hwnd, lpstr);
    return TRUE;
}




// Call back for the Object windows numeration. data  field
// has the command and the extra information


BOOL    FAR PASCAL FindItemWnd(
    HWND    hwnd,
    LONG    data
){

    LPCLIENT    lpclient;
    int         cmd;
    HANDLE      hclinfo;
    PCLINFO     pclinfo = NULL;


    lpclient = (LPCLIENT)GetWindowLongPtr (hwnd, 0);

    cmd = HIWORD(data);
    switch (cmd) {
        case    ITEM_FIND:
            if (lpclient->aItem == (ATOM)(LOWORD (data))) {
                // we found the window we required. Remember the
                // object window.

                hwndItem = hwnd;
                return FALSE; // terminate enumeration.

            }
            break;

        case    ITEM_SAVED:
            if (lpclient->lpoleobject) {
                if (ItemCallBack ((LPOLECLIENT) lpclient, OLE_SAVED,
                        lpclient->lpoleobject) == OLE_ERROR_CANT_UPDATE_CLIENT)
                    fAdviseSaveDoc = FALSE;
            }
            break;

        case    ITEM_DELETECLIENT:

            // delete the client from our list if we have one

            hclinfo = FindClient (hwnd, (HWND) (LOWORD(data)));
            if (hclinfo){
                // delete the printer dev info block
                if(pclinfo = (PCLINFO)LocalLock (hclinfo)){
                    if(pclinfo->hdevInfo)
                        GlobalFree (pclinfo->hdevInfo);
                    LocalUnlock (hclinfo);
                }
                LocalFree (hclinfo);
                DeleteClient ( hwnd, (HWND) (LOWORD(data)));
            }
            break;

        case    ITEM_DELETE:
            // delete the client it self.
            RevokeObject ((LPOLECLIENT)lpclient, FALSE);
            break;

    }
    return TRUE;        // continue enumeration.
}



//DeleteFromItemsList: Deletes a client from the object lists of
//all the objects of a given  document. Thie client possibly
//is terminating the conversation with our doc window.


void INTERNAL   DeleteFromItemsList (
    HWND    hwndDoc,
    HWND    hwndClient
){

    EnumChildWindows (hwndDoc, (WNDENUMPROC)lpFindItemWnd,
        MAKELONG (hwndClient, ITEM_DELETECLIENT));

}


// DeleteAllItems: Deletes all the objects of a given
// document window.


void INTERNAL   DeleteAllItems (
    HWND    hwndDoc
){

    EnumChildWindows (hwndDoc, (WNDENUMPROC)lpFindItemWnd, MAKELONG (NULL, ITEM_DELETE));

}


// Object widnow proc:

LRESULT FAR PASCAL ItemWndProc(
    HWND   hwnd,
    UINT   msg,
    WPARAM wParam,
    LPARAM lParam
){

    LPCLIENT    lpclient;

    lpclient = (LPCLIENT)GetWindowLongPtr (hwnd, 0);

    switch (msg) {
       case WM_DESTROY:
            DEBUG_OUT("Item: Destroy window",0)

            break;
       default:
            DEBUG_OUT("item:  Default message",0)
            return DefWindowProc (hwnd, msg, wParam, lParam);

    }
    return 0L;

}

// PokeData: Prepares and gives the data to the server app thru
// the SetData object method.

OLESTATUS    INTERNAL PokeData (
    LPDOC       lpdoc,
    HWND        hwndClient,
    LPARAM      lparam
){
    OLESTATUS       retval = OLE_ERROR_MEMORY;
    LPCLIENT        lpclient;
    DDEPOKE FAR *   lpPoke = NULL;
    HANDLE          hPoke = NULL;
    HANDLE          hnew   = NULL;
    OLECLIPFORMAT   format;
    BOOL            fRelease = FALSE;
    ATOM            aItem = GET_WM_DDE_POKE_ITEM((WPARAM)NULL,lparam);

    UNREFERENCED_PARAMETER(hwndClient);

    // Get the object handle first. Look in the registration
    // tree and if one is not created otherwise create one.

    retval = FindItem (lpdoc, (LPSTR) MAKEINTATOM(aItem),
                (LPCLIENT FAR *)&lpclient);

    if (retval != OLE_OK)
        goto errRtn;

    hPoke = GET_WM_DDE_POKE_HDATA((WPARAM)NULL,lparam);
    if(!(hPoke && (lpPoke = (DDEPOKE FAR *) GlobalLock (hPoke))))
        goto errRtn;

    GlobalUnlock (hPoke);

    format   = lpPoke->cfFormat;
    fRelease = lpPoke->fRelease;

    // We found the item. Now prepare the data to be given to the object
    if (!(hnew = MakeItemData (lpPoke, hPoke, format)))
        goto errRtn;

    // Now send the data to the object


    retval = (*lpclient->lpoleobject->lpvtbl->SetData) (lpclient->lpoleobject,
                                                format, hnew);

    // We free the data if server returns OLE_ERROR_SETDATA_FORMAT.
    // Otherwise server must've deleted it.

    if (retval == OLE_ERROR_SETDATA_FORMAT) {
        if (!FreeGDIdata (hnew, format))
            GlobalFree (hnew);
    }


errRtn:
    if (retval == OLE_OK && fRelease) {
        if (hPoke)
            GlobalFree (hPoke);
    }

    return retval;
}




OLESTATUS  INTERNAL UnAdviseData (
    LPDOC   lpdoc,
    HWND    hwndClient,
    LPARAM  lparam
){
    char      buf[MAX_STR];
    int       options;
    LPCLIENT  lpclient;
    OLESTATUS retval  = OLE_ERROR_MEMORY;
    HANDLE    hclinfo = NULL;
    PCLINFO   pclinfo = NULL;

    UNREFERENCED_PARAMETER(hwndClient);

    if (!(HIWORD (lparam)))
        buf[0] = '\0';
    else
        if (!GlobalGetAtomName ((ATOM)(HIWORD (lparam)), (LPSTR)buf, MAX_STR))
            return OLE_ERROR_BLANK;

    // Scan for the advise options like "Close", "Save" etc
    // at the end of the item.

    if((retval = ScanItemOptions ((LPSTR)buf, (int far *)&options)) !=
            OLE_OK)
        goto errRtn;


    if (buf[0] == '\0') {
        // Unadvise for null should terminate all the advises
        DeleteFromItemsList (lpdoc->hwnd, hwndClient);
        return OLE_OK;
    }

    // Now get the corresponding object.
    retval = FindItem (lpdoc, (LPSTR)buf, (LPCLIENT FAR *)&lpclient);
    if (retval != OLE_OK)
        goto errRtn;


    // Find the client structure to be attcahed to the object.
    if ((hclinfo = FindClient (lpclient->hwnd, hwndClient)) == NULL ||
        (pclinfo = (PCLINFO) LocalLock (hclinfo)) == NULL ){
            retval = OLE_ERROR_MEMORY;
            goto errRtn;
    }

    pclinfo->options &= (~(0x0001 << options));

errRtn:
    if (pclinfo)
        LocalUnlock (hclinfo);
    return retval;

}



// AdviseStdItems: This routine takes care of the DDEADVISE for a
//particular object in given document. Creates a client strutcure
//and attaches to the property list of the object window.

OLESTATUS INTERNAL  AdviseStdItems (
    LPDOC       lpdoc,
    HWND        hwndClient,
    LPARAM      lparam,
    BOOL FAR *  lpfack
){
    HANDLE          hopt   = GET_WM_DDE_ADVISE_HOPTIONS((WPARAM)NULL,lparam);
    ATOM            aItem  = GET_WM_DDE_ADVISE_ITEM((WPARAM)NULL,lparam);
    DDEADVISE FAR  *lpopt;
    OLESTATUS       retval = OLE_ERROR_MEMORY;

    if(!(lpopt = (DDEADVISE FAR *) GlobalLock (hopt)))
        goto errrtn;

    *lpfack = lpopt->fAckReq;
    retval = SetStdInfo (lpdoc, hwndClient, (LPSTR)"StdDocumentName",  NULL);

    if (lpopt)
        GlobalUnlock (hopt);

errrtn:

    if (retval == OLE_OK)
        // !!! make sure that we have to free the data for error case
        GlobalFree (hopt);
    return retval;
}



//AdviseData: This routine takes care of the DDEADVISE for a
//particular object in given document. Creates a client strutcure
//and attaches to the property list of the object window.

OLESTATUS INTERNAL  AdviseData (
    LPDOC       lpdoc,
    HWND        hwndClient,
    LPARAM      lparam,
    BOOL FAR *  lpfack
){
    HANDLE          hopt   = GET_WM_DDE_ADVISE_HOPTIONS((WPARAM)NULL,lparam);
    ATOM            aitem  = GET_WM_DDE_ADVISE_ITEM((WPARAM)NULL,lparam);
    DDEADVISE FAR   *lpopt = NULL;
    OLECLIPFORMAT   format = 0;
    char            buf[MAX_STR];
    int             options;
    LPCLIENT        lpclient;
    OLESTATUS       retval  = OLE_ERROR_MEMORY;
    HANDLE          hclinfo = NULL;
    PCLINFO         pclinfo = NULL;

    if(!(lpopt = (DDEADVISE FAR *) GlobalLock (hopt)))
        goto errRtn;

    if (!aitem)
        buf[0] = '\0';
    else
    {
        if (!GlobalGetAtomName (aitem, (LPSTR)buf, MAX_STR))
        {
            retval = OLE_ERROR_BLANK;
            goto errRtn;
        }
    }

    // Scan for the advise options like "Close", "Save" etc
    // at the end of the item.

    if((retval = ScanItemOptions ((LPSTR)buf, (int far *)&options)) !=
            OLE_OK)
        goto errRtn;


    // Now get the corresponding object.
    retval = FindItem (lpdoc, (LPSTR)buf, (LPCLIENT FAR *)&lpclient);
    if (retval != OLE_OK)
        goto errRtn;

    if (!IsFormatAvailable (lpclient, lpopt->cfFormat)){
        retval = OLE_ERROR_DATATYPE;       // this format is not supported;
        goto errRtn;
    }

    *lpfack = lpopt->fAckReq;

    // Create the client structure to be attcahed to the object.
    if (!(hclinfo = FindClient (lpclient->hwnd, hwndClient)))
        hclinfo = LocalAlloc (LMEM_MOVEABLE | LMEM_ZEROINIT, sizeof (CLINFO));

    if (hclinfo == NULL || (pclinfo = (PCLINFO) LocalLock (hclinfo)) == NULL){
        retval = OLE_ERROR_MEMORY;
        goto errRtn;
    }

    // Remember the client window (Needed for sending DATA later on
    // when the data change message comes from the server)

    pclinfo->hwnd = hwndClient;
    if (lpopt->cfFormat == (SHORT)cfNative)
        pclinfo->bnative = TRUE;
    else
        pclinfo->format = lpopt->cfFormat;

    // Remeber the data transfer options.
    pclinfo->options |= (0x0001 << options);
    pclinfo->bdata   = !lpopt->fDeferUpd;
    LocalUnlock (hclinfo);
    pclinfo = (PCLINFO)NULL;


    // if the entry exists already, delete it.
    DeleteClient (lpclient->hwnd, hwndClient);

    // Now add this client to item client list
    // !!! This error recovery is not correct.
    if(!AddClient (lpclient->hwnd, hwndClient, hclinfo))
        goto errRtn;


errRtn:
    if (lpopt)
        GlobalUnlock (hopt);

    if (pclinfo)
        LocalUnlock (hclinfo);

    if (retval == OLE_OK) {
        // !!! make sure that we have to free the data
        GlobalFree (hopt);

    }else {
        if (hclinfo)
            LocalFree (hclinfo);
    }
    return retval;

}

BOOL INTERNAL IsFormatAvailable (
    LPCLIENT        lpclient,
    OLECLIPFORMAT   cfFormat
){
      OLECLIPFORMAT  cfNext = 0;


      do{

        cfNext = (*lpclient->lpoleobject->lpvtbl->EnumFormats)
                                (lpclient->lpoleobject, cfNext);
        if (cfNext == cfFormat)
            return TRUE;

      }while (cfNext != 0);

      return FALSE;
}

//ScanItemOptions: Scan for the item options like Close/Save etc.

OLESTATUS INTERNAL ScanItemOptions (
    LPSTR   lpbuf,
    int far *lpoptions
){
    ATOM    aModifier;

    *lpoptions = OLE_CHANGED;
    while ( *lpbuf && *lpbuf != '/')
           lpbuf++;

    // no modifier same as /change

    if (*lpbuf == '\0')
        return OLE_OK;

    *lpbuf++ = '\0';        // seperate out the item string
                            // We are using this in the caller.

    if (!(aModifier = GlobalFindAtom (lpbuf)))
        return OLE_ERROR_SYNTAX;

    if (aModifier == aChange)
        return OLE_OK;

    // Is it a save?
    if (aModifier == aSave){
        *lpoptions = OLE_SAVED;
        return  OLE_OK;
    }
    // Is it a Close?
    if (aModifier == aClose){
        *lpoptions = OLE_CLOSED;
        return OLE_OK;
    }

    // unknow modifier
    return OLE_ERROR_SYNTAX;

}

//RequestData: Sends data in response to a DDE Request message.
// for  agiven doc and an object.

OLESTATUS INTERNAL   RequestData (
    LPDOC       lpdoc,
    HWND        hwndClient,
    LPARAM      lparam,
    LPHANDLE    lphdde
){
    OLESTATUS   retval = OLE_OK;
    HANDLE      hdata;
    LPCLIENT    lpclient;
    char        buf[20];

    // If edit environment Send data if we can
    if ((HIWORD (lparam)) == aEditItems)
        return RequestDataStd (lparam, lphdde);

    // Get the object.
    retval = FindItem (lpdoc, (LPSTR) MAKEINTATOM(HIWORD(lparam)),
                (LPCLIENT FAR *)&lpclient);
    if (retval != OLE_OK)
        goto errRtn;

    retval = OLE_ERROR_DATATYPE;
    if (!IsFormatAvailable (lpclient, (OLECLIPFORMAT)(LOWORD (lparam))))
        goto errRtn;

    // Now ask the item for the given format  data

    MapToHexStr ((LPSTR)buf, hwndClient);
    SendDevInfo (lpclient, (LPSTR)buf);

    retval = (*lpclient->lpoleobject->lpvtbl->GetData) (lpclient->lpoleobject,
                (OLECLIPFORMAT)(LOWORD(lparam)), (LPHANDLE)&hdata);

    if (retval != OLE_OK)
        goto errRtn;

    if (LOWORD(lparam) == CF_METAFILEPICT)
        ChangeOwner (hdata);

    // Duplicate the DDE data
    if (MakeDDEData(hdata, (OLECLIPFORMAT)(LOWORD (lparam)), lphdde, TRUE)){
        // !!! Why do we have to duplicate the atom
        DuplicateAtom ((ATOM)(HIWORD (lparam)));
        return OLE_OK;
    }
    else
       return OLE_ERROR_MEMORY;

errRtn:
    return retval;

}

#ifdef WIN32
HANDLE INTERNAL BmDuplicate (
   HBITMAP     hold
){
    HANDLE      hMem;
    LPSTR       lpMem;
    LONG        retVal = TRUE;
    DWORD       dwSize;
    BITMAP      bm;

     // !!! another way to duplicate the bitmap

    GetObject (hold, sizeof(BITMAP), (LPSTR) &bm);
    dwSize = ((DWORD) bm.bmHeight) * ((DWORD) bm.bmWidthBytes) *
             ((DWORD) bm.bmPlanes) * ((DWORD) bm.bmBitsPixel);

    if (!(hMem = GlobalAlloc (GMEM_MOVEABLE | GMEM_ZEROINIT | GMEM_DDESHARE, dwSize+sizeof(BITMAP)+sizeof(DWORD))))
        return NULL;

    if (!(lpMem = (LPBYTE)GlobalLock (hMem))){
        GlobalFree (hMem);
        return NULL;
    }
    *((DWORD FAR *) lpMem) = dwSize;
    *(BITMAP FAR *) (lpMem+sizeof(DWORD)) = bm;
    lpMem += (sizeof(DWORD) + sizeof (BITMAP));
    dwSize = GetBitmapBits (hold, 0, NULL);
    retVal = GetBitmapBits (hold, dwSize, lpMem);

    GlobalUnlock (hMem);
    return hMem;


}
#endif

//MakeDDEData: Create a Global DDE data handle from the server
// app data handle.

BOOL    INTERNAL MakeDDEData (
    HANDLE        hdata,
    OLECLIPFORMAT format,
    LPHANDLE      lph,
    BOOL          fResponse
){
    DWORD       size;
    HANDLE      hdde   = NULL;
    DDEDATA FAR *lpdata= NULL;
    BOOL        bnative;
    LPSTR       lpdst;
    LPSTR       lpsrc;

    if (!hdata) {
        *lph = NULL;
        return TRUE;
    }

    if (bnative = !(format == CF_METAFILEPICT || format == CF_DIB ||
                            format == CF_BITMAP || format == CF_ENHMETAFILE))
       size = (DWORD)GlobalSize (hdata) + sizeof (DDEDATA);
    else
#ifdef WIN32HACK
    {
       if (format == CF_BITMAP)
           hdata = BmDuplicate(hdata);

           size = sizeof (HANDLE_PTR) + sizeof (DDEDATA);
    }
#else
           size = sizeof (HANDLE_PTR) + sizeof (DDEDATA);
#endif


    hdde = (HANDLE) GlobalAlloc (GMEM_DDESHARE | GMEM_ZEROINIT, size);
    if (hdde == NULL || (lpdata = (DDEDATA FAR *) GlobalLock (hdde)) == NULL)
        goto errRtn;

    // set the data otions. Ask the client to delete
    // it always.

    lpdata->fRelease  = TRUE;  // release the data
    lpdata->cfFormat  = (WORD)format;
    lpdata->fResponse = (WORD)fResponse;

    if (!bnative) {
        // If not native, stick in the handle what the server gave us.
        
        // Com1x bug 23211: data misalignment: truncate handle to 32 bits on Win64 
        // because a) handle is only 32 bit significant; b) this was causing data misalignment 
        // error; c) we're only allocating 32 bits for it above. 
#ifdef _WIN64
        if (format == CF_METAFILEPICT)
            *(void* __unaligned*)lpdata->Value = hdata;
    	else
#endif
            *(LONG*)lpdata->Value = HandleToLong(hdata);
   	}
    else {
        // copy the native data junk here.
        lpdst = (LPSTR)lpdata->Value;
        if(!(lpsrc = (LPSTR)GlobalLock (hdata)))
            goto errRtn;

         size -= sizeof (DDEDATA);
         UtilMemCpy (lpdst, lpsrc, size);
         GlobalUnlock (hdata);
         GlobalFree (hdata);

    }

    GlobalUnlock (hdde);
    *lph = hdde;
    return TRUE;

errRtn:
    if (lpdata)
        GlobalUnlock (hdde);

    if (hdde)
        GlobalFree (hdde);

    if (bnative)
         GlobalFree (hdata);

    return FALSE;
}


// ItemCallback: Calback routine for the server to inform the
// data changes. When the change message is received, DDE data
// message is sent to each of the clients depending on the
// options.

int FAR PASCAL  ItemCallBack (
    LPOLECLIENT      lpoleclient,
    OLE_NOTIFICATION msg,        // notification message
    LPOLEOBJECT      lpoleobject
){

    LPCLIENT    lpclient;
    int         retval = OLE_OK;
    HANDLE      hdata  = NULL;
    LPSTR       lpdata = NULL;
    LPDOC       lpdoc;
    HWND        hStdWnd;

    lpclient  = (LPCLIENT)lpoleclient;
    lpdoc = (LPDOC)GetWindowLongPtr (GetParent (lpclient->hwnd), 0);

    if (msg == OLE_RENAMED) {
        if (IsFormatAvailable (lpclient, cfLink)) {

            // Get the link data.

            retval = (*lpoleobject->lpvtbl->GetData) (lpoleobject,
                                cfLink, (LPHANDLE)&hdata);
        }
        else {
            if(IsFormatAvailable (lpclient, cfOwnerLink)) {

                // Get the link data.
                retval = (*lpoleobject->lpvtbl->GetData) (lpoleobject,
                                    cfOwnerLink, (LPHANDLE)&hdata);
            } else
                retval = OLE_ERROR_DATATYPE;
        }

        if (retval != OLE_OK)
            goto errrtn;

        if (!(lpdata = (LPSTR)GlobalLock (hdata)))
            goto errrtn;

        if (lpdoc->aDoc) {
            GlobalDeleteAtom (lpdoc->aDoc);
            lpdoc->aDoc = (ATOM)0;
        }

        // Move the string to the beginning and still terminated by null;
        lstrcpy (lpdata, lpdata + lstrlen (lpdata) + 1);
        lpdoc->aDoc = GlobalAddAtom (lpdata);

        // Now make the DDE data block
        GlobalUnlock (hdata);
        lpdata = NULL;

        // find if any StdDocName item is present at all
        if (!(hStdWnd = SearchItem (lpdoc, (LPSTR) MAKEINTATOM(aStdDocName))))
            GlobalFree (hdata);
        else {

            // hdata is freed by Makeddedata
            if (!MakeDDEData (hdata, cfBinary, (LPHANDLE)&hddeRename,
                        FALSE)) {
                retval = OLE_ERROR_MEMORY;
                goto errrtn;
            }

            EnumProps(hStdWnd, (PROPENUMPROC)lpSendRenameMsg);
            // post all the messages with yield which have been collected in enum
            // UnblockPostMsgs (hStdWnd, FALSE);
            GlobalFree (hddeRename);
        }

        // static. Avoid this. This may not cause any problems for now.
        // if there is any better way, change it.
        hwndRename = hStdWnd;

        // Post termination for each of the doc clients.
        EnumProps(lpdoc->hwnd, (PROPENUMPROC)lpEnumForTerminate);

        lpdoc->fEmbed = FALSE;

        // post all the messages with yield which have been collected in enum
        // UnblockPostMsgs (lpdoc->hwnd, FALSE);
        return OLE_OK;

     errrtn:
        if (lpdata)
            GlobalUnlock (hdata);

        if (hdata)
            GlobalFree (hdata);

        return retval;

    } else {

        // !!! any better way to do instead of putting in static
        // (There may not be any problems since we are not allowing
        // any messages to get thru while we are posting messages).


        if ((enummsg = msg) == OLE_SAVED)
            fAdviseSaveItem = FALSE;

        enumlpoleobject = lpoleobject;

        // Enumerate all the clients and send DDE_DATA if necessary.
        EnumProps(lpclient->hwnd, (PROPENUMPROC)lpSendDataMsg);
        // post all the messages with yield which have been collected in enum
        // UnblockPostMsgs (lpclient->hwnd, FALSE);

        if ((msg == OLE_SAVED) && lpdoc->fEmbed && !fAdviseSaveItem)
            return OLE_ERROR_CANT_UPDATE_CLIENT;

        return OLE_OK;
    }
}


BOOL    FAR PASCAL  EnumForTerminate (
    HWND    hwnd,
    LPSTR   lpstr,
    HANDLE  hdata
){
    LPDOC   lpdoc;

    UNREFERENCED_PARAMETER(lpstr);

    lpdoc = (LPDOC)GetWindowLongPtr (hwnd , 0);

    // This client is in the rename list. So, no terminate
    if(hwndRename && FindClient (hwndRename, (HWND)hdata))
        return TRUE;

    if (PostMessageToClientWithBlock ((HWND)hdata, WM_DDE_TERMINATE, (WPARAM)hwnd, (LPARAM)0))
        lpdoc->termNo++;

    //DeleteClient (hwnd, (HWND)hdata);
    //lpdoc->cClients--;
    return TRUE;
}


BOOL    FAR PASCAL  SendRenameMsg (
    HWND    hwnd,
    LPSTR   lpstr,
    HANDLE  hclinfo
){
    ATOM    aData       = (ATOM)0;
    HANDLE  hdde        = NULL;
    PCLINFO pclinfo     = NULL;
    HWND    hwndClient;
    LPARAM  lParamNew;

    UNREFERENCED_PARAMETER(lpstr);

    if (!(pclinfo = (PCLINFO) LocalLock (hclinfo)))
        goto errrtn;

    // Make the item atom with the options.
    aData =  DuplicateAtom (aStdDocName);
    hdde  = DuplicateData (hddeRename);

    hwndClient  = pclinfo->hwnd;
    LocalUnlock (hclinfo);

    // Post the message
    lParamNew = MAKE_DDE_LPARAM(WM_DDE_DATA,hdde,aData);
    if (!PostMessageToClientWithBlock (hwndClient,WM_DDE_DATA,
            (WPARAM)GetParent(hwnd),lParamNew))
    {
        DDEFREE(WM_DDE_DATA,lParamNew);
        goto errrtn;
    }

    return TRUE;

errrtn:

    if (hdde)
        GlobalFree (hdde);
    if (aData)
        GlobalDeleteAtom (aData);

    return TRUE;

}



//SendDataMsg: Send data to the clients, if the data change options
//match the data advise options.

BOOL    FAR PASCAL  SendDataMsg (
    HWND    hwnd,
    LPSTR   lpstr,
    HANDLE  hclinfo
){
    PCLINFO    pclinfo = NULL;
    HANDLE      hdde    = NULL;
    ATOM        aData   = (ATOM)0;
    int         retval;
    HANDLE      hdata;
    LPCLIENT    lpclient;
    LPARAM      lParamNew;


    if (!(pclinfo = (PCLINFO) LocalLock (hclinfo)))
        goto errRtn;

    lpclient = (LPCLIENT)GetWindowLongPtr (hwnd, 0);

    // if the client dead, then no message
    if (!IsWindowValid(pclinfo->hwnd))
        goto errRtn;

    if (pclinfo->options & (0x0001 << enummsg)) {
        fAdviseSaveItem = TRUE;
        SendDevInfo (lpclient, lpstr);

        // send message if the client needs data for every change or
        // only for the selective ones he wants.

        // now look for the data option.
        if (pclinfo->bnative){
            // prepare native data
            if (pclinfo->bdata){

                // Wants the data with DDE_DATA message
                // Get native data from the server.

                retval = (*enumlpoleobject->lpvtbl->GetData) (enumlpoleobject,
                            cfNative, (LPHANDLE)&hdata);
                if (retval != OLE_OK)
                    goto errRtn;

                // Prepare the DDE data block.
                if(!MakeDDEData (hdata, cfNative, (LPHANDLE)&hdde, FALSE))
                    goto errRtn;

            }


            // Make the item atom with the options.
            aData =  MakeDataAtom (lpclient->aItem, enummsg);
            lParamNew = MAKE_DDE_LPARAM(WM_DDE_DATA,hdde,aData);
            // Post the message
            if (!PostMessageToClientWithBlock (pclinfo->hwnd, WM_DDE_DATA,
                    (WPARAM)GetParent(hwnd), lParamNew))
            {
                DDEFREE(WM_DDE_DATA,lParamNew);
                goto errRtn;
            }
            hdde = NULL;
            aData = (ATOM)0;
        }

        // Now post the data for the disply format
        if (pclinfo->format){
            if (pclinfo->bdata){
                retval = (*enumlpoleobject->lpvtbl->GetData) (enumlpoleobject,
                            pclinfo->format, (LPHANDLE)&hdata);

                if (retval != OLE_OK)
                    goto errRtn;

                if (pclinfo->format == CF_METAFILEPICT)
                    ChangeOwner (hdata);
Puts("sending metafile...");
                if(!MakeDDEData (hdata, pclinfo->format, (LPHANDLE)&hdde, FALSE))
                    goto errRtn;

            }
            // atom is deleted. So, we need to duplicate for every post
            aData =  MakeDataAtom (lpclient->aItem, enummsg);
            lParamNew = MAKE_DDE_LPARAM(WM_DDE_DATA,hdde,aData);
            // now post the message to the client;
            if (!PostMessageToClientWithBlock (pclinfo->hwnd, WM_DDE_DATA,
                    (WPARAM)GetParent(hwnd), lParamNew))
            {
                DDEFREE(WM_DDE_DATA,lParamNew);
                goto errRtn;
            }

            hdde = NULL;
            aData = (ATOM)0;

        }

    }


errRtn:
    if (pclinfo)
        LocalUnlock (hclinfo);

    if (hdde)
        GlobalFree (hdde);

    if (aData)
        GlobalDeleteAtom (aData);

    return TRUE;

}


// IsAdviseStdItems: returns true if the item is one of the standard items
// StdDocName;
BOOL    INTERNAL IsAdviseStdItems (
    ATOM   aItem
){

    if ( aItem == aStdDocName)
        return TRUE;
    else
        return FALSE;
}

// GetStdItemIndex: returns index to Stditems in the "stdStrTable" if the item
// is one of the standard items StdHostNames, StdTargetDevice,
// StdDocDimensions, StdColorScheme

int INTERNAL GetStdItemIndex (
    ATOM  aItem
){
    char  str[MAX_STR];

    if (!aItem)
        return 0;

    if (!GlobalGetAtomName (aItem, (LPSTR) str, MAX_STR))
        return 0;

    if (!lstrcmpi (str, stdStrTable[STDTARGETDEVICE]))
        return STDTARGETDEVICE;
    else if (!lstrcmpi (str, stdStrTable[STDHOSTNAMES]))
        return STDHOSTNAMES;
    else if (!lstrcmpi (str, stdStrTable[STDDOCDIMENSIONS]))
        return STDDOCDIMENSIONS;
    else if (!lstrcmpi (str, stdStrTable[STDCOLORSCHEME]))
        return STDCOLORSCHEME;

    return 0;
}

//
// The wire representation of STDDOCDIMENSIONS is a 16-bit
// format. This means instead of 4 longs, there are
// 4 shorts. This structure is used below to pick the data
// from the wire representation.
// backward compatible is the name of the game.
//
typedef struct tagRECT16
{
  SHORT left;
  SHORT top;
  SHORT right;
  SHORT bottom;

} RECT16, *LPRECT16;

// PokeStdItems: Pokes the data for the standard items.
// For StdHostnames, StdDocDimensions and SetColorScheme the data is
// sent immediately and for the the StdTargetDeviceinfo the
// data is set in each client block and the data is sent just
// before the GetData call for rendering the right data.


OLESTATUS    INTERNAL PokeStdItems (
    LPDOC   lpdoc,
    HWND    hwndClient,
    HANDLE  hdata,
    int     index
){
    DDEDATA FAR *   lpdata = NULL;
    HANDLE          hnew   = NULL;
    LPOLESERVERDOC  lpoledoc;
    LPHOSTNAMES     lphostnames;
    OLESTATUS       retval = OLE_ERROR_MEMORY;
    OLECLIPFORMAT   format;
    BOOL            fRelease;
    RECT            rcDoc;

    if(!(hdata && (lpdata = (DDEDATA FAR *)GlobalLock (hdata))))
        goto errRtn;

    format   = lpdata->cfFormat;
    fRelease = lpdata->fRelease;

#ifdef FIREWALSS
    ASSERT (format == cfBinary, "Format is not binary");
#endif

    // we have extracted the data successfully.
    lpoledoc = lpdoc->lpoledoc;

    if (index == STDHOSTNAMES){
        lphostnames = (LPHOSTNAMES)lpdata->Value;
        retval = (*lpoledoc->lpvtbl->SetHostNames)(lpdoc->lpoledoc,
                       (LPSTR)lphostnames->data,
                       ((LPSTR)lphostnames->data) +
                        lphostnames->documentNameOffset);
        goto end;
    }

    if (index == STDDOCDIMENSIONS){
        rcDoc.left   = 0;
        rcDoc.top    = ((LPRECT16)(lpdata->Value))->top;
        rcDoc.bottom = 0;
        rcDoc.right  = ((LPRECT16)lpdata->Value)->left;

        retval = (*lpoledoc->lpvtbl->SetDocDimensions)(lpdoc->lpoledoc,
                                            (LPRECT)&rcDoc);

        goto end;

    }

    if (index == STDCOLORSCHEME) {
        retval = (*lpoledoc->lpvtbl->SetColorScheme)(lpdoc->lpoledoc,
                                            (LPLOGPALETTE) lpdata->Value);
        goto end;
    }

    // case of the printer decvice info

    if (!(hnew = MakeItemData ((DDEPOKE FAR *)lpdata, hdata, format)))
        goto errRtn;

    // Go thru the all the items lists for this doc and replace the
    // printer device info information.
    // Free the block we duplicated.
    retval = SetStdInfo (lpdoc, hwndClient,
                (LPSTR) ULongToPtr(MAKELONG(STDTARGETDEVICE,0)),hnew);


end:
errRtn:
    if (hnew)
        // can only be global memory block
        GlobalFree (hnew);

    if (lpdata) {
        GlobalUnlock (hdata);
        if (retval == OLE_OK && fRelease)
            GlobalFree (hdata);
    }
    return retval;
}


// SetStdInfo: Sets the targetdevice info. Creates a client
// for "StdTargetDevice". This item is created only within the
// lib and it is never visible in server app. When the change
// message comes from the server app, before we ask for
// the data, we send the targetdevice info if there is
// info for the client whom we are trying to send the data
// on advise.


int INTERNAL   SetStdInfo (
    LPDOC   lpdoc,
    HWND    hwndClient,
    LPSTR   lpitemname,
    HANDLE  hdata
){
    HWND        hwnd;
    HANDLE      hclinfo  = NULL;
    PCLINFO    pclinfo = NULL;
    LPCLIENT    lpclient;
    OLESTATUS   retval   = OLE_OK;


    // first create/find the StdTargetDeviceItem.

    if ((hwnd = SearchItem (lpdoc, lpitemname))
                == NULL){
         retval = RegisterItem ((LHDOC)lpdoc, lpitemname,
                          (LPCLIENT FAR *)&lpclient, FALSE);

         if (retval != OLE_OK)
            goto errRtn;

         hwnd = lpclient->hwnd;

      }

    if(hclinfo = FindClient (hwnd, hwndClient)){
        if (pclinfo = (PCLINFO) LocalLock (hclinfo)){
            if (pclinfo->hdevInfo)
                GlobalFree (pclinfo->hdevInfo);
            pclinfo->bnewDevInfo = TRUE;
            if (hdata)
                pclinfo->hdevInfo = DuplicateData (hdata);
            else
                pclinfo->hdevInfo = NULL;
            pclinfo->hwnd = hwndClient;
            LocalUnlock (hclinfo);

            // We do not have to reset the client because we did not
            // change the handle it self.
        }
    } else {
        // Create the client structure to be attcahed to the object.
        hclinfo = LocalAlloc (LMEM_MOVEABLE | LMEM_ZEROINIT, sizeof (CLINFO));
        if (hclinfo == NULL || (pclinfo = (PCLINFO) LocalLock (hclinfo)) == NULL)
            goto errRtn;

        pclinfo->bnewDevInfo = TRUE;
        if (hdata)
            pclinfo->hdevInfo = DuplicateData (hdata);
        else
            pclinfo->hdevInfo = NULL;

        pclinfo->hwnd = hwndClient;
        LocalUnlock (hclinfo);


        // Now add this client to item client list
        // !!! This error recovery is not correct.
        if (!AddClient (hwnd, hwndClient, hclinfo))
            goto errRtn;

    }
    return OLE_OK;
errRtn:
    if (pclinfo)
        LocalUnlock (hclinfo);

    if (hclinfo)
        LocalFree (hclinfo);
    return OLE_ERROR_MEMORY;
}


// SendDevInfo: Sends targetdevice info to the  the object.
// Caches the last targetdevice info sent to the object.
// If the targetdevice block is same as the one in the
// cache, then no targetdevice info is sent.
// (!!! There might be some problem here getting back
// the same global handle).

void INTERNAL    SendDevInfo (
    LPCLIENT    lpclient,
    LPSTR       lppropname
){
    HANDLE      hclinfo  = NULL;
    PCLINFO    pclinfo = NULL;
    HANDLE      hdata;
    OLESTATUS   retval;
    HWND        hwnd;
    LPDOC       lpdoc;



    lpdoc = (LPDOC)GetWindowLongPtr (GetParent (lpclient->hwnd), 0);

    // find if any StdTargetDeviceInfo item is present at all
    hwnd = SearchItem (lpdoc, (LPSTR)ULongToPtr(MAKELONG(STDTARGETDEVICE, 0)));
    if (hwnd == NULL)
        return;

    hclinfo = GetProp(hwnd, lppropname);

    // This client has not set any target device info. no need to send
    // any stdtargetdevice info
    if (hclinfo != NULL) {
        if (!(pclinfo = (PCLINFO)LocalLock (hclinfo)))
            goto end;

        // if we cached it, do not send it again.
        if ((!pclinfo->bnewDevInfo) && pclinfo->hdevInfo == lpclient->hdevInfo)
            goto end;

        pclinfo->bnewDevInfo = FALSE;
        if(!(hdata = DuplicateData (pclinfo->hdevInfo)))
            goto end;
    } else {

        // already screen
        if (!lpclient->hdevInfo)
            goto end;

        //for screen send NULL.
        hdata = NULL;
    }


    // Now send the targetdevice info
    retval = (*lpclient->lpoleobject->lpvtbl->SetTargetDevice)
                    (lpclient->lpoleobject, hdata);

    if (retval == OLE_OK) {
        if (pclinfo)
            lpclient->hdevInfo = pclinfo->hdevInfo;
        else
            lpclient->hdevInfo = NULL;

    }
    // !!! error case who frees the data?'

end:
    if (pclinfo)
        LocalUnlock (hclinfo);

    return;
}

void ChangeOwner (
    HANDLE hmfp
){
    LPMETAFILEPICT  lpmfp;

#ifdef WIN32
    UNREFERENCED_PARAMETER(hmfp);
    UNREFERENCED_PARAMETER(lpmfp);
#endif

}


HANDLE INTERNAL MakeItemData (
    DDEPOKE FAR *   lpPoke,
    HANDLE          hPoke,
    OLECLIPFORMAT   cfFormat
){
    HANDLE  hnew;
    LPSTR   lpnew;
    DWORD   dwSize;

 
    if (cfFormat == CF_ENHMETAFILE)
        return CopyEnhMetaFile (LongToHandle(*(LONG*)lpPoke->Value), NULL);

    if (cfFormat == CF_METAFILEPICT) {
#ifdef _WIN64
        return DuplicateMetaFile(*(void* _unaligned*)lpPoke->Value);
#else
        return DuplicateMetaFile (*(LPHANDLE)lpPoke->Value);
#endif
    }

    if (cfFormat == CF_BITMAP)
        return DuplicateBitmap (LongToHandle(*(LONG*)lpPoke->Value));

    if (cfFormat == CF_DIB)
        return DuplicateData (LongToHandle(*(LONG*)lpPoke->Value));

    // Now we are dealing with normal case
    if (!(dwSize = (DWORD)GlobalSize (hPoke)))
        return NULL;

    dwSize = dwSize - sizeof (DDEPOKE) + sizeof(BYTE);

    if (hnew = GlobalAlloc (GMEM_MOVEABLE | GMEM_DDESHARE, dwSize)) {
        if (lpnew = GlobalLock (hnew)) {
            UtilMemCpy (lpnew, (LPSTR) lpPoke->Value, dwSize);
            GlobalUnlock (hnew);
        }
        else {
            GlobalFree (hnew);
            hnew = NULL;
        }
    }

    return hnew;
}



HANDLE INTERNAL DuplicateMetaFile (
    HANDLE hSrcData
){
    LPMETAFILEPICT  lpSrcMfp;
    LPMETAFILEPICT  lpDstMfp = NULL;
    HANDLE          hMF = NULL;
    HANDLE          hDstMfp = NULL;

    if (!(lpSrcMfp = (LPMETAFILEPICT) GlobalLock(hSrcData)))
        return NULL;

    GlobalUnlock (hSrcData);

    if (!(hMF = CopyMetaFile (lpSrcMfp->hMF, NULL)))
        return NULL;

    if (!(hDstMfp = GlobalAlloc (GMEM_MOVEABLE, sizeof(METAFILEPICT))))
        goto errMfp;

    if (!(lpDstMfp = (LPMETAFILEPICT) GlobalLock (hDstMfp)))
        goto errMfp;

    GlobalUnlock (hDstMfp);

    *lpDstMfp = *lpSrcMfp;
    lpDstMfp->hMF = hMF;
    return hDstMfp;
errMfp:
    if (hMF)
        DeleteMetaFile (hMF);

    if (hDstMfp)
        GlobalFree (hDstMfp);

     return NULL;
}



HBITMAP INTERNAL DuplicateBitmap (
    HBITMAP     hold
){
    HBITMAP     hnew;
    HANDLE      hMem;
    LPSTR       lpMem;
    LONG        retVal = TRUE;
    DWORD       dwSize;
    BITMAP      bm;

     // !!! another way to duplicate the bitmap

    GetObject (hold, sizeof(BITMAP), (LPSTR) &bm);
    dwSize = ((DWORD) bm.bmHeight) * ((DWORD) bm.bmWidthBytes) *
             ((DWORD) bm.bmPlanes) * ((DWORD) bm.bmBitsPixel);

    if (!(hMem = GlobalAlloc (GMEM_MOVEABLE | GMEM_ZEROINIT, dwSize)))
        return NULL;

    if (!(lpMem = GlobalLock (hMem))){
        GlobalFree (hMem);
        return NULL;
    }

    GetBitmapBits (hold, dwSize, lpMem);
    if (hnew = CreateBitmap (bm.bmWidth, bm.bmHeight,
                    bm.bmPlanes, bm.bmBitsPixel, NULL))
        retVal = SetBitmapBits (hnew, dwSize, lpMem);

    GlobalUnlock (hMem);
    GlobalFree (hMem);

    if (hnew && (!retVal)) {
        DeleteObject (hnew);
        hnew = NULL;
    }

    return hnew;
}



