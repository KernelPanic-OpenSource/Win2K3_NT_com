//------------------------------------------------------------------------------
// <copyright file="FontDialog.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms {

    using System.Diagnostics;

    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Windows.Forms;
    using System.Security;
    using System.Security.Permissions;

    using Microsoft.Win32;

    /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents
    ///       a common dialog box that displays a list of fonts that are currently installed
    ///       on
    ///       the system.
    ///    </para>
    /// </devdoc>
    [
    DefaultEvent("Apply"),
    DefaultProperty("Font")
    ]
    public class FontDialog : CommonDialog {
        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.EventApply"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected static readonly object EventApply = new object();

        private const int defaultMinSize = 0;
        private const int defaultMaxSize = 0;

        private int options;
        private Font font;
        private Color color;
        private int minSize = defaultMinSize;
        private int maxSize = defaultMaxSize;
        private bool showColor = false;

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.FontDialog"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Windows.Forms.FontDialog'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public FontDialog() {
            Reset();
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.AllowSimulations"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box allows graphics device interface
        ///       (GDI) font simulations.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(true),
        SRDescription(SR.FnDallowSimulationsDescr)
        ]
        public bool AllowSimulations {
            get {
                return !GetOption(NativeMethods.CF_NOSIMULATIONS);
            }

            set {
                SetOption(NativeMethods.CF_NOSIMULATIONS, !value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.AllowVectorFonts"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box allows vector font selections.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(true),
        SRDescription(SR.FnDallowVectorFontsDescr)
        ]
        public bool AllowVectorFonts {
            get {
                return !GetOption(NativeMethods.CF_NOVECTORFONTS);
            }

            set {
                SetOption(NativeMethods.CF_NOVECTORFONTS, !value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.AllowVerticalFonts"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether
        ///       the dialog box displays both vertical and horizontal fonts or only
        ///       horizontal fonts.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(true),
        SRDescription(SR.FnDallowVerticalFontsDescr)
        ]
        public bool AllowVerticalFonts {
            get {
                return !GetOption(NativeMethods.CF_NOVERTFONTS);
            }

            set {
                SetOption(NativeMethods.CF_NOVERTFONTS, !value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.AllowScriptChange"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       or sets a value indicating whether the user can change the character set specified
        ///       in the Script combo box to display a character set other than the one
        ///       currently displayed.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(true),
        SRDescription(SR.FnDallowScriptChangeDescr)
        ]
        public bool AllowScriptChange {
            get {
                return !GetOption(NativeMethods.CF_SELECTSCRIPT);
            }

            set {
                SetOption(NativeMethods.CF_SELECTSCRIPT, !value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.Color"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating the selected font color.
        ///    </para>
        /// </devdoc>
        [
        SRDescription(SR.FnDcolorDescr)
        ]
        public Color Color {
            get {
                return color;
            }
            set {
                if (!value.IsEmpty) {
                    color = value;
                }
                else {
                    color = Color.Black;
                }
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.FixedPitchOnly"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       a value indicating whether the dialog box allows only the selection of fixed-pitch fonts.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(false),
        SRDescription(SR.FnDfixedPitchOnlyDescr)
        ]
        public bool FixedPitchOnly {
            get {
                return GetOption(NativeMethods.CF_FIXEDPITCHONLY);
            }

            set {
                SetOption(NativeMethods.CF_FIXEDPITCHONLY, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.Font"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating the selected font.
        ///    </para>
        /// </devdoc>
        [
        SRDescription(SR.FnDfontDescr)
        ]
        public Font Font {
            get {
                Font result = font;
                if (result == null)
                    result = Control.DefaultFont;

                float actualSize =  result.SizeInPoints;
                if (minSize != defaultMinSize && actualSize < MinSize)
                    result = new Font(result.FontFamily, MinSize, result.Style, GraphicsUnit.Point);
                if (maxSize != defaultMaxSize && actualSize > MaxSize)
                    result = new Font(result.FontFamily, MaxSize, result.Style, GraphicsUnit.Point);

                return result;
            }
            set {
                font = value;
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.FontMustExist"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box specifies an error condition if the
        ///       user attempts to select a font or style that does not exist.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(false),
        SRDescription(SR.FnDfontMustExistDescr)
        ]
        public bool FontMustExist {
            get {
                return GetOption(NativeMethods.CF_FORCEFONTEXIST);
            }

            set {
                SetOption(NativeMethods.CF_FORCEFONTEXIST, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.MaxSize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the maximum
        ///       point size a user can select.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(defaultMaxSize),
        SRDescription(SR.FnDmaxSizeDescr)
        ]
        public int MaxSize {
            get {
                return maxSize;
            }
            set {
                if (value < 0) {
                    value = 0;
                }
                maxSize = value;

                if (maxSize > 0 && maxSize < minSize) {
                    minSize = maxSize;
                }
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.MinSize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating the minimum point size a user can select.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(defaultMinSize),
        SRDescription(SR.FnDminSizeDescr)
        ]
        public int MinSize {
            get {
                return minSize;
            }
            set {
                if (value < 0) {
                    value = 0;
                }
                minSize = value;

                if (maxSize > 0 && maxSize < minSize) {
                    maxSize = minSize;
                }
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.Options"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Gets the value passed to CHOOSEFONT.Flags.
        ///    </para>
        /// </devdoc>
        protected int Options {
            get {
                return options;
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ScriptsOnly"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a
        ///       value indicating whether the dialog box allows selection of fonts for all non-OEM and Symbol character
        ///       sets, as well as the American National Standards Institute (ANSI) character set.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(false),
        SRDescription(SR.FnDscriptsOnlyDescr)
        ]
        public bool ScriptsOnly {
            get {
                return GetOption(NativeMethods.CF_SCRIPTSONLY);
            }
            set {
                SetOption(NativeMethods.CF_SCRIPTSONLY, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ShowApply"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box contains an Apply button.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(false),
        SRDescription(SR.FnDshowApplyDescr)
        ]
        public bool ShowApply {
            get {
                return GetOption(NativeMethods.CF_APPLY);
            }
            set {
                SetOption(NativeMethods.CF_APPLY, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ShowColor"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box displays the color choice.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(false),
        SRDescription(SR.FnDshowColorDescr)
        ]
        public bool ShowColor {
            get {
                return showColor;
            }
            set {
                this.showColor = value;
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ShowEffects"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box contains controls that allow the
        ///       user to specify strikethrough, underline, and text color options.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(true),
        SRDescription(SR.FnDshowEffectsDescr)
        ]
        public bool ShowEffects {
            get {
                return GetOption(NativeMethods.CF_EFFECTS);
            }
            set {
                SetOption(NativeMethods.CF_EFFECTS, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ShowHelp"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the dialog box displays a Help button.
        ///    </para>
        /// </devdoc>
        [
        DefaultValue(false),
        SRDescription(SR.FnDshowHelpDescr)
        ]
        public bool ShowHelp {
            get {
                return GetOption(NativeMethods.CF_SHOWHELP);
            }
            set {
                SetOption(NativeMethods.CF_SHOWHELP, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.Apply"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Occurs when the user clicks the Apply button in the font
        ///       dialog box.
        ///    </para>
        /// </devdoc>
        [SRDescription(SR.FnDapplyDescr)]
        public event EventHandler Apply {
            add {
                Events.AddHandler(EventApply, value);
            }
            remove {
                Events.RemoveHandler(EventApply, value);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.GetOption"]/*' />
        /// <devdoc>
        ///     Returns the state of the given option flag.
        /// </devdoc>
        /// <internalonly/>
        internal bool GetOption(int option) {
            return(options & option) != 0;
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.HookProc"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies the common dialog box hook procedure that is overridden to add
        ///       specific functionality to a common dialog box.
        ///    </para>
        /// </devdoc>
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)] 
        protected override IntPtr HookProc(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam) {
            
            switch (msg) {
                case NativeMethods.WM_COMMAND:
                    if ((int)wparam == 0x402) {
                        NativeMethods.LOGFONT lf = new NativeMethods.LOGFONT();
                        UnsafeNativeMethods.SendMessage(new HandleRef(null, hWnd), NativeMethods.WM_CHOOSEFONT_GETLOGFONT, 0, lf);
                        UpdateFont(lf);
                        int index = (int)UnsafeNativeMethods.SendDlgItemMessage(new HandleRef(null, hWnd), 0x473, NativeMethods.CB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
                        if (index != NativeMethods.CB_ERR) {
                            UpdateColor((int)UnsafeNativeMethods.SendDlgItemMessage(new HandleRef(null, hWnd), 0x473,
                                                                         NativeMethods.CB_GETITEMDATA, (IntPtr) index, IntPtr.Zero));
                        }
                        try {
                            OnApply(EventArgs.Empty);
                        }
                        catch (Exception e) {
                            Application.OnThreadException(e);
                        }
                    }
                    break;
                case NativeMethods.WM_INITDIALOG:
                    if (!showColor) {
                        IntPtr hWndCtl = UnsafeNativeMethods.GetDlgItem(new HandleRef(null, hWnd), NativeMethods.cmb4);
                        SafeNativeMethods.ShowWindow(new HandleRef(null, hWndCtl), NativeMethods.SW_HIDE);
                        hWndCtl = UnsafeNativeMethods.GetDlgItem(new HandleRef(null, hWnd), NativeMethods.stc4);
                        SafeNativeMethods.ShowWindow(new HandleRef(null, hWndCtl), NativeMethods.SW_HIDE);
                    }
                    break;
            }
            
            return base.HookProc(hWnd, msg, wparam, lparam);
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.OnApply"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Raises the <see cref='System.Windows.Forms.FontDialog.Apply'/> event.
        ///    </para>
        /// </devdoc>
        protected virtual void OnApply(EventArgs e) {
            EventHandler handler = (EventHandler)Events[EventApply];
            if (handler != null) handler(this, e);
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.Reset"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Resets all dialog box options to their default values.
        ///    </para>
        /// </devdoc>
        public override void Reset() {
            options = NativeMethods.CF_SCREENFONTS | NativeMethods.CF_EFFECTS;
            font = null;
            color = Color.Black;
            showColor = false;
            minSize = defaultMinSize;
            maxSize = defaultMaxSize;
            SetOption(NativeMethods.CF_TTONLY, true);
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.RunDialog"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       The actual implementation of running the dialog. Inheriting classes
        ///       should override this if they want to add more functionality, and call
        ///       base.runDialog() if necessary
        ///       
        ///    </para>
        /// </devdoc>
        protected override bool RunDialog(IntPtr hWndOwner) {
            NativeMethods.WndProc hookProcPtr = new NativeMethods.WndProc(this.HookProc);
            NativeMethods.CHOOSEFONT cf = new NativeMethods.CHOOSEFONT();
            IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
            NativeMethods.LOGFONT lf = new NativeMethods.LOGFONT();

            Graphics graphics = Graphics.FromHdcInternal(screenDC);
            IntSecurity.ObjectFromWin32Handle.Assert();
            try {
                Font.ToLogFont(lf, graphics);
            }
            finally {
                graphics.Dispose();
                CodeAccessPermission.RevertAssert();
            }
            UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));

            IntPtr logFontPtr = IntPtr.Zero;
            try {
                logFontPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(NativeMethods.LOGFONT)));
                Marshal.StructureToPtr(lf, logFontPtr, false);

                cf.lStructSize = Marshal.SizeOf(typeof(NativeMethods.CHOOSEFONT));
                cf.hwndOwner = hWndOwner;
                cf.hDC = IntPtr.Zero;
                cf.lpLogFont = logFontPtr;
                cf.Flags = Options | NativeMethods.CF_INITTOLOGFONTSTRUCT | NativeMethods.CF_ENABLEHOOK;
                if (minSize > 0 || maxSize > 0) {
                    cf.Flags |= NativeMethods.CF_LIMITSIZE;
                }

                //if ShowColor=true then try to draw the sample text in color,
                //if ShowEffects=false then we will draw the sample text in black regardless.
                //(limitation of windows control)
                //
                if (ShowColor || ShowEffects) {
                    cf.rgbColors = ColorTranslator.ToWin32(color);
                }
                else {
                    cf.rgbColors = ColorTranslator.ToWin32(Color.Black);
                }

                cf.lpfnHook = hookProcPtr;
                cf.hInstance = UnsafeNativeMethods.GetModuleHandle(null);
                cf.nSizeMin = minSize;
                if (maxSize == 0) {
                    cf.nSizeMax = Int32.MaxValue;
                }
                else {
                    cf.nSizeMax = maxSize;
                }
                Debug.Assert(cf.nSizeMin <= cf.nSizeMax, "min and max font sizes are the wrong way around");
                if (!SafeNativeMethods.ChooseFont(cf)) return false;


                NativeMethods.LOGFONT lfReturned = null;
                lfReturned = (NativeMethods.LOGFONT)UnsafeNativeMethods.PtrToStructure(logFontPtr, typeof(NativeMethods.LOGFONT));

                if (lfReturned.lfFaceName != null && lfReturned.lfFaceName.Length > 0) {
                    lf = lfReturned;
                    UpdateFont(lf);
                    UpdateColor(cf.rgbColors);
                }

                return true;
            }
            finally {
                if (logFontPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(logFontPtr);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.SetOption"]/*' />
        /// <devdoc>
        ///     Sets the given option to the given boolean value.
        /// </devdoc>
        /// <internalonly/>
        internal void SetOption(int option, bool value) {
            if (value) {
                options |= option;
            }
            else {
                options &= ~option;
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ShouldSerializeColor"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Indicates whether the <see cref='System.Windows.Forms.FontDialog.Color'/> property should be persisted.
        ///    </para>
        /// </devdoc>
        private bool ShouldSerializeColor() {
            return !Color.Equals(Color.Black);
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ShouldSerializeFont"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Indicates whether the <see cref='System.Windows.Forms.FontDialog.Font'/> property should be
        ///       persisted.
        ///    </para>
        /// </devdoc>
        private bool ShouldSerializeFont() {
            return !Font.Equals(Control.DefaultFont);
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Retrieves a string that includes the name of the current font selected in
        ///       the dialog box.
        ///       
        ///    </para>
        /// </devdoc>
        public override string ToString() {
            string s = base.ToString();
            return s + ",  Font: " + Font.ToString();
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.UpdateColor"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        private void UpdateColor(int rgb) {
            if (ColorTranslator.ToWin32(color) != rgb) {
                color = ColorTranslator.FromOle(rgb);
            }
        }

        /// <include file='doc\FontDialog.uex' path='docs/doc[@for="FontDialog.UpdateFont"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        private void UpdateFont(NativeMethods.LOGFONT lf) {
            IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
            try {
                Font fontInWorldUnits = null;
                try {
                    IntSecurity.UnmanagedCode.Assert();
                    try {
                        fontInWorldUnits = Font.FromLogFont(lf, screenDC);
                    }
                    finally {
                        CodeAccessPermission.RevertAssert();
                    }

                    // The dialog claims its working in points (a device-independent unit),
                    // but actually gives us something in world units (device-dependent).
                    font = ControlPaint.FontInPoints(fontInWorldUnits);
                }
                finally {
                    if (fontInWorldUnits != null) {
                        fontInWorldUnits.Dispose();
                    }
                }
            }
            finally {
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));
            }
        }
    }
}
