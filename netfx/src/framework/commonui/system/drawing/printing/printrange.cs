//------------------------------------------------------------------------------
// <copyright file="PrintRange.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Drawing.Printing {

    using System.Diagnostics;
    using System;
    using System.Runtime.InteropServices;
    using System.Drawing;
    using System.ComponentModel;
    using Microsoft.Win32;

    /// <include file='doc\PrintRange.uex' path='docs/doc[@for="PrintRange"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the option buttons in the print dialog box that
    ///       designate the part of the document to print.
    ///    </para>
    /// </devdoc>
    public enum PrintRange {
        /// <include file='doc\PrintRange.uex' path='docs/doc[@for="PrintRange.AllPages"]/*' />
        /// <devdoc>
        ///    <para>
        ///       All pages are printed.
        ///       
        ///    </para>
        /// </devdoc>
        AllPages = SafeNativeMethods.PD_ALLPAGES,

        /// <include file='doc\PrintRange.uex' path='docs/doc[@for="PrintRange.SomePages"]/*' />
        /// <devdoc>
        /// <para> The pages between <see cref='System.Drawing.Printing.PrinterSettings.FromPage'/> and
        /// <see cref='System.Drawing.Printing.PrinterSettings.ToPage'/>
        /// are
        /// printed.</para>
        /// </devdoc>
        SomePages = SafeNativeMethods.PD_PAGENUMS,

        /// <include file='doc\PrintRange.uex' path='docs/doc[@for="PrintRange.Selection"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The selected pages are printed.
        ///       
        ///    </para>
        /// </devdoc>
        Selection = SafeNativeMethods.PD_SELECTION,
        
        /*
        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///       The
        ///       current page is printed. The print dialog box requires Windows 2000 or
        ///       later for this setting; if used with an earlier operating system, all pages will be printed.
        ///       
        ///    </para>
        /// </summary>
        CurrentPage = SafeNativeMethods.PD_CURRENTPAGE,
        */
        
        // When adding new members, be sure to update PrintDialog.printRangeMask.
    }
}

