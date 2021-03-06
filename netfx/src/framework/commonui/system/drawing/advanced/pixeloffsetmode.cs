//------------------------------------------------------------------------------
// <copyright file="PixelOffsetMode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-2000, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   PixelOffsetMode.cs
*
* Abstract:
*
*   PixelOffset mode constants
*
* Revision History:
*
*   05/01/2000 ericvan
*       Created it.
*
\**************************************************************************/

namespace System.Drawing.Drawing2D {

    using System.Diagnostics;

    using System.Drawing;
    using System;

    /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode"]/*' />
    /// <devdoc>
    ///    Specifies how pixels are offset during
    ///    rendering.
    /// </devdoc>
    public enum PixelOffsetMode
    {
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.Invalid"]/*' />
        /// <devdoc>
        ///    Specifies an invalid mode.
        /// </devdoc>
        Invalid = QualityMode.Invalid,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.Default"]/*' />
        /// <devdoc>
        ///    Specifies the default mode.
        /// </devdoc>
        Default = QualityMode.Default,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.HighSpeed"]/*' />
        /// <devdoc>
        ///    Specifies high low quality (high
        ///    performance) mode.
        /// </devdoc>
        HighSpeed = QualityMode.Low,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.HighQuality"]/*' />
        /// <devdoc>
        ///    Specifies high quality (lower performance)
        ///    mode.
        /// </devdoc>
        HighQuality = QualityMode.High,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.None"]/*' />
        /// <devdoc>
        ///    Specifies no pixel offset.
        /// </devdoc>
        None,                   // no pixel offset
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.Half"]/*' />
        /// <devdoc>
        ///    Specifies that pixels are offset by -.5
        ///    units both horizontally and vertically for high performance anti-aliasing.
        /// </devdoc>
        Half                    // offset by -0.5, -0.5 for fast anti-alias perf
    }
}
