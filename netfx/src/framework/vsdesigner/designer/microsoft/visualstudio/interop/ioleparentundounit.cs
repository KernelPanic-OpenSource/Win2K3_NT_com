//------------------------------------------------------------------------------
/// <copyright file="IOleParentUndoUnit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IOleUndoUnit.cs
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

    [ComImport, ComVisible(true),Guid("A1FAF330-EF97-11CE-9BC9-00AA00608E01"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleParentUndoUnit {

        [PreserveSig]
            int Do(IOleUndoManager pUndoManager);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetDescription();

        [PreserveSig]
            int GetUnitType(
            ref System.Guid pClsid,
            out int plID);

        void OnNextAdd();

        [PreserveSig]
		int Open(IOleParentUndoUnit pPUU);

		[PreserveSig]
		int Close(IOleParentUndoUnit pPUU, bool fCommit);

		[PreserveSig]
		int Add(IOleUndoUnit pUU);

		[PreserveSig]
		int FindUnit(IOleUndoUnit pUU);
        
		int GetParentState();
    }
}
