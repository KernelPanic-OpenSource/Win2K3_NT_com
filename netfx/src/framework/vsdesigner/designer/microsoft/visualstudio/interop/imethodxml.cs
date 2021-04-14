//------------------------------------------------------------------------------
/// <copyright file="IMethodXML.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsWindowFrame.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Diagnostics;
    using System;
    using System.Runtime.InteropServices;

    ////////////////////////////////////////////////////////////////////////////
    // IMethodXML
    [
    ComImport, ComVisible(true),Guid("3E596484-D2E4-461a-A876-254C4F097EBB"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)
    ]
    internal interface IMethodXML
    {
        // Generate XML describing the contents of this function's body.
            void GetXML (ref string pbstrXML);

        // Parse the incoming XML with respect to the CodeModel XML schema and
        // use the result to regenerate the body of the function.
        [PreserveSig]
            int SetXML (string pszXML);

        // This is really a textpoint
        [PreserveSig]
            int GetBodyPoint([MarshalAs(UnmanagedType.Interface)]out object bodyPoint);
    }
}
