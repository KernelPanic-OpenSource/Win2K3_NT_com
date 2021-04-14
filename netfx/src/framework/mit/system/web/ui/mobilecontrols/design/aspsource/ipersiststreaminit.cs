//------------------------------------------------------------------------------
// <copyright file="IPersistStreamInit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IPersistStreamInit.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VSDesigner.Interop {
    using System;
    using System.Runtime.InteropServices;

    [
        System.Runtime.InteropServices.ComVisible(true), 
        ComImport,
        Guid("7FD52380-4E07-101B-AE2D-08002B2EC713"),
        System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown),
        CLSCompliantAttribute(false)
    ]
    internal interface IPersistStreamInit {

        void GetClassID(
                       [In, Out] 
                       ref Guid pClassID);

        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int IsDirty();

        
        void Load(
                 [In, MarshalAs(UnmanagedType.Interface)] 
                 IStream pstm);

        
        void Save(
                 [In, MarshalAs(UnmanagedType.Interface)] 
                 IStream pstm,
                 [In, MarshalAs(UnmanagedType.Bool)] 
                 bool fClearDirty);

        
        void GetSizeMax(
                       [Out, MarshalAs(UnmanagedType.LPArray)] 
                       long pcbSize);

        
        void InitNew();
    }
}

