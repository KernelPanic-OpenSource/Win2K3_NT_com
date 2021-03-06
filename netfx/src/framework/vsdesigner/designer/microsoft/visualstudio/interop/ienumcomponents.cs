//------------------------------------------------------------------------------
/// <copyright file="IEnumComponents.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IEnumComponents.cs
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

    [ComImport(),Guid("9a04b730-656c-11d3-85fc-00c04f6123b3"), CLSCompliantAttribute(false), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumComponents {
    
        [PreserveSig]
        int Next(uint celt, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] _VSCOMPONENTSELECTORDATA[] rgelt, out uint pceltFetched);

        void Skip(uint celt);
        
        void Reset();
        
        void Clone(out IEnumComponents ppIEnumComponents);
    }
}

