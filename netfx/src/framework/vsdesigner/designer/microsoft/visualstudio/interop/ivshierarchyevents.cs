//------------------------------------------------------------------------------
/// <copyright file="IVsHierarchyEvents.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsHierarchyEvents.cs
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
    Guid("6DDD8DC3-32B2-4bf1-A1E1-B6DA40526D1E"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(false)
    ]
    internal interface IVsHierarchyEvents {

        void OnItemAdded(int itemidParent, int itemidSiblingPrev, int itemidAdded);
        void OnItemsAppended(int itemidParent);
        void OnItemDeleted(int itemid);
        void OnPropertyChanged(int itemid, int propid, int flags); // 0x1=This node & all children have changed.  Use VSHPROPID_NIL to indicate all properties.
        void OnInvalidateItems(int itemidParent);
        void OnInvalidateIcon(IntPtr hicon);
    }
}