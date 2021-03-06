//------------------------------------------------------------------------------
// <copyright file="PenAlignment.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   PenAlignment.cs
*
* Abstract:
*
*   Pen alignment constants
*
* Revision History:
*
*   11/1/1999 ericvan
*       Created it.
*
\**************************************************************************/

namespace System.Drawing.Drawing2D {

    using System.Drawing;
    using System;

    /**
     * Pen alignment constants
     */
    /// <include file='doc\PenAlignment.uex' path='docs/doc[@for="PenAlignment"]/*' />
    /// <devdoc>
    ///    Specifies the algnment of a <see cref='System.Drawing.Pen'/> in relation
    ///    to the line being drawn.
    /// </devdoc>
    public enum PenAlignment
    {
        /// <include file='doc\PenAlignment.uex' path='docs/doc[@for="PenAlignment.Center"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that the <see cref='System.Drawing.Pen'/> is positioned at the center of
        ///       the line being drawn.
        ///    </para>
        /// </devdoc>
        Center      = 0,
        /// <include file='doc\PenAlignment.uex' path='docs/doc[@for="PenAlignment.Inset"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that the <see cref='System.Drawing.Pen'/> is positioned on the insede of
        ///       the line being drawn.
        ///    </para>
        /// </devdoc>
        Inset       = 1,
        /// <include file='doc\PenAlignment.uex' path='docs/doc[@for="PenAlignment.Outset"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that the <see cref='System.Drawing.Pen'/> is positioned on the outside
        ///       of the line being drawn.
        ///    </para>
        /// </devdoc>
        Outset      = 2,
        /// <include file='doc\PenAlignment.uex' path='docs/doc[@for="PenAlignment.Left"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that the <see cref='System.Drawing.Pen'/> is positioned to the left of
        ///       the line being drawn.
        ///    </para>
        /// </devdoc>
        Left        = 3,
        /// <include file='doc\PenAlignment.uex' path='docs/doc[@for="PenAlignment.Right"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that the <see cref='System.Drawing.Pen'/> is positioned to the right of
        ///       the line being drawn.
        ///    </para>
        /// </devdoc>
        Right       = 4
    }
}
