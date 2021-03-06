//------------------------------------------------------------------------------
// <copyright file="ColorMode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   ColorMode.cs
*
* Abstract:
*
*   Color mode constants
*
* Revision History:
*
*   1/25/2k ericvan
*       Created it.
*
\**************************************************************************/

namespace System.Drawing.Imaging {

    using System.Diagnostics;

    using System.Drawing;
    using System;

    /**
     * Color mode constants
     */
    /// <include file='doc\ColorMode.uex' path='docs/doc[@for="ColorMode"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies two modes for color component
    ///       values.
    ///    </para>
    /// </devdoc>
    public enum ColorMode
    {
        /// <include file='doc\ColorMode.uex' path='docs/doc[@for="ColorMode.Argb32Mode"]/*' />
        /// <devdoc>
        ///    Specifies that integer values supplied are
        ///    32-bit values.
        /// </devdoc>
        Argb32Mode = 0,
        /// <include file='doc\ColorMode.uex' path='docs/doc[@for="ColorMode.Argb64Mode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that integer values supplied are
        ///       64-bit values.
        ///    </para>
        /// </devdoc>
        Argb64Mode = 1
    }
}
