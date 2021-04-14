//------------------------------------------------------------------------------
/// <copyright file="IVsProject.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsProject.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;

    [
    ComImport(),Guid("CD4028ED-C4D8-44BA-890F-E7FB02A380C6"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(false)
    ]
    internal interface IVsProject {

        void IsDocumentInProject(
            [MarshalAs(UnmanagedType.BStr)]
            string pszMkDocument,
            [Out,MarshalAs(UnmanagedType.LPArray)]
            int[] pfFound,
            [Out,MarshalAs(UnmanagedType.LPArray)]
            int[] pdwPriority,
            [Out,MarshalAs(UnmanagedType.LPArray)]
            int[] pitemid);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetMkDocument(int itemid);

        void OpenItem(
            int itemid,
            ref Guid rguidLogicalView,
            [MarshalAs(UnmanagedType.Interface)]
            object punkDocDataExisting,
            out IVsWindowFrame ppWindowFrame);

        [return: MarshalAs(UnmanagedType.Interface)]
            object GetItemContext(int itemid);

        void GenerateUniqueItemName(
            int itemidLoc,
            [MarshalAs(UnmanagedType.BStr)]
            string pszExt,
            [MarshalAs(UnmanagedType.BStr)]
            string pszSuggestedRoot,
            out string pbstrItemName);

        int AddItem(
            int itemidLoc,
            int dwAddItemOperation,
            [MarshalAs(UnmanagedType.BStr)]
            string pszItemName,
            int cFilesToOpen,
            [MarshalAs(UnmanagedType.LPArray)]
            String[] rgpszFilesToOpen,
            IntPtr hwndDlg);

    }
}
