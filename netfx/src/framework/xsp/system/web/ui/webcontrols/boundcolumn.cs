//------------------------------------------------------------------------------
// <copyright file="BoundColumn.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Web.UI.WebControls {

    using System;
    using System.Collections;    
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Web;
    using System.Web.UI;
    using System.Security.Permissions;

    /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn"]/*' />
    /// <devdoc>
    /// <para>Creates a column bounded to a data field in a <see cref='System.Web.UI.WebControls.DataGrid'/>.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class BoundColumn : DataGridColumn {
        
        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.thisExpr"]/*' />
        /// <devdoc>
        ///    <para>Specifies a string that represents "this". This field is read-only. </para>
        /// </devdoc>
        public static readonly string thisExpr = "!";

        private PropertyDescriptor boundFieldDesc;
        private bool boundFieldDescValid;
        private string boundField;
        private string formatting;

        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.BoundColumn"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of a <see cref='System.Web.UI.WebControls.BoundColumn'/> class.</para>
        /// </devdoc>
        public BoundColumn() {
        }
        
        
        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.DataField"]/*' />
        /// <devdoc>
        ///    <para> Gets or sets the field name from the data model bound to this column.</para>
        /// </devdoc>
        [
            WebCategory("Data"),
            DefaultValue(""),
            WebSysDescription(SR.BoundColumn_DataField)
        ]
        public virtual string DataField {
            get {
                object o = ViewState["DataField"];
                if (o != null)
                    return (string)o;
                return String.Empty;
            }
            set {
                ViewState["DataField"] = value;
                OnColumnChanged();
            }
        }
        
        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.DataFormatString"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the display format of data in this
        ///       column.</para>
        /// </devdoc>
        [
            WebCategory("Behavior"),
            DefaultValue(""),
            WebSysDescription(SR.BoundColumn_DataFormatString)
        ]
        public virtual string DataFormatString {
            get {
                object o = ViewState["DataFormatString"];
                if (o != null)
                    return (string)o;
                return String.Empty;
            }
            set {
                ViewState["DataFormatString"] = value;
                OnColumnChanged();
            }
        }
        
        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.ReadOnly"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the property that prevents modification to data
        ///       in this column.</para>
        /// </devdoc>
        [
            WebCategory("Behavior"),
            DefaultValue(false),
            WebSysDescription(SR.BoundColumn_ReadOnly)
        ]
        public virtual bool ReadOnly {
            get {
                object o = ViewState["ReadOnly"];
                if (o != null)
                    return (bool)o;
                return false;
            }
            set {
                ViewState["ReadOnly"] = value;
                OnColumnChanged();
            }
        }
        
        
        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.FormatDataValue"]/*' />
        /// <devdoc>
        /// </devdoc>
        protected virtual string FormatDataValue(object dataValue) {
            string formattedValue = String.Empty;

            if ((dataValue != null) && (dataValue != System.DBNull.Value)) {
                if (formatting.Length == 0) {
                    formattedValue = dataValue.ToString();
                }
                else {
                    formattedValue = String.Format(formatting, dataValue);
                }
            }

            return formattedValue;
        }

        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.Initialize"]/*' />
        /// <devdoc>
        /// </devdoc>
        public override void Initialize() {
            base.Initialize();

            boundFieldDesc = null;
            boundFieldDescValid = false;

            boundField = DataField;
            formatting = DataFormatString;
        }

        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.InitializeCell"]/*' />
        /// <devdoc>
        /// <para>Initializes a cell in the DataGridColumn.</para>
        /// </devdoc>
        public override void InitializeCell(TableCell cell, int columnIndex, ListItemType itemType) {
            base.InitializeCell(cell, columnIndex, itemType);
            
            Control childControl = null;
            Control boundControl = null;
            
            switch (itemType) {
                case ListItemType.Header:
                case ListItemType.Footer:
                    break;

                case ListItemType.Item:
                case ListItemType.AlternatingItem:
                case ListItemType.SelectedItem:
                    if (DataField.Length != 0) {
                        boundControl = cell;
                    }
                    break;
                    
                case ListItemType.EditItem:
                    if (ReadOnly == true) {
                        goto case ListItemType.Item;
                    }
                    else {
                        // CONSIDER, nikhilko: Use a control map here
                        TextBox editor = new TextBox();
                        childControl = editor;
                        
                        if (boundField.Length != 0) {
                            boundControl = editor;
                        }
                    }
                    break;
            }
            
            if (childControl != null) {
                cell.Controls.Add(childControl);
            }

            if (boundControl != null) {
                boundControl.DataBinding += new EventHandler(this.OnDataBindColumn);
            }
        }

        /// <include file='doc\BoundColumn.uex' path='docs/doc[@for="BoundColumn.OnDataBindColumn"]/*' />
        /// <devdoc>
        /// </devdoc>
        private void OnDataBindColumn(object sender, EventArgs e) {
            Debug.Assert(DataField.Length != 0, "Shouldn't be DataBinding without a DataField");

            Control boundControl = (Control)sender;
            DataGridItem item = (DataGridItem)boundControl.NamingContainer;
            object dataItem = item.DataItem;

            if (boundFieldDescValid == false) {
                if (!boundField.Equals(thisExpr)) {
                    boundFieldDesc = TypeDescriptor.GetProperties(dataItem).Find(boundField, true);
                    if ((boundFieldDesc == null) && !DesignMode) {
                        throw new HttpException(HttpRuntime.FormatResourceString(SR.Field_Not_Found, boundField));
                    }
                }
                boundFieldDescValid = true;
            }
            
            object data = dataItem;
            string dataValue;

            if ((boundFieldDesc == null) && DesignMode) {
                dataValue = SR.GetString(SR.Sample_Databound_Text);
            }
            else {
                if (boundFieldDesc != null) {
                    data = boundFieldDesc.GetValue(dataItem);
                }
                dataValue = FormatDataValue(data);
            }

            if (boundControl is TableCell) {
                if (dataValue.Length == 0) {
                    dataValue = "&nbsp;";
                }
                ((TableCell)boundControl).Text = dataValue;
            }
            else {
                Debug.Assert(boundControl is TextBox, "Expected the bound control to be a TextBox");
                ((TextBox)boundControl).Text = dataValue;
            }
        }
    }
}

