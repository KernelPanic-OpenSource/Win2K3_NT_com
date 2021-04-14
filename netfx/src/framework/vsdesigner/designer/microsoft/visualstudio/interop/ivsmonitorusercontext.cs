//------------------------------------------------------------------------------
/// <copyright file="IVsMonitorUserContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsMonitorUserContext.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;

    [ComImport(),Guid("9C074FDB-3D7D-4512-9604-72B3B0A5F609"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsMonitorUserContext {

        void SetSite(
            [MarshalAs(UnmanagedType.Interface)] 
            object pSP);

        IVsUserContext GetApplicationContext();

        void SetApplicationContext(
            IVsUserContext ppContext);

        IVsUserContext CreateEmptyContext();

        void GetContextItems(
            [Out,MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.Interface)] 
            object[] pplist);

        void FindTargetItems(
            [MarshalAs(UnmanagedType.BStr)] 
            string pszTargetAttr,
            [MarshalAs(UnmanagedType.BStr)] 
            string pszTargetAttrValue,
            [Out,MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.Interface)] 
            object[] pplist);

        int RegisterItemProvider(
            [MarshalAs(UnmanagedType.Interface)] 
            object pProvider);

        void UnregisterItemProvider(int dwCookie);

        int AdviseContextItemEvents(
            [MarshalAs(UnmanagedType.Interface)] 
            object pEvents);

        void UnadviseContextItemEvent(int dwCookie);

    }
}
