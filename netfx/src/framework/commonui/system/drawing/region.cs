//------------------------------------------------------------------------------
// <copyright file="Region.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   Region.cs
*
* Abstract:
*
*   COM+ wrapper for GDI+ region objects
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
    using System.Drawing.Drawing2D;
    using System.Drawing;
    using System.Drawing.Internal;


    /**
     * Represent a Region object
     */
    /// <include file='doc\Region.uex' path='docs/doc[@for="Region"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Describes the interior of a graphics shape
    ///       composed of rectangles and paths.
    ///    </para>
    /// </devdoc>
#if !CPB        // cpb 50004
    [ComVisible(false)]
#endif
    public sealed class Region : MarshalByRefObject, IDisposable {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        // A random integer to distinguish between the Region(int hrgn) and Region(int native, int ignored) constructors
        private const int NativeRegionOverload = 42;

        /**
         * Construct a new region object
         */
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Region"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Region'/> class.
        ///    </para>
        /// </devdoc>
        public Region() {
            IntPtr region = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCreateRegion(out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            SetNativeRegion(region);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Region1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Region'/> class from the specified <see cref='System.Drawing.RectangleF'/> .
        ///    </para>
        /// </devdoc>
        public Region(RectangleF rect) {
            IntPtr region = IntPtr.Zero;

            GPRECTF gprectf = rect.ToGPRECTF();

            int status = SafeNativeMethods.GdipCreateRegionRect(ref gprectf, out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            SetNativeRegion(region);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Region2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Region'/> class from the specified <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </devdoc>
        public Region(Rectangle rect) {
            IntPtr region = IntPtr.Zero;

            GPRECT gprect = new GPRECT(rect);

            int status = SafeNativeMethods.GdipCreateRegionRectI(ref gprect, out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            SetNativeRegion(region);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Region3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Region'/> class
        ///       with the specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
        ///    </para>
        /// </devdoc>
        public Region(GraphicsPath path) {
            if (path == null)
                throw new ArgumentNullException("path");
            
            IntPtr region = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCreateRegionPath(new HandleRef(path, path.nativePath), out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            SetNativeRegion(region);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Region4"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Region'/> class
        ///    from the specified data.
        /// </devdoc>
        public Region(RegionData rgnData) {
            if (rgnData == null)
                throw new ArgumentNullException("regionData");
            IntPtr region = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCreateRegionRgnData(rgnData.Data,
                                                         rgnData.Data.Length, 
                                                         out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            SetNativeRegion(region);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.FromHrgn"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Region'/> class
        ///    from the specified existing <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public static Region FromHrgn(IntPtr hrgn) {
            IntSecurity.ObjectFromWin32Handle.Demand();

            IntPtr region = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCreateRegionHrgn(new HandleRef(null, hrgn), out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return new Region(region, NativeRegionOverload);
        }

        // We need this ignored parameter to distinguish from the hrgn constructor
        internal Region(IntPtr nativeRegion, int ignored) {
            SetNativeRegion(nativeRegion);
        }

        private void SetNativeRegion(IntPtr nativeRegion) {
            if (nativeRegion == IntPtr.Zero)
                throw new ArgumentNullException("nativeRegion");

            this.nativeRegion = nativeRegion;
        }

        /**
         * Make a copy of the region object
         */
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy if this <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public Region Clone() {
            IntPtr region = IntPtr.Zero;

            int status = SafeNativeMethods.GdipCloneRegion(new HandleRef(this, nativeRegion), out region);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return new Region(region,0);
        }

        /**
         * Dispose of resources associated with the
         */
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Dispose"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Cleans up Windows resources for this
        ///    <see cref='System.Drawing.Region'/>.
        ///    </para>
        /// </devdoc>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing) {
#if FINALIZATION_WATCH
            if (!disposing && nativeRegion != IntPtr.Zero)
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif
            if (nativeRegion != IntPtr.Zero) {
                int status = SafeNativeMethods.GdipDeleteRegion(new HandleRef(this, nativeRegion));
                nativeRegion = IntPtr.Zero;

                if (status != SafeNativeMethods.Ok)
                    throw SafeNativeMethods.StatusException(status);
            }
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Finalize"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this
        /// <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        ~Region() {
            Dispose(false);
        }

        /*
         * Region operations
         */

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.MakeInfinite"]/*' />
        /// <devdoc>
        ///    Initializes this <see cref='System.Drawing.Region'/> to an
        ///    infinite interior.
        /// </devdoc>
        public void MakeInfinite() {
            int status = SafeNativeMethods.GdipSetInfinite(new HandleRef(this, nativeRegion));

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.MakeEmpty"]/*' />
        /// <devdoc>
        ///    Initializes this <see cref='System.Drawing.Region'/> to an
        ///    empty interior.
        /// </devdoc>
        public void MakeEmpty() {
            int status = SafeNativeMethods.GdipSetEmpty(new HandleRef(this, nativeRegion));

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // float version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Intersect"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the intersection of itself
        ///    with the specified <see cref='System.Drawing.RectangleF'/>.
        /// </devdoc>
        public void Intersect(RectangleF rect) {
            GPRECTF gprectf = rect.ToGPRECTF();
            int status = SafeNativeMethods.GdipCombineRegionRect(new HandleRef(this, nativeRegion), ref gprectf, CombineMode.Intersect);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // int version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Intersect1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Updates this <see cref='System.Drawing.Region'/> to the intersection of itself with the specified
        ///    <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </devdoc>
        public void Intersect(Rectangle rect) {
            GPRECT gprect = new GPRECT(rect);
            int status = SafeNativeMethods.GdipCombineRegionRectI(new HandleRef(this, nativeRegion), ref gprect, CombineMode.Intersect);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Intersect2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Updates this <see cref='System.Drawing.Region'/> to the intersection of itself with the specified
        ///    <see cref='System.Drawing.Drawing2D.GraphicsPath'/>. 
        ///    </para>
        /// </devdoc>
        public void Intersect(GraphicsPath path) {
            if (path == null)
                throw new ArgumentNullException("path");
            
            int status = SafeNativeMethods.GdipCombineRegionPath(new HandleRef(this, nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Intersect);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Intersect3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Updates this <see cref='System.Drawing.Region'/> to the intersection of itself with the specified
        ///    <see cref='System.Drawing.Region'/>. 
        ///    </para>
        /// </devdoc>
        public void Intersect(Region region) {
            if (region == null)
                throw new ArgumentNullException("region");
                
            int status = SafeNativeMethods.GdipCombineRegionRegion(new HandleRef(this, nativeRegion), new HandleRef(region, region.nativeRegion), CombineMode.Intersect);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // float version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Union"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Updates this <see cref='System.Drawing.Region'/> to the union of itself and the
        ///       specified <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </devdoc>
        public void Union(RectangleF rect) {
            GPRECTF gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.GdipCombineRegionRect(new HandleRef(this, nativeRegion), ref gprectf, CombineMode.Union);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // int version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Union1"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the union of itself and the
        ///    specified <see cref='System.Drawing.Rectangle'/>.
        /// </devdoc>
        public void Union(Rectangle rect) {
            GPRECT gprect = new GPRECT(rect);
            int status = SafeNativeMethods.GdipCombineRegionRectI(new HandleRef(this, nativeRegion), ref gprect, CombineMode.Union);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Union2"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the union of itself and the
        ///    specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
        /// </devdoc>
        public void Union(GraphicsPath path) {
            if (path == null)
                throw new ArgumentNullException("path");
            
            int status = SafeNativeMethods.GdipCombineRegionPath(new HandleRef(this, nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Union);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Union3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Updates this <see cref='System.Drawing.Region'/> to the union of itself and the specified <see cref='System.Drawing.Region'/>.
        ///    </para>
        /// </devdoc>
        public void Union(Region region) {
            if (region == null)
                throw new ArgumentNullException("region");
                
            int status = SafeNativeMethods.GdipCombineRegionRegion(new HandleRef(this, nativeRegion), new HandleRef(region, region.nativeRegion), CombineMode.Union);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // float version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Xor"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the union minus the
        ///    intersection of itself with the specified <see cref='System.Drawing.RectangleF'/>.
        /// </devdoc>
        public void Xor(RectangleF rect) {
            GPRECTF gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.GdipCombineRegionRect(new HandleRef(this, nativeRegion), ref gprectf, CombineMode.Xor);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // int version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Xor1"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the union minus the
        ///    intersection of itself with the specified <see cref='System.Drawing.Rectangle'/>.
        /// </devdoc>
        public void Xor(Rectangle rect) {
            GPRECT gprect = new GPRECT(rect);
            int status = SafeNativeMethods.GdipCombineRegionRectI(new HandleRef(this, nativeRegion), ref gprect, CombineMode.Xor);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Xor2"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the union minus the
        ///    intersection of itself with the specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
        /// </devdoc>
        public void Xor(GraphicsPath path) {
            if (path == null)
                throw new ArgumentNullException("path");
            
            int status = SafeNativeMethods.GdipCombineRegionPath(new HandleRef(this, nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Xor);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Xor3"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the union minus the
        ///    intersection of itself with the specified <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public void Xor(Region region) {
            if (region == null)
                throw new ArgumentNullException("region");
            
            int status = SafeNativeMethods.GdipCombineRegionRegion(new HandleRef(this, nativeRegion), new HandleRef(region, region.nativeRegion), CombineMode.Xor);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // float version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Exclude"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of its interior
        ///    that does not intersect with the specified <see cref='System.Drawing.RectangleF'/>.
        /// </devdoc>
        public void Exclude(RectangleF rect) {
            GPRECTF gprectf = new GPRECTF(rect);
            int status = SafeNativeMethods.GdipCombineRegionRect(new HandleRef(this, nativeRegion), ref gprectf, CombineMode.Exclude);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // int version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Exclude1"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of its interior
        ///    that does not intersect with the specified <see cref='System.Drawing.Rectangle'/>.
        /// </devdoc>
        public void Exclude(Rectangle rect) {
            GPRECT gprect = new GPRECT(rect);
            int status = SafeNativeMethods.GdipCombineRegionRectI(new HandleRef(this, nativeRegion), ref gprect, CombineMode.Exclude);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Exclude2"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of its interior
        ///    that does not intersect with the specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
        /// </devdoc>
        public void Exclude(GraphicsPath path) {
            if (path == null)
                throw new ArgumentNullException("path");
            
            int status = SafeNativeMethods.GdipCombineRegionPath(new HandleRef(this, nativeRegion), new HandleRef(path, path.nativePath),
                                                       CombineMode.Exclude);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Exclude3"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of its interior
        ///    that does not intersect with the specified <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public void Exclude(Region region) {
            if (region == null)
                throw new ArgumentNullException("region");
            
            int status = SafeNativeMethods.GdipCombineRegionRegion(new HandleRef(this, nativeRegion), new HandleRef(region, region.nativeRegion),
                                                         CombineMode.Exclude);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // float version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Complement"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of the
        ///    specified <see cref='System.Drawing.RectangleF'/> that does not intersect with this <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public void Complement(RectangleF rect) {
            GPRECTF gprectf = rect.ToGPRECTF();
            int status = SafeNativeMethods.GdipCombineRegionRect(new HandleRef(this, nativeRegion), ref gprectf, CombineMode.Complement);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        // int version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Complement1"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of the
        ///    specified <see cref='System.Drawing.Rectangle'/> that does not intersect with this <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public void Complement(Rectangle rect) {
            GPRECT gprect = new GPRECT(rect);
            int status = SafeNativeMethods.GdipCombineRegionRectI(new HandleRef(this, nativeRegion), ref gprect, CombineMode.Complement);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Complement2"]/*' />
        /// <devdoc>
        ///    Updates this <see cref='System.Drawing.Region'/> to the portion of the
        ///    specified <see cref='System.Drawing.Drawing2D.GraphicsPath'/> that does not intersect with this
        /// <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public void Complement(GraphicsPath path) {
            if (path == null)
                throw new ArgumentNullException("path");
            
            int status = SafeNativeMethods.GdipCombineRegionPath(new HandleRef(this, nativeRegion), new HandleRef(path, path.nativePath), CombineMode.Complement);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Complement3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Updates this <see cref='System.Drawing.Region'/> to the portion of the
        ///       specified <see cref='System.Drawing.Region'/> that does not intersect with this <see cref='System.Drawing.Region'/>.
        ///    </para>
        /// </devdoc>
        public void Complement(Region region) {
            if (region == null)
                throw new ArgumentNullException("region");
            
            int status = SafeNativeMethods.GdipCombineRegionRegion(new HandleRef(this, nativeRegion), new HandleRef(region, region.nativeRegion), CombineMode.Complement);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /**
         * Transform operations
         */
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Translate"]/*' />
        /// <devdoc>
        ///    Offsets the coordinates of this <see cref='System.Drawing.Region'/> by the
        ///    specified amount.
        /// </devdoc>
        public void Translate(float dx, float dy) {
            int status = SafeNativeMethods.GdipTranslateRegion(new HandleRef(this, nativeRegion), dx, dy);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Translate1"]/*' />
        /// <devdoc>
        ///    Offsets the coordinates of this <see cref='System.Drawing.Region'/> by the
        ///    specified amount.
        /// </devdoc>
        public void Translate(int dx, int dy) {
            int status = SafeNativeMethods.GdipTranslateRegionI(new HandleRef(this, nativeRegion), dx, dy);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Transform"]/*' />
        /// <devdoc>
        ///    Transforms this <see cref='System.Drawing.Region'/> by the
        ///    specified <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        public void Transform(Matrix matrix) {
            if (matrix == null)
                throw new ArgumentNullException("matrix");
            
            int status = SafeNativeMethods.GdipTransformRegion(new HandleRef(this, nativeRegion),
                                                     new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);
        }

        /**
         * Get region attributes
         */
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.GetBounds"]/*' />
        /// <devdoc>
        ///    Returns a <see cref='System.Drawing.RectangleF'/> that represents a rectangular
        ///    region that bounds this <see cref='System.Drawing.Region'/> on the drawing surface of a <see cref='System.Drawing.Graphics'/>.
        /// </devdoc>
        public RectangleF GetBounds(Graphics g) {
            if (g == null)
                throw new ArgumentNullException("graphics");
            
            GPRECTF gprectf = new GPRECTF();

            int status = SafeNativeMethods.GdipGetRegionBounds(new HandleRef(this, nativeRegion), new HandleRef(g, g.nativeGraphics), ref gprectf);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return gprectf.ToRectangleF();
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.GetHrgn"]/*' />
        /// <devdoc>
        ///    Returns a Windows handle to this <see cref='System.Drawing.Region'/> in the
        ///    specified graphics context.
        /// </devdoc>
        public IntPtr GetHrgn(Graphics g) {
            if (g == null)
                throw new ArgumentNullException("graphics");
            
            IntPtr hrgn = IntPtr.Zero;

            int status = SafeNativeMethods.GdipGetRegionHRgn(new HandleRef(this, nativeRegion), new HandleRef(g, g.nativeGraphics), out hrgn);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return hrgn;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsEmpty"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.Region'/> has an
        ///       empty interior on the specified drawing surface.
        ///    </para>
        /// </devdoc>
        public bool IsEmpty(Graphics g) {
            if (g == null)
                throw new ArgumentNullException("graphics");
            
            int isEmpty;
            int status = SafeNativeMethods.GdipIsEmptyRegion(new HandleRef(this, nativeRegion), new HandleRef(g, g.nativeGraphics), out isEmpty);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isEmpty != 0;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsInfinite"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.Region'/> has
        ///       an infinite interior on the specified drawing surface.
        ///    </para>
        /// </devdoc>
        public bool IsInfinite(Graphics g) {
            if (g == null)
                throw new ArgumentNullException("graphics");

            int isInfinite;
            int status = SafeNativeMethods.GdipIsInfiniteRegion(new HandleRef(this, nativeRegion), new HandleRef(g, g.nativeGraphics), out isInfinite);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isInfinite != 0;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.Equals"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.Region'/> is
        ///       identical to this <see cref='System.Drawing.Region'/>
        ///       on the specified drawing surface.
        ///    </para>
        /// </devdoc>
        public bool Equals(Region region, Graphics g) {
            if (g == null)
                throw new ArgumentNullException("graphics");
            
            if (region == null)
                throw new ArgumentNullException("region");

            int isEqual;
            int status = SafeNativeMethods.GdipIsEqualRegion(new HandleRef(this, nativeRegion), new HandleRef(region, region.nativeRegion), new HandleRef(g, g.nativeGraphics), out isEqual);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isEqual != 0;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.GetRegionData"]/*' />
        /// <devdoc>
        ///    Returns a <see cref='System.Drawing.Drawing2D.RegionData'/> that represents the
        ///    information that describes this <see cref='System.Drawing.Region'/>.
        /// </devdoc>
        public RegionData GetRegionData() {

            int regionSize = 0;

            int status = SafeNativeMethods.GdipGetRegionDataSize(new HandleRef(this, nativeRegion), out regionSize);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            if (regionSize == 0)
                return null;

            byte[] regionData = new byte[regionSize];

            status = SafeNativeMethods.GdipGetRegionData(new HandleRef(this, nativeRegion), regionData, regionSize, out regionSize);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return new RegionData(regionData);
        }

        /*
         * Hit testing operations
         */
        // float version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified point is
        ///       contained within this <see cref='System.Drawing.Region'/> in the specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(float x, float y) {
            return IsVisible(new PointF(x, y), null);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.PointF'/> is contained within this <see cref='System.Drawing.Region'/>.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(PointF point) {
            return IsVisible(point, null);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified point is contained within this <see cref='System.Drawing.Region'/> in the
        ///       specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(float x, float y, Graphics g) {
            return IsVisible(new PointF(x, y), g);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.PointF'/> is
        ///       contained within this <see cref='System.Drawing.Region'/> in the specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(PointF point, Graphics g) {
            int isVisible;

            int status = SafeNativeMethods.GdipIsVisibleRegionPoint(new HandleRef(this, nativeRegion), point.X, point.Y,
                                                          new HandleRef(g, (g==null) ? IntPtr.Zero : g.nativeGraphics), 
                                                          out isVisible);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isVisible != 0;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified rectangle is contained within this <see cref='System.Drawing.Region'/>
        ///       .
        ///    </para>
        /// </devdoc>
        public bool IsVisible(float x, float y, float width, float height) {
            return IsVisible(new RectangleF(x, y, width, height), null);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.RectangleF'/> is contained within this
        ///    <see cref='System.Drawing.Region'/>. 
        ///    </para>
        /// </devdoc>
        public bool IsVisible(RectangleF rect) {
            return IsVisible(rect, null);
        }
            
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified rectangle is contained within this <see cref='System.Drawing.Region'/> in the
        ///       specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(float x, float y, float width, float height, Graphics g) {
            return IsVisible(new RectangleF(x, y, width, height), g);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.RectangleF'/> is contained within this <see cref='System.Drawing.Region'/> in the specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(RectangleF rect, Graphics g) {
            
            int isVisible = 0;
            int status = SafeNativeMethods.GdipIsVisibleRegionRect(new HandleRef(this, nativeRegion), rect.X, rect.Y,
                                                         rect.Width, rect.Height,
                                                         new HandleRef(g, (g==null) ? IntPtr.Zero : g.nativeGraphics), 
                                                         out isVisible);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isVisible != 0;
        }

        // int version
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified point is contained within this <see cref='System.Drawing.Region'/> in the
        ///       specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(int x, int y, Graphics g) {
            return IsVisible(new Point(x, y), g);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible9"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.Point'/> is contained within this <see cref='System.Drawing.Region'/>.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(Point point) {
            return IsVisible(point, null);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible10"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.Point'/> is contained within this
        ///    <see cref='System.Drawing.Region'/> in the specified 
        ///       graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(Point point, Graphics g) {
            int isVisible = 0;
            int status = SafeNativeMethods.GdipIsVisibleRegionPointI(new HandleRef(this, nativeRegion), point.X, point.Y,
                                                           new HandleRef(g, (g == null) ? IntPtr.Zero : g.nativeGraphics), 
                                                           out isVisible);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isVisible != 0;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified rectangle is contained within this <see cref='System.Drawing.Region'/>
        ///       .
        ///    </para>
        /// </devdoc>
        public bool IsVisible(int x, int y, int width, int height) {
            return IsVisible(new Rectangle(x, y, width, height), null);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible12"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.Rectangle'/> is contained within this
        ///    <see cref='System.Drawing.Region'/>. 
        ///    </para>
        /// </devdoc>
        public bool IsVisible(Rectangle rect) {
            return IsVisible(rect, null);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible13"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified rectangle is contained within this <see cref='System.Drawing.Region'/> in the
        ///       specified graphics context.
        ///    </para>
        /// </devdoc>
        public bool IsVisible(int x, int y, int width, int height, Graphics g) {
            return IsVisible(new Rectangle(x, y, width, height), g);
        }
        
        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.IsVisible14"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified <see cref='System.Drawing.Rectangle'/> is contained within this
        ///    <see cref='System.Drawing.Region'/> 
        ///    in the specified graphics context.
        /// </para>
        /// </devdoc>
        public bool IsVisible(Rectangle rect, Graphics g) {
            
            int isVisible = 0;
            int status = SafeNativeMethods.GdipIsVisibleRegionRectI(new HandleRef(this, nativeRegion), rect.X, rect.Y,
                                                          rect.Width, rect.Height,
                                                          new HandleRef(g, (g == null) ? IntPtr.Zero : g.nativeGraphics), 
                                                          out isVisible);

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            return isVisible != 0;
        }

        /// <include file='doc\Region.uex' path='docs/doc[@for="Region.GetRegionScans"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns an array of <see cref='System.Drawing.RectangleF'/>
        ///       objects that approximate this Region on the specified
        ///    </para>
        /// </devdoc>
        public RectangleF[] GetRegionScans(Matrix matrix) {
            if (matrix == null)
                throw new ArgumentNullException("matrix");
            
            int count = 0;

            // call first time to get actual count of rectangles

            int status = SafeNativeMethods.GdipGetRegionScansCount(new HandleRef(this, nativeRegion),
                                                         out count,
                                                         new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Ok)
                throw SafeNativeMethods.StatusException(status);

            int rectsize = (int)Marshal.SizeOf(typeof(GPRECTF));
            IntPtr memoryRects = Marshal.AllocHGlobal(rectsize*count);

            status = SafeNativeMethods.GdipGetRegionScans(new HandleRef(this, nativeRegion),
                                                memoryRects,
                                                out count,
                                                new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Ok) {
                Marshal.FreeHGlobal(memoryRects);
                throw SafeNativeMethods.StatusException(status);
            }

            int index;
            GPRECTF gprectf = new GPRECTF();

            RectangleF[] rectangles = new RectangleF[count];

            for (index=0; index<count; index++) {
                gprectf = (GPRECTF) UnsafeNativeMethods.PtrToStructure((IntPtr)((long)memoryRects + rectsize*index), typeof(GPRECTF));
                rectangles[index] = gprectf.ToRectangleF();
            }

            return rectangles;
        }

        /*
         * handle to native region object
         */
        internal IntPtr nativeRegion;
    }
}
