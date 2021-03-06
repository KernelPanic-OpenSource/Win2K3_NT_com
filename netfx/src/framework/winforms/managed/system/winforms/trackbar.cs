//------------------------------------------------------------------------------
// <copyright file="TrackBar.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Windows.Forms {
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;

    using System.Diagnostics;

    using System;
    using System.Security.Permissions;
    using Microsoft.Win32;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using System.ComponentModel.Design;

    /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar"]/*' />
    /// <devdoc>
    ///     The TrackBar is a scrollable control similar to the ScrollBar, but
    ///     has a different UI.  You can configure ranges through which it should
    ///     scroll, and also define increments for off-button clicks.  It can be
    ///     aligned horizontally or vertically.  You can also configure how many
    ///     'ticks' are shown for the total range of values
    /// </devdoc>
    [
    DefaultProperty("Value"),
    DefaultEvent("Scroll"),
    Designer("System.Windows.Forms.Design.TrackBarDesigner, " + AssemblyRef.SystemDesign)
    ]
    public class TrackBar : Control, ISupportInitialize {

        private static readonly object EVENT_SCROLL = new object();
        private static readonly object EVENT_VALUECHANGED = new object();

        private bool autoSize = true;
        private int largeChange = 5;
        private int maximum = 10;
        private int minimum = 0;
        private Orientation orientation = System.Windows.Forms.Orientation.Horizontal;
        private int value = 0;
        private int smallChange = 1;
        private int tickFrequency = 1;
        private TickStyle tickStyle = System.Windows.Forms.TickStyle.BottomRight;

        private int requestedDim;

        // Disable value range checking while initializing the control
        private bool initializing = false;

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.TrackBar"]/*' />
        /// <devdoc>
        ///     Creates a new TrackBar control with a default range of 0..10 and
        ///     ticks shown every value.
        /// </devdoc>
        public TrackBar()
        : base() {
            SetStyle(ControlStyles.UserPaint, false);
            requestedDim = PreferredDimension;
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.AutoSize"]/*' />
        /// <devdoc>
        ///     Indicates if the control is being auto-sized.  If true, the
        ///     TrackBar will adjust either it's height or width [depending on
        ///     orientation] to make sure that only the required amount of
        ///     space is used.
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(true),
        SRDescription(SR.TrackBarAutoSizeDescr)
        ]
        public bool AutoSize {
            get {
                return autoSize;
            }

            set {
                if (autoSize != value) {
                    autoSize = value;
                    if (orientation == Orientation.Horizontal) {
                        SetStyle(ControlStyles.FixedHeight, autoSize);
                        SetStyle(ControlStyles.FixedWidth, false);
                    }
                    else {
                        SetStyle(ControlStyles.FixedWidth, autoSize);
                        SetStyle(ControlStyles.FixedHeight, false);
                    }
                    AdjustSize();
                    
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.BackgroundImage"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Image BackgroundImage {
            get {
                return base.BackgroundImage;
            }
            set {
                base.BackgroundImage = value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.BackgroundImageChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler BackgroundImageChanged {
            add {
                base.BackgroundImageChanged += value;
            }
            remove {
                base.BackgroundImageChanged -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.CreateParams"]/*' />
        /// <devdoc>
        ///     This is called when creating a window.  Inheriting classes can ovveride
        ///     this to add extra functionality, but should not forget to first call
        ///     base.getCreateParams() to make sure the control continues to work
        ///     correctly.
        /// </devdoc>
        /// <internalonly/>
        protected override CreateParams CreateParams {
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassName = NativeMethods.WC_TRACKBAR;

                switch (tickStyle) {
                    case TickStyle.None:
                        cp.Style |= NativeMethods.TBS_NOTICKS;
                        break;
                    case TickStyle.TopLeft:
                        cp.Style |= (NativeMethods.TBS_AUTOTICKS | NativeMethods.TBS_TOP);
                        break;
                    case TickStyle.BottomRight:
                        cp.Style |= (NativeMethods.TBS_AUTOTICKS | NativeMethods.TBS_BOTTOM);
                        break;
                    case TickStyle.Both:
                        cp.Style |= (NativeMethods.TBS_AUTOTICKS | NativeMethods.TBS_BOTH);
                        break;
                }

                if (orientation == Orientation.Vertical) {
                    cp.Style |= NativeMethods.TBS_VERT; // HORIZ == 0
                }

                return cp;
            }
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.DefaultImeMode"]/*' />
        /// <internalonly/>
        protected override ImeMode DefaultImeMode {
            get {
                return ImeMode.Disable;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.DefaultSize"]/*' />
        /// <devdoc>
        ///     Deriving classes can override this to configure a default size for their control.
        ///     This is more efficient than setting the size in the control's constructor.
        /// </devdoc>
        protected override Size DefaultSize {
            get {
                return new Size(104, 42);
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Font"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Font Font {
            get {
                return base.Font;
            }
            set {
                base.Font = value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.FontChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler FontChanged {
            add {
                base.FontChanged += value;
            }
            remove {
                base.FontChanged -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.ForeColor"]/*' />
        /// <devdoc>
        ///     The current foreground color of the TrackBar.  Note that users
        ///     are unable to change this.  It is always Color.WINDOWTEXT
        /// </devdoc>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color ForeColor {
            get {
                return SystemColors.WindowText;
            }
            set {
            }
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.ForeColorChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler ForeColorChanged {
            add {
                base.ForeColorChanged += value;
            }
            remove {
                base.ForeColorChanged -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.ImeMode"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public ImeMode ImeMode {
            get {
                return base.ImeMode;
            }
            set {
                base.ImeMode = value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.ImeModeChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler ImeModeChanged {
            add {
                base.ImeModeChanged += value;
            }
            remove {
                base.ImeModeChanged -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.LargeChange"]/*' />
        /// <devdoc>
        ///     The number of ticks by which the TrackBar will change when an
        ///     event considered a "large change" occurs.  These include, Clicking the
        ///     mouse to the side of the button, or using the PgUp/PgDn keys on the
        ///     keyboard.
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(5),
        SRDescription(SR.TrackBarLargeChangeDescr)
        ]
        public int LargeChange {
            get {
                return largeChange;
            }
            set {
                if (value < 0) {
                    throw new ArgumentException(SR.GetString(SR.TrackBarLargeChangeError, value), "value");
                }

                if (largeChange != value) {
                    largeChange = value;
                    if (IsHandleCreated)
                        SendMessage(NativeMethods.TBM_SETPAGESIZE, 0, value);
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Maximum"]/*' />
        /// <devdoc>
        ///     The upper limit of the range this TrackBar is working with.
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(10),
        RefreshProperties(RefreshProperties.All),
        SRDescription(SR.TrackBarMaximumDescr)
        ]
        public int Maximum {
            get {
                return maximum;
            }
            set {
                if (maximum != value) {
                    if (value < minimum) {
                        minimum = value;
                    }
                    SetRange(minimum, value);
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Minimum"]/*' />
        /// <devdoc>
        ///     The lower limit of the range this TrackBar is working with.
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(0),
        RefreshProperties(RefreshProperties.All),
        SRDescription(SR.TrackBarMinimumDescr)
        ]
        public int Minimum {
            get {
                return minimum;
            }
            set {
                if (minimum != value) {
                    if (value > maximum) {
                        maximum = value;
                    }
                    SetRange(value, maximum);
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Orientation"]/*' />
        /// <devdoc>
        ///    <para>The orientation for this TrackBar. Valid values are from
        ///       the Orientation enumeration. The control currently supports being
        ///       oriented horizontally and vertically.</para>
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(Orientation.Horizontal),
        Localizable(true),
        SRDescription(SR.TrackBarOrientationDescr)
        ]
        public Orientation Orientation {
            get {
                return orientation;
            }
            set {
                // Confirm that value is a valid enum
                if (!Enum.IsDefined(typeof(Orientation), value)) {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(Orientation));
                }

                if (orientation != value) {
                    orientation = value;

                    if (orientation == Orientation.Horizontal) {
                        SetStyle(ControlStyles.FixedHeight, autoSize);
                        SetStyle(ControlStyles.FixedWidth, false);
                    }
                    else {
                        SetStyle(ControlStyles.FixedHeight, false);
                        SetStyle(ControlStyles.FixedWidth, autoSize);
                    }

                    if (IsHandleCreated) {
                        Rectangle r = Bounds;
                        RecreateHandle();
                        SetBounds(r.X, r.Y, r.Height, r.Width, BoundsSpecified.All);
                        AdjustSize();
                    }
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.PreferredDimension"]/*' />
        /// <devdoc>
        ///     Little private routine that helps with auto-sizing.
        /// </devdoc>
        /// <internalonly/>
        private int PreferredDimension {
            get {
                int cyhscroll = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYHSCROLL);

                // this is our preferred size
                //
                return((cyhscroll * 8) / 3);
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.SmallChange"]/*' />
        /// <devdoc>
        ///     The number of ticks by which the TrackBar will change when an
        ///     event considered a "small change" occurs.  These are most commonly
        ///     seen by using the arrow keys to move the TrackBar thumb around.
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(1),
        SRDescription(SR.TrackBarSmallChangeDescr)
        ]
        public int SmallChange {
            get {
                return smallChange;
            }
            set {
                if (value < 0) {
                    throw new ArgumentException(SR.GetString(SR.TrackBarSmallChangeError, value), "value");
                }
                if (smallChange != value) {
                    smallChange = value;
                    if (IsHandleCreated)
                        SendMessage(NativeMethods.TBM_SETLINESIZE, 0, value);
                }
            }
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Text"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>        
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), Bindable(false)]        
        public override string Text {
            get {
                return base.Text;
            }
            set {
                base.Text = value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.TextChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler TextChanged {
            add {
                base.TextChanged += value;
            }
            remove {
                base.TextChanged -= value;
            }
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.TickStyle"]/*' />
        /// <devdoc>
        ///     Indicates how the TrackBar control will draw itself.  This affects
        ///     both where the ticks will be drawn in relation to the moveable thumb,
        ///     and how the thumb itself will be drawn.  values are taken from the
        ///     TickStyle enumeration.
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(TickStyle.BottomRight),
        SRDescription(SR.TrackBarTickStyleDescr)
        ]
        public TickStyle TickStyle {
            get {
                return tickStyle;
            }
            set {
                // Confirm that value is a valid enum
                if (!Enum.IsDefined(typeof(TickStyle), value)) {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(TickStyle));
                }

                if (tickStyle != value) {
                    tickStyle = value;
                    RecreateHandle();
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.TickFrequency"]/*' />
        /// <devdoc>
        ///     Indicates just how many ticks will be drawn.  For a TrackBar with a
        ///     range of 0..100, it might be impractical to draw all 100 ticks for a
        ///     very small control.  Passing in a value of 5 here would only draw
        ///     20 ticks -- i.e. Each tick would represent 5 units in the TrackBars
        ///     range of values.
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(1),
        SRDescription(SR.TrackBarTickFrequencyDescr)
        ]
        public int TickFrequency {
            get {
                return tickFrequency;
            }
            set {
                if (tickFrequency != value) {
                    tickFrequency = value;
                    if (IsHandleCreated) {
                        SendMessage(NativeMethods.TBM_SETTICFREQ, value, 0);
                        Invalidate();
                    }
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Value"]/*' />
        /// <devdoc>
        ///     The current location of the TrackBar thumb.  This value must
        ///     be between the lower and upper limits of the TrackBar range, of course.
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(0),
        Bindable(true),
        SRDescription(SR.TrackBarValueDescr)
        ]
        public int Value {
            get {
                GetTrackBarValue();
                return value;
            }
            set {
                if (this.value != value) {
                    if (!initializing && ((value < minimum) || (value > maximum)))
                        throw new ArgumentException(SR.GetString(SR.InvalidBoundArgument,
                                                                  "Value", (value).ToString(),
                                                                  "'Minimum'", "'Maximum'"));
                    this.value = value;
                    SetTrackBarPosition();
                    OnValueChanged(EventArgs.Empty);
                }
            }
        }    

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Click"]/*' />
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler Click {
            add {
                base.Click += value;
            }
            remove {
                base.Click -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.DoubleClick"]/*' />
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler DoubleClick {
            add {
                base.DoubleClick += value;
            }
            remove {
                base.DoubleClick -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.Scroll"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SRCategory(SR.CatBehavior), SRDescription(SR.TrackBarOnScrollDescr)]
        public event EventHandler Scroll {
            add {
                Events.AddHandler(EVENT_SCROLL, value);
            }
            remove {
                Events.RemoveHandler(EVENT_SCROLL, value);
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.OnPaint"]/*' />
        /// <devdoc>
        ///     TrackBar Onpaint.
        /// </devdoc>
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event PaintEventHandler Paint {
            add {
                base.Paint += value;
            }
            remove {
                base.Paint -= value;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.ValueChanged"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SRCategory(SR.CatAction), SRDescription(SR.valueChangedEventDescr)]
        public event EventHandler ValueChanged {
            add {
                Events.AddHandler(EVENT_VALUECHANGED, value);
            }
            remove {
                Events.RemoveHandler(EVENT_VALUECHANGED, value);
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.AdjustSize"]/*' />
        /// <devdoc>
        ///     Enforces autoSizing
        /// </devdoc>
        /// <internalonly/>
        private void AdjustSize() {
            if (IsHandleCreated) {
                int saveDim = requestedDim;
                try {
                    if (orientation == Orientation.Horizontal)
                        Height = autoSize ? PreferredDimension : saveDim;
                    else
                        Width = autoSize ? PreferredDimension : saveDim;
                }
                finally {
                    requestedDim = saveDim;
                }
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.BeginInit"]/*' />
        /// <devdoc>
        ///      Handles tasks required when the control is being initialized.
        /// </devdoc>
        /// <internalonly/>
        public void BeginInit() {
            initializing = true;
        }

        // Constrain the current value of the control to be within
        // the minimum and maximum.
        //
        private void ConstrainValue() {

            // Don't constrain the value while we're initializing the control
            if (initializing) {
                return;
            }

            Debug.Assert(minimum <= maximum, "Minimum should be <= Maximum");

            // Keep the current value within the minimum and maximum
            if (Value < minimum) {
                Value = minimum;
            }
            if (Value > maximum) {
                Value = maximum;
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.CreateHandle"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        protected override void CreateHandle() {
            if (!RecreatingHandle) {
                NativeMethods.INITCOMMONCONTROLSEX icc = new NativeMethods.INITCOMMONCONTROLSEX();
                icc.dwICC = NativeMethods.ICC_BAR_CLASSES;
                SafeNativeMethods.InitCommonControlsEx(icc);
            }
            base.CreateHandle();
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.EndInit"]/*' />
        /// <devdoc>
        ///      Called when initialization of the control is complete.
        /// </devdoc>
        /// <internalonly/>
        public void EndInit() {
            initializing = false;

            // Make sure the value is constrained by the minimum and maximum
            ConstrainValue();
        }

        private void GetTrackBarValue() {
            if (IsHandleCreated) {
                value = (int)SendMessage(NativeMethods.TBM_GETPOS, 0, 0);
                
                // See SetTrackBarValue() for a description of why we sometimes reflect the trackbar value
                //                   
                   
                if (orientation == Orientation.Vertical) {
                    // Reflect value
                    value = Minimum + Maximum - value;
                }
                
                // Reflect for a RightToLeft horizontal trackbar
                //
                if (orientation == Orientation.Horizontal && RightToLeft == RightToLeft.Yes) {
                    value = Minimum + Maximum - value;
                }
            }
        }                                               

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.IsInputKey"]/*' />
        /// <devdoc>
        ///      Handling special input keys, such as pgup, pgdown, home, end, etc...
        /// </devdoc>
        protected override bool IsInputKey(Keys keyData) {
            if ((keyData & Keys.Alt) == Keys.Alt) return false;
            switch (keyData & Keys.KeyCode) {
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    return true;
            }
            return base.IsInputKey(keyData);
        }
        
        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.OnHandleCreated"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SendMessage(NativeMethods.TBM_SETRANGEMIN, 0, minimum);
            SendMessage(NativeMethods.TBM_SETRANGEMAX, 0, maximum);
            SendMessage(NativeMethods.TBM_SETTICFREQ, tickFrequency, 0);
            SendMessage(NativeMethods.TBM_SETPAGESIZE, 0, largeChange);
            SendMessage(NativeMethods.TBM_SETLINESIZE, 0, smallChange);
            SetTrackBarPosition();
            AdjustSize();
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.OnScroll"]/*' />
        /// <devdoc>
        ///     Actually fires the "scroll" event.  Inheriting classes should override
        ///     this method in favor of actually adding an EventHandler for this
        ///     event.  Inheriting classes should not forget to call
        ///     base.onScroll(e)
        /// </devdoc>
        protected virtual void OnScroll(EventArgs e) {
            EventHandler handler = (EventHandler)Events[EVENT_SCROLL];
            if (handler != null) handler(this,e);
        }


        /// <include file='doc\Trackbar.uex' path='docs/doc[@for="Trackbar.OnMouseWheel"]/*' />
        /// <devdoc>
        /// <para>Raises the <see cref='System.Windows.Forms.Control.MouseWheel'/> event.</para>
        /// </devdoc>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel( e );
            if (e.Delta != Value) {
                OnScroll(EventArgs.Empty);
                OnValueChanged(EventArgs.Empty);
            }
        }


        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.OnValueChanged"]/*' />
        /// <devdoc>
        ///     Actually fires the "valueChanged" event.
        /// </devdoc>
        /// <internalonly/>
        protected virtual void OnValueChanged(EventArgs e) {
            EventHandler handler = (EventHandler)Events[EVENT_VALUECHANGED];
            if (handler != null) handler(this,e);
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.OnBackColorChanged"]/*' />
        /// <devdoc>
        ///     This method is called by the control when any property changes. Inheriting
        ///     controls can overide this method to get property change notification on
        ///     basic properties. Inherting controls must call base.propertyChanged.
        /// </devdoc>
        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);

            // Trackbar uses a double buffered bitmap, and we must
            // somehow convince it to totally redraw.  This works and is
            // pretty cheap.
            //
            UnsafeNativeMethods.SendMessage(new HandleRef(this, Handle), NativeMethods.WM_WININICHANGE, 0, 0);
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.SetBoundsCore"]/*' />
        /// <devdoc>
        ///     Overrides Control.setBoundsCore to enforce autoSize.
        /// </devdoc>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
            //SetBoundsCore .. sets the height for a control in designer .. we should obey the requested 
            //height is Autosize is false..
            //if (IsHandleCreated) {
                requestedDim = (orientation == Orientation.Horizontal)
                               ? height
                               : width;

                if (autoSize) {
                    if (orientation == Orientation.Horizontal) {
                        if ((specified & BoundsSpecified.Height) != BoundsSpecified.None)
                            height = PreferredDimension;
                    }
                    else {
                        if ((specified & BoundsSpecified.Width) != BoundsSpecified.None)
                            width = PreferredDimension;
                    }
                }
            //}
            base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.SetRange"]/*' />
        /// <devdoc>
        ///     Lets you set the the entire range for the TrackBar control at once.
        ///     The values passed are both the lower and upper limits to the range
        ///     with which the control will work.
        /// </devdoc>
        public void SetRange(int minValue, int maxValue) {
            if (minimum != minValue || maximum != maxValue) {
            
                // The Minimum and Maximum properties contain the logic for
                // ensuring that minValue <= maxValue. It is possible, however,
                // that this function will be called somewhere other than from
                // these two properties, so we'll check that here anyway.
                // CONSIDER: Perhaps there's a better way of doing this
                if (minValue > maxValue) {
                    // We'll just adjust maxValue to match minValue
                    maxValue = minValue;
                }

                minimum = minValue;
                maximum = maxValue;

                if (IsHandleCreated) {
                    SendMessage(NativeMethods.TBM_SETRANGEMIN, 0, minimum);

                    // We must repaint the trackbar after changing
                    // the range. The '1' in the call to
                    // SendMessage below indicates that the trackbar
                    // should be redrawn (see TBM_SETRANGEMAX in MSDN)
                    SendMessage(NativeMethods.TBM_SETRANGEMAX, 1, maximum);

                    Invalidate();
                }
                
                // When we change the range, the comctl32 trackbar's internal position can change 
                // (because of the reflection that occurs with vertical trackbars)
                // so we make sure to explicitly set the trackbar position.
                //
                if (value < minimum) {
                    value = minimum;
                }
                if (value > maximum) {
                    value = maximum;
                }
                SetTrackBarPosition();
            }
        }
        
        private void SetTrackBarPosition() {
            if (IsHandleCreated) {
            
                // There are two situations where we want to reflect the track bar position:
                //
                // 1. For a vertical trackbar, it seems to make more sense for the trackbar to increase in value
                //    as the slider moves up the trackbar (this is opposite what the underlying winctl control does)
                //
                // 2. For a RightToLeft horizontal trackbar, we want to reflect the position.
                //
                int reflectedValue = value;
                
                // 1. Reflect for a vertical trackbar
                //
                if (orientation == Orientation.Vertical) {
                    reflectedValue = Minimum + Maximum - value;
                }
                
                // 2. Reflect for a RightToLeft horizontal trackbar
                //
                if (orientation == Orientation.Horizontal && RightToLeft == RightToLeft.Yes) {
                    reflectedValue = Minimum + Maximum - value;
                }
                
                SendMessage(NativeMethods.TBM_SETPOS, 1, reflectedValue);
            }
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.ToString"]/*' />
        /// <devdoc>
        ///     Returns a string representation for this control.
        /// </devdoc>
        /// <internalonly/>
        public override string ToString() {

            string s = base.ToString();
            return s + ", Minimum: " + Minimum.ToString() + ", Maximum: " + Maximum.ToString() + ", Value: " + Value.ToString();
        }

        /// <include file='doc\TrackBar.uex' path='docs/doc[@for="TrackBar.WndProc"]/*' />
        /// <devdoc>
        ///     The button's window procedure.  Inheriting classes can override this
        ///     to add extra functionality, but should not forget to call
        ///     base.wndProc(m); to ensure the button continues to function properly.
        /// </devdoc>
        /// <internalonly/>
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case NativeMethods.WM_REFLECT+NativeMethods.WM_HSCROLL:
                case NativeMethods.WM_REFLECT+NativeMethods.WM_VSCROLL:
                    switch ((int)m.WParam & 0x0000FFFF) {
                        case NativeMethods.TB_LINEUP:
                        case NativeMethods.TB_LINEDOWN:
                        case NativeMethods.TB_PAGEUP:
                        case NativeMethods.TB_PAGEDOWN:
                            //case NativeMethods.TB_THUMBPOSITION:
                        case NativeMethods.TB_THUMBTRACK:
                        case NativeMethods.TB_TOP:
                        case NativeMethods.TB_BOTTOM:
                        case NativeMethods.TB_ENDTRACK:
                            if (value != Value) {
                                OnScroll(EventArgs.Empty);
                                OnValueChanged(EventArgs.Empty);
                            }
                            break;
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }
}
