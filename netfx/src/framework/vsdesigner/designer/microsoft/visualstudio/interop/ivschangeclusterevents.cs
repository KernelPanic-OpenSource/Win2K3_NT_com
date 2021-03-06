//------------------------------------------------------------------------------
/// <copyright file="IVsChangeClusterEvents.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// __FRAMESHOW.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999; Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;
    
    /*
        IVsChangeClusterEvents is used to bracket a series of changes from an undo manager.  To get it, QI an
        undo manager for IConnectionPointContainer and go from there.

        A cluster is a series of edits that is grouped into an undo action.

        NOTE: This event set, which is used internally for the text buffer's commit event, is being exposed for a
        very specific purpose by the edit & continue group.  Listening to this event set imposes nontrivial
        overhead for every modification made to a text buffer.  You may look at this interface and be tempted to
        use it for many different things, but this is not encouraged.  Do not use this interface unless you have
        talked to the text editor team and have confirmed that you have no other options.  In particular,
        IVsPreliminaryTextChangeCommitEvents (which is built on top of this event) should be able to address the
        needs of most clients without needing to listen to IVsChangeClusterEvents. -CFlaat
    */
	[ComImport(),Guid("E55C4E80-A01C-47E8-9E94-D664B94DF6CF"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IVsChangeClusterEvents
	{
        [PreserveSig]
        void OnChangeClusterOpening(int dwFlags);
        [PreserveSig]
        void OnChangeClusterClosing(int dwFlags);
	};
        
}
