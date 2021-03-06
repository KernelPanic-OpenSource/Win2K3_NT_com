//------------------------------------------------------------------------------
// <copyright file="RotateFlipType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   RotateFlipType.cs
*
* Abstract:
*
*   Rotate / Flip type for image object
*
* Revision History:
*
*   11/66/2000 YungT
*       Created it.
*
\**************************************************************************/

namespace System.Drawing {

    using System;

    /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType"]/*' />
    /// <devdoc>
    ///    Specifies the different patterns available 'RotateFlipType' objects.
    /// </devdoc>

    public enum RotateFlipType {
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.RotateNoneFlipNone"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RotateNoneFlipNone = 0,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate90FlipNone"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate90FlipNone   = 1,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate180FlipNone"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate180FlipNone  = 2,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate270FlipNone"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate270FlipNone  = 3,

        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.RotateNoneFlipX"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RotateNoneFlipX    = 4,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate90FlipX"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate90FlipX      = 5,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate180FlipX"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate180FlipX     = 6,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate270FlipX"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate270FlipX     = 7,

        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.RotateNoneFlipY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RotateNoneFlipY    = Rotate180FlipX,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate90FlipY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate90FlipY      = Rotate270FlipX,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate180FlipY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate180FlipY     = RotateNoneFlipX,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate270FlipY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate270FlipY     = Rotate90FlipX,

        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.RotateNoneFlipXY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RotateNoneFlipXY   = Rotate180FlipNone,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate90FlipXY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate90FlipXY     = Rotate270FlipNone,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate180FlipXY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate180FlipXY    = RotateNoneFlipNone,
        /// <include file='doc\RotateFlipType.uex' path='docs/doc[@for="RotateFlipType.Rotate270FlipXY"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Rotate270FlipXY    = Rotate90FlipNone
    }
}    

