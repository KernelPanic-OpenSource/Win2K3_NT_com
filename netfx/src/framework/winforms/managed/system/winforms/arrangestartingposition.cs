//------------------------------------------------------------------------------
// <copyright file="ArrangeStartingPosition.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Windows.Forms {

    using System.Diagnostics;

    /// <include file='doc\ArrangeStartingPosition.uex' path='docs/doc[@for="ArrangeStartingPosition"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the starting position that the system uses to
    ///       arrange minimized
    ///       windows.
    ///    </para>
    /// </devdoc>
    public enum ArrangeStartingPosition {

        /// <include file='doc\ArrangeStartingPosition.uex' path='docs/doc[@for="ArrangeStartingPosition.BottomLeft"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Starts at the lower-left corner of the screen, which is the default position.
        ///    </para>
        /// </devdoc>
        BottomLeft = NativeMethods.ARW_BOTTOMLEFT,

        /// <include file='doc\ArrangeStartingPosition.uex' path='docs/doc[@for="ArrangeStartingPosition.BottomRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Starts at the lower-right corner of the screen.
        ///    </para>
        /// </devdoc>
        BottomRight = NativeMethods.ARW_BOTTOMRIGHT,

        /// <include file='doc\ArrangeStartingPosition.uex' path='docs/doc[@for="ArrangeStartingPosition.Hide"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Hides minimized windows by moving them off the visible area of the
        ///       screen.
        ///    </para>
        /// </devdoc>
        Hide = NativeMethods.ARW_HIDE,

        /// <include file='doc\ArrangeStartingPosition.uex' path='docs/doc[@for="ArrangeStartingPosition.TopLeft"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Starts at the upper-left corner of the screen.
        ///    </para>
        /// </devdoc>

        TopLeft = NativeMethods.ARW_TOPLEFT,

        /// <include file='doc\ArrangeStartingPosition.uex' path='docs/doc[@for="ArrangeStartingPosition.TopRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Starts at the upper-right corner of the screen.
        ///    </para>
        /// </devdoc>
        TopRight = NativeMethods.ARW_TOPRIGHT,
    }
}

