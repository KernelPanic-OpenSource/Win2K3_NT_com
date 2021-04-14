//------------------------------------------------------------------------------
// <copyright file="tagMSG.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// tagMSG.cs
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
    /// <include file='doc\tagMSG.uex' path='docs/doc[@for="tagMSG"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public struct tagMSG {

        /// <include file='doc\tagMSG.uex' path='docs/doc[@for="tagMSG.hwnd"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public   IntPtr hwnd;
        /// <include file='doc\tagMSG.uex' path='docs/doc[@for="tagMSG.message"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public   int message;
        /// <include file='doc\tagMSG.uex' path='docs/doc[@for="tagMSG.wParam"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public   IntPtr wParam;
        /// <include file='doc\tagMSG.uex' path='docs/doc[@for="tagMSG.lParam"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public   IntPtr lParam;
        /// <include file='doc\tagMSG.uex' path='docs/doc[@for="tagMSG.time"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public   int time;

    }
}