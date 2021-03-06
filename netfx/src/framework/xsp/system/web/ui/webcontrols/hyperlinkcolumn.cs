//------------------------------------------------------------------------------
// <copyright file="HyperLinkColumn.cs" company="Microsoft">
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

    /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn"]/*' />
    /// <devdoc>
    /// <para>Creates a column within the <see cref='System.Web.UI.WebControls.DataGrid'/> containing hyperlinks that
    ///    navigate to specified URLs.</para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class HyperLinkColumn : DataGridColumn {

        private PropertyDescriptor textFieldDesc;
        private PropertyDescriptor urlFieldDesc;

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.HyperLinkColumn"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.HyperLinkColumn'/> class.</para>
        /// </devdoc>
        public HyperLinkColumn() {
        }


        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.DataNavigateUrlField"]/*' />
        /// <devdoc>
        /// <para>Gets or sets the field in the DataSource that provides the URL of the page to navigate to.</para>
        /// </devdoc>
        [
        WebCategory("Data"),
        DefaultValue(""),
        WebSysDescription(SR.HyperLinkColumn_DataNavigateUrlField)
        ]
        public virtual string DataNavigateUrlField {
            get {
                object o = ViewState["DataNavigateUrlField"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["DataNavigateUrlField"] = value;
                OnColumnChanged();
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.DataNavigateUrlFormatString"]/*' />
        /// <devdoc>
        /// <para>Gets or sets the formatting applied to the <see cref='System.Web.UI.WebControls.HyperLinkColumn.NavigateUrl'/>
        /// property.</para>
        /// </devdoc>
        [
        WebCategory("Data"),
        DefaultValue(""),
        DescriptionAttribute("The formatting applied to the value bound to the NavigateUrl property.")
        ]
        public virtual string DataNavigateUrlFormatString {
            get {
                object o = ViewState["DataNavigateUrlFormatString"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["DataNavigateUrlFormatString"] = value;
                OnColumnChanged();
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.DataTextField"]/*' />
        /// <devdoc>
        /// <para>Gets or sets the field in the DataSource that will be used as the source of 
        ///    data for the <see cref='System.Web.UI.WebControls.HyperLinkColumn.Text'/> property.</para>
        /// </devdoc>
        [
        WebCategory("Data"),
        DefaultValue(""),
        WebSysDescription(SR.HyperLinkColumn_DataTextField)
        ]
        public virtual string DataTextField {
            get {
                object o = ViewState["DataTextField"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["DataTextField"] = value;
                OnColumnChanged();
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.DataTextFormatString"]/*' />
        /// <devdoc>
        /// <para>Gets or sets the formatting applied to the <see cref='System.Web.UI.WebControls.HyperLinkColumn.Text'/> 
        /// property.</para>
        /// </devdoc>
        [
        WebCategory("Data"),
        DefaultValue(""),
        DescriptionAttribute("The formatting applied to the value bound to the Text property.")
        ]
        public virtual string DataTextFormatString {
            get {
                object o = ViewState["DataTextFormatString"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["DataTextFormatString"] = value;
                OnColumnChanged();
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.NavigateUrl"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the URL to navigate to when the hyperlink is clicked.</para>
        /// </devdoc>
        [
        WebCategory("Behavior"),
        DefaultValue(""),
        WebSysDescription(SR.HyperLinkColumn_NavigateUrl)
        ]
        public virtual string NavigateUrl {
            get {
                object o = ViewState["NavigateUrl"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["NavigateUrl"] = value;
                OnColumnChanged();
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.Target"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the window or target frame that is 
        ///       used to display the contents resulting from the hyperlink.</para>
        /// </devdoc>
        [
        WebCategory("Behavior"),
        DefaultValue(""),
        WebSysDescription(SR.HyperLinkColumn_Target)
        ]
        public virtual string Target {
            get {
                object o = ViewState["Target"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["Target"] = value;
                OnColumnChanged();
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.Text"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the text to display for the hyperlink.</para>
        /// </devdoc>
        [
        WebCategory("Appearance"),
        DefaultValue(""),
        WebSysDescription(SR.HyperLinkColumn_Text)
        ]
        public virtual string Text {
            get {
                object o = ViewState["Text"];
                if (o != null)
                    return(string)o;
                return String.Empty;
            }
            set {
                ViewState["Text"] = value;
                OnColumnChanged();
            }
        }


        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.FormatDataNavigateUrlValue"]/*' />
        /// <devdoc>
        /// </devdoc>
        protected virtual string FormatDataNavigateUrlValue(object dataUrlValue) {
            string formattedUrlValue = String.Empty;

            if ((dataUrlValue != null) && (dataUrlValue != System.DBNull.Value)) {
                string formatting = DataNavigateUrlFormatString;
                if (formatting.Length == 0) {
                    formattedUrlValue = dataUrlValue.ToString();
                }
                else {
                    formattedUrlValue = String.Format(formatting, dataUrlValue);
                }
            }

            return formattedUrlValue;
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.FormatDataTextValue"]/*' />
        /// <devdoc>
        /// </devdoc>
        protected virtual string FormatDataTextValue(object dataTextValue) {
            string formattedTextValue = String.Empty;

            if ((dataTextValue != null) && (dataTextValue != System.DBNull.Value)) {
                string formatting = DataTextFormatString;
                if (formatting.Length == 0) {
                    formattedTextValue = dataTextValue.ToString();
                }
                else {
                    formattedTextValue = String.Format(formatting, dataTextValue);
                }
            }

            return formattedTextValue;
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.Initialize"]/*' />
        /// <devdoc>
        /// </devdoc>
        public override void Initialize() {
            base.Initialize();
            textFieldDesc = null;
            urlFieldDesc = null;
        }
        
        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.InitializeCell"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Initializes the cell representing this column with the
        ///       contained hyperlink.</para>
        /// </devdoc>
        public override void InitializeCell(TableCell cell, int columnIndex, ListItemType itemType) {
            base.InitializeCell(cell, columnIndex, itemType);

            if ((itemType != ListItemType.Header) &&
                (itemType != ListItemType.Footer)) {
                HyperLink hyperLink = new HyperLink();

                hyperLink.Text = Text;
                hyperLink.NavigateUrl = NavigateUrl;
                hyperLink.Target = Target;

                if ((DataNavigateUrlField.Length != 0) ||
                    (DataTextField.Length != 0)) {
                    hyperLink.DataBinding += new EventHandler(this.OnDataBindColumn);
                }

                cell.Controls.Add(hyperLink);
            }
        }

        /// <include file='doc\HyperLinkColumn.uex' path='docs/doc[@for="HyperLinkColumn.OnDataBindColumn"]/*' />
        /// <devdoc>
        /// </devdoc>
        private void OnDataBindColumn(object sender, EventArgs e) {
            Debug.Assert((DataTextField.Length != 0) || (DataNavigateUrlField.Length != 0),
                         "Shouldn't be DataBinding without a DataTextField and DataNavigateUrlField");

            HyperLink boundControl = (HyperLink)sender;
            DataGridItem item = (DataGridItem)boundControl.NamingContainer;
            object dataItem = item.DataItem;

            if ((textFieldDesc == null) && (urlFieldDesc == null)) {
                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(dataItem);
                string fieldName;

                fieldName = DataTextField;
                if (fieldName.Length != 0) {
                    textFieldDesc = props.Find(fieldName, true);
                    if ((textFieldDesc == null) && !DesignMode) {
                        throw new HttpException(HttpRuntime.FormatResourceString(SR.Field_Not_Found, fieldName));
                    }
                }

                fieldName = DataNavigateUrlField;
                if (fieldName.Length != 0) {
                    urlFieldDesc = props.Find(fieldName, true);
                    if ((urlFieldDesc == null) && !DesignMode) {
                        throw new HttpException(HttpRuntime.FormatResourceString(SR.Field_Not_Found, fieldName));
                    }
                }
            }

            if (textFieldDesc != null) {
                object data = textFieldDesc.GetValue(dataItem);
                string dataValue = FormatDataTextValue(data);

                boundControl.Text = dataValue;
            }
            else if (DesignMode && (DataTextField.Length != 0)) {
                boundControl.Text = SR.GetString(SR.Sample_Databound_Text);
            }

            if (urlFieldDesc != null) {
                object data = urlFieldDesc.GetValue(dataItem);
                string dataValue = FormatDataNavigateUrlValue(data);

                boundControl.NavigateUrl = dataValue;
            }
            else if (DesignMode && (DataNavigateUrlField.Length != 0)) {
                boundControl.NavigateUrl = "url";
            }
        }
    }
}

