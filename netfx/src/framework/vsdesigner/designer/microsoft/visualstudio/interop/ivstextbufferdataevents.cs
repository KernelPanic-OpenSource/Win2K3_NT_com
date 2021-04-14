//------------------------------------------------------------------------------
/// <copyright file="IVsTextBufferDataEvents.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsTextBufferDataEvents.cs
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
    
    [ComImport, ComVisible(true),Guid("B7515E7A-70F0-44ED-96B7-FB7EB6450C10"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTextBufferDataEvents {

    	[PreserveSig]
    	int OnFileChanged( int grfChange, int dwFileAttrs);
    	
        [PreserveSig]
        int OnLoadCompleted(bool fReload);
    }
}
