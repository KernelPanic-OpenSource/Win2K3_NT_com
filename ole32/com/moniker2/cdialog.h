//+---------------------------------------------------------------------------
//
//  Microsoft Windows
//  Copyright (C) Microsoft Corporation, 1992 - 1994.
//
//  File:       cdialog.h
//
//  Contents:   definition for common dialog functionality
//
//  Classes:    CHlprDialog (pure virtual class)
//
//  Functions:  DialogProc
//
//  History:    4-12-94   stevebl   Created
//
//----------------------------------------------------------------------------

#ifndef __CDIALOG_H__
#define __CDIALOG_H__

#ifdef __cplusplus
extern "C" {
#endif

INT_PTR CALLBACK DialogProc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);

#ifdef __cplusplus
}

//+---------------------------------------------------------------------------
//
//  Class:      CHlprDialog
//
//  Purpose:    virtual base class for wrapping Windows' dialog functionality
//
//  Interface:  ShowDialog -- analagous to the Windows DialogBox function
//              DialogProc -- pure virtual DialogProc for the dialog box
//              ~CHlprDialog   -- destructor
//
//  History:    4-12-94   stevebl   Created
//              7-02-1997   stevebl   added CreateDlg
//
//  Notes:      This class allows a dialog box to be cleanly wrapped in
//              a c++ class.  Specifically, it provides a way for a c++ class
//              to use one of its methods as a DialogProc, giving it a "this"
//              pointer and allowing it to have direct access to all of its
//              private members.
//
//----------------------------------------------------------------------------

class CHlprDialog
{
public:
    virtual INT_PTR ShowDialog(HINSTANCE hinst, LPCTSTR lpszTemplate, HWND hwndOwner);
    virtual HWND    CreateDlg(HINSTANCE hinst, LPCTSTR lpszTemplate, HWND hwndOwner);
    virtual INT_PTR DialogProc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam) = 0;
    virtual ~CHlprDialog(){};
protected:
    HINSTANCE m_hInstance;
};

#endif //__cplusplus

#endif //__CDIALOG_H__

