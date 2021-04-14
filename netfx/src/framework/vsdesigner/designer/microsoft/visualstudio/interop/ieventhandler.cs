//------------------------------------------------------------------------------
/// <copyright file="IEventHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsEditorFactory.cs
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

    [ComImport(),Guid("9BDA66AE-CA28-4e22-AA27-8A7218A0E3FA"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), CLSCompliant(false)]
    internal interface IEventHandler {
    
        // converts the underlying codefunction into an event handler for the given event
        // if the given event is NULL, then the function will handle no events
        [PreserveSig]
            int AddHandler(string bstrEventName); 

        [PreserveSig]
            int RemoveHandler(string bstrEventName);

        IVsEnumBstr GetHandledEvents();

        bool HandlesEvent(string bstrEventName);
    }
}
