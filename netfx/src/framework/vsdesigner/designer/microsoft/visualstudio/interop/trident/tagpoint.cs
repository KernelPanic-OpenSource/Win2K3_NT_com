//------------------------------------------------------------------------------
// <copyright file="tagPOINT.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// tagPOINT.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Trident {
    using System;
    using System.Runtime.InteropServices;
    using UnmanagedType = System.Runtime.InteropServices.UnmanagedType;

    // C#r: noAutoOffset
    /// <include file='doc\tagPOINT.uex' path='docs/doc[@for="tagPOINT"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(false), System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]

    

    public sealed class tagPOINT {

    	/// <include file='doc\tagPOINT.uex' path='docs/doc[@for="tagPOINT.x"]/*' />
    	/// <devdoc>
    	///    <para>[To be supplied.]</para>
    	/// </devdoc>
    	[System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
    	public   int x;
    	/// <include file='doc\tagPOINT.uex' path='docs/doc[@for="tagPOINT.y"]/*' />
    	/// <devdoc>
    	///    <para>[To be supplied.]</para>
    	/// </devdoc>
    	[System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
    	public   int y;

    }
}
