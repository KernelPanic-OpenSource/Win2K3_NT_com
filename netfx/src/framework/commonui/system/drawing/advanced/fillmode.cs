//------------------------------------------------------------------------------
// <copyright file="FillMode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   FillMode.cs
*
* Abstract:
*
*   Fill mode constants
*
* Revision History:
*
*   12/14/1998 davidx
*       Created it.
*
\**************************************************************************/

namespace System.Drawing.Drawing2D {

    using System.Diagnostics;

    using System;
    using System.Drawing;

    /*
     * Fill mode constants
     */

/// <include file='doc\FillMode.uex' path='docs/doc[@for="FillMode"]/*' />
/// <devdoc>
///    <para>
///       Specifies how the interior of a closed path
///       is filled.
///    </para>
/// </devdoc>
public enum FillMode {
        /**
         * Odd-even fill rule
         */
        /// <include file='doc\FillMode.uex' path='docs/doc[@for="FillMode.Alternate"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies the alternate fill mode.
        ///    </para>
        /// </devdoc>
        Alternate = 0,

        /**
         * Non-zero winding fill rule
         */
        /// <include file='doc\FillMode.uex' path='docs/doc[@for="FillMode.Winding"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies the winding fill mode.
        ///    </para>
        /// </devdoc>
        Winding = 1
    }
}

