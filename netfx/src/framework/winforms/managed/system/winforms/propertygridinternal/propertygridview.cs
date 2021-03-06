//------------------------------------------------------------------------------
// <copyright file="PropertyGridView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Windows.Forms.PropertyGridInternal {
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System;
    using System.Collections;   
    using System.Windows.Forms;
    using System.Windows.Forms.ComponentModel;
    using System.Windows.Forms.Design;    
    using System.ComponentModel.Design;
    using System.IO;
    using System.Drawing;
    using Microsoft.Win32;
    using Message = System.Windows.Forms.Message;
    using System.Drawing.Drawing2D;

    internal class PropertyGridView :
    Control,
    IWin32Window,
    IWindowsFormsEditorService,
    IServiceProvider {

        protected static readonly Point InvalidPoint = new Point(int.MinValue, int.MinValue);

#if true // RENDERMODE
        public const int RENDERMODE_LEFTDOT = 2;
        public const int RENDERMODE_BOLD = 3;
        public const int RENDERMODE_TRIANGLE = 4;

        public static int inheritRenderMode = RENDERMODE_BOLD;
#endif


        public static TraceSwitch GridViewDebugPaint = new TraceSwitch("GridViewDebugPaint", "PropertyGridView: Debug property painting");


        private PropertyGrid ownerGrid;                      // the properties window host.


#if true // RENDERMODE
        private const int LEFTDOT_SIZE = 4;
#endif
        // constants
        protected const int     EDIT_INDENT = 0;
        protected const int     OUTLINE_INDENT = 10;
        protected const int     OUTLINE_SIZE = 9;
        protected const int     PAINT_WIDTH = 20;
        protected const int     PAINT_INDENT = 26;
        protected const int     ROWLABEL = 1;
        protected const int     ROWVALUE = 2;
        protected const int     MAX_LISTBOX_HEIGHT = 200;

        protected const short    ERROR_NONE = 0;
        protected const short    ERROR_THROWN = 1;
        protected const short    ERROR_MSGBOX_UP = 2;
        internal  const short    GDIPLUS_SPACE = 2;
        internal   const int      MaxRecurseExpand = 10;

        protected static readonly Point InvalidPosition = new Point(int.MinValue, int.MinValue);


        // colors and fonts
        private Brush                               backgroundBrush = null;
        private   Font                              fontBold = null;

        // property collections
        private GridEntryCollection                 topLevelGridEntries = null;     // top level props
        private GridEntryCollection                 allGridEntries = null;  // cache of viewable props
        
        // row information
        internal   int                               totalProps = -1;        // # of viewable props
        private   int                               visibleRows = -1;         // # of visible rows
        private   int                               labelWidth = -1;
        public double                               labelRatio = 2; // ratio of whole row to label width
        
        private short                               requiredLabelPaintMargin = GDIPLUS_SPACE;

        // current selected row and tooltip.
        private   int                               selectedRow = -1;
        private GridEntry                           selectedGridEntry = null;
        private   int                               tipInfo = -1;

        // editors & controls
        private   GridViewEdit                      edit = null;
        private   Button                            btnDropDown = null;
        private   Button                            btnDialog = null;
        private   GridViewListBox                   listBox = null;
        private   DropDownHolder                    dropDownHolder = null;
        private   Rectangle                         lastClientRect = Rectangle.Empty;
        private   Control                           currentEditor = null;
        private   ScrollBar                         scrollBar = null;
        internal  GridToolTip                       toolTip = null;
        private   GridErrorDlg                      errorDlg = null;
        

        // flags
        private const short FlagNeedsRefresh            = 0x0001;
        private const short FlagIsNewSelection          = 0x0002;
        private const short FlagIsSplitterMove          = 0x0004;
        private const short FlagIsSpecialKey            = 0x0008;
        private const short FlagInPropertySet           = 0x0010;
        private const short FlagDropDownClosing         = 0x0020;
        private const short FlagDropDownCommit          = 0x0040;
        private const short FlagNeedUpdateUIBasedOnFont = 0x0080;
        private const short FlagBtnLaunchedEditor       = 0x0100;
        private const short FlagNoDefault               = 0x0200;
        

        private   short                             flags = FlagNeedsRefresh | FlagIsNewSelection | FlagNeedUpdateUIBasedOnFont;
        private   short                             errorState = ERROR_NONE;

        private   Point                             ptOurLocation = new Point(1,1);
        
        private   string                            originalTextValue = null;     // original text, in case of ESC
        private   int                               wheelDelta = 0;
        private   long                              rowSelectTime = 0;
        private   Point                             lastMouseDown = InvalidPosition;
        private   int                               lastMouseMove;
        private   GridEntry                         lastClickedEntry;
        
        private IServiceProvider                    serviceProvider;
        private IHelpService                        topHelpService;
        private IHelpService                        helpService;

        private EventHandler                        ehValueClick;
        private EventHandler                        ehLabelClick;
        private EventHandler                        ehOutlineClick;
        private EventHandler                        ehValueDblClick;
        private EventHandler                        ehLabelDblClick;
        private GridEntryRecreateChildrenEventHandler ehRecreateChildren;

        private int                                 cachedRowHeight = -1;
        IntPtr baseHfont;
        IntPtr boldHfont;

        
        public PropertyGridView(IServiceProvider serviceProvider, PropertyGrid propertyGrid)
        : base() {
              
            this.ehValueClick = new EventHandler(this.OnGridEntryValueClick);
            this.ehLabelClick = new EventHandler(this.OnGridEntryLabelClick);
            this.ehOutlineClick = new EventHandler(this.OnGridEntryOutlineClick);
            this.ehValueDblClick = new EventHandler(this.OnGridEntryValueDoubleClick);
            this.ehLabelDblClick = new EventHandler(this.OnGridEntryLabelDoubleClick);
            this.ehRecreateChildren = new GridEntryRecreateChildrenEventHandler(this.OnRecreateChildren);

            ownerGrid = propertyGrid;
            this.serviceProvider = serviceProvider;
            
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, false);
            SetStyle(ControlStyles.UserMouse, true);
            

            // properties
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
            backgroundBrush = SystemBrushes.Window;
            TabStop = true;
            
            this.Text = "PropertyGridView";

            CreateUI();
            LayoutWindow(true); 
        }
        
        public override Color BackColor {
            get {
                return base.BackColor;
            }
            set {
                this.backgroundBrush = new SolidBrush(value);
                base.BackColor = value;
            }
        }
        
        internal Brush GetBackgroundBrush(Graphics g) {
            return backgroundBrush;
        }
        
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool CanCopy {
            get {
                return selectedGridEntry != null && selectedGridEntry.GetPropertyTextValue() != null;
            }
        }
        
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool CanCut {
            get {
                return CanCopy && selectedGridEntry.IsTextEditable; 
            }
        }
        
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool CanPaste {
            get {
                return selectedGridEntry != null && selectedGridEntry.IsTextEditable; // return gridView.CanPaste;
            }
        }
        
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool CanUndo {
            get {
                if (!Edit.Visible || !Edit.Focused) {
                    return false;
                }
                return (0 != (int)Edit.SendMessage(NativeMethods.EM_CANUNDO, 0, 0));
            }
        }

        private Button DropDownButton {
            get {
                if (btnDropDown == null) {
                    #if DEBUG
                        if (ownerGrid.inGridViewCreate) {
                            throw new Exception("PERF REGRESSION - Creating item in grid view create");
                        }
                    #endif
                    
                    btnDropDown = new DropDownButton();
                    Bitmap bitmap = CreateDownArrow();
                    btnDropDown.Image = bitmap;
                    btnDropDown.BackColor = SystemColors.Control;
                    btnDropDown.ForeColor = SystemColors.ControlText;
                    btnDropDown.Click += new EventHandler(this.OnBtnClick);
                   // btnDropDown.MouseUp += new MouseEventHandler(this.OnBtnMouseUp);
                   // btnDropDown.MouseDown += new MouseEventHandler(this.OnBtnMouseDown);
                    btnDropDown.LostFocus += new EventHandler(this.OnChildLostFocus);
                    btnDropDown.TabIndex = 2;
                    CommonEditorSetup(btnDropDown);
                    btnDropDown.Size = new Size(SystemInformation.VerticalScrollBarArrowHeight, RowHeight);
                }
                return btnDropDown;
            }
        }

        private Button DialogButton {
            get {
                if (btnDialog == null) {

                    #if DEBUG
                    if (ownerGrid.inGridViewCreate) {
                        throw new Exception("PERF REGRESSION - Creating item in grid view create");
                    }
                    #endif
                    btnDialog = new DropDownButton();
                    btnDialog.BackColor = SystemColors.Control;
                    btnDialog.ForeColor = SystemColors.ControlText;
                    btnDialog.TabIndex = 3;
                    Icon icon = new Icon(typeof(PropertyGrid), "dotdotdot.ico");
                    btnDialog.Image = icon.ToBitmap();
                    icon.Dispose();
                    btnDialog.Click += new EventHandler(this.OnBtnClick);
                    //btnDialog.MouseUp += new MouseEventHandler(this.OnBtnMouseUp);
                    //btnDialog.MouseDown += new MouseEventHandler(this.OnBtnMouseDown);
                    btnDialog.KeyDown += new KeyEventHandler(this.OnBtnKeyDown);
                    btnDialog.LostFocus += new EventHandler(this.OnChildLostFocus);
                    btnDialog.Size = new Size(SystemInformation.VerticalScrollBarArrowHeight, RowHeight);
                    CommonEditorSetup(btnDialog);
                }
                return btnDialog;
            }
        }

        private GridViewEdit Edit {
            get{
                if (edit == null) {

                    #if DEBUG
                    if (ownerGrid.inGridViewCreate) {
                        throw new Exception("PERF REGRESSION - Creating item in grid view create");
                    }
                    #endif

                    edit = new GridViewEdit(this);
                    edit.BorderStyle = BorderStyle.None;
                    edit.AutoSize = false;
                    edit.TabStop = false;
                    edit.AcceptsReturn = true;
                    edit.BackColor = BackColor;
                    edit.ForeColor = ForeColor;
                    edit.KeyDown += new KeyEventHandler(this.OnEditKeyDown);
                    edit.KeyPress += new KeyPressEventHandler(this.OnEditKeyPress);
                    edit.GotFocus += new EventHandler(this.OnEditGotFocus);
                    edit.LostFocus += new EventHandler(this.OnEditLostFocus);
                    edit.MouseDown += new MouseEventHandler(this.OnEditMouseDown);
                    edit.TextChanged += new EventHandler(this.OnEditChange);
                    edit.ImeModeChanged += new EventHandler(this.OnEditImeModeChanged);
                    edit.TabIndex = 1;
                    CommonEditorSetup(edit);
                }
                return edit;
            }
        }

        private GridViewListBox DropDownListBox {
            get {
                if (listBox == null) {
                    #if DEBUG
                    if (ownerGrid.inGridViewCreate) {
                        throw new Exception("PERF REGRESSION - Creating item in grid view create");
                    }
                    #endif

                    listBox = new GridViewListBox(this);
                    listBox.DrawMode = DrawMode.OwnerDrawFixed;
                    //listBox.Click += new EventHandler(this.OnListClick);
                    listBox.MouseUp += new MouseEventHandler(this.OnListMouseUp);
                    listBox.DrawItem += new DrawItemEventHandler(this.OnListDrawItem);
                    listBox.SelectedIndexChanged += new EventHandler(this.OnListChange);
                    listBox.KeyDown += new KeyEventHandler(this.OnListKeyDown);
                    listBox.LostFocus += new EventHandler(this.OnChildLostFocus);
                    listBox.Visible = true;
                    listBox.ItemHeight = RowHeight;
                }
                return listBox;
            }
        }
        
        internal bool DrawValuesRightToLeft {
            get {
                if (edit != null && edit.IsHandleCreated) {
                    int exStyle = (int)UnsafeNativeMethods.GetWindowLong(new HandleRef(edit, edit.Handle), NativeMethods.GWL_EXSTYLE);
                    return ((exStyle & NativeMethods.WS_EX_RTLREADING) != 0);
                }
                else {
                    return false;
                }
            }
        }

        
        public bool FocusInside {
            get {
                return(this.ContainsFocus || (dropDownHolder != null && dropDownHolder.ContainsFocus));
            }
        }
        
        internal Color GrayTextColor{
            get {
                if (this.ForeColor.ToArgb() == SystemColors.WindowText.ToArgb()) {
                    return SystemColors.GrayText;
                }
                
                // compute the new color by halving the value of the old one.
                //
                int colorRGB = this.ForeColor.ToArgb();
                
                int alphaValue = (colorRGB >> 24) & 0xff;
                if (alphaValue != 0) {
                    alphaValue /= 2;
                    colorRGB &= 0xFFFFFF;
                    colorRGB |= (int)((alphaValue << 24) & 0xFF000000);
                }
                else {
                    colorRGB /= 2;
                }
                return Color.FromArgb(colorRGB);
            }
        }

        private GridErrorDlg ErrorDialog {
            get {
                if (this.errorDlg == null) {
                    errorDlg = new GridErrorDlg();
                }
                return errorDlg;
            }
        }

        private bool HasEntries {
            get{
                return topLevelGridEntries != null && topLevelGridEntries.Count > 0;
            }
        }
        
        protected int InternalLabelWidth {
            get {
                if (GetFlag(FlagNeedUpdateUIBasedOnFont)) {
                    UpdateUIBasedOnFont(true);
                }
                if (labelWidth == -1) {
                    SetConstants();
                }
                return labelWidth;
            }
        }
        
        internal int LabelPaintMargin {
            get {
                return requiredLabelPaintMargin;
            }
            set {
                requiredLabelPaintMargin = (short)Math.Max(Math.Max(value, requiredLabelPaintMargin), GDIPLUS_SPACE);
            }
        }

        protected bool NeedsCommit{
            get {
                string text;

                if (edit==null || !Edit.Visible) {
                    return false;
                }

                text = Edit.Text;

                if (((text == null || text.Length == 0) && (originalTextValue == null || originalTextValue.Length == 0)) ||
                    (text != null && originalTextValue != null && text.Equals(originalTextValue))) {
                    return false;
                }
                return true;
            }
        }

        protected int RowHeight {
            get {
                if (cachedRowHeight == -1) {
                    cachedRowHeight = (int)Font.Height + 2;
                }
                return cachedRowHeight;
            }
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.ContextMenuDefaultLocation"]/*' />
        /// <devdoc>
        /// Returns a default location for showing the context menu.  This
        /// location is the center of the active property label in the grid, and
        /// is used useful to position the context menu when the menu is invoked
        /// via the keyboard.
        /// </devdoc>
        public Point ContextMenuDefaultLocation {
            get {
                // get the rect for the currently selected prop name, find the middle
                Rectangle rect = GetRectangle( selectedRow, ROWLABEL );
                Point pt = PointToScreen( new Point( rect.X, rect.Y ) );
                return new Point(pt.X + (rect.Width / 2), pt.Y + (rect.Height / 2));
            }
        }

        private ScrollBar ScrollBar {
            get {
                if (scrollBar == null) {
                    #if DEBUG
                    if (ownerGrid.inGridViewCreate) {
                        throw new Exception("PERF REGRESSION - Creating item in grid view create");
                    }
                    #endif
                    scrollBar = new VScrollBar();
                    scrollBar.Scroll += new ScrollEventHandler(this.OnScroll);
                    Controls.Add(scrollBar);
                }
                return scrollBar;
            }
        }
        
        internal GridEntry SelectedGridEntry {
            get {
                return selectedGridEntry;
            }
            set {
                if (allGridEntries != null) {
                    foreach (GridEntry e in allGridEntries) {
                        if (e == value) {
                            SelectGridEntry(value, true);
                            return;
                        }
                    }
                }
                
                GridEntry gr = FindEquivalentGridEntry(new GridEntryCollection(null, new GridEntry[]{value}));
                
                if (gr != null) {
                    SelectGridEntry(gr, true);
                    return;
                }
                
                throw new ArgumentException(SR.GetString(SR.PropertyGridInvalidGridEntry));
            }
        }


        public PropertyDescriptor SelectedPropertyDescriptor {
            get {
                if (selectedGridEntry != null && (selectedGridEntry is PropertyDescriptorGridEntry)) {
                    return ((PropertyDescriptorGridEntry) selectedGridEntry).PropertyDescriptor;
                }
                else {
                    return null;
                }
            }
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.SelectedPropertyName"]/*' />
        /// <devdoc>
        /// Returns the currently selected property name.
        /// If no property or a category name is selected, "" is returned.
        /// If the category is a sub property, it is concatenated onto its
        /// parent property name with a ".".
        /// </devdoc>
        public string SelectedPropertyName {
            get {
                if (selectedGridEntry == null) {
                    return "";
                }
                GridEntry gridEntry = selectedGridEntry;
                string name = "";
                while (gridEntry != null && gridEntry.PropertyDepth >= 0) {
                    if (name.Length > 0) {
                        name = gridEntry.PropertyName + "." + name;
                    }
                    else {
                        name = gridEntry.PropertyName;
                    }
                    gridEntry = gridEntry.ParentGridEntry;
                }
                return name;
            }
            /*set{
                if (value==null){
                    return;
                }
                if (value.Equals(selectedGridEntry.PropertyLabel)){
                    return;
                }

                string curName;
                string remain = value;

                int dot = remain.IndexOf('.');
                GridEntry[] ipes = GetAllGridEntries();
                int pos = 0;

                while (dot != -1){
                    curName = remain.Substring(0, dot);
                    Debug.WriteLine("Looking for: " + curName);
                    for (int i = pos; i < ipes.Length ; i++){
                        Debug.WriteLine("Checking : " + ipes[i].PropertyLabel);
                        if (ipes[i].PropertyLabel.Equals(curName)){
                            if (ipes[i].Expandable){
                                pos = i;
                                remain = remain.Substring(dot + 1);
                                dot = remain.IndexOf('.');
                                if (dot != -1){
                                    Debug.WriteLine("Expanding: " + ipes[i].PropertyLabel);
                                    ipes[i].SetPropertyExpand(true);
                                    ipes = GetAllGridEntries();
                                    break;
                                }
                                else{
                                    SelectGridEntry(ipes[i], true);
                                    return;
                                }
                            }
                        }
                    }
                    // uh oh
                    dot = -1;
                }
                // oops, didn't find it
                SelectRow(0);
                return;

            }*/
        }

        
        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.ServiceProvider"]/*' />
        /// <devdoc>
        /// Returns or sets the IServiceProvider the PropertyGridView will use to obtain
        /// services.  This may be null.
        /// </devdoc>
        public IServiceProvider ServiceProvider {
            get { 
               return serviceProvider;
            }
            set {
               if (value != serviceProvider) {
                    this.serviceProvider = value;
                    
                    topHelpService = null;

                    if (helpService != null && helpService is IDisposable)
                        ((IDisposable)helpService).Dispose();

                    helpService = null;
               }
            }
        }    
        
        private int TipColumn {
            get{
                return(tipInfo & unchecked((int)0xFFFF0000)) >> 16;
            }
            set{

                // clear the column
                tipInfo &= 0xFFFF;

                // set the row
                tipInfo |= ((value & 0xFFFF) << 16);
            }
        }

        private int TipRow {
            get{
                return tipInfo & 0xFFFF;
            }
            set{

                // clear the row
                tipInfo &= unchecked((int)0xFFFF0000);

                // set the row
                tipInfo |= (value & 0xFFFF);
            }
        }

        private GridToolTip ToolTip {
            get {
                if (toolTip == null) {
                    #if DEBUG
                    if (ownerGrid.inGridViewCreate) {
                        throw new Exception("PERF REGRESSION - Creating item in grid view create");
                    }
                    #endif
                    toolTip = new GridToolTip(new Control[]{this, Edit});
                    toolTip.ToolTip = "";
                    toolTip.Font = this.Font;
                }   
                return toolTip;
            }
        }

        public object Value {
            get {
                return GetGridEntryFromRow(selectedRow).PropertyValue;
            }
            set {
                CommitValue(value);
            }
        }

        internal GridEntryCollection AccessibilityGetGridEntries() {
            return GetAllGridEntries();
        }

        internal Rectangle AccessibilityGetGridEntryBounds(GridEntry gridEntry) {
            int row = GetRowFromGridEntry(gridEntry);
            if (row == -1) {
                return new Rectangle(0, 0, 0, 0);
            }
            Rectangle rect = GetRectangle(row, ROWVALUE | ROWLABEL);

            // Translate rect to screen coordinates
            //
            NativeMethods.POINT pt = new NativeMethods.POINT(rect.X, rect.Y);
            UnsafeNativeMethods.ClientToScreen(new HandleRef(this, Handle), pt);

            return new Rectangle(pt.x, pt.y, rect.Width, rect.Height);
        }
        
        internal int AccessibilityGetGridEntryChildID(GridEntry gridEntry) {
         
            GridEntryCollection ipes = GetAllGridEntries();
            
            if (ipes == null) {
                return -1;
            }
            
            // Find the grid entry and return its ID
            //
            for(int index = 0; index < ipes.Count; ++index) {
                if (ipes[index].Equals(gridEntry)) {
                    return index;
                }
            }
            
            return -1;
        }

        internal void AccessibilitySelect(GridEntry entry) {
            SelectGridEntry(entry, true);            
            FocusInternal();
        }

        private void AddGridEntryEvents(GridEntryCollection ipeArray, int startIndex, int count) {
            if (ipeArray == null) {
                return;
            }
            
            if (count == -1) {
                count = ipeArray.Count - startIndex;
            }

            for (int i= startIndex; i < (startIndex + count); i++) {
                if (ipeArray[i] != null) {
                    GridEntry ge = ipeArray.GetEntry(i);
                    ge.AddOnValueClick(ehValueClick);
                    ge.AddOnLabelClick(ehLabelClick);
                    ge.AddOnOutlineClick(ehOutlineClick);
                    ge.AddOnOutlineDoubleClick(ehOutlineClick);
                    ge.AddOnValueDoubleClick(ehValueDblClick);
                    ge.AddOnLabelDoubleClick(ehLabelDblClick);
                    ge.AddOnRecreateChildren(ehRecreateChildren);
                }
            }
        }
        
        protected virtual void AdjustOrigin(System.Drawing.Graphics g, Point newOrigin, ref Rectangle r) {
            
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Adjusting paint origin to (" + newOrigin.X.ToString() + "," + newOrigin.Y.ToString() + ")");

            g.ResetTransform();
            g.TranslateTransform(newOrigin.X, newOrigin.Y);
            r.Offset(-newOrigin.X, -newOrigin.Y);
        }

        private void CancelSplitterMove() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CancelSplitterMove");
            if (GetFlag(FlagIsSplitterMove)) {
                SetFlag(FlagIsSplitterMove, false);
                CaptureInternal = false;
            
                if (selectedRow != -1) {
                    SelectRow(selectedRow);
                }
            }
        }

        private void ClearGridEntryEvents(GridEntryCollection ipeArray, int startIndex, int count) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:ClearGridEntryEvents");
            if (ipeArray == null) {
                return;
            }
            
            if (count == -1) {
                count = ipeArray.Count - startIndex;
            }

            for (int i = startIndex ; i < (startIndex + count); i++) {
                if (ipeArray[i] != null) {
                    GridEntry ge = ipeArray.GetEntry(i);
                    ge.RemoveOnValueClick(ehValueClick);
                    ge.RemoveOnLabelClick(ehLabelClick);
                    ge.RemoveOnOutlineClick(ehOutlineClick);
                    ge.RemoveOnOutlineDoubleClick(ehOutlineClick);
                    ge.RemoveOnValueDoubleClick(ehValueDblClick);
                    ge.RemoveOnLabelDoubleClick(ehLabelDblClick);
                    ge.RemoveOnRecreateChildren(ehRecreateChildren);
                }
            }
        }

        public void ClearProps() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:ClearProps");


            if (!HasEntries) {
                return;
            }

            CommonEditorHide();
            topLevelGridEntries = null;
            ClearGridEntryEvents(allGridEntries, 0, -1);
            allGridEntries = null;
            selectedRow = -1;
            //selectedGridEntry = null; // we don't wanna clear this because then we can't save where we were on a Refresh()
            tipInfo = -1;
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.CloseDropDown"]/*' />
        /// <devdoc>
        ///      Closes a previously opened drop down.  This should be called by the
        ///      drop down when the user does something that should close it.
        /// </devdoc>
        public void /* IWindowsFormsEditorService. */ CloseDropDown() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CloseDropDown");

            // the activation code in the DropDownHolder can cause this to recurse...

            if (GetFlag(FlagDropDownClosing)) {
                return;
            }
            try {
                SetFlag(FlagDropDownClosing, true);
                if (dropDownHolder != null && dropDownHolder.Visible) {

                    if (dropDownHolder.Component == DropDownListBox && GetFlag(FlagDropDownCommit)) {
                        OnListClick(null,  null);
                    }

                    Edit.Filter = false;
                    
                    // disable the ddh so it wont' steal the focus back
                    dropDownHolder.SetComponent(null);
                    dropDownHolder.Visible = false;
                    
                    // when we disable the dropdown holder, focus will be lost,
                    // so put it onto one of our children first.
                    if (DialogButton.Visible) {
                        DialogButton.FocusInternal();
                    }
                    else if (DropDownButton.Visible) {
                        DropDownButton.FocusInternal();
                    }
                    else if (Edit.Visible) {
                        Edit.FocusInternal();
                    }
                    else {
                        FocusInternal();
                    }
                
                    if (selectedRow != -1) {
                        SelectRow(selectedRow);
                    }
                    
                }
            }
            finally {
                SetFlag(FlagDropDownClosing, false);
            }
        }

        private void CommonEditorHide() {
                CommonEditorHide(false);
        }

        private void CommonEditorHide(bool always) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CommonEditorHide");

            if (!always && !HasEntries) {
                return;
            }
            
            CloseDropDown();

            bool gotfocus = false;
            
            if (Edit.Focused || DialogButton.Focused || DropDownButton.Focused) {

                if (IsHandleCreated && Visible && Enabled) {

                    gotfocus = IntPtr.Zero != UnsafeNativeMethods.SetFocus(new HandleRef(this, Handle));
                }
            }
            
            try {
               // We do this becuase the Focus call above doesn't always stick, so
               // we make the Edit think that it doesn't have focus.  this prevents
               // ActiveControl code on the containercontrol from moving focus elsewhere
               // when the dropdown closes.
               Edit.DontFocus = true;
               if (Edit.Focused && !gotfocus) {
                 gotfocus = this.FocusInternal();
               }
               Edit.Visible = false;
               
               Edit.SelectionStart = 0;
               Edit.SelectionLength = 0;
               
               if (DialogButton.Focused && !gotfocus) {
                  gotfocus = this.FocusInternal();
               }
               DialogButton.Visible = false;
               
               if (DropDownButton.Focused && !gotfocus) {
                   gotfocus = this.FocusInternal();
               }
               DropDownButton.Visible = false;
               currentEditor = null;
            }
            finally {
               Edit.DontFocus = false;
            }
        }

        protected virtual void CommonEditorSetup(Control ctl) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CommonEditorSetup");
            ctl.Visible = false;
            Controls.Add(ctl);
        }

        protected virtual void CommonEditorUse(Control ctl, Rectangle rectTarget) {

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CommonEditorUse");
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Showing common editors");

            Debug.Assert(ctl != null, "Null control passed to CommonEditorUse");

            Rectangle rectCur = ctl.Bounds;

            // the client rect minus the border line
            Rectangle clientRect = this.ClientRectangle;

            clientRect.Inflate(-1,-1);

            try {
                rectTarget = Rectangle.Intersect(clientRect, rectTarget);
                 //if (ctl is Button)
                 //   Debug.WriteStackTrace();


                if (!rectTarget.IsEmpty) {
                    if (!rectTarget.Equals(rectCur)) {
                        ctl.SetBounds(rectTarget.X,rectTarget.Y,
                                      rectTarget.Width,rectTarget.Height);
                    }
                    ctl.Visible = true;
                }
            }
            catch (Exception) {
                rectTarget = Rectangle.Empty;
            }

            if (rectTarget.IsEmpty) {

                ctl.Visible = false;
            }

            currentEditor = ctl;

        }

        private /*protected virtual*/ int CountPropsFromOutline(GridEntryCollection rgipes) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CountPropsFromOutLine");
            if (rgipes == null) return 0;
            int cProps = rgipes.Count;
            for (int i = 0; i < rgipes.Count; i++) {
                if (((GridEntry)rgipes[i]).InternalExpanded)
                    cProps += CountPropsFromOutline(((GridEntry)rgipes[i]).Children);
            }
            return cProps;
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.CreateAccessibilityInstance"]/*' />
        /// <devdoc>
        ///     Constructs the new instance of the accessibility object for this control. Subclasses
        ///     should not call base.CreateAccessibilityObject.
        /// </devdoc>
        protected override AccessibleObject CreateAccessibilityInstance() {
            return new PropertyGridViewAccessibleObject(this);
        }
        
        private Bitmap CreateDownArrow() {
            Bitmap bitmap = null;

            try {
                Icon icon = new Icon(typeof(PropertyGrid), "Arrow.ico");
                bitmap = icon.ToBitmap();
                icon.Dispose();
            }
            catch (Exception e) {
                Debug.Fail(e.ToString());
                bitmap= new Bitmap(16, 16);
            }
            return bitmap;
        }

        
        protected virtual void CreateUI() {
            UpdateUIBasedOnFont(false);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:Dispose");
                if (scrollBar != null) scrollBar.Dispose();
                if (listBox != null) listBox.Dispose();
                if (dropDownHolder != null) dropDownHolder.Dispose();
                scrollBar = null;
                listBox = null;
                dropDownHolder = null;

                ownerGrid = null;
                topLevelGridEntries = null;
                allGridEntries = null;
                serviceProvider = null;
                
                if (topHelpService != null && topHelpService is IDisposable)
                    ((IDisposable)topHelpService).Dispose();

                topHelpService = null;

                if (helpService != null && helpService is IDisposable)
                    ((IDisposable)helpService).Dispose();

                helpService = null;

                if (fontBold != null) {
                    fontBold.Dispose();
                    fontBold = null;

                }

                if (edit != null) {
                    edit.Dispose();
                    edit = null;
                }

                if (btnDropDown != null) {
                    btnDropDown.Dispose();
                    btnDropDown = null;
                }

                if (btnDialog != null) {
                    btnDialog.Dispose();
                    btnDialog = null;
                }

                if (toolTip != null) {
                    toolTip.Dispose();
                    toolTip = null;
                }
            }

            base.Dispose(disposing);
        }
        
        public void DoCopyCommand() {
            if (this.CanCopy) {    
               if (Edit.Focused) {
                    Edit.Copy();
               }
               else {
                    Clipboard.SetDataObject(selectedGridEntry.GetPropertyTextValue());           
               }
            }
        }
        
        public void DoCutCommand() {
            if (this.CanCut) {
               DoCopyCommand();
               if (Edit.Visible) {
                    Edit.Cut();
               }
            }
        }


        public void DoPasteCommand() {
            if (this.CanPaste && Edit.Visible) {
               if (Edit.Focused) {
                    Edit.Paste();
               }
               else {
                   IDataObject dataObj = Clipboard.GetDataObject();
                   if (dataObj != null) {
                      string data = (string)dataObj.GetData(typeof(string));
                      if (data != null) {
                         Edit.FocusInternal();
                         Edit.Text = data;
                         SetCommitError(ERROR_NONE, true);
                      }   
                   }
               }
            }
        }
        
        public void DoUndoCommand() {
            if (this.CanUndo && Edit.Visible) {
               Edit.SendMessage(NativeMethods.WM_UNDO, 0, 0);
            }
        }
        
        internal void DumpPropsToConsole(GridEntry entry, string prefix) { 
        
            Type propType = entry.PropertyType;
            
            if (entry.PropertyValue != null) {
               propType = entry.PropertyValue.GetType();
            }
            
            System.Console.WriteLine(prefix + entry.PropertyLabel + ", value type=" + (propType == null ? "(null)" : propType.FullName) + ", value=" + (entry.PropertyValue == null ? "(null)" : entry.PropertyValue.ToString()) + ", flags=" + entry.Flags.ToString() + ", TypeConverter=" + (entry.TypeConverter == null ? "(null)" : entry.TypeConverter.GetType().FullName) + ", UITypeEditor=" + ((entry.UITypeEditor == null ? "(null)" : entry.UITypeEditor.GetType().FullName)));
            GridEntryCollection children = entry.Children;
            
            if (children != null) {
               foreach(GridEntry g in children) {
                  DumpPropsToConsole(g, prefix + "\t");
               }
            }
        }

        private int GetIPELabelIndent(GridEntry gridEntry) {
            //return OUTLINE_INDENT*(gridEntry.PropertyDepth + 1);
            return gridEntry.PropertyLabelIndent + 1;
        }

        private int GetIPELabelLength(System.Drawing.Graphics g,GridEntry gridEntry) {
            Size size = Size.Ceiling(g.MeasureString(gridEntry.PropertyLabel, Font));
            return ptOurLocation.X + GetIPELabelIndent(gridEntry) + size.Width;
        }

        private bool IsIPELabelLong(System.Drawing.Graphics g,GridEntry gridEntry) {
            if (gridEntry == null) return false;
            int length = GetIPELabelLength(g,gridEntry);
            return(length > ptOurLocation.X + InternalLabelWidth);
        }

        protected virtual void DrawLabel(System.Drawing.Graphics g, int row, Rectangle rect, bool selected, bool fLongLabelRequest, ref Rectangle clipRect) {

            GridEntry gridEntry = GetGridEntryFromRow(row);

            if (gridEntry == null || rect.IsEmpty)
                return;

            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Drawing label for property " + gridEntry.PropertyLabel);
            
            Point newOrigin = new Point(rect.X, rect.Y);
            Rectangle cr = Rectangle.Intersect(rect, clipRect);
            
            if (cr.IsEmpty) {
                return;
            }
            
            AdjustOrigin(g, newOrigin, ref rect);
            cr.Offset(-newOrigin.X, -newOrigin.Y);
            
            try {
                try {
                    bool fLongLabel = false;
                    int labelEnd = 0;
                    int labelIndent = GetIPELabelIndent(gridEntry);

                    if (fLongLabelRequest) {
                        labelEnd = GetIPELabelLength(g,gridEntry);
                        fLongLabel = IsIPELabelLong(g,gridEntry);
                    }

                    gridEntry.PaintLabel(g, rect, cr, selected, fLongLabel);
                }
                catch (Exception ex) {
                    IUIService uiService = (IUIService)GetService(typeof(IUIService));
                    if (uiService != null) {
                        uiService.ShowError(ex, "An Exception has occurred while painting the label for property '" + gridEntry.PropertyLabel + "'. See below for details.");
                    }
                    else {
                        Debug.Fail(ex.ToString());
                    }
                }
            }
            finally {
                ResetOrigin(g);
            }
        }

        protected virtual void DrawValueEntry(System.Drawing.Graphics g, int row, ref Rectangle clipRect) {
            GridEntry gridEntry = GetGridEntryFromRow(row);
            if (gridEntry == null)
                return;

            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Drawing value for property " + gridEntry.PropertyLabel);

            Rectangle r = GetRectangle(row,ROWVALUE);
            Point newOrigin = new Point(r.X, r.Y);
            Rectangle cr = Rectangle.Intersect(clipRect, r);

            if (cr.IsEmpty) {
                return;
            }
            
            AdjustOrigin(g, newOrigin, ref r);
            cr.Offset(-newOrigin.X, -newOrigin.Y);

            try {
                try {
                    DrawValueEntry(g,r, cr,gridEntry,null, true);
                }
                catch (Exception) {
                }
            }
            finally {
                ResetOrigin(g);
            }
        }

        private /*protected virtual*/ void DrawValueEntry(System.Drawing.Graphics g, Rectangle rect, Rectangle clipRect, GridEntry gridEntry, object value, bool fetchValue) {
            DrawValue(g, rect, clipRect, gridEntry, value, false, true, fetchValue, true);
        }
        private void DrawValue(System.Drawing.Graphics g, Rectangle rect, Rectangle clipRect, GridEntry gridEntry, object value, bool drawSelected, bool checkShouldSerialize, bool fetchValue, bool paintInPlace) { 
            GridEntry.PaintValueFlags paintFlags = GridEntry.PaintValueFlags.None;

            if(drawSelected) {
                paintFlags |= GridEntry.PaintValueFlags.DrawSelected;
            }

            if (checkShouldSerialize) {
               paintFlags |= GridEntry.PaintValueFlags.CheckShouldSerialize;
            }

            if (fetchValue) {
                paintFlags |= GridEntry.PaintValueFlags.FetchValue;
            }

            if (paintInPlace) {
                paintFlags |= GridEntry.PaintValueFlags.PaintInPlace;
            }

            gridEntry.PaintValue(value, g, rect, clipRect, paintFlags);
        }
        
        private void F4Selection(bool popupModalDialog) {
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null) return;

            // if we are in an errorState, just put the focus back on the Edit
            if (errorState != ERROR_NONE && Edit.Visible) {
                Edit.FocusInternal();
                return;
            }

            if (DropDownButton.Visible) {
                PopupDialog(selectedRow);
            }
            else if (DialogButton.Visible) {
                if (popupModalDialog) {
                    PopupDialog(selectedRow);
                }
                else {
                    DialogButton.FocusInternal();
                }
            }
            else if (Edit.Visible) {
                Edit.FocusInternal();
                SelectEdit(false);
            }
            return;
        }

        public void DoubleClickRow(int row, bool toggleExpand, int type) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:DoubleClickRow");
            GridEntry gridEntry = GetGridEntryFromRow(row);
            if (gridEntry == null) return;

            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Property " + gridEntry.PropertyLabel + " double clicked");

            if (!toggleExpand || type == ROWVALUE) {
                bool action = gridEntry.DoubleClickPropertyValue();
                if (action) {
                    SelectRow(row);
                    return;
                }
            }

            SelectGridEntry(gridEntry, true);

            if (type == ROWLABEL && toggleExpand && gridEntry.Expandable) {
                SetExpand(gridEntry,!gridEntry.InternalExpanded);
                return;
            }
            
            if (gridEntry.IsValueEditable && gridEntry.Enumerable) {
                int index = GetCurrentValueIndex(gridEntry);

                if (index != -1) {
                    object[] values = gridEntry.GetPropertyValueList();

                    if (values == null || index >= (values.Length - 1)) {
                        index = 0;
                    }
                    else {
                        index++;
                    }

                    CommitValue(values[index]);
                    SelectRow(selectedRow);
                    return;
                }
            }
        
            if (Edit.Visible) {
                Edit.FocusInternal();
                SelectEdit(false);
                return;
            }
        }

        public Font GetBaseFont() {
            return Font;
        }

        public Font GetBoldFont() {
            if (fontBold == null) {
                fontBold = new Font(this.Font, FontStyle.Bold);
            }
            return fontBold;
        }

        internal IntPtr GetBaseHfont() {
            if (baseHfont == IntPtr.Zero) {
                baseHfont = GetBaseFont().ToHfont();
            }
            return baseHfont;
        }

        internal IntPtr GetBoldHfont() {
            if (boldHfont == IntPtr.Zero) {
                boldHfont = GetBoldFont().ToHfont();
            }
            return boldHfont;
        }


        private bool GetFlag(short flag) {
            return (this.flags & flag) != 0;
        }

        public virtual Color GetLineColor() {
            return ownerGrid.LineColor;
        }

        public virtual Brush GetLineBrush(Graphics g) {
            if (ownerGrid.lineBrush == null) {
                Color clr = g.GetNearestColor(ownerGrid.LineColor);
                ownerGrid.lineBrush = new SolidBrush(clr);
            }
            return ownerGrid.lineBrush;
        }

        public virtual IntPtr GetHostHandle() {
            return Handle;
        }

        public virtual int GetLabelWidth() {
            return InternalLabelWidth;
        }

        public virtual int GetOutlineIconSize() {
            return OUTLINE_SIZE;
        }

        public virtual int GetGridEntryHeight() {
            return RowHeight;
        }

        // for qa automation
        internal int GetPropertyLocation(string propName, bool getXY, bool rowValue) {
            if (allGridEntries != null && allGridEntries.Count > 0) {
                for (int i = 0; i < allGridEntries.Count; i++) {
                    if (0 == String.Compare(propName, allGridEntries.GetEntry(i).PropertyLabel, true, CultureInfo.InvariantCulture)) {
                        if (getXY) {
                            int row = GetRowFromGridEntry(allGridEntries.GetEntry(i));

                            if (row < 0 || row >= this.visibleRows) {
                                return -1;
                            }
                            else {
                                Rectangle r = GetRectangle(row, rowValue ? ROWVALUE : ROWLABEL);
                                return(r.Y << 16 | (r.X & 0xFFFF));
                            }
                        }
                        else {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public new object GetService(Type classService) {
            if (classService == typeof(IWindowsFormsEditorService)) {
                return this;
            }
            if (ServiceProvider != null) {
                return serviceProvider.GetService(classService);
            }
            return null;
        }

        public virtual int GetSplitterWidth() {
            return 1;
        }

        public virtual int GetTotalWidth() {
            return GetLabelWidth() + GetSplitterWidth() + GetValueWidth();
        }

        public virtual int GetValuePaintIndent() {
            return PAINT_INDENT;
        }

        public virtual int GetValuePaintWidth() {
            return PAINT_WIDTH;
        }

        public virtual int GetValueStringIndent() {
            return EDIT_INDENT;
        }

        public virtual int GetValueWidth() {
            return(int)(InternalLabelWidth * (labelRatio - 1));
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.DropDownControl"]/*' />
        /// <devdoc>
        ///      Displays the provided control in a drop down.  When possible, the
        ///      current dimensions of the control will be respected.  If this is not possible
        ///      for the current screen layout the control may be resized, so it should
        ///      be implemented using appropriate docking and anchoring so it will resize
        ///      nicely.  If the user performs an action that would cause the drop down
        ///      to prematurely disappear the control will be hidden.
        /// </devdoc>
        public void /* cpr IWindowsFormsEditorService. */ DropDownControl(Control ctl) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:DropDownControl");
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "DropDownControl(ctl = " + ctl.GetType().Name + ")");
            if (dropDownHolder == null) {
                dropDownHolder = new DropDownHolder(this);
            }

            dropDownHolder.Visible = false;
            dropDownHolder.SetComponent(ctl);
            Rectangle rect = GetRectangle(selectedRow,ROWVALUE);
            Size size = dropDownHolder.Size;
            Point loc = PointToScreen(new Point(0, 0));
            Rectangle rectScreen = Screen.FromControl(Edit).WorkingArea;
            size.Width = Math.Max(rect.Width+1,size.Width);

            // Not needed... CYMAXDDLHEIGHT used to be 200, but why limit it???
            //size.Height = Math.Min(size.Height,CYMAXDDLHEIGHT);

            loc.X = Math.Min(rectScreen.X + rectScreen.Width - size.Width,
                             Math.Max(rectScreen.X,loc.X + rect.X + rect.Width - size.Width));
            loc.Y += rect.Y;
            if (rectScreen.Y + rectScreen.Height < (size.Height + loc.Y + Edit.Height))
                loc.Y -= size.Height;
            else
                loc.Y += rect.Height + 1;

            UnsafeNativeMethods.SetWindowLong(new HandleRef(dropDownHolder, dropDownHolder.Handle), NativeMethods.GWL_HWNDPARENT, new HandleRef(this, Handle));
            dropDownHolder.SetBounds(loc.X,loc.Y,size.Width,size.Height);
            SafeNativeMethods.ShowWindow(new HandleRef(dropDownHolder, dropDownHolder.Handle), NativeMethods.SW_SHOWNA);
            Edit.Filter = true;
            dropDownHolder.Visible = true;

            dropDownHolder.FocusComponent();
            SelectEdit(false);

            dropDownHolder.DoModalLoop();
            if (selectedRow != -1) {
                FocusInternal();
                SelectRow(selectedRow);
            }
        }

        public virtual void DropDownDone() {
            CloseDropDown();
        }

        public virtual void DropDownUpdate() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:DropDownUpdate");
            if (dropDownHolder != null && dropDownHolder.GetUsed()) {
                int row = selectedRow;
                GridEntry gridEntry = GetGridEntryFromRow(row);
                Edit.Text = gridEntry.GetPropertyTextValue();
            }
        }
        
        public bool EnsurePendingChangesCommitted() {
            this.CloseDropDown();
            return this.Commit();
        }
        
        private bool FilterEditWndProc(ref Message m) {
            // if it's the TAB key, we keep it since we'll give them focus with it.
            if (dropDownHolder != null && dropDownHolder.Visible && m.Msg == NativeMethods.WM_KEYDOWN && (int)m.WParam != (int)Keys.Tab) {
                Control ctl = dropDownHolder.Component;
                if (ctl != null) {
                    m.Result = ctl.SendMessage(m.Msg, m.WParam, m.LParam);
                    return true;
                }
            }
            return false;
        }

        private bool FilterReadOnlyEditKeyPress(char keyChar) {
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry.Enumerable && gridEntry.IsValueEditable) {
                int index = GetCurrentValueIndex(gridEntry);

                object[] values = gridEntry.GetPropertyValueList();
                string letter = new string(new char[] {keyChar});
                for (int i = 0; i < values.Length; i++) {
                    object valueCur = values[(i + index + 1) % values.Length];
                    string text = gridEntry.GetPropertyTextValue(valueCur);
                    if (text != null && text.Length > 0 && String.Compare(text.Substring(0,1), letter, true, CultureInfo.InvariantCulture) == 0) {
                        CommitValue(valueCur);
                        if (Edit.Focused) {
                            SelectEdit(false);
                        }
                        return true;
                    }
                }

            }
            return false;
        }

        public virtual bool WillFilterKeyPress(char charPressed) {
            if (!Edit.Visible) {
                return false;
            }

            Keys modifiers = ModifierKeys;
            if ((int)(modifiers & ~Keys.Shift) != 0) {
                return false;
            }

            // try to activate the Edit.
            // we don't activate for +,-, or * on expandable items because they have special meaning
            // for the tree.
            //

            if (selectedGridEntry != null) {
                switch (charPressed) {
                    case '+':
                    case '-':
                    case '*':
                        return !selectedGridEntry.Expandable;
                    case (char)(int)Keys.Tab:
                        return false;
                }
            }

            return true;
        }

        public void FilterKeyPress(char keyChar) {

            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null)
                return;

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:FilterKeyPress()");

            Edit.FilterKeyPress(keyChar);
        }

        private /*protected virtual*/ GridEntry FindEquivalentGridEntry(GridEntryCollection ipeHier) {
            if (ipeHier == null || ipeHier.Count == 0)
                return null;
            GridEntryCollection rgipes = GetAllGridEntries();

            if (rgipes == null || rgipes.Count == 0) {
                return null;
            }

            GridEntry targetEntry = null;
            int row = 0;
            int count = rgipes.Count;

            for (int i = 0; i < ipeHier.Count; i++) {

                if (ipeHier[i] == null) {
                    continue;
                }

                // if we've got one above, and it's expandable,
                // expand it
                if (targetEntry != null) {

                    // how many do we have?
                    int items = rgipes.Count;

                    // expand and get the new count
                    if (!targetEntry.InternalExpanded) {
                      SetExpand(targetEntry, true);
                      rgipes = GetAllGridEntries();
                    }
                    count = targetEntry.ChildCount;
                }

                int start = row;
                targetEntry = null;

                // now, we will only go as many as were expanded...
                for (; row < rgipes.Count && ((row - start) <= count); row++) {
                    if (ipeHier.GetEntry(i).NonParentEquals(rgipes[row])) {
                        targetEntry = rgipes.GetEntry(row);
                        row++;
                        break;
                    }
                }

                // didn't find it...
                if (targetEntry == null) {
                    break;
                }
            }

            return targetEntry;
        }

        protected virtual Point FindPosition(int x, int y) {
            if (RowHeight == -1)
                return InvalidPosition;
            Size size = this.GetOurSize();

            if (x < 0 || x > size.Width + ptOurLocation.X)
                return InvalidPosition;
            Point pt = new Point(ROWLABEL,0);
            if (x > InternalLabelWidth + ptOurLocation.X)
                pt.X = ROWVALUE;
            pt.Y = (y-ptOurLocation.Y)/(1+RowHeight);
            return pt;
        }

        public virtual void Flush() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView::Flush()");
            if (Commit() && Edit.Focused) {
                this.FocusInternal();
            }
        }

        private GridEntryCollection GetAllGridEntries() {
            return GetAllGridEntries(false);
        }

        private GridEntryCollection GetAllGridEntries(bool fUpdateCache) {
            if (visibleRows == -1 || totalProps == -1 || !HasEntries) {
                return null;
            }

            if (allGridEntries != null && !fUpdateCache) {
                return allGridEntries;
            }

            GridEntry[] rgipes = new GridEntry[totalProps];
            try {
                GetGridEntriesFromOutline(topLevelGridEntries,0,0,rgipes);
            }
            catch (Exception ex) {
                Debug.Fail(ex.ToString());
            }
            allGridEntries = new GridEntryCollection(null, rgipes);
            AddGridEntryEvents(allGridEntries, 0, -1);
            return allGridEntries;
        }

        private int GetCurrentValueIndex(GridEntry gridEntry) {

            if (!gridEntry.Enumerable) {
                return -1;
            }

            object[] values  = gridEntry.GetPropertyValueList();
            object value     = gridEntry.PropertyValue;
            string textValue = gridEntry.TypeConverter.ConvertToString(gridEntry, value);

            if (values != null && values.Length > 0) {
                string itemTextValue;
                int stringMatch = -1;
                int equalsMatch = -1;
                for (int i = 0; i < values.Length; i++) {
                    
                    object curValue = values[i];

                    // check real values against string values.
                    itemTextValue = gridEntry.TypeConverter.ConvertToString(curValue);
                    if (value == curValue || 0 == String.Compare(textValue, itemTextValue, true, CultureInfo.InvariantCulture)) {
                        stringMatch = i;
                    }
                    // now try .equals if they are both non-null
                    if (value != null && curValue != null && curValue.Equals(value)) {
                        equalsMatch = i;
                    }

                    if (stringMatch == equalsMatch && stringMatch != -1) {
                        return stringMatch;
                    }
                }

                if (stringMatch != -1) {
                    return stringMatch;
                }

                if (equalsMatch != -1) {
                    return equalsMatch;
                }
            }
            return -1;

        }

        public virtual int GetDefaultOutlineIndent() {
            return OUTLINE_INDENT;
        }

        private IHelpService GetHelpService() {
            if (helpService == null && ServiceProvider != null) {
                topHelpService = (IHelpService)ServiceProvider.GetService(typeof(IHelpService));
                if (topHelpService != null) {
                     IHelpService localHelpService = topHelpService.CreateLocalContext(HelpContextType.ToolWindowSelection);
                     if (localHelpService != null) {
                        helpService = localHelpService;
                     }
                }
            }
            return helpService;
        }

        public virtual int GetScrollOffset() {
            //Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:GetScrollOffset");
            if (scrollBar == null) {
                return 0;
            }
            int pos = ScrollBar.Value;
            return pos;
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.GetGridEntryHierarchy"]/*' />
        /// <devdoc>
        /// returns an array of IPE specifying the current heirarchy of ipes from the given
        /// gridEntry through its parents to the root.
        /// </devdoc>
        private GridEntryCollection GetGridEntryHierarchy(GridEntry gridEntry) {
            if (gridEntry == null) {
                return null;
            }

            int depth = gridEntry.PropertyDepth;
            if (depth > 0) {
                GridEntry[] entries = new GridEntry[depth + 1];

                while (gridEntry != null && depth >= 0) {
                    entries[depth] = gridEntry;
                    gridEntry = gridEntry.ParentGridEntry;
                    depth = gridEntry.PropertyDepth;
                }
                return new GridEntryCollection(null, entries);
            }
            return new GridEntryCollection(null, new GridEntry[]{gridEntry});
        }

        private /*protected virtual*/ GridEntry GetGridEntryFromRow(int row) {
            GridEntryCollection rgipesAll = GetAllGridEntries();
            if (rgipesAll != null) {
                int offset = row + GetScrollOffset();
                if (offset >= 0 && offset < rgipesAll.Count)
                    return rgipesAll.GetEntry(offset);
            }
            return null;
        }

        private /*protected virtual*/ int GetGridEntriesFromOutline(GridEntryCollection rgipe, int cCur,
                                                 int cTarget, GridEntry[] rgipeTarget) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:GetGridEntriesFromOutline");
            if (rgipe == null || rgipe.Count == 0)
                return cCur;

            cCur--; // want to account for each entry as we find it.

            for (int cLocal = 0; cLocal < rgipe.Count; cLocal++) {
                cCur++;
                if (cCur >= cTarget + rgipeTarget.Length)
                    break;

                GridEntry ipeCur = rgipe.GetEntry(cLocal);

                //Debug.Assert(ipeCur != null, "Null IPE at position " + cLocal.ToString());


                if (cCur >= cTarget)
                    rgipeTarget[cCur - cTarget] = ipeCur;

                if (ipeCur.InternalExpanded) {
                    GridEntryCollection subGridEntry = ipeCur.Children;
                    //Debug.Assert(subGridEntry != null && subGridEntry.Length > 0 && subGridEntry[0] != null, "Expanded property " + ipeCur.PropertyLabel + " has no children!");
                    if (subGridEntry != null && subGridEntry.Count > 0) {
                        cCur = GetGridEntriesFromOutline(subGridEntry,
                                                  cCur+1,cTarget,rgipeTarget);
                    }
                }
            }

            return cCur;
        }

        private Size GetOurSize() {
                Size size = ClientSize;
                if (size.Width == 0) {
                    Size sizeWindow = Size;
                    if (sizeWindow.Width > 10) {
                        Debug.Fail("We have a bad client width!");
                        size.Width = sizeWindow.Width;
                        size.Height = sizeWindow.Height;
                    }
                }
                if (!GetScrollbarHidden()) {
                    Size sizeScroll = ScrollBar.Size;
                    size.Width -= sizeScroll.Width;
                }
                size.Width -= 2;
                size.Height -= 2;
                return size;
        }

        public Rectangle GetRectangle(int row, int flRow) {
            Rectangle rect = new Rectangle(0,0,0,0);
            Size size = this.GetOurSize();
            
            rect.X = ptOurLocation.X;

            bool fLabel = ((flRow & ROWLABEL) != 0);
            bool fValue = ((flRow & ROWVALUE) != 0);

            if (fLabel && fValue) {
                rect.X = 1;
                rect.Width = size.Width - 1;
            }
            else if (fLabel) {
                rect.X = 1;
                rect.Width = InternalLabelWidth - 1;
            }
            else if (fValue) {
                rect.X = ptOurLocation.X + InternalLabelWidth;
                rect.Width = size.Width - InternalLabelWidth;
            }

            rect.Y = (row)*(RowHeight+1)+1+ptOurLocation.Y;
            rect.Height = RowHeight;

            return rect;
        }

        private /*protected virtual*/ int GetRowFromGridEntry(GridEntry gridEntry) {
            GridEntryCollection rgipesAll = GetAllGridEntries();
            if (gridEntry == null || rgipesAll == null)
                return -1;

            int bestMatch = -1;

            for (int i = 0; i < rgipesAll.Count; i++) {

                // try for an exact match.  semantics of equals are a bit loose here...
                //
                if (gridEntry == rgipesAll[i]) {
                    return i - GetScrollOffset();
                }
                else if (bestMatch == -1 && gridEntry.Equals(rgipesAll[i])) {
                    bestMatch = i - GetScrollOffset();
                }
            }

            if (bestMatch != -1) {
                return bestMatch;
            }

            return -1 - GetScrollOffset();
        }

        public virtual bool GetInPropertySet() {
            return GetFlag(FlagInPropertySet);
        }

        protected virtual bool GetScrollbarHidden() {
            if (scrollBar == null) {
                return true;
            }
            return !ScrollBar.Visible;
        }

        public Color GetTextColor() {
            return this.ForeColor;
        }

        private void LayoutWindow(bool invalidate) {
            Rectangle rect = ClientRectangle;
            Size sizeWindow = new Size(rect.Width,rect.Height);

            if (scrollBar != null) {
                Rectangle boundsScroll = ScrollBar.Bounds;
                boundsScroll.X = sizeWindow.Width - boundsScroll.Width - 1;
                boundsScroll.Y = 1;
                boundsScroll.Height = sizeWindow.Height - 2;
                ScrollBar.Bounds = boundsScroll;
            }

            if (invalidate) {
                Invalidate();
            }
        }

        private void InvalidateRow(int row) {
            InvalidateRows(row, row, ROWVALUE | ROWLABEL);
        }

        private void InvalidateRow(int row, int type ) {
            InvalidateRows(row, row, type);
        }

        private void InvalidateRows(int startRow, int endRow) {
            InvalidateRows(startRow, endRow, ROWVALUE | ROWLABEL);
        }

        private void InvalidateRows(int startRow, int endRow, int type) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:InvalidateRows");

            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Invalidating rows " + startRow.ToString() + " through " + endRow.ToString());
            Rectangle rect;

            // invalidate from the start row down
            if (endRow == -1) {
                rect = GetRectangle(startRow, type);
                rect.Height = (Size.Height - rect.Y) - 1;
                Invalidate(rect);
            }
            else {
                for (int i = startRow; i <= endRow; i++) {
                    rect = GetRectangle(i, type);
                    Invalidate(rect);
                }
            }
        }

        private void InvalidateLabels() {
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Invalidating labels");
            Invalidate(new Rectangle(0, 0, InternalLabelWidth + 2 , Size.Height));
        }

        private void InvalidateValues() {
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Invalidating values");
            int left = InternalLabelWidth - 2;
            Invalidate(new Rectangle(left, 0, Size.Width - left, Size.Height));
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.IsInputKey"]/*' />
        /// <devdoc>
        ///     Overridden to handle TAB key.
        /// </devdoc>
        protected override bool IsInputKey(Keys keyData) {
            switch (keyData & Keys.KeyCode) {
                case Keys.Escape:
                case Keys.Tab:
                case Keys.F4:
                    return false;

                case Keys.Return:
                     if (Edit.Focused) {
                        return false;
                     }
                     break;
            }
            return base.IsInputKey(keyData);
        }

        private bool IsMyChild(Control c) {

            if (c == this || c == null) {
                return false;
            }

            Control cParent = c.ParentInternal;

            while (cParent != null) {
                if (cParent == this) {
                    return true;
                }
                cParent = cParent.ParentInternal;
            }
            return false;
        }

        private bool IsScrollValueValid(int newValue) {
            /*Debug.WriteLine("se.newValue = " + se.newValue.ToString());
            Debug.WriteLine("ScrollBar.Value = " + ScrollBar.Value.ToString());
            Debug.WriteLine("visibleRows = " + visibleRows.ToString());
            Debug.WriteLine("totalProps = " + totalProps.ToString());
            Debug.WriteLine("ScrollBar.Max = " + ScrollBar.Maximum.ToString());
            Debug.WriteLine("ScrollBar.LargeChange = " + ScrollBar.LargeChange.ToString());*/

            // is this move valid?
            if (newValue == ScrollBar.Value ||
                newValue < 0 ||
                newValue > ScrollBar.Maximum ||
                (newValue + (ScrollBar.LargeChange-1) >= totalProps)) {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView: move not needed, returning");
                return false;
            }
            return true;
        }

        internal bool IsSiblingControl(Control c1, Control c2) {

            Control parent1 = c1.ParentInternal;
            Control parent2 = c2.ParentInternal;

            while (parent2 != null) {
                if (parent1 == parent2) {
                    return true;
                }
                parent2 = parent2.ParentInternal;
            }
            return false;
        }

        private void MoveSplitterTo(int xpos) {

            int widthPS = GetOurSize().Width;
            int startPS = ptOurLocation.X;
            int pos = Math.Max(Math.Min(xpos,widthPS-10),GetOutlineIconSize() * 2);

            int oldLabelWidth = InternalLabelWidth;

            labelRatio = ((double)widthPS / (double) (pos - startPS));

            SetConstants();

            if (selectedRow != -1) {
                // do this to move any editor we have
                SelectRow(selectedRow);
            }
            
            Rectangle r = ClientRectangle;
            
            // if we're moving to the left, just invalidate the values
            if (oldLabelWidth > InternalLabelWidth) {
                int left = InternalLabelWidth - requiredLabelPaintMargin;
                Invalidate(new Rectangle(left, 0, Size.Width - left, Size.Height));
            }
            else {
                // to the right, just invalidate from where the splitter was
                // to the right
                r.X = oldLabelWidth - requiredLabelPaintMargin;
                r.Width -= r.X;
                Invalidate(r);
            }
        }

        private void OnBtnClick(object sender, EventArgs e) {

            if (GetFlag(FlagBtnLaunchedEditor)) {
                return;
            }

            if (sender == DialogButton && !Commit()) {
                return;
            }
            SetCommitError(ERROR_NONE);

            try {
                SetFlag(FlagBtnLaunchedEditor, true);
                PopupDialog(selectedRow);
            }
            finally {
                SetFlag(FlagBtnLaunchedEditor, false);
            }
        }

        private void OnBtnKeyDown(object sender, KeyEventArgs ke) {
            OnKeyDown(sender,ke);
        }

        private void OnBtnMouseDown(object sender, MouseEventArgs e) {
            SetFlag(FlagBtnLaunchedEditor, this.dropDownHolder == null || !this.dropDownHolder.Visible);
        }

        private void OnBtnMouseUp(object sender, MouseEventArgs e) {
            if (!GetFlag(FlagBtnLaunchedEditor)) {
                return;
            }

            OnBtnClick(sender, e);
            SetFlag(FlagBtnLaunchedEditor, false);
        }

        private void OnChildLostFocus(object sender, EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnChildLostFocus");
            OnLostFocus(null);
        }

        protected override void OnGotFocus(EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnGotFocus");
            
            base.OnGotFocus(e);
            
            if (e != null && !GetInPropertySet()) {
                if (!Commit()) {
                    Edit.FocusInternal();
                    return;
                }
            }

            if (selectedGridEntry != null && GetRowFromGridEntry(selectedGridEntry) != -1) {
                selectedGridEntry.Focus = true;
                SelectGridEntry(selectedGridEntry, false);
            }
            else {
                SelectRow(0);
            }            
        }

         protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnSysColorChange);
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnSysColorChange);
            // We can leak this if we aren't disposed.
            //
            if (toolTip != null && !RecreatingHandle) {
                toolTip.Dispose();
                toolTip = null;
            }
            base.OnHandleDestroyed(e);
        }

        public bool OnHelp() {
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null || !(Focused || Edit.Focused)) {
                return false;
            }

            string keyword = gridEntry.HelpKeyword;
            if (keyword != null && keyword.Length != 0) {
                try {
                    IHelpService hsvc = GetHelpService();
                    if (hsvc != null) {
                        hsvc.ShowHelpFromKeyword(keyword);
                    }
                }
                catch (Exception) {
                }
            }
            return true;
        }

        protected override void OnImeModeChanged(EventArgs e) {
            if (edit != null) {
                // URT  #51190
                // Keep the ImeMode of the property grid and edit box in step
                //
                edit.ImeMode = this.ImeMode;
            }
            base.OnImeModeChanged(e);
        }                                       

        private void OnListChange(object sender, EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnListChange");
            if (!DropDownListBox.InSetSelectedIndex()) {
                GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
                Edit.Text = gridEntry.GetPropertyTextValue(DropDownListBox.SelectedItem);
                Edit.FocusInternal();
                SelectEdit(false);
            }
            SetFlag(FlagDropDownCommit, true);
        }

        private void OnListMouseUp(object sender, MouseEventArgs me) {
            OnListClick(sender, me);
        }

        private void OnListClick(object sender, EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnListClick");
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            
            if (DropDownListBox.Items.Count == 0) {
               CommonEditorHide();
               SetCommitError(ERROR_NONE);
               SelectRow(selectedRow);
               return;
            }
            else {
                object value = DropDownListBox.SelectedItem;

                // don't need the commit becuase we're committing anyway.
                //
                SetFlag(FlagDropDownCommit, false);
                if (value != null && !CommitText((string)value)) {
                    SetCommitError(ERROR_NONE);
                    SelectRow(selectedRow);
                }
            }
        }
        
        private void OnListDrawItem(object sender, DrawItemEventArgs die) {
            int index = die.Index;

            if (index < 0 || selectedGridEntry == null) {
                return;
            }

            string text = (string)DropDownListBox.Items[die.Index];

            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Drawing list item, value='" + text + "'");
            die.DrawBackground();
            die.DrawFocusRectangle();

            Rectangle drawBounds = die.Bounds;
            drawBounds.Y += 1;
            drawBounds.X -= 1;
            

            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            DrawValue(die.Graphics,drawBounds, drawBounds,gridEntry, gridEntry.ConvertTextToValue(text), (int)(die.State & DrawItemState.Selected) != 0, false, false, false);
        }

        private void OnListKeyDown(object sender, KeyEventArgs ke) {
            if (ke.KeyCode == Keys.Return) {
                OnListClick(null, null);
                selectedGridEntry.OnValueReturnKey();
            }

            OnKeyDown(sender,ke);
        }

        protected override void OnLostFocus(EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnLostFocus");
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "PropertyGridView lost focus");
            
            if (e != null) {
                base.OnLostFocus(e);
            }
            if (this.FocusInside) {
                base.OnLostFocus(e);
                return;
            }
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry != null) {
                Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "removing gridEntry focus");
                gridEntry.Focus = false;;
                CommonEditorHide();
                InvalidateRow(selectedRow);
            }
            RefreshHelpAttributes();
            base.OnLostFocus(e);
        }

        private void OnEditChange(object sender, EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEditChange");
            SetCommitError(ERROR_NONE, Edit.Focused);
            
            ToolTip.ToolTip = "";
            ToolTip.Visible = false;
            
            if (!Edit.InSetText()) {
                GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
                if (gridEntry != null && (gridEntry.Flags & GridEntry.FLAG_IMMEDIATELY_EDITABLE) != 0)
                    Commit();
            }
        }

        private void OnEditGotFocus(object sender, EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEditGotFocus");

            if (!Edit.Visible) {
                this.FocusInternal();
                return;
            }

            switch (errorState) {
                case ERROR_MSGBOX_UP:
                    return;
                case ERROR_THROWN:
                    if (Edit.Visible) {
                        Edit.HookMouseDown = true;
                    }
                    break;
                default:
                    if (this.NeedsCommit) {
                        SetCommitError(ERROR_NONE, true);
                    }
                    break;
            }

            if (selectedGridEntry != null && GetRowFromGridEntry(selectedGridEntry) != -1) {
                Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "adding gridEntry focus");
                selectedGridEntry.Focus = true;
                InvalidateRow(selectedRow);
            }
            else {
                SelectRow(0);
            }
        }
        
        private void OnEditImeModeChanged(object sender, EventArgs e) {
            // URT  #51190
            // The property grid ImeMode tracks the ImeMode of the edit control.
            // We require this because the first character the goes into the edit control
            // is composed while the PropertyGrid still has focus - so the ImeMode
            // of the grid and the edit need to be the same or we get inconsistent IME composition.
            //
            if (this.ImeMode != edit.ImeMode) {
                this.ImeMode = edit.ImeMode;            
            }
        }
        
        private void OnEditKeyDown(object sender, KeyEventArgs ke) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEditKeyDown");
            bool fAlt = ke.Alt;
            if (!fAlt && (ke.KeyCode == Keys.Up || ke.KeyCode == Keys.Down)) {
                GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
                if (!gridEntry.Enumerable || !gridEntry.IsValueEditable) {
                    return;
                }
                object value = gridEntry.PropertyValue;
                object[] rgvalues = gridEntry.GetPropertyValueList();
                ke.Handled = true;
                if (rgvalues != null) {
                    for (int i = 0; i < rgvalues.Length; i++) {
                        object rgvalue = rgvalues[i];
                        if (value != null && rgvalue != null && value.GetType() != rgvalue.GetType() && gridEntry.TypeConverter.CanConvertTo(gridEntry, value.GetType())) {
                            rgvalue = gridEntry.TypeConverter.ConvertTo(gridEntry, CultureInfo.CurrentCulture, rgvalue, value.GetType());
                        }

                        bool equal = (value == rgvalue) || (value != null && value.Equals(rgvalue));

                        if (!equal && value is string && rgvalue != null) {
                            equal = 0 == String.Compare((string)value, rgvalue.ToString(), true, CultureInfo.CurrentCulture);
                        }
                        if (equal) {
                            object valueNew = null;
                            if (ke.KeyCode == Keys.Up) {
                                if (i == 0) return;
                                valueNew = rgvalues[i - 1];
                            }
                            else {
                                if (i == rgvalues.Length - 1) return;
                                valueNew = rgvalues[i + 1];
                            }
                            CommitValue(valueNew);
                            SelectEdit(false);
                            return;
                        }
                    }
                }
            }
            // VS7 # 13336:  handle non-expand/collapse case of left & right as up & down
            else if ((ke.KeyCode == Keys.Left || ke.KeyCode == Keys.Right) &&
                     (ke.Modifiers & ~Keys.Shift) != 0) {
                return;
            }
            OnKeyDown(sender,ke);
        }

        private void OnEditKeyPress(object sender, KeyPressEventArgs ke) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEditKeyPress");
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null)
                return;

            if (!gridEntry.IsTextEditable) {
                ke.Handled = FilterReadOnlyEditKeyPress(ke.KeyChar);
            }
        }


        private void OnEditLostFocus(object sender, EventArgs e) {

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEditLostFocus");

            // believe it or not, this can actually happen.
            if (Edit.Focused || (errorState == ERROR_MSGBOX_UP) || (errorState == ERROR_THROWN)|| GetInPropertySet()) {
                return;
            }

            // check to see if the focus is on the drop down or one of it's children
            // if so, return;
            if (dropDownHolder != null && dropDownHolder.Visible) {
                bool found = false;
                for (IntPtr hwnd = UnsafeNativeMethods.GetForegroundWindow();
                    hwnd != IntPtr.Zero; hwnd = UnsafeNativeMethods.GetParent(new HandleRef(null, hwnd))) {
                    if (hwnd == dropDownHolder.Handle) {
                        found = true;
                    }
                }
                if (found)
                    return;
            }
            
            if (this.FocusInside) {
               return;
            }
            
            // if the focus isn't goint to a child of the view
            if (!Commit()) {
                Edit.FocusInternal();
                return;
            }
            // change our focus state.
            OnLostFocus(null);
        }

        private void OnEditMouseDown(object sender, MouseEventArgs me) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEditMouseDown");
            
            if (!FocusInside) {
               SelectGridEntry(selectedGridEntry, false);
            }
            
            if (me.Clicks % 2 == 0) {
                DoubleClickRow(selectedRow,false, ROWVALUE);
                Edit.SelectAll();
            }
            
            if (rowSelectTime == 0) {
                return;
            }
            
            // check if the click happened within the double click time since the row was selected.
            // this allows the edits to be selected with two clicks instead of 3 (select row, double click).
            //
            long timeStamp = DateTime.Now.Ticks;
            int delta = (int)((timeStamp - rowSelectTime) / 10000); // make it milleseconds
            
            if (delta < SystemInformation.DoubleClickTime) {
                DoubleClickRow(selectedRow,false, ROWVALUE);
                
                Edit.SendMessage(NativeMethods.WM_LBUTTONUP, 0, (int)(me.Y << 16 | (me.X & 0xFFFF)));
                Edit.SelectAll();
                rowSelectTime = 0;
            }
        }
      
        private bool OnF4(Control sender) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnF4");
            if (ModifierKeys != 0) {
                return false;
            }
            if (sender == this || sender == this.ownerGrid)
                F4Selection(true);
            else
                UnfocusSelection();
            return true;
        }

        private bool OnEscape(Control sender) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnEscape");
            if ((ModifierKeys & (Keys.Alt | Keys.Control)) != 0) {
                return false;
            }

            SetFlag(FlagDropDownCommit, false);

            if (sender == Edit && Edit.Focused) {
            
                // if we aren't in an error state, just quit
                if (errorState == ERROR_NONE) {
                   Edit.Text = originalTextValue;
                   FocusInternal();
                   return true;
                }
            
                if (this.NeedsCommit) {
                    bool success = false;
                    Edit.Text = originalTextValue;
                    bool needReset = true;

                    if (selectedGridEntry != null) {
                                    
                        string curTextValue = selectedGridEntry.GetPropertyTextValue();
                        needReset = originalTextValue != curTextValue;
                    }
        
                    if (needReset) {
                        try {
                            success = CommitText(originalTextValue);
                        }
                        catch (Exception) {
                        }
                    }
                    else {
                        success = true;
                    }

                    // this would be an odd thing to happen, but...
                    if (!success) {
                        Edit.FocusInternal();
                        SelectEdit(false);
                        return true;
                    }
                }

                SetCommitError(ERROR_NONE);
                FocusInternal();
                return true;
            }
            else if (sender != this) {
                CloseDropDown();
                FocusInternal();
            }
            return false;
        }

        protected override void OnKeyDown(KeyEventArgs ke) {
            OnKeyDown(this,ke);
        }

        private void OnKeyDown(object sender, KeyEventArgs ke) {

            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null) return;

            ke.Handled = true;
            bool fControl = ke.Control;
            bool fShift = ke.Shift;
            bool fBoth = fControl && fShift;
            bool fAlt = ke.Alt;
            Keys keyCode = ke.KeyCode;
            bool fallingThorugh = false;

            // SBurke, we have to do this here because if we are
            // hosted in a non-windows forms dialog, we never get a chance to
            // peek at the messages, we just get called,
            // so we have to do this here...
            //
            if (keyCode == Keys.Tab) {
                if (ProcessDialogKey(ke.KeyData)) {
                    ke.Handled = true;
                    return;
                }
            }

            // Alt-Arrow support... sigh...
            if (keyCode == Keys.Down && fAlt && DropDownButton.Visible) {
                F4Selection(false);
                return;
            }

            if (keyCode == Keys.Up && fAlt && DropDownButton.Visible && (dropDownHolder != null) && dropDownHolder.Visible) {
                UnfocusSelection();
                return;
            }

            if (ToolTip.Visible) {
                ToolTip.ToolTip = "";
            }


            if (fBoth || sender == this || sender == this.ownerGrid) {
                switch (keyCode) {
                    case Keys.Up:
                    case Keys.Down:
                        int pos = (keyCode == Keys.Up ? selectedRow - 1 : selectedRow + 1);
                        SelectGridEntry(GetGridEntryFromRow(pos),true);
                        SetFlag(FlagNoDefault, false);
                        return;
                    case Keys.Left:
                        if (fControl) {
                            // move the splitter 3 pixels to the left
                            MoveSplitterTo(InternalLabelWidth - 3);
                            return;
                        }
                        if (gridEntry.InternalExpanded)
                            SetExpand(gridEntry,false);
                        else {
                            // VS7 # 13336:  handle non-expand/collapse case of left & right as up & down
                            SelectGridEntry( GetGridEntryFromRow( selectedRow - 1 ), true );
                        }
                        return;
                    case Keys.Right:
                        if (fControl) {
                            // move the splitter 3 pixels to the right
                            MoveSplitterTo(InternalLabelWidth + 3);
                            return;
                        }
                        if (gridEntry.Expandable) {
                            if (gridEntry.InternalExpanded) {
                                GridEntryCollection rgipes2 = gridEntry.Children;
                                SelectGridEntry(rgipes2.GetEntry(0),true);
                            }
                            else
                                SetExpand(gridEntry,true);
                        }
                        else {
                            // VS7 # 13336:  handle non-expand/collapse case of left & right as up & down
                            SelectGridEntry( GetGridEntryFromRow( selectedRow + 1 ), true );
                        }
                        return;
                    case Keys.Return:
                        if (gridEntry.Expandable) {
                            SetExpand(gridEntry,!gridEntry.InternalExpanded);
                        }
                        else {
                           gridEntry.OnValueReturnKey();
                        }
                        
                        return;
                    case Keys.Home:
                    case Keys.End:
                        GridEntryCollection rgipes = GetAllGridEntries();
                        int pos2 = (keyCode == Keys.Home ? 0 : rgipes.Count-1);
                        SelectGridEntry(rgipes.GetEntry(pos2),true);
                        return;
                    case Keys.Add:
                    case Keys.Oemplus:
                    case Keys.OemMinus:
                    case Keys.Subtract:
                        SetFlag(FlagIsSpecialKey, true);
                        bool expand = (keyCode == Keys.Add || keyCode == Keys.Oemplus);
                        SetExpand(gridEntry,expand);
                        Invalidate();
                        ke.Handled = true;
                        return;

                
                case Keys.D8:
                        if (fShift) {
                            goto case Keys.Multiply;
                        }
                        break;
                case Keys.Multiply:
                        SetFlag(FlagIsSpecialKey, true);
                        RecursivelyExpand(gridEntry,true, true, MaxRecurseExpand);
                        ke.Handled = false;
                        return;

                    
                    
                    case Keys.Prior:  //PAGE_UP:
                    case Keys.Next: //PAGE_DOWN
                        
                        bool next = (keyCode == Keys.Next);
                        //int rowGoal = next ? visibleRows - 1 : 0;
                        int offset = next ? visibleRows - 1 : 1 - visibleRows;

                        int row = selectedRow;

                        if (fControl && !fShift) {
                            return;
                        }
                        if (selectedRow != -1) { // actual paging.

                            int start = GetScrollOffset();
                            SetScrollOffset(start + offset);
                            SetConstants();
                            if (GetScrollOffset() != (start + offset)) {
                                // we didn't make a full page
                                if (next) {
                                    row = visibleRows - 1;
                                }
                                else {
                                    row = 0;
                                }
                            }
                        }

                        SelectRow(row);
                        Refresh();
                        return;   
                    
                    // Copy/paste support...    
                    
                    case Keys.Insert:
                        if (fShift && !fControl && !fAlt) {
                           fallingThorugh = true;
                           goto case Keys.V;
                        }
                        goto case Keys.C;
                    case Keys.C:
                        // copy text in current property
                        if (fControl && !fAlt && !fShift) {
                           DoCopyCommand();
                           return;
                        }
                        break;
                    case Keys.Delete:
                        // cut text in current property
                        if (fShift && !fControl && !fAlt) {
                           fallingThorugh = true;
                           goto case Keys.X;
                        }
                        break;
                    case Keys.X:
                        // cut text in current property
                        if (fallingThorugh || (fControl && !fAlt && !fShift)) {
                           Clipboard.SetDataObject(gridEntry.GetPropertyTextValue());
                           CommitText("");
                           return;
                        }
                        break;
                    case Keys.V:
                        // paste the text
                        if (fallingThorugh || (fControl && !fAlt && !fShift)) {
                           DoPasteCommand();
                        }
                        break;
                    case Keys.A:
                        if (fControl && !fAlt && !fShift && Edit.Visible) {
                           Edit.FocusInternal();
                           Edit.SelectAll();       
                        }
                        break;
                }
            }

            if (gridEntry != null && ke.KeyData == (Keys.C | Keys.Alt | Keys.Shift | Keys.Control)) {
                Clipboard.SetDataObject(gridEntry.GetTestingInfo());
                return;
            }

            /* SBurke, VS30371 -- Due to conflicts with other VS commands,
               we are removing this functionality.

               // Ctrl + Shift + 'X' selects the property that starts with 'X'
               if (fBoth) {
                   // now get the array to work with.
                   GridEntry[] rgipes = GetAllGridEntries();
                   int cLength = rgipes.Length;

                   // now get our char.
                   string strCh = (new string(new char[] {(char)ke.KeyCode})).ToLower(CultureInfo.InvariantCulture);

                   int cCur = -1;
                   if (gridEntry != null)
                       for (int i = 0; i < cLength; i++) {
                           if (rgipes[i] == gridEntry) {
                               cCur = i;
                               break;
                           }
                       }

                   cCur += 1; // this indicated where we start...
                   // find next label that starts with this letter.
                   for (int i = 0; i < cLength; i++) {
                       GridEntry ipeCur = rgipes[(i + cCur) % cLength];
                       if (ipeCur.PropertyLabel.ToLower(CultureInfo.InvariantCulture).StartsWith(strCh)) {
                           if (gridEntry != ipeCur) {
                               SelectGridEntry(ipeCur,true);
                               return;
                           }
                           break;
                       }
                   }
               }
            */
            
            ke.Handled = false;
            return;
        }

        protected override void OnKeyPress(KeyPressEventArgs ke) {

            bool fControl = false; //ke.getControl();
            bool fShift = false; //ke.getShift();
            bool fBoth = fControl && fShift;
            if (!fBoth && WillFilterKeyPress(ke.KeyChar))
                // find next property with letter typed.
                FilterKeyPress(ke.KeyChar);
            SetFlag(FlagIsSpecialKey, false);
        }

        protected override void OnMouseDown(MouseEventArgs me) {
            // check for a splitter
            if (me.Button == MouseButtons.Left && SplitterInside(me.X,me.Y) && totalProps != 0) {
                if (!Commit()) {
                    return;
                }
                
                if (me.Clicks == 2) {
                     MoveSplitterTo(this.Width / 2);
                     return;
                }
                
                UnfocusSelection();
                SetFlag(FlagIsSplitterMove, true);
                tipInfo = -1;
                CaptureInternal = true;
                return;
            }

            // are ew on a propentry?
            Point pos = FindPosition(me.X,me.Y);

            if (pos == InvalidPosition) {
                return;
            }

            // Notify that prop entry of the click...but normalize
            // it's coords first...we really  just need the x, y
            GridEntry gridEntry = GetGridEntryFromRow(pos.Y);

            if (gridEntry != null) {
                // get the origin of this pe
                Rectangle r = GetRectangle(pos.Y, ROWLABEL);
                
                lastMouseDown = new Point(me.X, me.Y);

                // offset the mouse points
                // notify the prop entry
                if (me.Button == MouseButtons.Left) {
                    gridEntry.OnMouseClick(me.X - r.X, me.Y - r.Y, me.Clicks, me.Button);
                }
                else {
                    SelectGridEntry(gridEntry, false);
                }
                lastMouseDown = InvalidPosition;
                gridEntry.Focus = true;
                SetFlag(FlagNoDefault, false);
            }
        }

        // this will make tool tip go away.
        protected override void OnMouseLeave(EventArgs e) {
            if (!GetFlag(FlagIsSplitterMove))
                Cursor = Cursors.Default; // Cursor = null;;

            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs me) {
            int rowMoveCur;
            Point pt = Point.Empty;
            bool onLabel = false;

            if (me == null) {
                rowMoveCur = -1;
                pt = InvalidPosition;
            }
            else {
                pt = FindPosition(me.X,me.Y);
                if (pt == InvalidPosition || (pt.X != ROWLABEL && pt.X != ROWVALUE)) {
                    rowMoveCur = -1;
                    ToolTip.ToolTip = "";
                }
                else {
                    rowMoveCur = pt.Y;
                    onLabel = pt.X == ROWLABEL;
                }

            }

            if (pt == InvalidPosition || me == null) {
                return;
            }

            if (GetFlag(FlagIsSplitterMove)) {
                MoveSplitterTo(me.X);
            }
            
            if ((rowMoveCur != this.TipRow || pt.X != this.TipColumn)  && !GetFlag(FlagIsSplitterMove)) {
                GridEntry gridItem = GetGridEntryFromRow(rowMoveCur);
                string tip = "";
                tipInfo = -1;

                if (gridItem != null) {
                    Rectangle itemRect = GetRectangle(pt.Y, pt.X);
                    if (onLabel && gridItem.GetLabelToolTipLocation(me.X - itemRect.X, me.Y - itemRect.Y) != InvalidPoint) {
                        tip = gridItem.LabelToolTipText;
                        this.TipRow = rowMoveCur;
                        this.TipColumn = pt.X;
                    }
                    else if (!onLabel && gridItem.ValueToolTipLocation != InvalidPoint && !Edit.Focused) {
                        if (!this.NeedsCommit) {
                           tip = gridItem.GetPropertyTextValue();
                        }
                        this.TipRow = rowMoveCur;
                        this.TipColumn = pt.X;
                    }
                }

                // vs  75848 -- don't show the tips if a
                // dropdown is showing
                if ((dropDownHolder == null || dropDownHolder.Component == null) || rowMoveCur == selectedRow) {
                    ToolTip.ToolTip = tip;
                }
            }

            if (totalProps != 0 && (SplitterInside(me.X,me.Y) || GetFlag(FlagIsSplitterMove))) {
                Cursor = Cursors.VSplit;
            }
            else {
                Cursor = Cursors.Default; // Cursor = null;;
            }
            base.OnMouseMove(me);
        }

        protected override void OnMouseUp(MouseEventArgs me) {
            CancelSplitterMove();
        }


        protected override void OnMouseWheel(MouseEventArgs me) {
        
            // Should this only work if the Edit has focus?  anyway
            // we use the mouse wheel to change the values in the dropdown if it's
            // an enumerable value.
            //
            if (selectedGridEntry != null && selectedGridEntry.Enumerable && Edit.Focused && selectedGridEntry.IsValueEditable){
                  int index = GetCurrentValueIndex(selectedGridEntry);
                  if (index != -1) {
                     int delta = me.Delta > 0 ? -1 : 1;
                     object[] values = selectedGridEntry.GetPropertyValueList();

                    if (delta > 0 && index >= (values.Length - 1)) {
                        index = 0;
                    }
                    else if (delta < 0 && index == 0) {
                        index = values.Length - 1;
                    }
                    else {
                        index += delta;
                    }

                    CommitValue(values[index]);
                    SelectGridEntry(selectedGridEntry, true);
                    Edit.FocusInternal();
                    return;
                 }
            }

            int initial = GetScrollOffset();
            wheelDelta += me.Delta;
            float movePerc = (float)wheelDelta / (float)NativeMethods.WHEEL_DELTA;
            
            // SystemInformation.MouseWheelScrollLines doesn't work under terminal server, so we use 1 instead
            int move = (int)((float)SystemInformation.MouseWheelScrollLines * movePerc);
            if (move != 0) {
                   if (ToolTip.Visible) {
                       ToolTip.ToolTip = "";
                   }
                   wheelDelta = 0;
                   GridEntry ipeCur = selectedGridEntry;
   
                   // adjust the offset if it's outside the max values
                   int newOffset = Math.Max(0,initial - move);
                   newOffset = Math.Min(newOffset, totalProps - visibleRows +1);
   
                   if (!IsScrollValueValid(newOffset) || !Commit()) {
                       return;
                   }
                   
                   bool showEdit = Edit.Visible;
                   bool showBtnDropDown = DropDownButton.Visible;
                   bool showBtnEdit     = DialogButton.Visible;
                 
                   Edit.Visible = false;
                   DialogButton.Visible = false;
                   DropDownButton.Visible = false;
                   
                   SetScrollOffset(newOffset);
                   if (ipeCur != null) {
                       int curRow = GetRowFromGridEntry(ipeCur);
                       if (curRow >=0 && curRow < visibleRows) {
                           Edit.Visible = showEdit;
                           DialogButton.Visible = showBtnEdit;
                           DropDownButton.Visible = showBtnDropDown;
                           SelectGridEntry(ipeCur, true);
                       }
                       else {
                           CommonEditorHide();
                       }
                   }
                   else {
                       CommonEditorHide();
                   }
                   Invalidate();
            }
        }

        protected override void OnMove(EventArgs e) {
            CloseDropDown();
        }
        
        protected override void OnPaintBackground(PaintEventArgs pe) {
        }

        protected override void OnPaint(PaintEventArgs pe) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnPaint");
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "On paint called.  Rect=" + pe.ClipRectangle.ToString());
            Graphics g = pe.Graphics;

            int yPos = 0;
            int startRow = 0;
            int endRow = visibleRows - 1;
            
            Rectangle clipRect = pe.ClipRectangle;
            
            // give ourselves a little breathing room to account for lines, etc., as well
            // as the entries themselves.
            //
            clipRect.Inflate(0,2);

            try {
                Size sizeWindow = this.Size;

                // figure out what rows we're painting
                Point posStart = FindPosition(clipRect.X, clipRect.Y);
                Point posEnd = FindPosition(clipRect.X, clipRect.Y + clipRect.Height);
                if (posStart != InvalidPosition) {
                    startRow = Math.Max(0,posStart.Y);
                }

                if (posEnd != InvalidPosition) {
                    endRow   = posEnd.Y;
                }

                int cPropsVisible = Math.Min(totalProps - GetScrollOffset(),1+visibleRows);
                
#if DEBUG
                GridEntry debugIPEStart = GetGridEntryFromRow(startRow);
                GridEntry debugIPEEnd   = GetGridEntryFromRow(endRow);
                string startName = debugIPEStart == null ? null : debugIPEStart.PropertyLabel;
                if (startName == null) {
                    startName = "(null)";
                }
                string endName = debugIPEEnd == null ? null : debugIPEEnd.PropertyLabel;
                if (endName == null) {
                    endName = "(null)";
                }
#endif

                SetFlag(FlagNeedsRefresh, false);

                //SetConstants();

                Size size = this.GetOurSize();
                Point loc = this.ptOurLocation;

                if (GetGridEntryFromRow(cPropsVisible-1) == null) {
                    cPropsVisible--;
                }


                // if we actually have some properties, then start drawing the grid
                //
                if (totalProps > 0) {
                
                    // draw splitter
                    cPropsVisible = Math.Min(cPropsVisible, endRow+1);

                    Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Drawing splitter");
                    Pen splitterPen = new Pen(ownerGrid.LineColor, GetSplitterWidth());
                    splitterPen.DashStyle = DashStyle.Solid;
                    g.DrawLine(splitterPen, labelWidth,loc.Y,labelWidth, (cPropsVisible)*(RowHeight+1)+loc.Y);
                    splitterPen.Dispose();

                    // draw lines.
                    Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Drawing lines");
                    Pen linePen = new Pen(g.GetNearestColor(ownerGrid.LineColor));
                    
                    int cHeightCurRow = 0;
                    int cLineEnd = loc.X + size.Width;
                    int cLineStart = loc.X;

                    // draw values.
                    int totalWidth = GetTotalWidth() + 1;
                    //g.TextColor = ownerGrid.TextColor;

                    // draw labels. set clip rect.
                    for (int i = startRow; i < cPropsVisible; i++) {
                        try {

                            // draw the line
                            cHeightCurRow = (i)*(RowHeight+1) + loc.Y;
                            g.DrawLine(linePen, cLineStart,cHeightCurRow,cLineEnd,cHeightCurRow);

                            // draw the value
                            DrawValueEntry(g,i, ref clipRect);

                            // draw the label
                            Rectangle rect = GetRectangle(i,ROWLABEL);
                            yPos = rect.Y + rect.Height;
                            DrawLabel(g,i, rect, (i==selectedRow),false, ref clipRect);
                            if (i == selectedRow) {
                                Edit.Invalidate();
                            }

                        }
                        catch (Exception) {
                            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Exception thrown during painting property " + GetGridEntryFromRow(i).PropertyLabel);
                        }
                    }

                    // draw the bottom line
                    cHeightCurRow = (cPropsVisible)*(RowHeight+1) + loc.Y;
                    g.DrawLine(linePen, cLineStart,cHeightCurRow,cLineEnd,cHeightCurRow);
                    
                    linePen.Dispose();
                }

                // fill anything left with window
                if (yPos < Size.Height) {
                    yPos++;
                    Rectangle clearRect = new Rectangle(1, yPos, Size.Width - 2, Size.Height - yPos - 1);
                    Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Filling remaining area rect=" + clearRect.ToString());
                    
                    g.FillRectangle(backgroundBrush, clearRect);
                }

                g.DrawRectangle(SystemPens.ControlDark, 0,0,sizeWindow.Width - 1, sizeWindow.Height - 1);
                
                fontBold = null;
            }
            catch (Exception e) {
                Debug.Fail(e.ToString());
                // Do nothing.
            }
            finally {
                ClearCachedFontInfo();
            }
        }

        private void OnGridEntryLabelDoubleClick(object s, EventArgs e) {
            GridEntry gridEntry = (GridEntry)s;

            // if we've changed since the click (probably because we moved a row into view), bail
            //
            if (gridEntry != lastClickedEntry) {
                return;
            }
            int row = GetRowFromGridEntry(gridEntry);
            DoubleClickRow(row, gridEntry.Expandable, ROWLABEL);
        }

        private void OnGridEntryValueDoubleClick(object s, EventArgs e) {

            GridEntry gridEntry = (GridEntry)s;
            // if we've changed since the click (probably because we moved a row into view), bail
            //
            if (gridEntry != lastClickedEntry) {
                return;
            } 
            int row = GetRowFromGridEntry(gridEntry);
            DoubleClickRow(row, gridEntry.Expandable, ROWVALUE);
        }

        private void OnGridEntryLabelClick(object s, EventArgs e) {
            this.lastClickedEntry = (GridEntry)s;
            SelectGridEntry(lastClickedEntry, true);
        }

        private void OnGridEntryOutlineClick(object s, EventArgs e) {
            GridEntry gridEntry = (GridEntry)s;
            Debug.Assert(gridEntry.Expandable, "non-expandable IPE firing outline click");

            Cursor oldCursor = Cursor;
            if (!ShouldSerializeCursor()) {
                oldCursor = null;
            }
            Cursor = Cursors.WaitCursor;

            try {
                SetExpand(gridEntry, !gridEntry.InternalExpanded);
                SelectGridEntry(gridEntry, false);
            }
            finally {
                Cursor = oldCursor;
            }
        }

        private void OnGridEntryValueClick(object s, EventArgs e) {
        
            this.lastClickedEntry = (GridEntry)s;
            bool setSelectTime = s != selectedGridEntry;
            SelectGridEntry(lastClickedEntry, true);
            Edit.FocusInternal();
            
            if (lastMouseDown != InvalidPosition) {
            
               // clear the row select time so we don't interpret this as a double click.
               //
               rowSelectTime = 0;    
               
               Point editPoint = PointToScreen(lastMouseDown);
               editPoint = Edit.PointToClientInternal(editPoint);
               Edit.SendMessage(NativeMethods.WM_LBUTTONDOWN, 0, (int)(editPoint.Y << 16 | (editPoint.X & 0xFFFF))); 
               Edit.SendMessage(NativeMethods.WM_LBUTTONUP, 0, (int)(editPoint.Y << 16 | (editPoint.X & 0xFFFF))); 
            }
            
            if (setSelectTime) {
               rowSelectTime = DateTime.Now.Ticks;
            }
            else {
                rowSelectTime = 0;
            }
        }

        private void ClearCachedFontInfo() {
            if (baseHfont != IntPtr.Zero) {
                SafeNativeMethods.ExternalDeleteObject(new HandleRef(this, baseHfont));
                baseHfont = IntPtr.Zero;
            }
            if (boldHfont != IntPtr.Zero) {
                SafeNativeMethods.ExternalDeleteObject(new HandleRef(this, boldHfont));
                boldHfont = IntPtr.Zero;
            }
        }

        protected override void OnFontChanged(EventArgs e) {
        
            ClearCachedFontInfo();
            cachedRowHeight = -1;

            if (this.Disposing || this.ParentInternal == null || this.ParentInternal.Disposing) {
                return;
            }
         
            fontBold = null;    // URT  #45662 - fontBold is cached based on Font                        
                        
            ToolTip.Font = this.Font;
            SetFlag(FlagNeedUpdateUIBasedOnFont, true);
            UpdateUIBasedOnFont(true);
            base.OnFontChanged(e);

            if (selectedGridEntry != null) {
                SelectGridEntry(selectedGridEntry, true);
            }
        }

        protected override void OnVisibleChanged(EventArgs e) {
            if (this.Disposing || this.ParentInternal == null || this.ParentInternal.Disposing) {
                return;
            }
         
            if (this.Visible && this.ParentInternal != null) {
                 SetConstants();
                 if (selectedGridEntry != null) {
                       SelectGridEntry(selectedGridEntry, true);
                 }
                 if (toolTip != null) {
                     ToolTip.Font = this.Font;
                 }
                 
            }

            base.OnVisibleChanged(e);
        }
        
        // a GridEntry recreated its children
        protected virtual void OnRecreateChildren(object s, GridEntryRecreateChildrenEventArgs e) {
            GridEntry parent = (GridEntry) s;

            if (parent.Expanded) {
                                          
                GridEntry[] entries = new GridEntry[allGridEntries.Count];
                allGridEntries.CopyTo(entries, 0);
                
                // find the index of the gridEntry that fired the event in our main list.
                int parentIndex = -1;
                for (int i = 0; i < entries.Length; i++) {
                    if (entries[i] == parent) {
                        parentIndex = i;
                        break;
                    }
                }
                
                Debug.Assert(parentIndex != -1, "parent GridEntry not found in allGridEntries");
                
                // clear our existing handlers
                ClearGridEntryEvents(allGridEntries, parentIndex + 1, e.OldChildCount);
                
                // resize the array if it's changed
                if (e.OldChildCount != e.NewChildCount) {
                    int newArraySize = entries.Length + (e.NewChildCount - e.OldChildCount);
                    GridEntry[] newEntries = new GridEntry[newArraySize];
                    
                    // copy the existing entries up to the parent
                    Array.Copy(entries, 0, newEntries, 0, parentIndex + 1);
                    
                    // copy the entries after the spot we'll be putting the new ones
                    Array.Copy(entries, parentIndex + e.OldChildCount+1, newEntries, parentIndex + e.NewChildCount+1, entries.Length - (parentIndex + e.OldChildCount + 1));
                    
                    entries = newEntries;
                }
                
                // from that point, replace the children with tne new children.
                GridEntryCollection children = parent.Children;
                int childCount = children.Count;
                
                Debug.Assert(childCount == e.NewChildCount, "parent reports " + childCount + " new children, event reports " + e.NewChildCount);
                
                // replace the changed items
                for (int i = 0; i < childCount; i++) {
                    entries[parentIndex + i + 1] = children.GetEntry(i);
                }
                
                // reset the array, rehook the handlers.
                allGridEntries.Clear();
                allGridEntries.AddRange(entries);
                AddGridEntryEvents(allGridEntries, parentIndex + 1, childCount);            
                
            }

            if (e.OldChildCount != e.NewChildCount) {
                totalProps = CountPropsFromOutline(topLevelGridEntries);
                SetConstants();
            }
            Invalidate();
        }
        
        protected override void OnResize(EventArgs e) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnResize");

            Rectangle newRect = ClientRectangle;
            int       yDelta = lastClientRect == Rectangle.Empty  ? 0 : newRect.Height - lastClientRect.Height;
            bool   lastRow = (selectedRow+1) == visibleRows;

            // if we are hiding or showing the scroll bar, update the selected row
            // or if we are changing widths
            //
            bool sbVisible = ScrollBar.Visible;

            if (!lastClientRect.IsEmpty && newRect.Width > lastClientRect.Width) {
                Rectangle rectInvalidate = new Rectangle(lastClientRect.Width-1,0,newRect.Width-lastClientRect.Width+1,lastClientRect.Height);
                Invalidate(rectInvalidate);
            }

            if (!lastClientRect.IsEmpty && yDelta > 0) {
                Rectangle rectInvalidate = new Rectangle(0,lastClientRect.Height-1,lastClientRect.Width,newRect.Height-lastClientRect.Height+1);
                Invalidate(rectInvalidate);
            }

            int scroll = GetScrollOffset();
            SetScrollOffset(0);
            SetConstants();
            SetScrollOffset(scroll);
            

            CommonEditorHide();

            LayoutWindow(false);
            // vs  69679
            bool selectionVisible = (selectedGridEntry != null && selectedRow >=0  && selectedRow <= visibleRows);
            SelectGridEntry(selectedGridEntry, selectionVisible);
            lastClientRect = newRect;
        }

        private void OnScroll(object sender, ScrollEventArgs se) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:OnScroll(" + ScrollBar.Value.ToString() + " -> " + se.NewValue.ToString() +")");

			if (!IsScrollValueValid(se.NewValue) || !Commit()) {
                // cancel the move
                se.NewValue = ScrollBar.Value;
                return;
            }

			int oldRow = -1;
            GridEntry oldGridEntry = selectedGridEntry;
            if (selectedGridEntry != null) {
                oldRow = GetRowFromGridEntry(oldGridEntry);
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "OnScroll: SelectedGridEntry=" + oldGridEntry.PropertyLabel);
            }

            ScrollBar.Value = se.NewValue;
            if (oldGridEntry != null) {
                // we need to zero out the selected row so we don't try to commit again...since selectedRow is now bogus.
                selectedRow = -1;
                SelectGridEntry(oldGridEntry, (ScrollBar.Value == totalProps ? true : false));
                int newRow = GetRowFromGridEntry(oldGridEntry);
                if (oldRow != newRow) {
                    Invalidate(); 
                }
            }
            else {
                Invalidate();
            }
        }

        private void OnSysColorChange(object sender, UserPreferenceChangedEventArgs e) {
            if (e.Category == UserPreferenceCategory.Color || e.Category == UserPreferenceCategory.Accessibility) {
                SetFlag(FlagNeedUpdateUIBasedOnFont, true);
            }
        }
        
        public virtual void PopupDialog(int row) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:PopupDialog");
            GridEntry gridEntry = GetGridEntryFromRow(row);
            if (gridEntry != null) {
                if (dropDownHolder != null && dropDownHolder.GetUsed()) {
                    CloseDropDown();
                    return;
                }

                bool fBtnDropDown = gridEntry.NeedsDropDownButton;
                bool fEnum = gridEntry.Enumerable;
                bool fBtnDialog = gridEntry.NeedsCustomEditorButton;
                if (fEnum && !fBtnDropDown) {
                    DropDownListBox.Items.Clear();
                    object value = gridEntry.PropertyValue;
                    object[] rgItems = gridEntry.GetPropertyValueList();
                    int maxWidth = 0;

                    // The listbox draws with GDI, not GDI+.  So, we
                    // use a normal DC here.
                    //
                    IntPtr hdc = UnsafeNativeMethods.GetDC(new HandleRef(DropDownListBox, DropDownListBox.Handle));
                    IntPtr hFont = Font.ToHfont();
                    NativeMethods.TEXTMETRIC tm = new NativeMethods.TEXTMETRIC();
                    int iSel = -1;
                    try {
                        hFont = SafeNativeMethods.SelectObject(new HandleRef(DropDownListBox, hdc), new HandleRef(Font, hFont));
                        
                        iSel = GetCurrentValueIndex(gridEntry);
                        if (rgItems != null && rgItems.Length > 0) {
                            string s;
                            NativeMethods.SIZE textSize = new NativeMethods.SIZE();
                            
                            for (int i = 0; i < rgItems.Length; i++) {
                                s = gridEntry.GetPropertyTextValue(rgItems[i]);
                                DropDownListBox.Items.Add(s);        
                                SafeNativeMethods.GetTextExtentPoint32(new HandleRef(DropDownListBox, hdc), s, s.Length, textSize);
                                maxWidth = Math.Max((int) textSize.cx, maxWidth);
                            }
                        }
                        SafeNativeMethods.GetTextMetrics(new HandleRef(DropDownListBox, hdc), tm);
                        
                        // border + padding + scrollbar
                        maxWidth += 2 + tm.tmMaxCharWidth + SystemInformation.VerticalScrollBarWidth;
                        
                        hFont = SafeNativeMethods.SelectObject(new HandleRef(DropDownListBox, hdc), new HandleRef(Font, hFont));
                    }
                    finally {
                        SafeNativeMethods.DeleteObject(new HandleRef(Font, hFont));
                        UnsafeNativeMethods.ReleaseDC(new HandleRef(DropDownListBox, DropDownListBox.Handle), new HandleRef(DropDownListBox, hdc));
                    }
                    
                    // CHRISAN, 4/25/1998 - must check for -1 and not call the set...
                    if (iSel != -1) {
                        DropDownListBox.SelectedIndex = iSel;
                    }
                    SetFlag(FlagDropDownCommit, false);
                    DropDownListBox.Height = Math.Max(tm.tmHeight + 2, Math.Min(MAX_LISTBOX_HEIGHT, DropDownListBox.PreferredHeight));
                    DropDownListBox.Width = Math.Max(maxWidth, GetRectangle(row,ROWVALUE).Width);
                    DropDownControl(DropDownListBox);
                }
                else if (fBtnDialog || fBtnDropDown) {
                     try {
                        SetFlag(FlagInPropertySet, true);
                        Edit.DisableMouseHook = true;
                        gridEntry.EditPropertyValue(this);
                     }
                     finally {
                        SetFlag(FlagInPropertySet, false);
                        Edit.DisableMouseHook = false;
                     }
                     Refresh();
                     
                     // as/urt  31468 -- we can't do this because
                     // some dialogs are non-modal, and
                     // this will pull focus from them.
                     // See ASURT 31468.
                     //
                     //if (fBtnDialog) {
                     //      this.Focus();
                     //}
                     
                     if (FocusInside) {
                        SelectGridEntry(gridEntry, false);
                     }
                }
            }
        }
        
        internal static void PositionTooltip(Control parent, GridToolTip ToolTip, Rectangle itemRect) {
            ToolTip.Visible = false;
            
            NativeMethods.RECT rect = NativeMethods.RECT.FromXYWH(itemRect.X, itemRect.Y, itemRect.Width, itemRect.Height);

            ToolTip.SendMessage(NativeMethods.TTM_ADJUSTRECT, 1, ref rect);

            // now offset it back to screen coords
            Point locPoint = parent.PointToScreen(new Point(rect.left, rect.top));
            
            ToolTip.Location = locPoint;   // set the position once so it updates it's size with it's real width.
            
            int overHang =  (ToolTip.Location.X + ToolTip.Size.Width) - SystemInformation.VirtualScreen.Width;
            if (overHang > 0) {
                locPoint.X -= overHang;
                ToolTip.Location = locPoint;
            }

            // tell the control we've repositioned it.
            ToolTip.Visible = true;  
        }
        
        

        protected override bool ProcessDialogKey(Keys keyData) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:ProcessDialogKey");
            if (HasEntries) {
                Keys keyCode = keyData & Keys.KeyCode;
                switch (keyCode) {
                    case Keys.F4:
                        if (FocusInside) {
                            return OnF4(this);
                        }
                        break;

                    case Keys.Tab:
                        bool forward = (keyData & Keys.Shift) == 0;

                        Control focusedControl = Control.FromHandleInternal(UnsafeNativeMethods.GetFocus());

                        if (focusedControl == null || !IsMyChild(focusedControl)) {
                            if (forward) {
                                TabSelection();
                                focusedControl = Control.FromHandleInternal(UnsafeNativeMethods.GetFocus());
                                // make sure the value actually took the focus
                                if (IsMyChild(focusedControl)) {
                                    return true;
                                }
                                else {
                                    return base.ProcessDialogKey(keyData);
                                }

                            }
                            else {
                                break;
                            }
                        }
                        else {
                            // one of our editors has focus

                            if (Edit.Focused) {
                                if (forward) {
                                    if (DropDownButton.Visible) {
                                        DropDownButton.FocusInternal();
                                        return true;
                                    }
                                    else if (DialogButton.Visible) {
                                        DialogButton.FocusInternal();
                                        return true;
                                    }
                                    // fall through
                                }
                                else {
                                    SelectGridEntry(GetGridEntryFromRow(selectedRow), false);
                                    return true;
                                }
                            }
                            else if (DialogButton.Focused || DropDownButton.Focused) {
                                if (!forward && Edit.Visible) {
                                    Edit.FocusInternal();
                                    return true;
                                }
                                // fall through
                            }
                        }
                        break;  
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                        return false;
                    case Keys.Return:
                        if (DialogButton.Focused || DropDownButton.Focused) {
                           OnBtnClick((DialogButton.Focused ? DialogButton : DropDownButton), new EventArgs());
                           return true;
                        }
                        else if (selectedGridEntry != null && selectedGridEntry.Expandable) {
                           SetExpand(selectedGridEntry, !selectedGridEntry.InternalExpanded);
                           return true;
                        }
                        break;
                }
            }
            return base.ProcessDialogKey(keyData);
        }

        protected virtual void RecalculateProps() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:RecalculateProps");
            int props = CountPropsFromOutline(topLevelGridEntries);
            if (totalProps != props) {
                totalProps = props;
                ClearGridEntryEvents(allGridEntries, 0, -1);
                allGridEntries = null;
            }
        }

        internal /*public virtual*/ void RecursivelyExpand(GridEntry gridEntry, bool fInit, bool expand, int maxExpands) {

            if (expand && --maxExpands < 0) {
                return;
            }

            SetExpand(gridEntry, expand);

            GridEntryCollection rgipes = gridEntry.Children;
            if (rgipes != null)
                for (int i = 0; i < rgipes.Count; i++)
                    RecursivelyExpand(rgipes.GetEntry(i),false, expand, maxExpands);

            if (fInit) {
                GridEntry ipeSelect = selectedGridEntry;
                Refresh();
                SelectGridEntry(ipeSelect,false);
                Invalidate();
            }

        }

        public override void Refresh() {
            Refresh(false, -1, -1);
            // make sure we got everything
            Invalidate();
        }

        public void Refresh(bool fullRefresh) {
            Refresh(fullRefresh, -1, -1);
        }

        
        GridPositionData positionData;
    
        private void Refresh(bool fullRefresh, int rowStart, int rowEnd) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:Refresh");
            Debug.WriteLineIf(GridViewDebugPaint.TraceVerbose,  "Refresh called for rows " + rowStart.ToString() + " through " + rowEnd.ToString());
            SetFlag(FlagNeedsRefresh, true);
            GridEntry gridEntry = null;

            bool pageInGridEntry = true;
            
            if (rowStart == -1) {
                rowStart = 0;
            }
            
            if (fullRefresh || this.ownerGrid.HavePropEntriesChanged()) {
                if (HasEntries && !GetInPropertySet() && !Commit()) {
                    OnEscape(this);
                }
                
                int oldLength = totalProps;
                object oldObject = topLevelGridEntries == null || topLevelGridEntries.Count == 0 ? null : ((GridEntry)topLevelGridEntries[0]).GetValueOwner();
                
                // walk up to the main IPE and refresh it.
                if (fullRefresh) {
                    this.ownerGrid.RefreshProperties(true);
                }
                
                if (oldLength > 0 && !GetFlag(FlagNoDefault)) {
                     positionData = new GridPositionData(this);
                     CommonEditorHide(true);
                }
                
                selectedGridEntry = null;
                SetFlag(FlagIsNewSelection, true);
                topLevelGridEntries = this.ownerGrid.GetPropEntries();
                
                
                ClearGridEntryEvents(allGridEntries, 0, -1);
                allGridEntries = null;
                RecalculateProps();
                
                int newLength = totalProps;
                if (newLength > 0) {
                   if (newLength < oldLength) {
                       SetScrollbarLength();
                       SetScrollOffset(0);
                   }
                   
                   SetConstants();

                   if (positionData != null) {
                       gridEntry = positionData.Restore(this);

                       // Upon restoring the grid entry position, we don't
                       // want to page it in
                       //
                       object newObject = topLevelGridEntries == null || topLevelGridEntries.Count == 0 ? null : ((GridEntry)topLevelGridEntries[0]).GetValueOwner();
                       pageInGridEntry = (gridEntry == null) || oldLength != newLength || newObject != oldObject;
                   }
                   
                   if (gridEntry == null) {
                       gridEntry = this.ownerGrid.GetDefaultGridEntry();
                       SetFlag(FlagNoDefault, gridEntry == null && totalProps > 0);
                   }

                   InvalidateRows(rowStart, rowEnd);
                   if (gridEntry == null) {
                       selectedRow = 0;
                       selectedGridEntry = GetGridEntryFromRow(selectedRow);
                   }
                   positionData = null;
               }
               else if (oldLength == 0) {
                  return;
               }
               else {
                   SetConstants();
               }
            }

            if (!HasEntries) {
                CommonEditorHide(selectedRow != -1);
                this.ownerGrid.SetStatusBox(null, null);
                SetScrollOffset(0);
                selectedRow = -1;
                Invalidate();
                return;
            }
            // in case we added or removed properties
            
            ownerGrid.ClearValueCaches();

            InvalidateRows(rowStart, rowEnd);

            if (gridEntry != null) {
                SelectGridEntry(gridEntry, pageInGridEntry);
            }
        }

        public virtual void Reset() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:Reset");
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null) return;

            gridEntry.ResetPropertyValue();
            SelectRow(selectedRow);
        }

        protected virtual void ResetOrigin(System.Drawing.Graphics g) {
            g.ResetTransform();
        }
        
        internal void RestoreHierarchyState(ArrayList expandedItems) {
            if (expandedItems == null) {
               return;
            }
            
            foreach(GridEntryCollection gec in expandedItems) {
               FindEquivalentGridEntry(gec);
            }
        }

        public virtual DialogResult RunDialog(Form dialog) {
            return ShowDialog(dialog);
        }
        
        internal ArrayList SaveHierarchyState(GridEntryCollection entries) {
            return SaveHierarchyState(entries, null);
        }
        
        private ArrayList SaveHierarchyState(GridEntryCollection entries, ArrayList expandedItems) {
            if (entries == null) {
               return new ArrayList();
            }
            
            if (expandedItems == null) {
               expandedItems = new ArrayList();
            }
        
            for (int i = 0; i < entries.Count; i++) {
               if (((GridEntry)entries[i]).InternalExpanded) {
                  GridEntry entry = entries.GetEntry(i);
                  expandedItems.Add(GetGridEntryHierarchy(entry.Children.GetEntry(0)));
                  SaveHierarchyState(entry.Children, expandedItems);
               }
            }
            
            return expandedItems;
        }

        private void SelectEdit(bool caretAtEnd) {
            if (edit != null) {
                Edit.SelectAll();
            }
        }

        // select functions... selectGridEntry and selectRow will select a Row
        // and install the appropriate editors.
        //
        internal /*protected virtual*/ void SelectGridEntry(GridEntry gridEntry, bool fPageIn) {

            if (gridEntry == null) return;
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:SelectGridEntry(" + gridEntry.PropertyLabel + ")");

            int row = GetRowFromGridEntry(gridEntry);
            if (row + GetScrollOffset() < 0) {
                // throw exception? return false?
                return;
            }

            int maxRows = (int)Math.Ceiling(((double)GetOurSize().Height)/(1+RowHeight));

            if ((row >= 0 && row < (maxRows-1)) || !fPageIn) {
                // great. just select it.
                if (row != -1) {
                    SelectRow(row);
                }
                return;
            }

            // clear the selected row since it's no longer a valid number
            selectedRow = -1;

            // oh well. we have to page it in.
            int cOffset = GetScrollOffset();
            if (row < 0) {
                SetScrollOffset(row + cOffset);
                Invalidate();
                SelectRow(0);
            }
            else {
                // try to put it one row up from the bottom
                int newOffset = row + cOffset - (maxRows - 2);

                if (newOffset >= ScrollBar.Minimum && newOffset < ScrollBar.Maximum) {
                    SetScrollOffset(newOffset);
                }
                Invalidate();
                SelectGridEntry(gridEntry, false);
            }
            return;
        }

        private void SelectRow(int row) {

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:SelectRow(" + row.ToString() + ")");

            if (!GetFlag(FlagIsNewSelection)) {
                if (this.FocusInside) {
                    // If we're in an error state, we want to bail out of this.
                    if (errorState != ERROR_NONE || (row != selectedRow && !Commit())) {
                        return;
                    }
                }
                else {
                    FocusInternal();
                }
            }

            GridEntry gridEntry = GetGridEntryFromRow(row);

            // Update our reset command.
            //
            if (row != selectedRow) {
                UpdateResetCommand(gridEntry);
            }

            if (GetFlag(FlagIsNewSelection) && GetGridEntryFromRow(selectedRow) == null) {
                CommonEditorHide();
            }

            UpdateHelpAttributes(selectedGridEntry, gridEntry);

            // tell the old selection it's not focused any more
            if (selectedGridEntry != null) {
                selectedGridEntry.Focus = false;
            }

            // selection not visible.
            if (row < 0 || row >= visibleRows) {
                CommonEditorHide();
                selectedRow = row;
                selectedGridEntry = gridEntry;
                Refresh();
                return;
            }

            // leave current selection.
            if (gridEntry == null)
                return;

            bool newRow = false;
            int oldSel = selectedRow;
            if (selectedRow != row || !gridEntry.Equals(selectedGridEntry)) {
                CommonEditorHide();
                newRow = true;
            }
            
            if (!newRow)
                CloseDropDown();

            Rectangle rect = GetRectangle(row,ROWVALUE);
            string s = gridEntry.GetPropertyTextValue();

            // what components are we using?
            bool fBtnDropDown = gridEntry.NeedsDropDownButton | gridEntry.Enumerable;
            bool fBtnDialog = gridEntry.NeedsCustomEditorButton;
            bool fEdit = gridEntry.IsTextEditable;
            bool fPaint = gridEntry.IsCustomPaint;

            rect.X += 1;
            rect.Width -= 1;

            // we want to allow builders on read-only properties
            if ((fBtnDialog || fBtnDropDown) && !gridEntry.ShouldRenderReadOnly) {
                Control btn = fBtnDropDown ? (Control)DropDownButton : (Control)DialogButton;
                Size sizeBtn = new Size(SystemInformation.VerticalScrollBarArrowHeight, RowHeight);
                Rectangle rectTarget = new Rectangle(rect.X+rect.Width-sizeBtn.Width,
                                                      rect.Y-1,
                                                      sizeBtn.Width,rect.Height+2);
                CommonEditorUse(btn,rectTarget);
                sizeBtn = btn.Size;
                rect.Width -= (sizeBtn.Width + 1);
                btn.Invalidate();
            }

            // if we're painting the value, size the rect between the button and the painted value
            if (fPaint) {
                rect.X += PAINT_INDENT + 1;
                rect.Width -= PAINT_INDENT;
            }
            else {
                rect.X += EDIT_INDENT + 1; // +1 to compensate for where GDI+ draws it's string relative to the rect.
                rect.Width -= EDIT_INDENT + 1;
            }
           
            if ((GetFlag(FlagIsNewSelection) || !Edit.Focused) && (s != null && !s.Equals(Edit.Text))) {
                Edit.Text = s;
                originalTextValue = s;
                Edit.SelectionStart = 0;
                Edit.SelectionLength = 0;
            }


#if true // RENDERMODE
            switch (inheritRenderMode) {
                case RENDERMODE_BOLD:
                    if (gridEntry.ShouldSerializePropertyValue()) {
                        Edit.Font = GetBoldFont();
                    }
                    else {
                        Edit.Font = Font;
                    }
                    break;
                case RENDERMODE_LEFTDOT:
                    if (gridEntry.ShouldSerializePropertyValue()) {
                        rect.X += (LEFTDOT_SIZE * 2);
                        rect.Width -= (LEFTDOT_SIZE * 2);
                    }
                    // nothing
                    break;
                case RENDERMODE_TRIANGLE:
                    // nothing
                    break;
            }
#endif

            if (GetFlag(FlagIsSplitterMove) || !gridEntry.HasValue || !FocusInside) {
                Edit.Visible = false;
            }
            else {
                rect.Offset(1,1);
                rect.Height -= 1;
                CommonEditorUse(Edit,rect);
                bool drawReadOnly = gridEntry.ShouldRenderReadOnly;
                Edit.ForeColor = drawReadOnly ? this.GrayTextColor : this.ForeColor;
                Edit.BackColor = this.BackColor;
                Edit.ReadOnly = drawReadOnly || !gridEntry.IsTextEditable;
            }

            GridEntry oldSelectedGridEntry = selectedGridEntry;
            selectedRow = row;
            selectedGridEntry = gridEntry;
            this.ownerGrid.SetStatusBox(gridEntry.PropertyLabel,gridEntry.PropertyDescription);

            // tell the new focused item that it now has focus
            if (selectedGridEntry != null) {
                selectedGridEntry.Focus = this.FocusInside;
            }

            if (!GetFlag(FlagIsNewSelection)) {
                FocusInternal();
            }
            
            // CONSIDER: -1 is overloaded -- to us, it means "no selection", but to InvalidateRow,
            // it means "invalidate everything".  Should we ever pass InvalidateRow a -1?
            InvalidateRow(oldSel);

            InvalidateRow(row);
            if (FocusInside)
            {
                SetFlag(FlagIsNewSelection, false);
            }
            
            try {
                if (selectedGridEntry != oldSelectedGridEntry) {
                    this.ownerGrid.OnSelectedGridItemChanged(oldSelectedGridEntry, selectedGridEntry);
                }               
            }
            catch {
            }
        }

        public virtual void SetConstants() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:SetConstants");
            Size size = this.GetOurSize();
            
            visibleRows = (int)Math.Ceiling(((double)size.Height)/(1+RowHeight));
            
            size = this.GetOurSize();
            
            if (size.Width >= 0) {
                labelRatio = Math.Max(Math.Min(labelRatio, 9), 1.1);
                labelWidth = ptOurLocation.X + (int) ((double)size.Width / labelRatio);
            }
            
            int oldWidth = labelWidth;

            
            bool adjustWidth = SetScrollbarLength();
            GridEntryCollection rgipesAll = GetAllGridEntries();
            if (rgipesAll != null) {
                int scroll = GetScrollOffset();
                if ((scroll + visibleRows) >= rgipesAll.Count) {
                    visibleRows = rgipesAll.Count - scroll;
                }
            }
            
            
            if (adjustWidth && size.Width >= 0) {
                labelRatio = ((double) GetOurSize().Width / (double) (oldWidth - ptOurLocation.X));
                //labelWidth = loc.X + (int) ((double)size.Width / labelRatio);
            }

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tsize       :" + size.ToString());
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tlocation   :" + ptOurLocation.ToString());
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tvisibleRows:" + (visibleRows).ToString());
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tlabelWidth :" + (labelWidth).ToString());
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tlabelRatio :" + (labelRatio).ToString());
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\trowHeight  :" + (RowHeight).ToString());
#if DEBUG
            if (rgipesAll == null) {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tIPE Count  :(null)");
            }
            else {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "\tIPE Count  :" + (rgipesAll.Count).ToString());
            }
