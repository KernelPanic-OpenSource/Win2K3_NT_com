//------------------------------------------------------------------------------
// <copyright file="_TextSpan.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// _TextSpan.cs
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

    using UnmanagedType = System.Runtime.InteropServices.UnmanagedType;

    // C#r: noAutoOffset
    /// <include file='doc\_TextSpan.uex' path='docs/doc[@for="_TextSpan"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [
    StructLayout(LayoutKind.Sequential),
    CLSCompliantAttribute(false)
    ]
    public sealed class  _TextSpan {

        /// <include file='doc\_TextSpan.uex' path='docs/doc[@for="_TextSpan.iStartLine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
        public   int iStartLine;
        /// <include file='doc\_TextSpan.uex' path='docs/doc[@for="_TextSpan.iStartIndex"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
        public   int iStartIndex;
        /// <include file='doc\_TextSpan.uex' path='docs/doc[@for="_TextSpan.iEndLine"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
        public   int iEndLine;
        /// <include file='doc\_TextSpan.uex' path='docs/doc[@for="_TextSpan.iEndIndex"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
        public   int iEndIndex;

    }
}
