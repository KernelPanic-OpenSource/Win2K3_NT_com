//------------------------------------------------------------------------------
/// <copyright file="IVsTaskProvider2.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsTaskProvider2.cs
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

    [ComImport(), ComVisible(true), Guid("A7E6B1F9-DFF1-4354-870F-196BE871F329"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTaskProvider2 /*: IVsTaskProvider*/ {

        [PreserveSig]
            int EnumTaskItems(out IVsEnumTaskItems ppEnum);

        [PreserveSig]
            int GetImageList(out IntPtr image);

        [PreserveSig]
            int GetSubcategoryList(
            int cbstr,
            [Out, MarshalAs(UnmanagedType.LPArray)]
            string[] rgbstr,
            out int pcActual);

        [PreserveSig]
            int GetReRegistrationKey(
            [MarshalAs(UnmanagedType.BStr)]
            out string pbstrKey);

        [PreserveSig]
            int OnTaskListFinalRelease(IVsTaskList pTaskList);

        [PreserveSig]
            int GetMaintainInitialTaskOrder(out int task);
    }
}