#endif
        }

        private void SetCommitError(short error) {
            SetCommitError(error, error == ERROR_THROWN);
        }

        private void SetCommitError(short error, bool capture) {
        #if DEBUG
            if (CompModSwitches.DebugGridView.TraceVerbose) {
                string err = "UNKNOWN!";
                switch (error) {
                    case ERROR_NONE:
                        err = "ERROR_NONE";
                        break;
                    case ERROR_THROWN:
                        err = "ERROR_THROWN";
                        break;
                    case ERROR_MSGBOX_UP:
                        err = "ERROR_MSGBOX_UP";
                        break;
                }
                Debug.WriteLine( "PropertyGridView:SetCommitError(error=" + err + ", capture=" + capture.ToString() + ")");
            }
        #endif
            errorState = error;
            if (error != ERROR_NONE) {
                CancelSplitterMove();
            }
            
            Edit.HookMouseDown = capture;
            
        }

        internal /*public virtual*/ void SetExpand(GridEntry gridEntry, bool value) {
            if (gridEntry.Expandable) {

                int row = GetRowFromGridEntry(gridEntry);
                int countFromEnd = visibleRows - row;
                int curRow = selectedRow;

                // if the currently selected row is below us, we need to commit now
                // or the offsets will be wrong
                if (selectedRow != -1 && row < selectedRow && Edit.Visible) {
                    // this will cause the commit
                    FocusInternal();

                }

                int offset = GetScrollOffset();
                int items = totalProps;

                gridEntry.InternalExpanded = value;
                RecalculateProps();
                GridEntry ipeSelect = selectedGridEntry;
                if (!value) {
                    for (GridEntry ipeCur = ipeSelect; ipeCur != null; ipeCur = ipeCur.ParentGridEntry) {
                        if (ipeCur.Equals(gridEntry)) {
                            ipeSelect = gridEntry;
                        }
                    }
                }
                row = GetRowFromGridEntry(gridEntry);

                SetConstants();

                int newItems = totalProps - items;

                if (value && newItems > 0 && newItems < visibleRows && (row + (newItems)) >= visibleRows && newItems < curRow) {
                    // scroll to show the newly opened items.
                    SetScrollOffset((totalProps - items) + offset);
                }

                Invalidate();

                SelectGridEntry(ipeSelect,false);

                int scroll = GetScrollOffset();
                SetScrollOffset(0);
                SetConstants();
                SetScrollOffset(scroll);
            }
        }

        private void SetFlag(short flag, bool value) {
            if (value) {
                flags |= flag;
            }
            else {
                flags &= (short)~flag;
            }
        }

        public virtual void SetScrollOffset(int cOffset) {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:SetScrollOffset(" + cOffset.ToString() + ")");
            int posNew = Math.Max(0, Math.Min(totalProps - visibleRows + 1, cOffset));
            int posOld = ScrollBar.Value;
            if (posNew != posOld && IsScrollValueValid(posNew) && visibleRows > 0) {
                ScrollBar.Value = posNew;
                Invalidate(); 
                selectedRow = GetRowFromGridEntry(selectedGridEntry);
            }
        }

        // C#r
        internal virtual bool _Commit() {
            return Commit();
        }

        private bool Commit() {

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:Commit()");

            if (errorState == ERROR_MSGBOX_UP) {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:Commit() returning false because an error has been thrown or we are in a property set");
                return false;
            }

            if (!this.NeedsCommit) {
                SetCommitError(ERROR_NONE);
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:Commit() returning true because no change has been made");
                return true;
            }

            if (GetInPropertySet()) {
                return false;
            }

            GridEntry ipeCur = GetGridEntryFromRow(selectedRow);
            if (ipeCur == null) {
                return true;
            }
            bool success = false;
            try {
                success = CommitText(Edit.Text);
            }
            finally { 
    
                if (!success) {
                    Edit.FocusInternal();
                    SelectEdit(false);
                }
                else {
                    SetCommitError(ERROR_NONE);   
                }
            }
            return success;
        }

        private bool CommitValue(object value) {

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CommitValue(" + (value==null ? "null" :value.ToString()) + ")");

            GridEntry ipeCur = selectedGridEntry;

            if (selectedGridEntry == null && selectedRow != -1) {
                ipeCur = GetGridEntryFromRow(selectedRow);
            }

            if (ipeCur == null) {
                Debug.Fail("Committing with no selected row!");
                return true;
            }

            int propCount = ipeCur.ChildCount;
            bool capture = Edit.HookMouseDown;
            object originalValue = null;

            try {
                originalValue = ipeCur.PropertyValue;
            }
            catch {
                // if the getter is failing, we still want to let
                // the set happen.
            }

            try {
                try {
                    SetFlag(FlagInPropertySet, true);

                    //if this propentry is enumerable, then once a value is selected from the editor,
                    //we'll want to close the drop down (like true/false).  Otherwise, if we're
                    //working with Anchor for ex., then we should be able to select different values
                    //from the editor, without having it close every time.
                    if (ipeCur != null &&
                        ipeCur.Enumerable) {
                           CloseDropDown();
                    }

                    try {
                        Edit.DisableMouseHook = true;
                        ipeCur.PropertyValue = value;
                    }
                    finally {
                        Edit.DisableMouseHook = false;
                        Edit.HookMouseDown = capture;
                    }
                }
                catch (Exception ex) {
                    SetCommitError(ERROR_THROWN);
                    ShowInvalidMessage(ipeCur.PropertyLabel, value, ex);
                    return false;
                }
            }
            finally {
                SetFlag(FlagInPropertySet, false);
            }

            SetCommitError(ERROR_NONE);

            string text = ipeCur.GetPropertyTextValue();
            if (!String.Equals(text, Edit.Text)) {
                Edit.Text = text;
                Edit.SelectionStart = 0;
                Edit.SelectionLength = 0;
            }
            originalTextValue = text;

            // Update our reset command.
            //
            UpdateResetCommand(ipeCur);

            if (ipeCur.ChildCount != propCount) {
                ClearGridEntryEvents(allGridEntries, 0, -1);
                allGridEntries = null;
                SelectGridEntry(ipeCur, true);
            }

            this.ownerGrid.OnPropertyValueSet(ipeCur, originalValue);

            return true;
        }

        private bool CommitText(string text) {

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:CommitValue(" + (text==null ? "null" :text.ToString()) + ")");

            object value = null;

            GridEntry ipeCur = selectedGridEntry;

            if (selectedGridEntry == null && selectedRow != -1) {
                ipeCur = GetGridEntryFromRow(selectedRow);
            }

            if (ipeCur == null) {
                Debug.Fail("Committing with no selected row!");
                return true;
            }

            try {
                value = ipeCur.ConvertTextToValue(text);
            }
            catch (Exception ex) {
                SetCommitError(ERROR_THROWN);
                ShowInvalidMessage(ipeCur.PropertyLabel, text, ex);
                return false;
            }

            SetCommitError(ERROR_NONE);

            return CommitValue(value);
        }
        
        internal void ReverseFocus() {
            if (selectedGridEntry == null) {
               FocusInternal();
            }
            else {
               SelectGridEntry(selectedGridEntry, true);
               
               if (DialogButton.Visible) {
                  DialogButton.FocusInternal();
               }
               else if (DropDownButton.Visible) {
                  DropDownButton.FocusInternal();
               }
               else if (Edit.Visible) {
                  Edit.SelectAll();
                  Edit.FocusInternal();
               }
            }
        }

        private bool SetScrollbarLength() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:SetScrollBarLength");
            bool sbChange = false;
            if (totalProps != -1) {
                if (totalProps < visibleRows) {
                    SetScrollOffset(0);
                }
                else if (GetScrollOffset() > totalProps) {
                    SetScrollOffset((totalProps+1) - visibleRows);
                }

                bool fHidden = !ScrollBar.Visible;
                if (visibleRows > 0) {
                    ScrollBar.LargeChange = visibleRows-1;
                }
                ScrollBar.Maximum = Math.Max(0,totalProps - 1);
                if (fHidden != (totalProps < visibleRows)) {
                    sbChange = true;
                    ScrollBar.Visible = fHidden;
                    Size size = GetOurSize();
                    if (labelWidth != -1 && size.Width > 0) {
                        if (labelWidth > ptOurLocation.X + size.Width) {
                            labelWidth = ptOurLocation.X + (int) ((double)size.Width / labelRatio);
                        }
                        else {
                            labelRatio = ((double) GetOurSize().Width / (double) (labelWidth - ptOurLocation.X));
                        }
                    }
                    Invalidate();
                }
            }
            return sbChange;
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.ShowDialog"]/*' />
        /// <devdoc>
        ///      Shows the given dialog, and returns its dialog result.  You should always
        ///      use this method rather than showing the dialog directly, as this will
        ///      properly position the dialog and provide it a dialog owner.
        /// </devdoc>
        public DialogResult /* IWindowsFormsEditorService. */ ShowDialog(Form dialog) {
            // try to shift down if sitting right on top of existing owner.
            if (dialog.StartPosition == FormStartPosition.CenterScreen) {
                Control topControl = this;
                if (topControl != null) {
                    while (topControl.ParentInternal != null) {
                        topControl = topControl.ParentInternal;
                    }
                    if (topControl.Size.Equals(dialog.Size)) {
                        dialog.StartPosition = FormStartPosition.Manual;
                        Point location = topControl.Location;
                        // CONSIDER what constant to get here?
                        location.Offset(25, 25);
                        dialog.Location = location;
                    }
                }
            }

            IntPtr priorFocus = UnsafeNativeMethods.GetFocus();

            IUIService service = (IUIService)GetService(typeof(IUIService));
            DialogResult result;
            if (service != null) {
                result = service.ShowDialog(dialog);
            }
            else {
                result = dialog.ShowDialog(this);
            }

            if (priorFocus != IntPtr.Zero) {
                UnsafeNativeMethods.SetFocus(new HandleRef(null, priorFocus));
            }

            return result;
        }

        private void ShowInvalidMessage(string propName, object value, Exception ex) {

            if (value == null) {
                value = "(null)";
            }

            if (propName == null) {
                propName = "(unknown)";
            }

            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:ShowInvalidMessage(prop=" + propName + ")");
            // we have to uninstall our hook so the user can push the button!
            bool hooked = Edit.HookMouseDown;
            Edit.DisableMouseHook = true;
            SetCommitError(ERROR_MSGBOX_UP, false);

            // These things are just plain useless.
            //
            if (ex is System.Reflection.TargetInvocationException) {
                ex = ex.InnerException;
            }

            // Try to find an exception message to display
            //
            string exMessage = ex.Message;

            bool revert = false;

            while (exMessage == null || exMessage.Length == 0) {
                ex = ex.InnerException;
                if (ex == null) {
                    break;
                }
                exMessage = ex.Message;
            }
            
            IUIService uiSvc = (IUIService)GetService(typeof(IUIService));

            ErrorDialog.Message = SR.GetString(SR.PBRSErrorInvalidPropertyValue);
            ErrorDialog.Text = SR.GetString(SR.PBRSErrorTitle);
            ErrorDialog.Details = exMessage;
            

            if (uiSvc != null) {
                revert = (DialogResult.Cancel == uiSvc.ShowDialog(ErrorDialog));
            }
            else {
                revert = (DialogResult.Cancel == this.ShowDialog(ErrorDialog));
            }
            
            Edit.DisableMouseHook = false;

            if (hooked) {
                SelectGridEntry(selectedGridEntry, true);
            }
            SetCommitError(ERROR_THROWN, hooked);

            if (revert) {
                OnEscape(Edit);
            }
        }

        private bool SplitterInside(int x, int y) {
            return(Math.Abs(x - InternalLabelWidth) < 4);
        }

        private void TabSelection() {
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null)
                return;

            if (Edit.Visible) {
                Edit.FocusInternal();
                SelectEdit(false);
            }
            else if (dropDownHolder != null && dropDownHolder.Visible) {
                dropDownHolder.FocusComponent();
                return;
            }
            else if (currentEditor != null) {
                currentEditor.FocusInternal();
            }

            return;
        }
        
        internal void RefreshHelpAttributes() {
            UpdateHelpAttributes(selectedGridEntry, null);
        }


        private void UpdateHelpAttributes(GridEntry oldEntry, GridEntry newEntry) {
            // Update the help context with the current property
            //
            IHelpService hsvc = GetHelpService();
            if (hsvc == null || oldEntry == newEntry) {
                return;
            }

            GridEntry temp = oldEntry;
            if (oldEntry != null && !oldEntry.Disposed) {
                
    
                while (temp != null) {
                    hsvc.RemoveContextAttribute("Keyword", temp.HelpKeyword);
                    temp = temp.ParentGridEntry;
                }
            }

            if (newEntry != null) {
                temp = newEntry;
            
                UpdateHelpAttributes(hsvc, temp, true);
            }
        }
        
        private void UpdateHelpAttributes(IHelpService helpSvc, GridEntry entry, bool addAsF1) {
            if (entry == null) {
               return;
            }
            
            UpdateHelpAttributes(helpSvc, entry.ParentGridEntry, false);
            string helpKeyword = entry.HelpKeyword;
            if (helpKeyword != null) {
               helpSvc.AddContextAttribute("Keyword", helpKeyword, addAsF1 ? HelpKeywordType.F1Keyword : HelpKeywordType.GeneralKeyword);
            }
        }


        private void UpdateUIBasedOnFont(bool layoutRequired) {
            if (IsHandleCreated && GetFlag(FlagNeedUpdateUIBasedOnFont)) {
                try {
                    if (listBox != null) {
                        DropDownListBox.ItemHeight = RowHeight + 2;
                    }
                    if (btnDropDown != null) {
                        btnDropDown.Size = new Size(SystemInformation.VerticalScrollBarArrowHeight, RowHeight);
                        if (btnDialog != null) {
                            DialogButton.Size = DropDownButton.Size;
                        }
                    }
                    
                    if (layoutRequired) {
                        LayoutWindow(true);
                    }
                }
                finally {
                    SetFlag(FlagNeedUpdateUIBasedOnFont, false);
                }
            }
        }

        private bool UnfocusSelection() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:UnfocusSelection()");
            GridEntry gridEntry = GetGridEntryFromRow(selectedRow);
            if (gridEntry == null)
                return true;

            bool commit = Commit();
            
            if (commit && this.FocusInside) {
                FocusInternal();
            }
            return commit;
        }

        private void UpdateResetCommand(GridEntry gridEntry) {
            IMenuCommandService mcs = (IMenuCommandService)GetService(typeof(IMenuCommandService));
            if (mcs != null) {
                MenuCommand reset = mcs.FindCommand(PropertyGridCommands.Reset);
                if (reset != null) {
                    reset.Enabled = gridEntry == null ? false : gridEntry.CanResetPropertyValue();
                }
            }
        }


        public void ValueChanged() {
            Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "PropertyGridView:ValueChanged");

            // We do a lot of work to fire OnComponentChanged ourselves. If the consume of
            // IValueAccess calls ValueChanged then we need to ensure that OnComponentChanged
            // gets fired. To do this we first have to get the Site of the selected component
            // so that we can GetService and get at the IDesignerHost and IComponentChanged.
            //
            // As a fall back, we will just call commit value if we can't find everything
            // we need.
            //

            bool didCommit = false;

            IDesignerHost host = null;
            IComponentChangeService changeSvc = null;

            GridEntry gridEntry = selectedGridEntry;
            while (gridEntry.ParentGridEntry != null) {
                gridEntry = gridEntry.ParentGridEntry;
            }

            if (gridEntry != null) {
                object[] values = null;
                object single = gridEntry.PropertyValue;
                if (single is Array) {
                    Array arr = (Array)single;
                    values = new object[arr.Length];
                    for (int i=0; i<values.Length; i++) {
                        values[i] = arr.GetValue(i);
                    }
                }
                else {
                    values = new object[] {single};
                }


                for (int i=0; i<values.Length; i++) {
                    if (values[i] is IComponent) {
                        host = (IDesignerHost)((IComponent)values[i]).Site.GetService(typeof(IDesignerHost));
                        changeSvc = (IComponentChangeService)((IComponent)values[i]).Site.GetService(typeof(IComponentChangeService));
                        break;
                    }
                }

                if (host != null && changeSvc != null) {
                    DesignerTransaction trans = host.CreateTransaction();
                    try {
                        CommitValue(Value);
                        didCommit = true;

                        for (int i=0; i<values.Length; i++) {
                            changeSvc.OnComponentChanged(values[i], null, null, null);
                        }
                    }
                    finally {
                        trans.Commit();
                    }
                }

            }

            if (!didCommit) {
                CommitValue(Value);
            }
        }

        // a mini version of process dialog key
        // for responding to WM_GETDLGCODE
        internal bool WantsTab(bool forward) {
            if (forward) {
                if (this.Focused) {
                    // we want a tab if the grid has focus and
                    // we have a button or an Edit
                    if (DropDownButton.Visible ||
                        DialogButton.Visible ||
                        Edit.Visible) {
                        return true;
                    }
                }
                else if (Edit.Focused && (DropDownButton.Visible || DialogButton.Visible)) {
                    // if the Edit has focus, and we have a button, we want the tab as well
                    return true;
                }
                return ownerGrid.WantsTab(forward);
            }
            else {
                if (Edit.Focused || DropDownButton.Focused || DialogButton.Focused) {
                    return true;
                }
                return ownerGrid.WantsTab(forward);
            }
        }
        
        private unsafe bool WmNotify(ref Message m) {
            if (m.LParam != IntPtr.Zero) {
                NativeMethods.NMHDR* nmhdr = (NativeMethods.NMHDR*)m.LParam;
            
                if (nmhdr->hwndFrom == ToolTip.Handle) {
                    switch (nmhdr->code) {
                        case NativeMethods.TTN_POP:
                            break;
                        case NativeMethods.TTN_SHOW:
                            // we want to move the tooltip over where our text would be
                            Point mouseLoc = Cursor.Position;
    
                            // convert to window coords
                            mouseLoc = this.PointToClientInternal(mouseLoc);
    
                            // figure out where we are and apply the offset
                            mouseLoc = FindPosition(mouseLoc.X, mouseLoc.Y);
    
                            if (mouseLoc == InvalidPosition) {
                                break;
                            }
    
                            GridEntry curEntry = GetGridEntryFromRow(mouseLoc.Y);
    
                            if (curEntry == null) {
                                break;
                            }
    
                            // get the proper rectangle
                            Rectangle itemRect = GetRectangle(mouseLoc.Y, mouseLoc.X);
                            Point     tipPt    = Point.Empty;
    
                            // and if we need a tooltip, move the tooltip control to that point.
                            if (mouseLoc.X == ROWLABEL) {
                                tipPt = curEntry.GetLabelToolTipLocation(mouseLoc.X - itemRect.X, mouseLoc.Y - itemRect.Y);
                            }
                            else if (mouseLoc.X == ROWVALUE) {
                                tipPt = curEntry.ValueToolTipLocation;
                            }
                            else {
                                break;
                            }
    
                            if (tipPt != InvalidPoint) {
                                itemRect.Offset(tipPt);
                                PositionTooltip(this, ToolTip, itemRect);
                                m.Result = (IntPtr)1;
                                return true;
                            }
    
                            break;
                    }
                }
            }
            return false;
        }
        
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                
                case NativeMethods.WM_SYSCOLORCHANGE:
                    Invalidate();
                    break;

                    // SBurke -- if we get focus in the error
                    // state, make sure we push it back to the
                    // Edit or bad bad things can happen with
                    // our state...
                    //
                case NativeMethods.WM_SETFOCUS:
                    if (!GetInPropertySet() && Edit.Visible && (errorState != ERROR_NONE || !Commit())) {
                        base.WndProc(ref m);
                        Edit.FocusInternal();
                        return;
                    }
                    break;
                    
                case NativeMethods.WM_IME_STARTCOMPOSITION:
                    Edit.FocusInternal();
                    Edit.Clear();
                    UnsafeNativeMethods.PostMessage(new HandleRef(Edit, Edit.Handle), NativeMethods.WM_IME_STARTCOMPOSITION, 0, 0); 
                    return;
                    
                case NativeMethods.WM_IME_COMPOSITION:
                    Edit.FocusInternal();
                    UnsafeNativeMethods.PostMessage(new HandleRef(Edit, Edit.Handle), NativeMethods.WM_IME_COMPOSITION, m.WParam, m.LParam);
                    return;
                    
            case NativeMethods.WM_GETDLGCODE:

                    int flags = NativeMethods.DLGC_WANTCHARS |  NativeMethods.DLGC_WANTARROWS;
                    

                    if (selectedGridEntry != null) {
                        if ((ModifierKeys & Keys.Shift) == 0) {
                            // if we're going backwards, we don't want the tab.
                            // otherwise, we only want it if we have an edit...
                            //
                            if (edit.Visible) {
                                flags |= NativeMethods.DLGC_WANTTAB;
                            }
                        }
                    }
                    m.Result = (IntPtr)(flags);
                    return;
                    
                case NativeMethods.WM_MOUSEMOVE: 
                    
                    // check if it's the same position, of so eat the message
                    if ((int)m.LParam == lastMouseMove) {
                        return;
                    }
                    lastMouseMove = (int)m.LParam;
                    break;

                case NativeMethods.WM_NOTIFY:
                    if (WmNotify(ref m))
                        return;
                    break;
                case AutomationMessages.PGM_GETSELECTEDROW:
                    m.Result = (IntPtr)GetRowFromGridEntry(selectedGridEntry);
                    return;
                case AutomationMessages.PGM_GETVISIBLEROWCOUNT:
                    m.Result = (IntPtr)Math.Min(visibleRows, totalProps);
                    return;
            }

            base.WndProc(ref m);
        }

        private class DropDownHolder : Form, IMouseHookClient {

            internal Control currentControl = null;
            internal const int BORDER = 1;
            internal PropertyGridView gridView;
            private  MouseHooker mouseHooker;
            
            internal DropDownHolder(PropertyGridView psheet)
            : base() {
               this.ShowInTaskbar = false;
               this.ControlBox = false;
               this.MinimizeBox = false;
               this.MaximizeBox = false;
               this.Text = "";
               this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
               mouseHooker = new MouseHooker(this, this, psheet);
               Visible = false;
               gridView = psheet;
               Font = psheet.Font;
               this.BackColor = gridView.BackColor;
            }

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                    cp.Style |= NativeMethods.WS_POPUP | NativeMethods.WS_BORDER;
                    if (gridView != null) {
                        cp.Parent = gridView.ParentInternal.Handle;
                    }
                    return cp;
                }
            }
            
             public bool DisableMouseHook {
                get {
                    return mouseHooker.DisableMouseHook;
                }
                set {
                    mouseHooker.DisableMouseHook = value;
                }
            }

            public virtual bool HookMouseDown{
                get{
                    return mouseHooker.HookMouseDown;
                }
                set{
                    mouseHooker.HookMouseDown = value;
                }
            }
            
            
            protected override void DestroyHandle() {
                  mouseHooker.HookMouseDown = false;
                  base.DestroyHandle();
            }
            
            public void DoModalLoop() {
               
                  // Push a modal loop.  This kind of stinks, but I think it is a
                  // better user model than returning from DropDownControl immediately.
                  //  
                  while (this.Visible) { 
                      Application.DoEvents();
                      UnsafeNativeMethods.MsgWaitForMultipleObjects(1, 0, true, 250, NativeMethods.QS_ALLINPUT);
                  }
            }
            
            public virtual Control Component {
                get {
                    return currentControl;
                }
            }

            public virtual bool GetUsed() {
                return(currentControl != null);
            }
            
            protected override void OnMouseDown(MouseEventArgs me) {
               if (me.Button == MouseButtons.Left) {
                  gridView.CloseDropDown();
               }
               base.OnMouseDown(me);
            }

            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
                if (currentControl != null) {
                    currentControl.SetBounds(0, 0, width - 2 * BORDER, height - 2 * BORDER);
                    width = currentControl.Width;
                    height = currentControl.Height;
                    if (height == 0 && currentControl is ListBox) {
                        height = ((ListBox)currentControl).ItemHeight;
                        currentControl.Height = height;
                    }
                    width += 2 * BORDER;
                    height += 2 * BORDER;
                }
                base.SetBoundsCore(x, y, width, height, specified);
            }

            public virtual void FocusComponent() {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:FocusComponent()");
                if (currentControl != null && Visible) {
                    currentControl.FocusInternal();
                }
            }
            
            public bool OnClickHooked() {
                  gridView.CloseDropDown();
                  return false;
            }
            
            private void OnCurrentControlResize(object o, EventArgs e) {
                if (currentControl != null) {
                    int oldWidth = this.Width;
                    this.Size = new Size(2 * BORDER + currentControl.Width, 2 * BORDER + currentControl.Height);
                    this.Left -= (this.Width - oldWidth);
                }
            }
          
            protected override bool ProcessDialogKey(Keys keyData) {
                if ((keyData & (Keys.Shift | Keys.Control | Keys.Alt)) == 0) {
                    switch (keyData & Keys.KeyCode) {
                        case Keys.Escape:
                            gridView.OnEscape(this);
                            return true;
                        case Keys.F4:
                            gridView.F4Selection(true);
                            return true;
                        case Keys.Return:
                            // make sure the return gets forwarded to the control that
                            // is being displayed
                            if (gridView.UnfocusSelection()) {
                              gridView.SelectedGridEntry.OnValueReturnKey();
                            }
                            return true;
                    }
                }

                return base.ProcessDialogKey(keyData);
            }

            public virtual void SetComponent(Control ctl) {

                if (currentControl != null) {
                    Controls.Remove(currentControl);
                    currentControl = null;
                }
                if (ctl != null) {
                    Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:SetComponent(" + (ctl.GetType().Name) + ")");
                    Controls.Add(ctl);
                    Size = new Size(2 * BORDER + ctl.Width, 2 * BORDER + ctl.Height);
                    ctl.Location = new Point(0, 0);
                    ctl.Visible = true;
                    currentControl = ctl;
                    currentControl.Resize += new EventHandler(this.OnCurrentControlResize);
                }
                Enabled = currentControl != null;
            }

            protected override void WndProc(ref Message m) {

                if (m.Msg == NativeMethods.WM_ACTIVATE) {
                    SetState(STATE_MODAL, true);
                    Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:WM_ACTIVATE()");
                    Control activatedControl = Control.FromHandleInternal(m.LParam);
                    if (Visible && NativeMethods.Util.LOWORD(m.WParam) == NativeMethods.WA_INACTIVE && (activatedControl == null || !this.Contains(activatedControl))) {
                        gridView.CloseDropDown();
                        return;
                    }
                    
                    // prevent the IMsoComponentManager active code from getting fired.
                    //Active = ((int)m.WParam & 0x0000FFFF) != NativeMethods.WA_INACTIVE;
                    //return;
                }
                else if (m.Msg == NativeMethods.WM_CLOSE) {
                    // don't let an ALT-F4 get you down
                    //
                    if (Visible) {
                        gridView.CloseDropDown();
                    }
                    return;
                }

                base.WndProc(ref m);
            }
        }

        private class GridViewListBox : ListBox {

            internal bool fInSetSelectedIndex = false;
            
            public GridViewListBox(PropertyGridView gridView) {
                base.IntegralHeight = false;
            }

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    cp.Style &= ~NativeMethods.WS_BORDER;
                    cp.ExStyle &= ~NativeMethods.WS_EX_CLIENTEDGE;
                    return cp;
                }
            }

            public virtual bool InSetSelectedIndex() {
                return fInSetSelectedIndex;
            }

            protected override void OnSelectedIndexChanged(EventArgs e) {
                fInSetSelectedIndex = true;
                base.OnSelectedIndexChanged(e);
                fInSetSelectedIndex = false;
            }

        }

        private class GridViewEdit : TextBox , IMouseHookClient {

            internal bool fInSetText = false;
            internal bool filter = false;
            internal PropertyGridView psheet = null;
            private  bool dontFocusMe = false;
            private int   lastMove;
            
            private MouseHooker mouseHooker;
            
            // We do this becuase the Focus call above doesn't always stick, so
            // we make the Edit think that it doesn't have focus.  this prevents
            // ActiveControl code on the containercontrol from moving focus elsewhere
            // when the dropdown closes.
            public bool DontFocus {
               set {
                  dontFocusMe = value;
               }
            }
                        
            public virtual bool Filter {
                get { return filter;}

                set {
                    this.filter = value;
                }
            }
            
            public override bool Focused {
                get {
                    if (dontFocusMe) {
                        return false;
                    }
                    return base.Focused;
                }
            }


            public override string Text {
                get {
                    return base.Text;
                }
                set {
                    fInSetText = true;
                    base.Text = value;
                    fInSetText = false;
                }
            }
            
            public bool DisableMouseHook {
                get {
                    return mouseHooker.DisableMouseHook;
                }
                set {
                    mouseHooker.DisableMouseHook = value;
                }
            }


            public virtual bool HookMouseDown{
                get{
                    return mouseHooker.HookMouseDown;
                }
                set{
                    mouseHooker.HookMouseDown = value;
                    if (value) {
                        this.FocusInternal();
                    }
                }
            }


            public GridViewEdit(PropertyGridView psheet) {
                this.psheet = psheet;
                mouseHooker = new MouseHooker(this, this, psheet);
            }
        
            protected override void DestroyHandle() {
                  mouseHooker.HookMouseDown = false;
                  base.DestroyHandle();
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    mouseHooker.Dispose();
                }
                base.Dispose(disposing);
            }

            public void FilterKeyPress(char keyChar) {
            
                if (IsInputChar(keyChar)) {
                    this.FocusInternal();
                    this.SelectAll();
                    UnsafeNativeMethods.PostMessage(new HandleRef(this, Handle), NativeMethods.WM_CHAR, (IntPtr)keyChar, IntPtr.Zero);
                }
            }
    
    

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.GridViewEdit.IsInputKey"]/*' />
            /// <devdoc>
            ///     Overridden to handle TAB key.
            /// </devdoc>
            protected override bool IsInputKey(Keys keyData) {
                switch (keyData & Keys.KeyCode) {
                    case Keys.Escape:
                    case Keys.Tab:
                    case Keys.F4:
                    case Keys.F1:
                    case Keys.Return:
                        return false;
                }
                if (psheet.NeedsCommit) {
                    return false;
                }
                return base.IsInputKey(keyData);
            }

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.GridViewEdit.IsInputChar"]/*' />
            /// <devdoc>
            ///     Overridden to handle TAB key.
            /// </devdoc>
            protected override bool IsInputChar(char keyChar) {
                switch ((Keys)(int)keyChar) {
                    case Keys.Tab:
                    case Keys.Return:
                        return false;
                }
                return base.IsInputChar(keyChar);
            }

            /*protected override void OnLostFocus(EventArgs e){
                base.OnLostFocus(e);
                Debug.Assert(!this.HookMouseDown || !this.Focused, "Bad mojo! Hook active after focus was lost");
            }*/

            protected override void OnKeyDown(KeyEventArgs ke) {

                // this is because on a dialog we may
                // not get a chance to pre-process
                //
                if (ProcessDialogKey(ke.KeyData)) {
                    ke.Handled = true;
                    return;
                }

                base.OnKeyDown(ke);
            }

            protected override void OnKeyPress(KeyPressEventArgs ke) {
                if (!IsInputChar(ke.KeyChar)) {
                    ke.Handled = true;
                    return;
                }
                base.OnKeyPress(ke);
            }
            
            
            public bool OnClickHooked() {
                 // can we commit this value?
                 // eat the value if we failed to commit.
                 return !psheet._Commit();
            }
            
            protected override void OnMouseEnter(EventArgs e) {
               base.OnMouseEnter(e);
               
               
               if (!this.Focused) {
                  Graphics g = CreateGraphics();
                  if (ClientRectangle.Width <= psheet.SelectedGridEntry.GetValueTextWidth(this.Text, g, this.Font)) {
                        psheet.ToolTip.ToolTip = this.Text;
                  }
                  g.Dispose();
               }
               
            }
            
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            
                 // make sure we allow the Edit to handle ctrl-z
                 switch (keyData & Keys.KeyCode) {
                     case Keys.Z:
                     case Keys.C:
                     case Keys.X:
                     case Keys.V:
                        if(
                           ((keyData & Keys.Control) != 0) && 
                           ((keyData & Keys.Shift) == 0) &&
                           ((keyData & Keys.Alt) == 0)) {
                           return false;
                        }
                        break;
                        
                     case Keys.A:
                        if(
                           ((keyData & Keys.Control) != 0) && 
                           ((keyData & Keys.Shift) == 0) &&
                           ((keyData & Keys.Alt) == 0)) {
                           SelectAll();
                           return true;
                        }
                        
                        break;
                        
                     case Keys.Insert:
                         if (((keyData & Keys.Alt) == 0)) {
                             if (((keyData & Keys.Control) != 0) ^ ((keyData & Keys.Shift) == 0)) {
                                 return false;
                             }
                         }
                         break;
                         
                     case Keys.Delete:
                         if(
                           ((keyData & Keys.Control) == 0) && 
                           ((keyData & Keys.Shift) != 0) &&
                           ((keyData & Keys.Alt) == 0)) {
                           return false;
                         }
                         else if(
                               ((keyData & Keys.Control) == 0) && 
                               ((keyData & Keys.Shift) == 0) &&
                               ((keyData & Keys.Alt) == 0)
                                 )
                         {
                             // if this is just the delete key and we're on a non-text editable property that is resettable,
                             // reset it now.
                             //
                             if (psheet.SelectedGridEntry != null && !psheet.SelectedGridEntry.Enumerable && !psheet.SelectedGridEntry.IsTextEditable && psheet.SelectedGridEntry.CanResetPropertyValue()) {
                                 object oldValue = psheet.SelectedGridEntry.PropertyValue;
                                 psheet.SelectedGridEntry.ResetPropertyValue();
                                 psheet.UnfocusSelection();
                                 psheet.ownerGrid.OnPropertyValueSet(psheet.SelectedGridEntry, oldValue);
                             }
                         }
                         break;
                 }
                 return base.ProcessCmdKey(ref msg, keyData);
            }


            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.GridViewEdit.ProcessDialogKey"]/*' />
            /// <devdoc>
            ///      Overrides Control.ProcessDialogKey to handle the Escape and Return
            ///      keys.
            /// </devdoc>
            /// <internalonly/>
            protected override bool ProcessDialogKey(Keys keyData) {

                // We don't do anything with modified keys here.
                //
                if ((keyData & (Keys.Shift | Keys.Control | Keys.Alt)) == 0) {
                    switch (keyData & Keys.KeyCode) {
                        case Keys.Return: 
                            bool fwdReturn = !psheet.NeedsCommit;
                            if (psheet.UnfocusSelection() && fwdReturn) {
                              psheet.SelectedGridEntry.OnValueReturnKey();
                            }
                            return true;
                        case Keys.Escape:
                            psheet.OnEscape(this);
                            return true;
                        case Keys.F4:
                            psheet.F4Selection(true);
                            return true;
                    }
                }
                
                // for the tab key, we want to commit before we allow it to be processed.
                if ((keyData & Keys.KeyCode) == Keys.Tab && ((keyData & (Keys.Control | Keys.Alt)) == 0)) {
                    return !psheet._Commit();
                }

                return base.ProcessDialogKey(keyData);
            }

            protected override void SetVisibleCore(bool value) {
                Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:Visible(" + (value.ToString()) + ")");
                // make sure we dont' have the mouse captured if
                // we're going invisible
                if (value == false && this.HookMouseDown) {
                    mouseHooker.HookMouseDown = false;
                }
                base.SetVisibleCore(value);
            }

            
            // a mini version of process dialog key
            // for responding to WM_GETDLGCODE
            internal bool WantsTab(bool forward) {
                return psheet.WantsTab(forward);
            }
      
            private unsafe bool WmNotify(ref Message m) {
                
                if (m.LParam != IntPtr.Zero) {
                   NativeMethods.NMHDR* nmhdr = (NativeMethods.NMHDR*)m.LParam;
                   
                   if (nmhdr->hwndFrom == psheet.ToolTip.Handle) {
                       switch (nmhdr->code) {
                          case NativeMethods.TTN_SHOW:
                             PropertyGridView.PositionTooltip(this, psheet.ToolTip, ClientRectangle);
                             m.Result = (IntPtr)1;
                             return true;
                          default:
                             psheet.WndProc(ref m);
                             break;
                       }         
                   }
                }
                return false;
            }
            
            protected override void WndProc(ref Message m) {

                if (filter) {
                    if (psheet.FilterEditWndProc(ref m)) {
                        return;
                    }
                }

                switch (m.Msg) {
                    case NativeMethods.WM_STYLECHANGED:
                        if (((int)m.WParam & NativeMethods.GWL_EXSTYLE) != 0) {
                            psheet.Invalidate();
                        }
                        break;
                    case NativeMethods.WM_MOUSEMOVE:
                        if ((int)m.LParam == lastMove) {
                            return;
                        }
                        lastMove = (int)m.LParam;
                        break;
                    case NativeMethods.WM_DESTROY:
                        mouseHooker.HookMouseDown = false;
                        break;
                    case NativeMethods.WM_SHOWWINDOW:
                        if (IntPtr.Zero == m.WParam) {
                            mouseHooker.HookMouseDown = false;
                        }
                        break;
                    case NativeMethods.WM_PASTE:
                        /*if (!this.ReadOnly) {
                            IDataObject dataObject = Clipboard.GetDataObject();
                            Debug.Assert(dataObject != null, "Failed to get dataObject from clipboard");
                            if (dataObject != null) {
                                object data = dataObject.GetData(typeof(string));
                                if (data != null) {
                                    string clipboardText = data.ToString();
                                    SelectedText = clipboardText;
                                    m.result = 1;
                                    return;
                                }
                            }
                        }*/
                        if (this.ReadOnly) {
                           return;
                        }
                        break;
                                                            
                    case NativeMethods.WM_GETDLGCODE:

                        m.Result = (IntPtr)((long)m.Result | NativeMethods.DLGC_WANTARROWS | NativeMethods.DLGC_WANTCHARS);
                        if (psheet.NeedsCommit || this.WantsTab((ModifierKeys & Keys.Shift) == 0)) {
                            m.Result = (IntPtr)((long)m.Result | NativeMethods.DLGC_WANTALLKEYS | NativeMethods.DLGC_WANTTAB);
                        }
                        return;
                        
                    case NativeMethods.WM_NOTIFY:
                        if (WmNotify(ref m))
                            return;
                        break;                                                              
                }
                base.WndProc(ref m);
            }

            public virtual bool InSetText() {
                return fInSetText;
            }

          
        }
        
        internal interface IMouseHookClient {
        
            // return true if the click is handled, false
            // to pass it on
            bool OnClickHooked();        
        }                                             
        
        internal class MouseHooker {
            private PropertyGridView gridView;
            private Control          control;
            private IMouseHookClient client;
            
            internal int        thisProcessID = 0;
            private GCHandle    mouseHookRoot;
            private IntPtr      mouseHookHandle = IntPtr.Zero;
            private bool        hookDisable = false;
            
            private bool processing;      
            
            public MouseHooker(Control control, IMouseHookClient client, PropertyGridView gridView) {
               this.control = control;
               this.gridView = gridView;
               this.client = client;
            }       
            
            
            public bool DisableMouseHook {
                get {
                    return hookDisable;
                }
                set {
                    hookDisable = value;
                    if (value) {
                        UnhookMouse();
                    }
                }
            }


            public virtual bool HookMouseDown{
                get{
                    return mouseHookHandle != IntPtr.Zero;
                }
                set{
                    if (value && !hookDisable) {
                        HookMouse();
                    }
                    else {
                        UnhookMouse();
                    }
                }
            }

            
            public void Dispose() {
               UnhookMouse();
            }  
                    

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.MouseHooker.HookMouse"]/*' />
            /// <devdoc>
            ///     Sets up the needed windows hooks to catch messages.
            /// </devdoc>
            /// <internalonly/>
            private void HookMouse() {
                lock(this) {
                    if (mouseHookHandle != IntPtr.Zero) {
                        return;
                    }
                    
                    if (thisProcessID == 0) {
                        SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(control, control.Handle), out thisProcessID);
                    }
                  
                    
                    NativeMethods.HookProc hook = new NativeMethods.HookProc(new MouseHookObject(this).Callback);
                    mouseHookRoot = GCHandle.Alloc(hook);

                    mouseHookHandle = UnsafeNativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE,
                                                               hook,
                                                               NativeMethods.NullHandleRef,
                                                               SafeNativeMethods.GetCurrentThreadId());
                    Debug.Assert(mouseHookHandle != IntPtr.Zero, "Failed to install mouse hook");
                    Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:HookMouse()");
                }
            }
            
            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.MouseHooker.MouseHookProc"]/*' />
            /// <devdoc>
            ///     HookProc used for catch mouse messages.
            /// </devdoc>
            /// <internalonly/>
            private IntPtr MouseHookProc(int nCode, IntPtr wparam, IntPtr lparam) {
                if (nCode == NativeMethods.HC_ACTION) {
                    NativeMethods.MOUSEHOOKSTRUCT mhs = (NativeMethods.MOUSEHOOKSTRUCT)UnsafeNativeMethods.PtrToStructure(lparam, typeof(NativeMethods.MOUSEHOOKSTRUCT));
                    if (mhs != null) {
                        switch ((int)wparam) {
                            case NativeMethods.WM_LBUTTONDOWN:
                            case NativeMethods.WM_MBUTTONDOWN:
                            case NativeMethods.WM_RBUTTONDOWN:
                            case NativeMethods.WM_NCLBUTTONDOWN:
                            case NativeMethods.WM_NCMBUTTONDOWN:
                            case NativeMethods.WM_NCRBUTTONDOWN:
                            case NativeMethods.WM_MOUSEACTIVATE:
                                if (ProcessMouseDown(mhs.hWnd, mhs.pt_x, mhs.pt_y)) {
                                    return (IntPtr)1;
                                }
                                break;
                        }

                    }
                }

                return UnsafeNativeMethods.CallNextHookEx(new HandleRef(this, mouseHookHandle), nCode, wparam, lparam);
            }
            
            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.MouseHooker.UnhookMouse"]/*' />
            /// <devdoc>
            ///     Removes the windowshook that was installed.
            /// </devdoc>
            /// <internalonly/>
            private void UnhookMouse() {
                lock(this) {
                    if (mouseHookHandle != IntPtr.Zero) {
                        UnsafeNativeMethods.UnhookWindowsHookEx(new HandleRef(this, mouseHookHandle));
                        mouseHookRoot.Free();
                        mouseHookHandle = IntPtr.Zero;
                        Debug.WriteLineIf(CompModSwitches.DebugGridView.TraceVerbose,  "DropDownHolder:UnhookMouse()");
                    }
                }
            }
             /*
            * Here is where we force validation on any clicks outside the
            */
            private bool ProcessMouseDown(IntPtr hWnd, int x, int y) {
            
               
               // com+ 12678
               // if we put up the "invalid" message box, it appears this 
               // method is getting called re-entrantly when it shouldn't be.
               // this prevents us from recursing.
               //
               if (processing) {
                  return false;
               }
               
                IntPtr hWndAtPoint = hWnd;
                IntPtr handle = control.Handle;
                Control ctrlAtPoint = Control.FromHandleInternal(hWndAtPoint);

                // if it's us or one of our children, just process as normal
                //
                if (hWndAtPoint != handle && !control.Contains(ctrlAtPoint)) {
                    Debug.Assert(thisProcessID != 0, "Didn't get our process id!");

                    // make sure the window is in our process
                    int pid;
                    SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(null, hWndAtPoint), out pid);

                    // if this isn't our process, unhook the mouse.
                    if (pid != thisProcessID) {
                        this.HookMouseDown = false;
                        return false;
                    }

                    bool needCommit = false;   

                    // if this a sibling control (e.g. the drop down or buttons), just forward the message and skip the commit
                    needCommit = ctrlAtPoint == null ?  true : !gridView.IsSiblingControl(control, ctrlAtPoint);
                    
                    try {
                       processing = true;
                       
                       if (needCommit && client.OnClickHooked()) {
                              return true;
                          }
                       }
                    finally {
                       processing = false;
                    }

                    // cancel our hook at this point
                    HookMouseDown = false;
                    //gridView.UnfocusSelection();
                }
                return false;
            }
            
              /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.MouseHooker.MouseHookObject"]/*' />
              /// <devdoc>
            ///     Forwards messageHook calls to ToolTip.messageHookProc
            /// </devdoc>
            /// <internalonly/>
            private class MouseHookObject {
                internal WeakReference reference;

                public MouseHookObject(MouseHooker parent) {
                    this.reference = new WeakReference(parent, false);
                }

                public virtual IntPtr Callback(int nCode, IntPtr wparam, IntPtr lparam) {
                    IntPtr ret = IntPtr.Zero;
                    try {
                        MouseHooker control = (MouseHooker)reference.Target;
                        if (control != null) {
                            ret = control.MouseHookProc(nCode, wparam, lparam);
                        }
                    }
                    catch (Exception) {
                        // ignore
                    }
                    return ret;
                }
            }
        }

        /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject"]/*' />
        /// <devdoc>
        ///     The accessible object class for a PropertyGridView. The child accessible objects
        ///     are accessible objects corresponding to the property grid entries.        
        /// </devdoc>
        [System.Runtime.InteropServices.ComVisible(true)]        
        internal class PropertyGridViewAccessibleObject : ControlAccessibleObject {

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.PropertyGridViewAccessibleObject"]/*' />
            /// <devdoc>
            ///     Construct a PropertyGridViewAccessibleObject
            /// </devdoc>
            public PropertyGridViewAccessibleObject(PropertyGridView owner) : base(owner) {
            }
            
            public override string Name {
                get {
                    string name = Owner.AccessibleName;
                    if (name != null) {
                        return name;
                    }
                    else {
                        return SR.GetString(SR.PropertyGridDefaultAccessibleName);
                    }
                }
            }
            
            public override AccessibleRole Role {
                get {
                    AccessibleRole role = Owner.AccessibleRole;
                    if (role != AccessibleRole.Default) {
                        return role;
                    }
                    else {
                        return AccessibleRole.Table;
                    }
                }
            }                                                         

            public AccessibleObject Next(GridEntry current) {
                int row = ((PropertyGridView)Owner).GetRowFromGridEntry(current);
                GridEntry nextEntry = ((PropertyGridView)Owner).GetGridEntryFromRow(++row);
                if (nextEntry != null) {
                    return nextEntry.AccessibilityObject;
                }
                return null;
            }

            public AccessibleObject Previous(GridEntry current) {
                int row = ((PropertyGridView)Owner).GetRowFromGridEntry(current);
                GridEntry prevEntry = ((PropertyGridView)Owner).GetGridEntryFromRow(--row);
                if (prevEntry != null) {
                    return prevEntry.AccessibilityObject;
                }
                return null;
            }

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.GetChild"]/*' />
            /// <devdoc>
            ///      Get the accessible child at the given index.
            ///      The accessible children of a PropertyGridView are accessible objects
            ///      corresponding to the property grid entries.
            /// </devdoc>
            public override AccessibleObject GetChild(int index) {

                GridEntryCollection properties = ((PropertyGridView)Owner).AccessibilityGetGridEntries();
                if (properties != null && index >= 0 && index < properties.Count) {
                    return properties.GetEntry(index).AccessibilityObject;
                }
                else {
                    return null;
                }
            }

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.GetChildCount"]/*' />
            /// <devdoc>
            ///      Get the number of accessible children.
            ///      The accessible children of a PropertyGridView are accessible objects
            ///      corresponding to the property grid entries.
            /// </devdoc>
            public override int GetChildCount() {
                GridEntryCollection properties = ((PropertyGridView)Owner).AccessibilityGetGridEntries();

                if (properties != null) {
                    return properties.Count;
                }
                else {
                    return 0;
                }
            }

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.GetFocused"]/*' />
            /// <devdoc>
            ///      Get the accessible object for the currently focused grid entry.
            /// </devdoc>
            public override AccessibleObject GetFocused() {
            
                GridEntry gridEntry = ((PropertyGridView)Owner).SelectedGridEntry;
                if (gridEntry != null && gridEntry.Focus) {
                    return gridEntry.AccessibilityObject;
                }
                return null;
            }

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.GetSelected"]/*' />
            /// <devdoc>
            ///      Get the accessible object for the currently selected grid entry.
            /// </devdoc>
            public override AccessibleObject GetSelected() {
                GridEntry gridEntry = ((PropertyGridView)Owner).SelectedGridEntry;
                if (gridEntry != null) {
                    return gridEntry.AccessibilityObject;
                }
                return null;
            }



            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.HitTest"]/*' />
            /// <devdoc>
            ///      Get the accessible child at the given screen location.
            ///      The accessible children of a PropertyGridView are accessible objects
            ///      corresponding to the property grid entries.
            /// </devdoc>
            public override AccessibleObject HitTest(int x, int y) {

                // Convert to client coordinates
                //
                NativeMethods.POINT pt = new NativeMethods.POINT(x, y);
                UnsafeNativeMethods.ScreenToClient(new HandleRef(Owner, Owner.Handle), pt);

                // Find the grid entry at the given client coordinates
                //
                Point pos = ((PropertyGridView)Owner).FindPosition(pt.x, pt.y);
                if (pos != PropertyGridView.InvalidPosition) {
                    GridEntry gridEntry = ((PropertyGridView)Owner).GetGridEntryFromRow(pos.Y);
                    if (gridEntry != null) {

                        // Return the accessible object for this grid entry
                        //
                        return gridEntry.AccessibilityObject;
                    }
                }

                // No grid entry at this point
                //
                return null;
            }

            /// <include file='doc\PropertyGridView.uex' path='docs/doc[@for="PropertyGridView.PropertyGridViewAccessibleObject.Navigate"]/*' />
            /// <devdoc>
            ///      Navigate to another object.
            /// </devdoc>
            public override AccessibleObject Navigate(AccessibleNavigation navdir) {

                if (GetChildCount() > 0) {
                    // We're only handling FirstChild and LastChild here
                    switch(navdir) {
                        case AccessibleNavigation.FirstChild:
                            return GetChild(0);
                        case AccessibleNavigation.LastChild:
                            return GetChild(GetChildCount() - 1);
                    }
                }
                return null;    // Perform default behavior
            }
        }

        private class GridPositionData {

            ArrayList expandedState;
            GridEntryCollection selectedItemTree;
            int       itemRow;
            int       itemCount;

            public GridPositionData(PropertyGridView gridView) {
                selectedItemTree = gridView.GetGridEntryHierarchy(gridView.selectedGridEntry);
                expandedState = gridView.SaveHierarchyState(gridView.topLevelGridEntries);
                itemRow = gridView.selectedRow;
                itemCount = gridView.totalProps;
            }

            public GridEntry Restore(PropertyGridView gridView) {
                    gridView.RestoreHierarchyState(expandedState);
                    GridEntry entry = gridView.FindEquivalentGridEntry(selectedItemTree);

                    if (entry != null) {
                        gridView.SelectGridEntry(entry, true);
                        
                        int delta = gridView.selectedRow - itemRow;
                        if (delta != 0 && gridView.ScrollBar.Visible) {
                            if (itemRow < gridView.visibleRows) {
                                delta += gridView.GetScrollOffset();
                                
                                if (delta < 0) {
                                    delta = 0;
                                }
                                else if (delta > gridView.ScrollBar.Maximum) {
                                    delta = gridView.ScrollBar.Maximum - 1;
                                }
                                gridView.SetScrollOffset(delta);
                            }
                            
                        }
                    }
                    return entry;
            }
        }
    }
}

