//**********************************************************************
// File name: IOIPAO.CPP
//
//    Implementation file for the CClassFactory Class
//
// Functions:
//
//    See ioipao.h for a list of member functions.
//
// Copyright (c) 1993 Microsoft Corporation. All rights reserved.
//**********************************************************************

#include "pre.h"
#include "obj.h"
#include "ioipao.h"
#include "app.h"
#include "doc.h"

//**********************************************************************
//
// COleInPlaceActiveObject::QueryInterface
//
// Purpose:
//      Used for interface negotiation
//
// Parameters:
//
//      REFIID riid         -   Interface being queried for.
//
//      LPVOID FAR *ppvObj  -   Out pointer for the interface.
//
// Return Value:
//
//      S_OK            - Success
//      E_NOINTERFACE   - Failure
//
// Function Calls:
//      Function                    Location
//
//      CSimpSvrObj::QueryInterface OBJ.CPP
//
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::QueryInterface ( REFIID riid, LPVOID FAR* ppvObj)
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::QueryInterface\r\n"));
    // need to NULL the out parameter
    return m_lpObj->QueryInterface(riid, ppvObj);
}

//**********************************************************************
//
// COleInPlaceActiveObject::AddRef
//
// Purpose:
//
//      Increments the reference count on CSimpSvrObj. Since
//      COleInPlaceActiveObject is a nested class of CSimpSvrObj, we don't
//      need a separate reference count for COleInPlaceActiveObject. We
//      can use the reference count of CSimpSvrObj.
//
// Parameters:
//
//      None
//
// Return Value:
//
//      The new reference count on the CSimpSvrObj
//
// Function Calls:
//      Function                    Location
//
//      OuputDebugString            Windows API
//      CSimpSvrObj::AddRef         OBJ.CPP
//
//
//********************************************************************

STDMETHODIMP_(ULONG) COleInPlaceActiveObject::AddRef ()
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::AddRef\r\n"));

    return m_lpObj->AddRef();
}

//**********************************************************************
//
// COleInPlaceActiveObject::Release
//
// Purpose:
//
//      Decrements the reference count on CSimpSvrObj. Since
//      COleInPlaceActiveObject is a nested class of CSimpSvrObj, we don't
//      need a separate reference count for COleInPlaceActiveObject. We
//      can use the reference count of CSimpSvrObj.
//
// Parameters:
//
//      None
//
// Return Value:
//
//      The new reference count
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//      CSimpSvrObj::Release        OBJ.CPP
//
//
//********************************************************************

STDMETHODIMP_(ULONG) COleInPlaceActiveObject::Release ()
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::Release\r\n"));

    return m_lpObj->Release();
}

//**********************************************************************
//
// COleInPlaceActiveObject::OnDocWindowActivate
//
// Purpose:
//
//      Called when the doc window (in an MDI App) is (de)activated.
//
// Parameters:
//
//      BOOL fActivate  - TRUE if activating, FALSE if deactivating
//
// Return Value:
//
//      S_OK
//
// Function Calls:
//      Function                            Location
//
//      TestDebugOut                   Windows API
//      IOleInPlaceFrame::SetActiveObject   Container
//      CSimpSvrObject::AddFrameLevelUI     OBJ.CPP
//
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::OnDocWindowActivate  ( BOOL fActivate )
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::OnDocWindowActivate\r\n"));

    // Activating?
    if (fActivate)
        m_lpObj->AddFrameLevelUI();

    // No frame level tools to remove...

    return ResultFromScode(S_OK);
}

//**********************************************************************
//
// COleInPlaceActiveObject::OnFrameWindowActivate
//
// Purpose:
//
//      Called when the Frame window is (de)activating
//
// Parameters:
//
//      BOOL fActivate  - TRUE if activating, FALSE if Deactivating
//
// Return Value:
//
//      S_OK
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//      SetFocus                    Windows API
//
//
// Comments:
//
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::OnFrameWindowActivate  ( BOOL fActivate)
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::OnFrameWindowActivate\r\n"));

    // set the focus to the object window if we are activating.
/*    if (fActivate)
        SetFocus(m_lpObj->m_lpDoc->GethDocWnd()); */

    return ResultFromScode( S_OK );
}

