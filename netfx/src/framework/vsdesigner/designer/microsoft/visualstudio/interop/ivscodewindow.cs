//------------------------------------------------------------------------------
/// <copyright file="IVsCodeWindow.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsCodeWindow.cs
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
    ComImport(),Guid("8560CECD-DFAC-4F7B-9D2A-E6D9810F3443"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(false)
    ]
    internal interface IVsCodeWindow {

        void SetBuffer(IVsTextLines pBuffer);

        void GetBuffer(out IVsTextLines ppBuffer);

        IVsTextView GetPrimaryView();

        IVsTextView GetSecondaryView();

        void SetViewClassID(ref Guid clsidView);

        void GetViewClassID(ref Guid pclsidView);

        void SetBaseEditorCaption([MarshalAs(UnmanagedType.LPWStr)] string pszBaseEditorCaption);

        void GetEditorCaption(
            int dwReadOnly,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrEditorCaption);

        void Close();

        void GetLastActiveView(
            [MarshalAs(UnmanagedType.Interface)]
            object ppView);

    }
}
