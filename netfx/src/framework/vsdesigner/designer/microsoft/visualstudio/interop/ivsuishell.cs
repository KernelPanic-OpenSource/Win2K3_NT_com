//------------------------------------------------------------------------------
/// <copyright file="IVsUIShell.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsUIShell.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    [
    ComImport(),Guid("B61FC35B-EEBF-4DEC-BFF1-28A2DD43C38F"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(false)
    ]
    internal interface IVsUIShell {

        [return: MarshalAs(UnmanagedType.Interface)]
            object GetToolWindowEnum();

        [return: MarshalAs(UnmanagedType.Interface)]
            object GetDocumentWindowEnum();


        void FindToolWindow(
            int grfFTW,
            ref Guid rguidPersistenceSlot,
            out IVsWindowFrame ppWindowFrame);


        void CreateToolWindow(
            int grfCTW,
            int dwReserved,
            [MarshalAs(UnmanagedType.Interface)]
            object punkTool,
            ref Guid rclsidTool,
            ref Guid rguidPersistenceSlot,
            ref Guid rguidAutoActivate,
            [MarshalAs(UnmanagedType.Interface)]
            object pSP,
            [MarshalAs(UnmanagedType.BStr)]
            string pszCaption,
            out bool pfDefaultPosition,
            out IVsWindowFrame ppWindowFrame);

        void CreateDocumentWindow(
            int grfCDW,
            [MarshalAs(UnmanagedType.BStr)]
            string pszMkDocument,
            IVsHierarchy pUIH,
            int itemid,
            [MarshalAs(UnmanagedType.Interface)]
            object punkDocView,
            [MarshalAs(UnmanagedType.Interface)]
            object punkDocData,
            ref Guid rguidEditorType,
            [MarshalAs(UnmanagedType.BStr)]
            string pszPhysicalView,
            ref Guid rguidCmdUI);

        void SetErrorInfo(
            int hr,
            [MarshalAs(UnmanagedType.BStr)]
            string pszDescription,
            int dwHelpContextID,
            [MarshalAs(UnmanagedType.BStr)]
            string pszHelpFile,
            [MarshalAs(UnmanagedType.BStr)]
            string pszSource);


        void ReportErrorInfo(
            int hr);


        void GetDialogOwnerHwnd(out IntPtr phwnd);


        void EnableModeless(int fEnable);


        void SaveDocDataToFile(
            int grfSave,
            [MarshalAs(UnmanagedType.Interface)]
            object pPersistFile,
            [MarshalAs(UnmanagedType.BStr)]
            string pszUntitledPath,
            [MarshalAs(UnmanagedType.BStr)]
            out string pbstrDocumentNew,
            out bool pfCanceled);

        void SetupToolbar(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.Interface)]
            object ptwt,
            [MarshalAs(UnmanagedType.Interface)]
            out object pptwth);


        void SetForegroundWindow();


        void TranslateAcceleratorAsACmd(int pMsg);


        void UpdateCommandUI(int fImmediateUpdate);


        void UpdateDocDataIsDirtyFeedback(
            int docCookie,
            int fDirty);


        void RefreshPropertyBrowser(int dispid);


        void SetWaitCursor();


        void PostExecCommand(
            ref Guid pguidCmdGroup,
            int nCmdID,
            int nCmdexecopt,
            ref Object pobIn);


        void ShowContextMenu (
            int dwCompRole,
            ref Guid rclsidActive,
            int nMenuId,
            tagPOINTS refPOINTS,
            NativeMethods.IOleCommandTarget pCmdTrgtActive);

        int ShowMessageBox (
            int dwCompRole,
            ref Guid rclsidComp,
            [MarshalAs(UnmanagedType.BStr)]
            string pszTitle,
            [MarshalAs(UnmanagedType.BStr)]
            string pszText,
            [MarshalAs(UnmanagedType.BStr)]
            string pszHelpFile,
            int dwHelpContextID,
            int       /*OLEMSGBUTTON*/ msgbtn,
            int       /*OLEMSGDEFBUTTON*/ msgdefbtn,
            int       /*OLEMSGICON*/ msgicon,
            bool fSysAlert);

        void SetMRUComboText (
            ref Guid pguidCmdGroup,
            int       dwCmdId,
            [MarshalAs(UnmanagedType.BStr)] 
            string lpszText,
            bool fAddToList);


        void SetToolbarVisibleInFullScreen (
            ref Guid pguidCmdGroup,
            int dwToolbarId,
            bool fVisibleInFullScreen);

        IVsWindowFrame FindToolWindowEx(
            int /*VSFINDTOOLWIN*/ grfFTW,
            ref Guid rguidPersistenceSlot,
            int  dwToolWinId);

        [return: MarshalAs(UnmanagedType.BStr)]                                        
            string GetAppName();

        int GetVSSysColor(int  /*VSSYSCOLOR*/  dwSysColIndex);


        void SetMRUComboTextW(
            ref Guid pguidCmdGroup,
            int        dwCmdId,
            [MarshalAs(UnmanagedType.BStr)] 
            string       pwszText,
            bool        fAddToList);                                                


        void PostSetFocusMenuCommand (Guid pguidCmdGroup, int nCmdId);

        void GetCurrentBFNavigationItem(out IVsWindowFrame ppWindowFrame, 
            [MarshalAs(UnmanagedType.BStr)] out string pbstrData,
            [MarshalAs(UnmanagedType.Interface)] out object ppunk);


        void AddNewBFNavigationItem(IVsWindowFrame pWindowFrame,
            [MarshalAs(UnmanagedType.BStr)] string bstrData,
            [MarshalAs(UnmanagedType.Interface)] object punk,
            bool fReplaceCurrent);

        void OnModeChange (int dbgmodeNew);
        void GetErrorInfo([MarshalAs(UnmanagedType.BStr)] out string pbstrErrText);

        // bring up the MSO Open dialog to obtain a open file name.  NOTE: using struct similar
        // to OPENFILENAMEW structure to facilitate conversion from GetOpenFileNameW system API.
        [PreserveSig]
            int GetOpenFileNameViaDlg([In] ref _VSOPENFILENAMEW pOpenFileName);

        // bring up the MSO Save As dialog to obtain a save file name.  NOTE: using struct similar
        // to OPENFILENAMEW structure to facilitate conversion from GetOpenFileNameW system API.
        [PreserveSig]
            int GetSaveFileNameViaDlg(IntPtr pSaveFileName);

        // bring up the MSO Browse dialog to obtain a directory name.  NOTE: using struct similar
        // to BROWSEINFOW structure to facilitate conversion from SHBrowseForFolderW Shell32 API.
        void GetDirectoryViaBrowseDlg(IntPtr pBrowse);

        // center the provided dialog HWND on the parent HWND (if provided), or on the main IDE window.
        // if the IDE is in SDI mode the dialog HWND will be centered on the monitor workspace.
        void CenterDialogOnWindow(IntPtr hwndDialog, IntPtr hwndParent);
        void GetPreviousBFNavigationItem(out IVsWindowFrame ppWindowFrame,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrData,
            [MarshalAs(UnmanagedType.Interface)] out object ppunk);

        void GetNextBFNavigationItem(out IVsWindowFrame ppWindowFrame,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrData,
            [MarshalAs(UnmanagedType.Interface)] out object ppunk);

        // bring up the simple URL entry dialog to obtain a URL from user.
        void GetURLViaDlg([MarshalAs(UnmanagedType.LPWStr)] string pszDlgTitle, 
            [MarshalAs(UnmanagedType.LPWStr)] string pszStaticLabel, 
            [MarshalAs(UnmanagedType.LPWStr)] string pszHelpTopic, 
            [MarshalAs(UnmanagedType.BStr)] out string pbstrURL);

        void RemoveAdjacentBFNavigationItem(int rdDir);
    }

}
