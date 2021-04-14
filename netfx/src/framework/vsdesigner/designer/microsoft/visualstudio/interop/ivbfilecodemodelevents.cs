//------------------------------------------------------------------------------
/// <copyright file="IVBFileCodeModelEvents.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVBFileCodeModelEvents
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

    [ComImport(),Guid("EA1A87AD-7BC5-4349-B3BE-CADC301F17A3"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), CLSCompliant(false)]
    internal interface IVBFileCodeModelEvents {
    
        // converts the underlying codefunction into an event handler for the given event
        // if the given event is NULL, then the function will handle no events
        [PreserveSig]
            int StartEdit(); 

        [PreserveSig]
            int EndEdit();

    }
}