//**********************************************************************
//
// COleInPlaceActiveObject::GetWindow
//
// Purpose:
//
//      Gets the objects Window Handle.
//
// Parameters:
//
//      HWND FAR* lphwnd    - Location to return the window handle.
//
// Return Value:
//
//      S_OK
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//      CSimpSvrDoc::GethDocWnd     DOC.H
//
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::GetWindow  ( HWND FAR* lphwnd)
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::GetWindow\r\n"));
    // need to NULL the out parameter
    *lphwnd = m_lpObj->m_lpDoc->GethDocWnd();
    return ResultFromScode( S_OK );
}

//**********************************************************************
//
// COleInPlaceActiveObject::ContextSensitiveHelp
//
// Purpose:
//
//      Used to implement Context Sensitive help
//
// Parameters:
//
//      None
//
// Return Value:
//
//      E_NOTIMPL
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//
//
// Comments:
//
//      See TECHNOTES.WRI include with the OLE SDK for proper
//      implementation of this function.
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::ContextSensitiveHelp  ( BOOL fEnterMode )
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::ContextSensitiveHelp\r\n"));
    return ResultFromScode( E_NOTIMPL);
}

//**********************************************************************
//
// COleInPlaceActiveObject::TranslateAccelerator
//
// Purpose:
//
//      Used for translating accelerators in .DLL objects.
//
// Parameters:
//
//      LPMSG lpmsg - Pointer to a message
//
// Return Value:
//
//      S_FALSE
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//
//
// Comments:
//
//      This method should never be called since we are implemented
//      in an executable.
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::TranslateAccelerator  ( LPMSG lpmsg)
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::TranslateAccelerator\r\n"));
    // no accelerator table, return FALSE
    return ResultFromScode( S_FALSE );
}

//**********************************************************************
//
// COleInPlaceActiveObject::ResizeBorder
//
// Purpose:
//
//      Called when the border changes size.
//
// Parameters:
//
//      LPCRECT lprectBorder                - New Border
//
//      LPOLEINPLACEUIWINDOW lpUIWindow     - Pointer to UIWindow
//
//      BOOL fFrameWindow                   - True if lpUIWindow is the
//                                            frame window.
//
// Return Value:
//
//      S_OK
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//
//
// Comments:
//
//      Need to call SetBorderSpace again...
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::ResizeBorder  ( LPCRECT lprectBorder,
                                                      LPOLEINPLACEUIWINDOW lpUIWindow,
                                                      BOOL fFrameWindow)
{
    HRESULT hRes;

    TestDebugOut(TEXT("In COleInPlaceActiveObject::ResizeBorder\r\n"));

    // should always have an inplace frame...
    if ((hRes=m_lpObj->GetInPlaceFrame()->SetBorderSpace(NULL)) != S_OK)
    {
       TestDebugOut(TEXT("COleInPlaceActiveObject::ResizeBorder  \
                               SetBorderSpace fails\n"));
       return(hRes);
    }


    // There will only be a UIWindow if in an MDI container
    if (m_lpObj->GetUIWindow())
    {
        if((hRes=m_lpObj->GetUIWindow()->SetBorderSpace(NULL)) != S_OK)
        {
          TestDebugOut(TEXT("COleInPlaceActiveObject::ResizeBorder  \
                                  SetBorderSpace fails\n"));
          return(hRes);
        }
    }

    return ResultFromScode( S_OK );
}

//**********************************************************************
//
// COleInPlaceActiveObject::EnableModeless
//
// Purpose:
//
//      Called to enable/disable modeless dialogs.
//
// Parameters:
//
//      BOOL fEnable    - TRUE to enable, FALSE to disable
//
// Return Value:
//
//      S_OK
//
// Function Calls:
//      Function                    Location
//
//      TestDebugOut           Windows API
//
//
// Comments:
//
//      Called by the container when a model dialog box is added/removed
//      from the screen.  The appropriate action for a server application
//      is to disable/enable any modeless dialogs currently being displayed.
//      Since this application doesn't display any modeless dialogs,
//      this method is essentially ignored.
//
//********************************************************************

STDMETHODIMP COleInPlaceActiveObject::EnableModeless  ( BOOL fEnable)
{
    TestDebugOut(TEXT("In COleInPlaceActiveObject::EnableModeless\r\n"));
    return ResultFromScode( S_OK );
}
