//------------------------------------------------------------------------------
// <copyright file="ArrangeDirection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
* Copyright (c) 1997, Microsoft Corporation. All Rights Reserved.
* Information Contained Herein is Proprietary and Confidential.
*/
namespace System.Windows.Forms {

    using System.Diagnostics;

    /// <include file='doc\ArrangeDirection.uex' path='docs/doc[@for="ArrangeDirection"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the direction the system uses to arrange
    ///       minimized windows.
    ///    </para>
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum ArrangeDirection {

        /// <include file='doc\ArrangeDirection.uex' path='docs/doc[@for="ArrangeDirection.Down"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Arranges vertically, from top to bottom.
        ///    </para>
        /// </devdoc>
        Down = NativeMethods.ARW_DOWN,

        /// <include file='doc\ArrangeDirection.uex' path='docs/doc[@for="ArrangeDirection.Left"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Arranges horizontally, from left to right.
        ///    </para>
        /// </devdoc>
        Left = NativeMethods.ARW_LEFT,

        /// <include file='doc\ArrangeDirection.uex' path='docs/doc[@for="ArrangeDirection.Right"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Arranges horizontally, from right to left.
        ///    </para>
        /// </devdoc>
        Right = NativeMethods.ARW_RIGHT,

        /// <include file='doc\ArrangeDirection.uex' path='docs/doc[@for="ArrangeDirection.Up"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Arranges vertically, from bottom to top.
        ///    </para>
        /// </devdoc>
        Up = NativeMethods.ARW_UP,
    }
}

