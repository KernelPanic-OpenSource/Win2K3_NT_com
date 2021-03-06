//------------------------------------------------------------------------------
// <copyright file="ContentAlignment.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing {

    using System.Diagnostics;

    using System;
    using System.ComponentModel;
    using System.Drawing.Design;

    /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies alignment of content on the drawing surface.
    ///    </para>
    /// </devdoc>
    [Editor("System.Drawing.Design.ContentAlignmentEditor, " + AssemblyRef.SystemDrawingDesign, typeof(UITypeEditor))]
    public enum ContentAlignment {

        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.TopLeft"]/*' />
        /// <devdoc>
        ///    Content is vertically aligned at the top, and horizontally
        ///    aligned on the left.
        /// </devdoc>
        TopLeft = 0x001,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.TopCenter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned at the top, and
        ///       horizontally aligned at the center.
        ///    </para>
        /// </devdoc>
        TopCenter = 0x002,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.TopRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned at the top, and
        ///       horizontally aligned on the right.
        ///    </para>
        /// </devdoc>
        TopRight = 0x004,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.MiddleLeft"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned in the middle, and
        ///       horizontally aligned on the left.
        ///    </para>
        /// </devdoc>
        MiddleLeft = 0x010,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.MiddleCenter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned in the middle, and
        ///       horizontally aligned at the center.
        ///    </para>
        /// </devdoc>
        MiddleCenter = 0x020,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.MiddleRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned in the middle, and horizontally aligned on the
        ///       right.
        ///    </para>
        /// </devdoc>
        MiddleRight = 0x040,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.BottomLeft"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned at the bottom, and horizontally aligned on the
        ///       left.
        ///    </para>
        /// </devdoc>
        BottomLeft = 0x100,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.BottomCenter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned at the bottom, and horizontally aligned at the
        ///       center.
        ///    </para>
        /// </devdoc>
        BottomCenter = 0x200,
        /// <include file='doc\ContentAlignment.uex' path='docs/doc[@for="ContentAlignment.BottomRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Content is vertically aligned at the bottom, and horizontally aligned on the
        ///       right.
        ///    </para>
        /// </devdoc>
        BottomRight = 0x400,
    }
}
