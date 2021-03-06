//------------------------------------------------------------------------------
// <copyright file="StatusBarPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Windows.Forms {
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using Microsoft.Win32;

    /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Stores the <see cref='System.Windows.Forms.StatusBar'/>
    ///       control panel's information.
    ///    </para>
    /// </devdoc>
    [
    DesignTimeVisible(false),
    DefaultProperty("Text")
    ]
    public class StatusBarPanel : Component, ISupportInitialize {

        private const int DEFAULTWIDTH = 100;
        private const int DEFAULTMINWIDTH = 10;
        private const int PANELTEXTINSET = 3;
        private const int PANELGAP = 2;

        private string          text          = "";
        private string          toolTipText   = "";
        private Icon            icon          = null;
        
        private HorizontalAlignment        alignment     = HorizontalAlignment.Left;
        private System.Windows.Forms.StatusBarPanelBorderStyle  borderStyle   = System.Windows.Forms.StatusBarPanelBorderStyle.Sunken;
        private StatusBarPanelStyle        style         = StatusBarPanelStyle.Text;

        // these are package scope so the parent can get at them.
        //
        internal StatusBar       parent        = null;
        internal int             width         = DEFAULTWIDTH;
        internal int             right         = 0;
        internal int             minWidth      = DEFAULTMINWIDTH;
        internal int             index         = 0;
        internal StatusBarPanelAutoSize autoSize = StatusBarPanelAutoSize.None;

        private bool initializing = false;                                      
                                      
        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.StatusBarPanel"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new default instance of the <see cref='System.Windows.Forms.StatusBarPanel'/> class.
        ///    </para>
        /// </devdoc>
        public StatusBarPanel() {
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Alignment"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the <see cref='System.Windows.Forms.StatusBarPanel.Alignment'/>
        ///       property.
        ///       
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(HorizontalAlignment.Left),
        Localizable(true),
        SRDescription(SR.StatusBarPanelAlignmentDescr)
        ]
        public HorizontalAlignment Alignment {
            get {
                return alignment;
            }

            set {
                if (!Enum.IsDefined(typeof(HorizontalAlignment), value)) {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(HorizontalAlignment));
                }
                if (alignment != value) {
                    alignment = value;
                    Realize();
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.AutoSize"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the <see cref='System.Windows.Forms.StatusBarPanel.AutoSize'/>
        ///       property.
        ///       
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(StatusBarPanelAutoSize.None),
        SRDescription(SR.StatusBarPanelAutoSizeDescr)
        ]
        public StatusBarPanelAutoSize AutoSize {
            get {
                return this.autoSize;
            }

            set {
                if (!Enum.IsDefined(typeof(StatusBarPanelAutoSize), value)) {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(StatusBarPanelAutoSize));
                }
                if (this.autoSize != value) {
                    this.autoSize = value;
                    UpdateSize();
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.BorderStyle"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the <see cref='System.Windows.Forms.StatusBarPanel.BorderStyle'/>
        ///       
        ///       property.
        ///       
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(System.Windows.Forms.StatusBarPanelBorderStyle.Sunken),
        DispId(NativeMethods.ActiveX.DISPID_BORDERSTYLE),
        SRDescription(SR.StatusBarPanelBorderStyleDescr)
        ]
        public StatusBarPanelBorderStyle BorderStyle {
            get {
                return borderStyle;
            }

            set {
                if (!Enum.IsDefined(typeof(StatusBarPanelBorderStyle), value)) {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(StatusBarPanelBorderStyle));
                }
                if (this.borderStyle != value) {
                    this.borderStyle = value;
                    Realize();
                    if (Created)
                        this.parent.Invalidate();
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Created"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        internal bool Created {
            get {
                return this.parent != null && this.parent.ArePanelsRealized();
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Icon"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the <see cref='System.Windows.Forms.StatusBarPanel.Icon'/>
        ///       property.
        ///       
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(null),
        Localizable(true),
        SRDescription(SR.StatusBarPanelIconDescr)
        ]
        public Icon Icon {
            get {
                // unfortunately we have no way of getting the icon from the control.
                return this.icon;
            }
            set {
                
                if (value != null && (((Icon)value).Height > SystemInformation.SmallIconSize.Height || ((Icon)value).Width > SystemInformation.SmallIconSize.Width)) {
                    this.icon  = new Icon(value, SystemInformation.SmallIconSize);
                }
                else {
                    this.icon = value;
                }
                
                if (Created) {
                    IntPtr handle = (this.icon == null) ? IntPtr.Zero : this.icon.Handle;
                    this.parent.SendMessage(NativeMethods.SB_SETICON, (IntPtr)GetIndex(), handle);
                    
                }
                UpdateSize();
                if (Created) {
                    this.parent.Invalidate();
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.MinWidth"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the minimum width the <see cref='System.Windows.Forms.StatusBarPanel'/> can be within the <see cref='System.Windows.Forms.StatusBar'/>
        ///       control.
        ///       
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(DEFAULTMINWIDTH),
        Localizable(true),
        RefreshProperties(RefreshProperties.All),
        SRDescription(SR.StatusBarPanelMinWidthDescr)
        ]
        public int MinWidth {
            get {
                return this.minWidth;
            }
            set {
                if (value < 0) {
                    throw new ArgumentException(SR.GetString(SR.InvalidLowBoundArgumentEx,
                                                              "value", value.ToString(), "0"));
                }
                
                if (value != this.minWidth) {
                    this.minWidth = value;

                    UpdateSize();
                    if (this.minWidth > this.Width) {
                        Width = value;
                    }
                }
            }
        }
        
        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Parent"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Represents the <see cref='System.Windows.Forms.StatusBar'/>
        ///       control which hosts the
        ///       panel.
        ///       
        ///    </para>
        /// </devdoc>
        [Browsable(false)]
        public StatusBar Parent {
            get {
                return parent;
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Style"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the style of the panel.
        ///       
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(StatusBarPanelStyle.Text),
        SRDescription(SR.StatusBarPanelStyleDescr)
        ]
        public StatusBarPanelStyle Style {
            get { return style;}
            set {
                if (!Enum.IsDefined(typeof(StatusBarPanelStyle), value)) {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(StatusBarPanelStyle));
                }
                if (this.style != value) {
                    this.style = value;
                    Realize();
                    if (Created) {
                        this.parent.Invalidate();
                    }
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Text"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the text of the panel.
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        Localizable(true),
        DefaultValue(""),
        SRDescription(SR.StatusBarPanelTextDescr)
        ]
        public string Text {
            get {
                if (text == null) {
                    return "";
                }
                else {
                    return text;
                }
            }
            set {
                if (value == null) {
                    value = "";
                }
                
                if (!Text.Equals(value)) {
                
                    if (value.Length == 0) {
                        this.text = null;
                    }
                    else {
                        this.text = value;
                    }
                    Realize();
                    UpdateSize();
                }
            }
        }


        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.ToolTipText"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       or sets the panel's tool tip text.
        ///    </para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        Localizable(true),
        DefaultValue(""),
        SRDescription(SR.StatusBarPanelToolTipTextDescr)
        ]
        public string ToolTipText {
            get {
                if (this.toolTipText == null) {
                    return "";
                }
                else {
                    return this.toolTipText;
                }
            }
            set {
                if (value == null) {
                    value = "";
                }
                
                if (!ToolTipText.Equals(value)) {
                
                    if (value.Length == 0) {
                        this.toolTipText = null;
                    }
                    else {
                        this.toolTipText = value;
                    }

                    if (Created) {
                        parent.UpdateTooltip(this);
                    }
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Width"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the width of the <see cref='System.Windows.Forms.StatusBarPanel'/> within the <see cref='System.Windows.Forms.StatusBar'/>
        ///       control.
        ///       
        ///    </para>
        /// </devdoc>
        [
        Localizable(true),
        SRCategory(SR.CatAppearance),
        DefaultValue(DEFAULTWIDTH),
        SRDescription(SR.StatusBarPanelWidthDescr)
        ]
        public int Width {
            get {
                return this.width;
            }
            set {
                if (!initializing && value < this.minWidth)
                    throw new ArgumentException(SR.GetString(SR.WidthGreaterThanMinWidth));

                this.width = value;
                UpdateSize();
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.BeginInit"]/*' />
        /// <devdoc>
        ///      Handles tasks required when the control is being initialized.
        /// </devdoc>
        public void BeginInit() {
            initializing = true;
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Dispose"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (parent != null) {
                    int index = GetIndex();
                    if (index != -1) {
                        parent.Panels.RemoveAt(index);
                    }
                }
            }
            base.Dispose(disposing);
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.EndInit"]/*' />
        /// <devdoc>
        ///      Called when initialization of the control is complete.
        /// </devdoc>
        public void EndInit() {
            initializing = false;

            if (Width < MinWidth) {
                Width = MinWidth;
            }
        }
        
        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.GetContentsWidth"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///     Gets the width of the contents of the panel
        /// </devdoc>
        internal int GetContentsWidth(bool newPanel) {
            string text;
            if (newPanel) {
                if (this.text == null)
                    text = "";
                else
                    text = this.text;
            }
            else
                text = Text;

            Graphics g = this.parent.CreateGraphicsInternal();
            Size sz = Size.Ceiling(g.MeasureString(text, parent.Font));
            if (this.icon != null) {
                sz.Width += this.icon.Size.Width + 5;
            }
            g.Dispose();
            
            int width = sz.Width + SystemInformation.BorderSize.Width*2 + PANELTEXTINSET*2 + PANELGAP;
            return Math.Max(width, minWidth);
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.GetIndex"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///     Returns the index of the panel by making the parent control search
        ///     for it within its list.
        /// </devdoc>
        private int GetIndex() {
            return index;
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.Realize"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///     Sets all the properties for this panel.
        /// </devdoc>
        internal void Realize() {
            if (Created) {
                string text;
                string  sendText;
                int     border = 0;

                if (this.text == null) {
                    text = "";
                }
                else {
                    text = this.text;
                }

                // Translate the alignment for Rtl apps
                //
                HorizontalAlignment align = alignment;
                if (parent.RightToLeft == RightToLeft.Yes) {
                    switch (align) {
                        case HorizontalAlignment.Left:
                            align = HorizontalAlignment.Right;
                            break;
                        case HorizontalAlignment.Right:
                            align = HorizontalAlignment.Left;
                            break;
                    }
                }
                
                switch (align) {
                    case HorizontalAlignment.Center:
                        sendText = "\t" + text;
                        break;
                    case HorizontalAlignment.Right:
                        sendText = "\t\t" + text;
                        break;
                    default:
                        sendText = text;
                        break;
                }
                switch (borderStyle) {
                    case StatusBarPanelBorderStyle.None:
                        border |= NativeMethods.SBT_NOBORDERS;
                        break;
                    case StatusBarPanelBorderStyle.Sunken:
                        break;
                    case StatusBarPanelBorderStyle.Raised:
                        border |= NativeMethods.SBT_POPOUT;
                        break;
                }
                switch (style) {
                    case StatusBarPanelStyle.Text:
                        break;
                    case StatusBarPanelStyle.OwnerDraw:
                        border |= NativeMethods.SBT_OWNERDRAW;
                        break;
                }

                
                int wparam = GetIndex() | border;
                if (parent.RightToLeft == RightToLeft.Yes) {
                    wparam |= NativeMethods.SBT_RTLREADING;
                }
                
                int result = (int) UnsafeNativeMethods.SendMessage(new HandleRef(parent, parent.Handle), NativeMethods.SB_SETTEXT, (IntPtr)wparam, sendText);

                if (result == 0)
                    throw new InvalidOperationException(SR.GetString(SR.UnableToSetPanelText));

                if (this.icon != null && style != StatusBarPanelStyle.OwnerDraw) {
                    this.parent.SendMessage(NativeMethods.SB_SETICON, (IntPtr)GetIndex(), this.icon.Handle);
                }
                else {
                    this.parent.SendMessage(NativeMethods.SB_SETICON, (IntPtr)GetIndex(), IntPtr.Zero);
                }

                if (style == StatusBarPanelStyle.OwnerDraw) {
                    NativeMethods.RECT rect = new NativeMethods.RECT();
                    result = (int) UnsafeNativeMethods.SendMessage(new HandleRef(parent, parent.Handle), NativeMethods.SB_GETRECT, (IntPtr)GetIndex(), ref rect);

                    if (result != 0) {
                        this.parent.Invalidate(Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom));
                    }
                }
            }
        }

        private void UpdateSize() {
            if (this.autoSize == StatusBarPanelAutoSize.Contents) {
                ApplyContentSizing();
            }
            else {
                if (Created) {
                    parent.DirtyLayout();
                    parent.PerformLayout();
                }
            }
        }

        private void ApplyContentSizing() {
            if (this.autoSize == StatusBarPanelAutoSize.Contents &&
                parent != null) {
                int newWidth = GetContentsWidth(false);
                if (newWidth != this.Width) {
                    this.Width = newWidth;
                    if (Created) {
                        parent.DirtyLayout();
                        parent.PerformLayout();
                    }
                }
            }
        }

        /// <include file='doc\StatusBarPanel.uex' path='docs/doc[@for="StatusBarPanel.ToString"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Retrieves a string that contains information about the
        ///       panel.
        ///    </para>
        /// </devdoc>
        public override string ToString() {
            return "StatusBarPanel: {" + Text + "}";
        }
    }
}
