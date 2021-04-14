//------------------------------------------------------------------------------
// <copyright file="_VSPROPSHEETPAGE.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// _VSPROPSHEETPAGE.cs
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
    /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [
    StructLayout(LayoutKind.Sequential),
    CLSCompliantAttribute(false)
    ]
    public sealed class  _VSPROPSHEETPAGE {

        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.dwSize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int dwSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(_VSPROPSHEETPAGE));
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.dwFlags"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int dwFlags;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.hInstance"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int hInstance;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.wTemplateId"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U2)]
        public   short wTemplateId;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.dwTemplateSize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int dwTemplateSize;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.pTemplate"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int pTemplate;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.pfnDlgProc"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int pfnDlgProc;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.lParam"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)]
        public   int lParam;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.pfnCallback"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int pfnCallback;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.pcRefParent"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int pcRefParent;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.dwReserved"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int dwReserved;
        /// <include file='doc\_VSPROPSHEETPAGE.uex' path='docs/doc[@for="_VSPROPSHEETPAGE.hwndDlg"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)]
        public   int hwndDlg;

    }
}
