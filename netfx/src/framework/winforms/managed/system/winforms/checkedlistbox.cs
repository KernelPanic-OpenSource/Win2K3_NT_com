//------------------------------------------------------------------------------
// <copyright file="CheckedListBox.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Windows.Forms {
    using System.Text;
    using System.Runtime.Remoting;

    using System.Diagnostics;

    using System;
    using System.Security.Permissions;
    using System.Collections;
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel;

    using System.Drawing;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    using Hashtable = System.Collections.Hashtable;
    using Microsoft.Win32;

    using System.Drawing.Design;

    /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox"]/*' />
    /// <devdoc>
    ///    <para>
    ///
    ///       Displays a list with a checkbox to the left
    ///
    ///       of each item.
    ///
    ///    </para>
    /// </devdoc>
    public class CheckedListBox : ListBox {

        private const int idealCheckSize = 13;

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.killnextselect"]/*' />
        /// <devdoc>
        ///     Decides whether or not to ignore the next LBN_SELCHANGE
        ///     message - used to prevent cursor keys from toggling checkboxes
        /// </devdoc>
        private bool killnextselect = false;

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.onItemCheck"]/*' />
        /// <devdoc>
        ///     Current listener of the onItemCheck event.
        /// </devdoc>
        private ItemCheckEventHandler onItemCheck;

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.checkOnClick"]/*' />
        /// <devdoc>
        ///     Indicates whether or not we should toggle check state on the first
        ///     click on an item, or whether we should wait for the user to click
        ///     again.
        /// </devdoc>
        private bool checkOnClick = false;

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.flat"]/*' />
        /// <devdoc>
        ///     Should we use 3d checkboxes or flat ones?
        /// </devdoc>
        private bool flat = true;

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.lastSelected"]/*' />
        /// <devdoc>
        ///     Indicates which item was last selected.  We want to keep track
        ///     of this so we can be a little less aggressive about checking/
        ///     unchecking the items as the user moves around.
        /// </devdoc>
        private int lastSelected = -1;
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.checkedItemCollection"]/*' />
        /// <devdoc>
        ///     The collection of checked items in the CheckedListBox.
        /// </devdoc>                                                                     
        private CheckedItemCollection checkedItemCollection = null;
        private CheckedIndexCollection checkedIndexCollection = null;

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedListBox"]/*' />
        /// <devdoc>
        ///     Creates a new CheckedListBox for the user.
        /// </devdoc>
        public CheckedListBox() : base() {
            // If we eat WM_ERASEBKGRND messages, the background will be 
            // painted sometimes but not others. See ASURT 28545.
            // SetStyle(ControlStyles.Opaque, true); 

            // If a long item is drawn with ellipsis, we must redraw the ellipsed part
            // as well as the newly uncovered region.
            SetStyle(ControlStyles.ResizeRedraw, true);
            
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckOnClick"]/*' />
        /// <devdoc>
        ///     Indicates whether or not the checkbox should be toggled whenever an
        ///     item is selected.  The default behaviour is to just change the
        ///     selection, and then make the user click again to check it.  However,
        ///     some may prefer checking the item as soon as it is clicked.
        /// </devdoc>
        [
        SRCategory(SR.CatBehavior),
        DefaultValue(false),
        SRDescription(SR.CheckedListBoxCheckOnClickDescr)
        ]
        public bool CheckOnClick {
            get {
                return checkOnClick;
            }

            set {
                checkOnClick = value;
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndices"]/*' />
        /// <devdoc>
        ///     Collection of checked indices in this CheckedListBox.
        /// </devdoc>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public CheckedIndexCollection CheckedIndices {
            get {
                if (checkedIndexCollection == null) {
                    checkedIndexCollection = new CheckedIndexCollection(this);
                }
                return checkedIndexCollection;
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItems"]/*' />
        /// <devdoc>
        ///     Collection of checked items in this CheckedListBox.
        /// </devdoc>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public CheckedItemCollection CheckedItems {
            get {
                if (checkedItemCollection == null) {
                    checkedItemCollection = new CheckedItemCollection(this);
                }
                return checkedItemCollection;
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CreateParams"]/*' />
        /// <devdoc>
        ///     This is called when creating a window.  Inheriting classes can ovveride
        ///     this to add extra functionality, but should not forget to first call
        ///     base.CreateParams() to make sure the control continues to work
        ///     correctly.
        /// </devdoc>
        protected override CreateParams CreateParams {
            [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
            get {
                CreateParams cp = base.CreateParams;
                cp.Style |= NativeMethods.LBS_OWNERDRAWFIXED | NativeMethods.LBS_WANTKEYBOARDINPUT;
                return cp;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.DataSource"]/*' />
        /// <devdoc>
        ///     CheckedListBox DataSource.
        /// </devdoc>
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new object DataSource {
            get {
                return base.DataSource;
            }
            set {
                base.DataSource = value;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.DisplayMember"]/*' />
        /// <devdoc>
        ///     CheckedListBox DisplayMember.
        /// </devdoc>
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new string DisplayMember {
            get {
                return base.DisplayMember ;
            }
            set {
                base.DisplayMember = value;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.DrawMode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [
        Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override DrawMode DrawMode {
            get {
                return DrawMode.Normal;
            }
            set {
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ItemHeight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [
        Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public override int ItemHeight {
            get {
                return Font.Height + 2;      // 2 = 2 * border size          
            }
            set {                
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.Items"]/*' />
        /// <devdoc>
        ///     Collection of items in this listbox.
        /// </devdoc>
        [
        SRCategory(SR.CatData),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        Localizable(true),
        SRDescription(SR.ListBoxItemsDescr),
        Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, " + AssemblyRef.SystemDesign, typeof(UITypeEditor))
        ]
        new public CheckedListBox.ObjectCollection Items {
            get {
                return(CheckedListBox.ObjectCollection)base.Items;
            }
        }

        // Computes the maximum width of all items in the ListBox
        //             
        internal override int MaxItemWidth {
            get {
                // Overridden to include the size of the checkbox
                // Allows for one pixel either side of the checkbox, plus another 1 pixel buffer = 3 pixels
                //
                return base.MaxItemWidth + idealCheckSize + 3;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.SelectionMode"]/*' />
        /// <devdoc>
        ///     For CheckedListBoxes, multi-selection is not supported.  You can set
        ///     selection to be able to select one item or no items.
        /// </devdoc>
        public override SelectionMode SelectionMode {
            get {
                return base.SelectionMode;
            }
            set {
                if ( !Enum.IsDefined(typeof(SelectionMode), value) )
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SelectionMode));
        
                if (value != SelectionMode.One
                    && value != SelectionMode.None) {
                    throw new ArgumentException(SR.GetString(SR.CheckedListBoxInvalidSelectionMode));
                }

                if (value != SelectionMode) {
                    base.SelectionMode = value;
                    RecreateHandle();
                }
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ThreeDCheckBoxes"]/*' />
        /// <devdoc>
        ///     Indicates if the CheckBoxes should show up as flat or 3D in appearance.
        /// </devdoc>
        [
        SRCategory(SR.CatAppearance),
        DefaultValue(false),
        SRDescription(SR.CheckedListBoxThreeDCheckBoxesDescr)
        ]
        public bool ThreeDCheckBoxes {
            get {
                return !flat;
            }
            set {
                // change the style and repaint.
                //
                if (flat == value) {
                    flat = !value;

                    // see if we have some items, and only invalidate if we do.                    
                    CheckedListBox.ObjectCollection items = (CheckedListBox.ObjectCollection) Items;
                    if ((items != null) && (items.Count > 0)) {
                        this.Invalidate();
                    }
                }
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ValueMember"]/*' />
        /// <devdoc>
        ///     CheckedListBox ValueMember.
        /// </devdoc>
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new string ValueMember {
            get {
                return base.ValueMember;
            }
            set {
                base.ValueMember = value;
            }
        }


        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="ComboBox.DataSourceChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler DataSourceChanged {
            add {
                base.DataSourceChanged += value;
            }
            remove {
                base.DataSourceChanged -= value;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="ComboBox.DisplayMemberChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler DisplayMemberChanged {
            add {
                base.DisplayMemberChanged += value;
            }
            remove {
                base.DisplayMemberChanged -= value;
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ItemCheck"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [SRCategory(SR.CatBehavior), SRDescription(SR.CheckedListBoxItemCheckDescr)]
        public event ItemCheckEventHandler ItemCheck {
            add {
                onItemCheck += value;
            }
            remove {
                onItemCheck -= value;
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.Click"]/*' />
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

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.DrawItem"]/*' />
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event DrawItemEventHandler DrawItem {
            add {
                base.DrawItem += value;
            }
            remove {
                base.DrawItem -= value;
            }
        }        

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.MeasureItem"]/*' />
        /// <internalonly/><hideinheritance/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public new event MeasureItemEventHandler MeasureItem {
            add {
                base.MeasureItem += value;
            }
            remove {
                base.MeasureItem -= value;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="ComboBox.ValueMemberChanged"]/*' />
        /// <internalonly/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        new public event EventHandler ValueMemberChanged {
            add {
                base.ValueMemberChanged += value;
            }
            remove {
                base.ValueMemberChanged -= value;
            }
        }

        internal override int ComputeMaxItemWidth(int oldMax) {
            int maxItemWidth = oldMax;

            using (Graphics g = CreateGraphics()) {
                foreach(object item in Items) {
                    int width = (int)Math.Ceiling(g.MeasureString(item.ToString(), Font).Width);

                    if (width > maxItemWidth) {
                        maxItemWidth = width;
                    }
                }
            }

            return maxItemWidth;
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CreateAccessibilityInstance"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    Constructs the new instance of the accessibility object for this control. Subclasses
        ///    should not call base.CreateAccessibilityObject.
        /// </devdoc>
        protected override AccessibleObject CreateAccessibilityInstance() {
            return new CheckedListBoxAccessibleObject(this);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CreateItemCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override ListBox.ObjectCollection CreateItemCollection() {
            return new ObjectCollection(this);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.GetItemCheckState"]/*' />
        /// <devdoc>
        ///     Gets the check value of the current item.  This value will be from the
        ///     System.Windows.Forms.CheckState enumeration.
        /// </devdoc>
        public CheckState GetItemCheckState(int index) {

            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                          "index",
                                                          (index).ToString()));
            return CheckedItems.GetCheckedState(index);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.GetItemChecked"]/*' />
        /// <devdoc>
        ///     Indicates if the given item is, in any way, shape, or form, checked.
        ///     This will return true if the item is fully or indeterminately checked.
        /// </devdoc>
        public bool GetItemChecked(int index) {
            return(GetItemCheckState(index) != CheckState.Unchecked);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.InvalidateItem"]/*' />
        /// <devdoc>
        ///     Invalidates the given item in the listbox
        /// </devdoc>
        /// <internalonly/>
        private void InvalidateItem(int index) {
            if (IsHandleCreated) {
                NativeMethods.RECT rect = new NativeMethods.RECT();
                SendMessage(NativeMethods.LB_GETITEMRECT, index, ref rect);
                SafeNativeMethods.InvalidateRect(new HandleRef(this, Handle), ref rect, false);
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.LbnSelChange"]/*' />
        /// <devdoc>
        ///     A redirected LBN_SELCHANGE message notification.
        /// </devdoc>
        /// <internalonly/>
        private void LbnSelChange() {

            // prepare to change the selection.  we'll fire an event for
            // this.  Note that we'll only change the selection when the
            // user clicks again on a currently selected item, or when the
            // user has CheckOnClick set to true.  Otherwise
            // just using the up and down arrows selects or unselects
            // every item around town ...
            //

            // Get the index of the item to check/uncheck
            int index = SelectedIndex;
            
            // make sure we have a valid index, otherwise we're going to
            // fail ahead...
            if (index < 0 || index >= Items.Count)
                return;
                
            // Send an accessibility notification
            //
            AccessibilityNotifyClients(AccessibleEvents.Focus, index);
            AccessibilityNotifyClients(AccessibleEvents.Selection, index);

                       
            //# VS7 86
            if (!killnextselect && (index == lastSelected || checkOnClick == true)) {
                CheckState currentValue = CheckedItems.GetCheckedState(index);
                CheckState newValue = (currentValue != CheckState.Unchecked)
                                      ? CheckState.Unchecked
                                      : CheckState.Checked;

                ItemCheckEventArgs itemCheckEvent = new ItemCheckEventArgs(index, newValue, currentValue);
                OnItemCheck(itemCheckEvent);

                // take whatever value the user set, and set that as the value.
                //
                CheckedItems.SetCheckedState(index, itemCheckEvent.NewValue);
                
            }

            lastSelected = index;
            InvalidateItem(index);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnClick"]/*' />
        /// <devdoc>
        ///     Ensures that mouse clicks can toggle...
        /// </devdoc>
        /// <internalonly/>
        protected override void OnClick(EventArgs e) {
            killnextselect = false;
            base.OnClick(e);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnHandleCreated"]/*' />
        /// <devdoc>
        ///     When the handle is created we can dump any cached item-check pairs.
        /// </devdoc>
        /// <internalonly/>
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SendMessage(NativeMethods.LB_SETITEMHEIGHT, 0, ItemHeight);            
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnDrawItem"]/*' />
        /// <devdoc>
        ///     Actually goes and fires the drawItem event.  Inheriting controls
        ///     should use this to know when the event is fired [this is preferable to
        ///     adding an event handler yourself for this event].  They should,
        ///     however, remember to call base.OnDrawItem(e); to ensure the event is
        ///     still fired to external listeners
        /// </devdoc>
        protected override void OnDrawItem(DrawItemEventArgs e) {
            object item;

            if (e.Index >= 0) {

                if (e.Index < Items.Count) {
                    item = Items[e.Index];
                }
                else {
                    // If the item is not part of our collection, we will just
                    // get the string for it and display it.
                    //
                    int len = (int)SendMessage(NativeMethods.LB_GETTEXTLEN, e.Index, 0);
                    StringBuilder sb = new StringBuilder(len + 1);
                    UnsafeNativeMethods.SendMessage(new HandleRef(this, Handle), NativeMethods.LB_GETTEXT, e.Index, sb);
                    item = sb.ToString();
                }

                Rectangle bounds = e.Bounds;
                int border = 1;
                int height = Font.Height + 2 * border;

                // Determine bounds for the checkbox
                //
                int centeringFactor = Math.Max((height - idealCheckSize) / 2, 0);

                // Keep the checkbox within the item's upper and lower bounds
                if (centeringFactor + idealCheckSize > bounds.Height) {
                    centeringFactor = bounds.Height - idealCheckSize;
                }

                Rectangle box = new Rectangle(bounds.X + border,
                                              bounds.Y + centeringFactor,
                                              idealCheckSize,
                                              idealCheckSize);
                if (RightToLeft == RightToLeft.Yes) {
                    // For a RightToLeft checked list box, we want the checkbox
                    // to be drawn at the right.
                    // So we override the X position.
                    box.X = bounds.X + bounds.Width - idealCheckSize - border;
                }

                // set up the appearance of the checkbox
                //
                ButtonState state = ButtonState.Normal;
                if (flat) {
                    state |= ButtonState.Flat;
                }
                if (e.Index < Items.Count) {
                    switch (CheckedItems.GetCheckedState(e.Index)) {
                        case CheckState.Checked:
                            state |= ButtonState.Checked;
                            break;
                        case CheckState.Indeterminate:
                            state |= ButtonState.Checked | ButtonState.Inactive;
                            break;
                    }
                }

                // Draw the checkbox.
                //
                ControlPaint.DrawCheckBox(e.Graphics, box, state);
                // Determine bounds for the text portion of the item
                //
                Rectangle textBounds = new Rectangle(
                                                    bounds.X + idealCheckSize + (border * 2),
                                                    bounds.Y,
                                                    bounds.Width - (idealCheckSize + (border * 2)) ,
                                                    bounds.Height);
                if (RightToLeft == RightToLeft.Yes) {
                    // For a RightToLeft checked list box, we want the text
                    // to be drawn at the left.
                    // So we override the X position.
                    textBounds.X = bounds.X;
                }

                // Setup text font, color, and text
                //
                string text = "";
                Color backColor = BackColor;
                Color foreColor = ForeColor;
                if (!Enabled) {
                    foreColor = SystemColors.GrayText;
                }
                Font font = Font;

                object value = FilterItemOnProperty(item);
                
                if (value != null) {
                    text = value.ToString();
                }

                if (SelectionMode != SelectionMode.None && (e.State & DrawItemState.Selected) == DrawItemState.Selected) {
                    backColor = SystemColors.Highlight;
                    foreColor = SystemColors.HighlightText;
                }

                // Draw the text
                //

                // Due to some sort of unpredictable painting optimization in the Windows ListBox control,
                // we need to always paint the background rectangle for the current line.
                using (Brush b = new SolidBrush(backColor)) {
                    e.Graphics.FillRectangle(b, textBounds);
                }

                Rectangle stringBounds = new Rectangle(
                                                      textBounds.X + 1,
                                                      textBounds.Y + border,
                                                      textBounds.Width - 1,
                                                      textBounds.Height - border * 2);

                using (StringFormat format = new StringFormat()) {
                    if (UseTabStops) {
                        //  Set tab stops so it looks similar to a ListBox, at least with the default font size.
                        float tabDistance = 3.6f * Font.Height; // about 7 characters
                        float[] tabStops = new float[15];
                        float tabOffset = -(idealCheckSize + (border * 2));
                        for (int i = 1; i < tabStops.Length; i++)
                            tabStops[i] = tabDistance;

                        //(bug 111825)
                        if (Math.Abs(tabOffset) < tabDistance) {
                            tabStops[0] =  tabDistance +tabOffset;
                        }
                        else {
                            tabStops[0] =  tabDistance;
                        }
                            

                        
                        format.SetTabStops(0, tabStops);
                    }

                    // Adjust string format for Rtl controls
                    if (RightToLeft == RightToLeft.Yes) {
                        format.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                    }

                    // ListBox doesn't word-wrap its items, so neither should CheckedListBox
                    //                                
                    format.FormatFlags |= StringFormatFlags.NoWrap;

                    // Do actual drawing
                    using (SolidBrush brush = new SolidBrush(foreColor)) {
                        e.Graphics.DrawString(text, font, brush, stringBounds, format);
                    }
                }

                // Draw the focus rect if required
                //
                if ((e.State & DrawItemState.Focus) == DrawItemState.Focus && 
                    (e.State & DrawItemState.NoFocusRect) != DrawItemState.NoFocusRect) {
                    ControlPaint.DrawFocusRectangle(e.Graphics, textBounds, foreColor, backColor);
                }
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnBackColorChanged"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            
            if (IsHandleCreated) {
                SafeNativeMethods.InvalidateRect(new HandleRef(this, Handle), null, true);
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnFontChanged"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnFontChanged(EventArgs e) {
        
            // Update the item height
            //
            if (IsHandleCreated) {
                SendMessage(NativeMethods.LB_SETITEMHEIGHT, 0, ItemHeight);            
            }
            
            // The base OnFontChanged will adjust the height of the CheckedListBox accordingly
            //
            base.OnFontChanged(e);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnKeyPress"]/*' />
        /// <devdoc>
        ///     This is the code that actually fires the "keyPress" event.  The Checked
        ///     ListBox overrides this to look for space characters, since we
        ///     want to use those to check or uncheck items periodically.  Don't
        ///     forget to call base.OnKeyPress() to ensure that KeyPrese events
        ///     are correctly fired for all other keys.
        /// </devdoc>
        /// <internalonly/>
        protected override void OnKeyPress(KeyPressEventArgs e) {
            if (e.KeyChar == ' '
                && SelectionMode != SelectionMode.None)
                LbnSelChange();
            else
                base.OnKeyPress(e);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnItemCheck"]/*' />
        /// <devdoc>
        ///     This is the code that actually fires the itemCheck event.  Don't
        ///     forget to call base.onItemCheck() to ensure that itemCheck vents
        ///     are correctly fired for all other keys.
        /// </devdoc>
        /// <internalonly/>
        protected virtual void OnItemCheck(ItemCheckEventArgs ice) {
            if (onItemCheck != null) onItemCheck(this, ice);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnMeasureItem"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnMeasureItem(MeasureItemEventArgs e) {
            base.OnMeasureItem(e);

            // we'll use the ideal checkbox size plus enough for padding on the top
            // and bottom
            //
            if (e.ItemHeight < idealCheckSize + 2) {
                e.ItemHeight = idealCheckSize + 2;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.OnSelectedIndexChanged"]/*' />
        /// <devdoc>
        ///     Actually goes and fires the selectedIndexChanged event.  Inheriting controls
        ///     should use this to know when the event is fired [this is preferable to
        ///     adding an event handler on yourself for this event].  They should,
        ///     however, remember to call base.OnSelectedIndexChanged(e); to ensure the event is
        ///     still fired to external listeners
        /// </devdoc>
        protected override void OnSelectedIndexChanged(EventArgs e) {
            
            base.OnSelectedIndexChanged(e);
            lastSelected = SelectedIndex;
            
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.SetItemCheckState"]/*' />
        /// <devdoc>
        ///     Sets the checked value of the given item.  This value should be from
        ///     the System.Windows.Forms.CheckState enumeration.
        /// </devdoc>
        public void SetItemCheckState(int index, CheckState value) {
            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                          "index",
                                                          (index).ToString()));
            if ( !Enum.IsDefined(typeof(CheckState), value)) 
                throw new InvalidEnumArgumentException("value", (int)value, typeof(CheckState));
        
            CheckState currentValue = CheckedItems.GetCheckedState(index);
            
            if (value != currentValue) {
                ItemCheckEventArgs itemCheckEvent = new ItemCheckEventArgs(index, value, currentValue);
                OnItemCheck(itemCheckEvent);

                if (itemCheckEvent.NewValue != currentValue) {
                    CheckedItems.SetCheckedState(index, itemCheckEvent.NewValue);
                    InvalidateItem(index);
                }
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.SetItemChecked"]/*' />
        /// <devdoc>
        ///     Sets the checked value of the given item.  This value should be a
        ///     boolean.
        /// </devdoc>
        public void SetItemChecked(int index, bool value) {
            SetItemCheckState(index, value ? CheckState.Checked : CheckState.Unchecked);
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.WmReflectCommand"]/*' />
        /// <devdoc>
        ///     We need to get LBN_SELCHANGE notifications
        /// </devdoc>
        /// <internalonly/>
        [
        System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)
        ]
        protected override void WmReflectCommand(ref Message m) {
            switch ((int)m.WParam >> 16) {
                case NativeMethods.LBN_SELCHANGE:
                    LbnSelChange();
                    // finally, fire the OnSelectionChange event.
                    base.WmReflectCommand(ref m);
                    break;
                    
                case NativeMethods.LBN_DBLCLK:
                    // We want double-clicks to change the checkstate on each click - just like the CheckBox control
                    //
                    LbnSelChange();
                    base.WmReflectCommand(ref m);
                    break;

                default:
                    base.WmReflectCommand(ref m);
                    break;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.WmReflectVKeyToItem"]/*' />
        /// <devdoc>
        ///     Handle keyboard input to prevent arrow keys from toggling
        ///     checkboxes in CheckOnClick mode.
        /// </devdoc>
        /// <internalonly/>
        private void WmReflectVKeyToItem(ref Message m) {
            int keycode = (int)m.WParam & 0xFFFF;
            switch ((Keys)keycode) {
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Left:
                case Keys.Right:
                    killnextselect = true;
                    break;
                default:
                    killnextselect = false;
                    break;
            }
            m.Result = NativeMethods.InvalidIntPtr;
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.WndProc"]/*' />
        /// <devdoc>
        ///     The listbox's window procedure.  Inheriting classes can override this
        ///     to add extra functionality, but should not forget to call
        ///     base.wndProc(m); to ensure the button continues to function properly.
        /// </devdoc>
        /// <internalonly/>
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m) {

            switch (m.Msg) {
                case NativeMethods.WM_REFLECT + NativeMethods.WM_CHARTOITEM:
                    m.Result = NativeMethods.InvalidIntPtr;
                    break;
                case NativeMethods.WM_REFLECT + NativeMethods.WM_VKEYTOITEM:
                    WmReflectVKeyToItem(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ObjectCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        new public class ObjectCollection : ListBox.ObjectCollection {
            private CheckedListBox owner;

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ObjectCollection.ObjectCollection"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public ObjectCollection(CheckedListBox owner)
            : base(owner) {
                this.owner = owner;
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ObjectCollection.Add"]/*' />
            /// <devdoc>
            ///     Lets the user add an item to the listbox with the given initial value
            ///     for the Checked portion of the item.
            /// </devdoc>
            public int Add(object item, bool isChecked) {
                return Add(item, isChecked ? CheckState.Checked : CheckState.Unchecked);
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.ObjectCollection.Add1"]/*' />
            /// <devdoc>
            ///     Lets the user add an item to the listbox with the given initial value
            ///     for the Checked portion of the item.
            /// </devdoc>
            public int Add(object item, CheckState check) {

                //validate the enum that's passed in here
                //
                if (!Enum.IsDefined(typeof(CheckState), check)) {
                    throw new InvalidEnumArgumentException("value", (int)check, typeof(CheckState));
                }
                
                int index = base.Add(item);
                owner.SetItemCheckState(index, check);

                return index;
            }
        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public class CheckedIndexCollection : IList {
            private CheckedListBox owner;

            internal CheckedIndexCollection(CheckedListBox owner) {
                this.owner = owner;
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.Count"]/*' />
            /// <devdoc>
            ///     Number of current checked items.
            /// </devdoc>
            public int Count {
                get {
                    return owner.CheckedItems.Count;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.ICollection.SyncRoot"]/*' />
            /// <internalonly/>
            object ICollection.SyncRoot {
                get {
                    return this;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.ICollection.IsSynchronized"]/*' />
            /// <internalonly/>
            bool ICollection.IsSynchronized {
                get {
                    return false;
                }
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.IsFixedSize"]/*' />
            /// <internalonly/>
            bool IList.IsFixedSize {
                get {
                    return true;
                }
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.IsReadOnly"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool IsReadOnly {
                get {
                    return true;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.this"]/*' />
            /// <devdoc>
            ///     Retrieves the specified checked item.
            /// </devdoc>
            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int this[int index] {
                get {
                    object identifier = InnerArray.GetEntryObject(index, CheckedItemCollection.AnyMask);
                    return InnerArray.IndexOfIdentifier(identifier, 0);
                }
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.this"]/*' />
            /// <internalonly/>
            object IList.this[int index] {
                get {
                    return this[index];
                }
                set {
                    throw new NotSupportedException();
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.Add"]/*' />
            /// <internalonly/>
            int IList.Add(object value) {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.Clear"]/*' />
            /// <internalonly/>
            void IList.Clear() {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.Insert"]/*' />
            /// <internalonly/>
            void IList.Insert(int index, object value) {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.Remove"]/*' />
            /// <internalonly/>
            void IList.Remove(object value) {
                throw new NotSupportedException();
            }                                        
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.RemoveAt"]/*' />
            /// <internalonly/>
            void IList.RemoveAt(int index) {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.Contains"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool Contains(int index) {
                 return (IndexOf(index) != -1);
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.Contains"]/*' />
            /// <internalonly/>
            bool IList.Contains(object index) {
                if (index is Int32) {
                    return Contains((int)index);
                }
                else {
                    return false;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.CopyTo"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public void CopyTo(Array dest, int index) {
                int cnt = owner.CheckedItems.Count;
                for (int i = 0; i < cnt; i++) {
                    dest.SetValue(this[i], i + index);
                }
            }
            
            /// <devdoc>
            ///     This is the item array that stores our data.  We share this backing store
            ///     with the main object collection.
            /// </devdoc>
            private ItemArray InnerArray {
                get {
                    return ((ObjectCollection)owner.Items).InnerArray;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.GetEnumerator"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public IEnumerator GetEnumerator() {
                int[] indices = new int[this.Count];
                CopyTo(indices, 0);
                return indices.GetEnumerator();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedIndexCollection.IndexOf"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public int IndexOf(int index) {
                if (index >= 0 && index < owner.Items.Count) {
                    object value = InnerArray.GetEntryObject(index, 0);
                    return owner.CheckedItems.IndexOfIdentifier(value);
                }
                return -1;
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedIndexCollection.IList.IndexOf"]/*' />
            /// <internalonly/>
            int IList.IndexOf(object index) {
                if (index is Int32) {
                    return IndexOf((int)index);
                }
                else {
                    return -1;
                }
            }

        }
        
        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public class CheckedItemCollection : IList {
        
            internal static int CheckedItemMask = ItemArray.CreateMask();
            internal static int IndeterminateItemMask = ItemArray.CreateMask();
            internal static int AnyMask = CheckedItemMask | IndeterminateItemMask;

            private CheckedListBox owner;

            internal CheckedItemCollection(CheckedListBox owner) {
                this.owner = owner;
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.Count"]/*' />
            /// <devdoc>
            ///     Number of current checked items.
            /// </devdoc>
            public int Count {
                get {
                    return InnerArray.GetCount(AnyMask);
                }
            }

            /// <devdoc>
            ///     This is the item array that stores our data.  We share this backing store
            ///     with the main object collection.
            /// </devdoc>
            private ItemArray InnerArray {
                get {
                    return ((ListBox.ObjectCollection)owner.Items).InnerArray;
                }
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.this"]/*' />
            /// <devdoc>
            ///     Retrieves the specified checked item.
            /// </devdoc>
            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public object this[int index] {
                get {
                    return InnerArray.GetItem(index, AnyMask);
                }
                set {
                    throw new NotSupportedException();
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.ICollection.SyncRoot"]/*' />
            /// <internalonly/>
            object ICollection.SyncRoot {
                get {
                    return this;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.ICollection.IsSynchronized"]/*' />
            /// <internalonly/>
            bool ICollection.IsSynchronized {
                get {
                    return false;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.IList.IsFixedSize"]/*' />
            /// <internalonly/>
            bool IList.IsFixedSize {
                get {
                    return true;
                }
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.IsReadOnly"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool IsReadOnly {
                get {
                    return true;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.Contains"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool Contains(object item) {
                return IndexOf(item) != -1;            
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.IndexOf"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public int IndexOf(object item) {
                return InnerArray.IndexOf(item, AnyMask);
            }
            
            internal int IndexOfIdentifier(object item) {
                return InnerArray.IndexOfIdentifier(item, AnyMask);
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.IList.Add"]/*' />
            /// <internalonly/>
            int IList.Add(object value) {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.IList.Clear"]/*' />
            /// <internalonly/>
            void IList.Clear() {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.IList.Insert"]/*' />
            /// <internalonly/>
            void IList.Insert(int index, object value) {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.IList.Remove"]/*' />
            /// <internalonly/>
            void IList.Remove(object value) {
                throw new NotSupportedException();
            }                                        
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedItemCollection.IList.RemoveAt"]/*' />
            /// <internalonly/>
            void IList.RemoveAt(int index) {
                throw new NotSupportedException();
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.CopyTo"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public void CopyTo(Array dest, int index) {
                int cnt = InnerArray.GetCount(AnyMask);
                for (int i = 0; i < cnt; i++) {
                    dest.SetValue(InnerArray.GetItem(i, AnyMask), i + index);
                }
            }
            
            /// <devdoc>
            ///     This method returns if the actual item index is checked.  The index is the index to the MAIN
            ///     collection, not this one.
            /// </devdoc>
            internal CheckState GetCheckedState(int index) {
                bool isChecked = InnerArray.GetState(index, CheckedItemMask);
                bool isIndeterminate = InnerArray.GetState(index, IndeterminateItemMask);
                Debug.Assert(!isChecked || !isIndeterminate, "Can't be both checked and indeterminate.  Somebody screwed up our state.");
                if (isIndeterminate) {
                    return CheckState.Indeterminate;
                }
                else if (isChecked) {
                    return CheckState.Checked;
                }
                
                return CheckState.Unchecked;
            }
            
            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedItemCollection.GetEnumerator"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public IEnumerator GetEnumerator() {
                return InnerArray.GetEnumerator(AnyMask, true);
            }
        
            /// <devdoc>
            ///     Same thing for GetChecked.
            /// </devdoc>
            internal void SetCheckedState(int index, CheckState value) {
                bool isChecked;
                bool isIndeterminate;
                
                switch(value) {
                    case CheckState.Checked:
                        isChecked = true;
                        isIndeterminate = false;
                        break;
                        
                    case CheckState.Indeterminate:
                        isChecked = false;
                        isIndeterminate = true;
                        break;
                        
                    default:
                        isChecked = false;
                        isIndeterminate = false;
                        break;
                }
                InnerArray.SetState(index, CheckedItemMask, isChecked);
                InnerArray.SetState(index, IndeterminateItemMask, isIndeterminate);
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedListBoxAccessibleObject"]/*' />
        /// <internalonly/>        
        /// <devdoc>
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(true)]        
        internal class CheckedListBoxAccessibleObject : ControlAccessibleObject {

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedListBoxAccessibleObject.CheckedListBoxAccessibleObject"]/*' />
            /// <devdoc>
            /// </devdoc>
            public CheckedListBoxAccessibleObject(CheckedListBox owner) : base(owner) {
            }
            
            private CheckedListBox CheckedListBox {
                get {
                    return (CheckedListBox)Owner;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedListBoxAccessibleObject.GetChild"]/*' />
            /// <devdoc>
            /// </devdoc>
            public override AccessibleObject GetChild(int index) {
                if (index >= 0 && index < CheckedListBox.Items.Count) {
                    return new CheckedListBoxItemAccessibleObject(CheckedListBox.Items[index].ToString(), index, this);
                }
                else {
                    return null;
                }
            }

            /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedListBoxAccessibleObject.GetChildCount"]/*' />
            /// <devdoc>
            /// </devdoc>
            public override int GetChildCount() {
                return CheckedListBox.Items.Count;
            }
            
            public override AccessibleObject GetFocused() {
                int index = CheckedListBox.FocusedIndex;
                if (index >= 0) {
                    return GetChild(index);
                }
                
                return null;
            }
            
            public override AccessibleObject GetSelected() {
                int index = CheckedListBox.SelectedIndex;
                if (index >= 0) {
                    return GetChild(index);
                }
                
                return null;
            }
            
            public override AccessibleObject HitTest(int x, int y) {
            
                // Within a child element?
                //
                int count = GetChildCount();
                for(int index=0; index < count; ++index) {
                    AccessibleObject child = GetChild(index);
                    if (child.Bounds.Contains(x, y)) {
                        return child;
                    }
                }
                
                // Within the CheckedListBox bounds?
                //
                if (this.Bounds.Contains(x, y)) {
                    return this;
                }
                
                return null;
            }
            
            public override AccessibleObject Navigate(AccessibleNavigation direction) {
                if (GetChildCount() > 0) {
                    if (direction == AccessibleNavigation.FirstChild) {
                        return GetChild(0);
                    }
                    if (direction == AccessibleNavigation.LastChild) {
                        return GetChild(GetChildCount() - 1);
                    }
                }
                return base.Navigate(direction);
            }
        }

        /// <include file='doc\CheckedListBox.uex' path='docs/doc[@for="CheckedListBox.CheckedListBoxItemAccessibleObject"]/*' />
        /// <internalonly/>        
        /// <devdoc>
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(true)]        
        internal class CheckedListBoxItemAccessibleObject : AccessibleObject {

            private string name;
            private int index;
            private CheckedListBoxAccessibleObject parent;

            public CheckedListBoxItemAccessibleObject(string name, int index, CheckedListBoxAccessibleObject parent) : base() {
                this.name = name;
                this.parent = parent;
                this.index = index;
            }

            public override Rectangle Bounds {
                get {
                    Rectangle rect = ParentCheckedListBox.GetItemRectangle(index);

                    // Translate rect to screen coordinates
                    //
                    NativeMethods.POINT pt = new NativeMethods.POINT(rect.X, rect.Y);
                    UnsafeNativeMethods.ClientToScreen(new HandleRef(ParentCheckedListBox, ParentCheckedListBox.Handle), pt);

                    return new Rectangle(pt.x, pt.y, rect.Width, rect.Height);
                }
            }

            public override string DefaultAction {
                get {
                    if (ParentCheckedListBox.GetItemChecked(index)) {
                        return SR.GetString(SR.AccessibleActionUncheck);
                    }
                    else {
                        return SR.GetString(SR.AccessibleActionCheck);
                    }
                }
            }

            private CheckedListBox ParentCheckedListBox {
                get {
                    return(CheckedListBox)parent.Owner;
                }
            }

            public override string Name {
                get {
                    return name;
                }
                set {
                    name = value;
                }
            }

            public override AccessibleObject Parent {
                get {
                    return parent;
                }
            }

            public override AccessibleRole Role {
                get {
                    return AccessibleRole.ListItem;
                }
            }

            public override AccessibleStates State {
                get {
                    AccessibleStates state = AccessibleStates.Selectable | AccessibleStates.Focusable;

                    // Checked state
                    //
                    switch (ParentCheckedListBox.GetItemCheckState(index)) {
                        case CheckState.Checked:
                            state |= AccessibleStates.Checked;
                            break;
                        case CheckState.Indeterminate:
                            state |= AccessibleStates.Indeterminate;
                            break;
                        case CheckState.Unchecked:
                            // No accessible state corresponding to unchecked
                            break;
                    }

                    // Selected state
                    //
                    if (ParentCheckedListBox.SelectedIndex == index) {
                        state |= AccessibleStates.Selected | AccessibleStates.Focused;
                    }

                    return state;

                }
            }

            public override string Value {
                get {
                    return name;
                }
            }

            public override void DoDefaultAction() {
                ParentCheckedListBox.SetItemChecked(index, !ParentCheckedListBox.GetItemChecked(index));
            }
            
            public override AccessibleObject Navigate(AccessibleNavigation direction) {
                // Down/Next
                //
                if (direction == AccessibleNavigation.Down || 
                    direction == AccessibleNavigation.Next) {
                    if (index < parent.GetChildCount() - 1) {
                        return parent.GetChild(index + 1);
                    }
                }
                
                // Up/Previous
                //
                if (direction == AccessibleNavigation.Up ||
                    direction == AccessibleNavigation.Previous) {
                    if (index > 0) {
                        return parent.GetChild(index - 1);
                    }
                }
                
                return base.Navigate(direction);
            }
        }

    }
}
