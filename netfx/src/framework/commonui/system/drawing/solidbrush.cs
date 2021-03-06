//------------------------------------------------------------------------------
// <copyright file="SolidBrush.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   SolidBrush.cs
*
* Abstract:
*
*   COM+ wrapper for GDI+ SolidBrush objects
*
* Revision History:
*
*   12/15/1998 ericvan
*       Created it.
*
\**************************************************************************/

namespace System.Drawing {
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;    
    using System.ComponentModel;
    using System.Drawing.Internal;

    /**
     * Represent a SolidBrush brush object
     */
    /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Defines a brush made up of a single color. Brushes are
    ///       used to fill graphics shapes such as rectangles, ellipses, pies, polygons, and paths.
    ///    </para>
    /// </devdoc>
    public sealed class SolidBrush : Brush, ISystemColorTracker {
        // GDI+ doesn't understand system colors, so we need to cache the value here
        private Color color;
        private bool immutable = false;

        internal SolidBrush() {
            nativeBrush = IntPtr.Zero;
        }

        /**
         * Create a new solid fill brush object
         */
        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.SolidBrush"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SolidBrush'/> class of the specified
        ///       color.
        ///    </para>
        /// </devdoc>
        public SolidBrush(Color color) {
            this.color = color;
            IntPtr brush = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCreateSolidFill(color.ToArgb(), out brush);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            SetNativeBrush(brush);

            if (color.IsSystemColor)
                SystemColorTracker.Add(this);
        }

        internal SolidBrush(Color color, bool immutable) : this(color) {
            this.immutable = immutable;
        }

        private SolidBrush(int argb) : this(System.Drawing.Color.FromArgb(argb)) {
        }

        private SolidBrush(IntPtr nativeBrush, int extra) {
            SetNativeBrush(nativeBrush);
        }

        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.SolidBrush'/>.
        /// </devdoc>
        public override object Clone() {
            IntPtr cloneBrush = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCloneBrush(new HandleRef(this, nativeBrush), out cloneBrush);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            // We intentionally lose the "immutable" bit.

            return new SolidBrush(cloneBrush, 0);
        }
        
        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.Dispose"]/*' />
        protected override void Dispose(bool disposing) {
            if (!disposing) {
                immutable = false;
            }
            else if (immutable) {
                throw new ArgumentException(SR.GetString(SR.CantChangeImmutableObjects, "Brush"));
            }
            
            base.Dispose(disposing);
        }
        
        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.Color"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The color of this <see cref='System.Drawing.SolidBrush'/>.
        ///    </para>
        /// </devdoc>
        public Color Color {
            get {
                if (color == Color.Empty) {
                    int colorARGB = 0;
                    int status = SafeNativeMethods.GdipGetSolidFillColor(new HandleRef(this, nativeBrush), out colorARGB);

                    if (status != SafeNativeMethods.Ok)
                        throw SafeNativeMethods.StatusException(status);

                    this.color = Color.FromArgb(colorARGB);
                }

                // GDI+ doesn't understand system colors, so we can't use GdipGetSolidFillColor in the general case
                return color;
            }
            set {
                if (immutable)
                    throw new ArgumentException(SR.GetString(SR.CantChangeImmutableObjects, "Brush"));

                Color oldColor = this.color;
                InternalSetColor(value);

                // CONSIDER: We never remove brushes from the active list, so if someone is
                // changing their brush colors a lot, this could be a problem.
                if (value.IsSystemColor && !oldColor.IsSystemColor)
                    SystemColorTracker.Add(this);
            }
        }

        // Sets the color even if the brush is considered immutable
        private void InternalSetColor(Color value) {
            int status = SafeNativeMethods.GdipSetSolidFillColor(new HandleRef(this, nativeBrush), value.ToArgb());

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            this.color = value;
        }

        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.ISystemColorTracker.OnSystemColorChanged"]/*' />
        /// <internalonly/>
        void ISystemColorTracker.OnSystemColorChanged() {
            if (nativeBrush != IntPtr.Zero)
                InternalSetColor(color);
        }
    }
}

