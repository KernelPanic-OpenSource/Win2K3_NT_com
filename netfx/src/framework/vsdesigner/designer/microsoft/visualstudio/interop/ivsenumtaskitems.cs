//------------------------------------------------------------------------------
/// <copyright file="IVsEnumTaskItems.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsEnumTaskItems.cs
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

    [ComImport, ComVisible(true),Guid("66638598-522B-4058-9E65-FAF237700E81"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsEnumTaskItems {

        [PreserveSig]
            int Next(
            int celt,
            [Out,MarshalAs(UnmanagedType.LPArray)]
            IVsTaskItem[] rgelt,
            [Out,MarshalAs(UnmanagedType.LPArray)] 
            int[] pceltFetched);

        [PreserveSig]
            int Skip(int celt);

        [PreserveSig]
            int Reset();

        [PreserveSig]
            int Clone(out IVsEnumTaskItems ppEnum);

    }
}
