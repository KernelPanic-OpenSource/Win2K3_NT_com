//------------------------------------------------------------------------------
/// <copyright file="IVsHierarchy.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsHierarchy.cs
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
    using Microsoft.VisualStudio;

    [
    ComImport, 
    ComVisible(true), 
    Guid("59B2D1D0-5DB0-4F9F-9609-13F0168516D6"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(false)
    ]
    internal interface IVsHierarchy {

        [PreserveSig]
            int SetSite(NativeMethods.IOleServiceProvider pSP);

        [PreserveSig]
            int GetSite(out NativeMethods.IOleServiceProvider ppSP);

        [PreserveSig]
            int QueryClose(out bool pfCanClose);

        void Close();

        [PreserveSig]
            int GetGuidProperty(
            int itemid,
            int propid,
            out Guid pguid);

        [PreserveSig]
            int SetGuidProperty(
            int itemid,
            int PropId,
            ref Guid rguid);

        [PreserveSig]
            int GetProperty(
            int itemid,
            int propid,
            out object pobj);

        [PreserveSig]
            int SetProperty(
            int _itemid,
            int _PropId,
            object _obj);

        [PreserveSig]
            int GetNestedHierarchy(
            int itemid,
            ref Guid iidHierarchyNested,
            [MarshalAs(UnmanagedType.Interface)] 
            out object ppHierarchyNested,
            out int pitemidNested);

        [PreserveSig]
            int GetCanonicalName(
            int itemid,
            [MarshalAs(UnmanagedType.BStr)] 
            out string pbstrName);

        [PreserveSig]
            int ParseCanonicalName(
            [MarshalAs(UnmanagedType.BStr)] 
            string pszName,
            out int pitemid);

        [PreserveSig]
            int Unused0();

        [PreserveSig]
            int AdviseHierarchyEvents(
            IVsHierarchyEvents pEventSink,
            out int pdwCookie);

        [PreserveSig]
            int UnadviseHierarchyEvents(int dwCookie);

        [PreserveSig]
            int Unused1();

        [PreserveSig]
            int Unused2();

        [PreserveSig]
            int Unused3();

        [PreserveSig]
            int Unused4();

    }
}