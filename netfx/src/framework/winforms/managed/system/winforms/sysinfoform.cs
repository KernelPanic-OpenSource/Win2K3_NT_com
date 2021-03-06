//------------------------------------------------------------------------------
// <copyright file="SysInfoForm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


/**************************************************************************\
*
* Copyright (c) 1998-2002, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   SysInfoForm.cs
*
* Abstract:
*
* Revision History:
*
\**************************************************************************/
#if SECURITY_DIALOG
namespace System.Windows.Forms {

    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Xml;
    using System.Windows.Forms;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Globalization;
    
    /// <include file='doc\SysInfoForm.uex' path='docs/doc[@for="SysInfoForm"]/*' />
    /// <devdoc>
    ///    Summary description for SysInfoForm.
    /// </devdoc>
    internal class SysInfoForm : Form {
        static Switch[] switches;

        /// <include file='doc\SysInfoForm.uex' path='docs/doc[@for="SysInfoForm.components"]/*' />
        /// <devdoc> 
        ///    Required by the Win Forms designer 
        /// </devdoc>
        Container components;
        ColumnHeader codeBaseColumn;
        ColumnHeader versionColumn;
        ColumnHeader fileVersionColumn;
        ColumnHeader asmNameColumn;
        ColumnHeader switchNameColumn;
        ColumnHeader displayNameColumn;
        ListView loadedAssemblyList;
        ListView switchesList;
        Button closeButton;
        TabPage appInfo;
        TabPage sysInfo;
        TabPage secInfo;
        TabPage switchInfo;
        TabPage bugReportInfo;
        TabControl tabControl1;
        PropertyGrid appProps;
        Label bugReportLabel;
        Label securityLabel;
        Label switchLabel;
        TextBox bugReportDescription;
        CheckBox includeSystemInformation;
        CheckBox includeApplicationInformation;
        Panel bugReportPanel;
        Button saveBugReport;
        Button submitBugReport;
        bool windowIsRestricted = false;

        private static Switch[] GetSwitchesFromLoadedAssemblies() {
            ArrayList list = new ArrayList();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            new ReflectionPermission(PermissionState.Unrestricted).Assert();
            try {
                foreach (Assembly assembly in assemblies) {
                    Module[] modules = assembly.GetLoadedModules();

                    foreach (Module module in modules) {

                        if (module != null) {
                            foreach (Type type in module.GetTypes()) {

                                if (type != null) {
                                    MemberInfo[] members = type.FindMembers(MemberTypes.Field, 
                                                                            BindingFlags.Static 
                                                                            | BindingFlags.Public 
                                                                            | BindingFlags.NonPublic, 
                                                                            new MemberFilter(Filter), 
                                                                            null);

                                    foreach (MemberInfo member in members) {
                                        if (member != null && member is FieldInfo) {
                                            FieldInfo field = (FieldInfo) member;
                                            object value = field.GetValue(null);
                                            if (value != null)
                                                list.Add(value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally {
                CodeAccessPermission.RevertAssert();
            }
            return(Switch[]) list.ToArray(typeof(Switch));
        }

        private static bool Filter(MemberInfo member, object criteria) {
            if (member == null) {
                return false;
            }
            FieldInfo field = (FieldInfo)member;            
            if (field.FieldType == null) {
                return false;
            }
            return(field.FieldType.IsSubclassOf(typeof(Switch)));
        }

        public SysInfoForm(bool windowIsRestricted) {
            this.windowIsRestricted = windowIsRestricted;

            // Required for Win Form Designer support
            InitializeComponent();

            MinimumSize = Size;
            if (windowIsRestricted) {
                securityLabel.Text = SR.GetString(SR.SecurityRestrictedText);
            }
            else {
                securityLabel.Text = SR.GetString(SR.SecurityUnrestrictedText);
            }
        }

        /// <include file='doc\SysInfoForm.uex' path='docs/doc[@for="SysInfoForm.Dispose"]/*' />
        /// <devdoc>
        ///    Clean up any resources being used
        /// </devdoc>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        private void PopulateAssemblyInfo() {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    AssemblyName name = asm.GetName();
                    string fileVer = "n/a";
                    if (name.EscapedCodeBase != null && name.EscapedCodeBase.Length > 0) {
                        Uri codeBase = new Uri(name.EscapedCodeBase);
                        if (codeBase.Scheme == "file") {
                            fileVer = FileVersionInfo.GetVersionInfo(NativeMethods.GetLocalPath(name.EscapedCodeBase)).FileVersion;
                        }
                    }
                    ListViewItem item = new ListViewItem(new string[] {
                                                             name.Name,
                                                             fileVer,
                                                             name.Version.ToString(),
                                                             name.EscapedCodeBase});
                    loadedAssemblyList.Items.Add(item);
                }
                catch (Exception) {
                    // ignore any exceptions... this dialog should never cause a fault...
                    //
                    loadedAssemblyList.Items.Add(new ListViewItem(new string[] {asm.GetName().Name, "Exception loading information"}));
                }
            }
        }

        private void PopulateApplicationInfo() {
            appProps.SelectedObject = new AppInfo();
        }

        private void WriteBugReport(TextWriter writer) {
            XmlTextWriter xml = new XmlTextWriter(writer);
            xml.Formatting = Formatting.Indented;

            xml.WriteStartElement("bug"); {
                xml.WriteAttributeString("product", ".NET Frameworks SDK");

                xml.WriteStartElement("problem"); {
                    xml.WriteString(bugReportDescription.Text);
                }
                xml.WriteEndElement();
                
                if (includeApplicationInformation.Checked) {
                    xml.WriteStartElement("assemblies"); {
                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                            AssemblyName name = asm.GetName();
                            xml.WriteStartElement("assembly"); {
                                xml.WriteAttributeString("name", name.Name);
                                xml.WriteAttributeString("codebase", name.CodeBase);
                                xml.WriteAttributeString("version", name.Version.ToString());
                                
                                string fileVer = "n/a";
                                if (name.EscapedCodeBase != null && name.EscapedCodeBase.Length > 0) {
                                    Uri codeBase = new Uri(name.EscapedCodeBase);
                                    if (codeBase.Scheme == "file") {
                                        fileVer = FileVersionInfo.GetVersionInfo(NativeMethods.GetLocalPath(name.EscapedCodeBase)).FileVersion;
                                    }
                                }
                                xml.WriteAttributeString("win32version", fileVer);
                            }
                            xml.WriteEndElement();
                        }
                    }
                    xml.WriteEndElement();
                }

                if (includeSystemInformation.Checked) {
                    xml.WriteStartElement("system"); {
                        xml.WriteAttributeString("os", Environment.OSVersion.Platform.ToString("G") + " " + Environment.OSVersion.Version.ToString());
                    }
                    xml.WriteEndElement();
                }
            }
            xml.WriteEndElement();
        }

        private void SubmitBugReport(object sender, EventArgs e) {
            UnsafeNativeMethods.ShellExecute(NativeMethods.NullHandleRef, null, SR.GetString(SR.SecuritySubmitBugUrl), null, null, NativeMethods.SW_NORMAL);
        }

        private void SaveBugReport(object sender, EventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = SR.GetString(SR.SecuritySaveFilter);
            if (dialog.ShowDialog() == DialogResult.OK) {
                Stream output = dialog.OpenFile();
                StreamWriter writer = new StreamWriter(output);
                try {
                    WriteBugReport(writer);
                }
                finally {
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        private void PopulateSwitchesInfo() {

            // grab it in a local in case assembly load causes static to null out
            // Until we start listening to assembly load events, we should not cache!
            Switch[] switchesLocal = null;  // switches
            if (switchesLocal == null) {
                switchesLocal = GetSwitchesFromLoadedAssemblies();
                switches = switchesLocal;
                Array.Sort(switchesLocal, new SwitchSorter());
            }

            foreach (Switch sw in switchesLocal) {
                bool value;
                if (sw is TraceSwitch)
                    value = ((TraceSwitch)sw).Level > TraceLevel.Off;
                else if (sw is BooleanSwitch)
                    value =  ((BooleanSwitch)sw).Enabled;
                else
                    continue; 

                ListViewItem item = new SwitchItem(sw);
                item.Checked = value;
                switchesList.Items.Add(item);
            }
        }

        private void SwitchChecked(object sender, ItemCheckEventArgs e) {
            SwitchItem item = (SwitchItem)switchesList.Items[e.Index];
            Switch sw = item.Value;
            bool value = e.NewValue == CheckState.Checked;
            if (sw is TraceSwitch) {
                ((TraceSwitch)sw).Level = (value ? TraceLevel.Verbose : TraceLevel.Off);
            }
            else if (sw is BooleanSwitch) {
                ((BooleanSwitch)sw).Enabled = value;
            }
        }

        void TabSelectionChanged(object sender, EventArgs e) {
            if (tabControl1.SelectedTab == switchInfo) {
                if (switchesList.Items.Count == 0) {
                    try {
                        PopulateSwitchesInfo();
                    }
                    catch {
                    }
                }
            }
            else if (tabControl1.SelectedTab == sysInfo) {
                if (loadedAssemblyList.Items.Count == 0) {
                    try {
                        PopulateAssemblyInfo();
                    }
                    catch {
                    }
                }
            }
            else if (tabControl1.SelectedTab == appInfo) {
                if (appProps.SelectedObject == null) {
                    try {
                        PopulateApplicationInfo();
                    }
                    catch {
                    }
                }
            }
        }

        /// <include file='doc\SysInfoForm.uex' path='docs/doc[@for="SysInfoForm.InitializeComponent"]/*' />
        /// <devdoc>
        ///    Required method for Designer support - do not modify
        ///    the contents of this method with an editor
        /// </devdoc>
        private void InitializeComponent() {
            this.components = new Container();
            this.sysInfo = new TabPage();
            this.versionColumn = new ColumnHeader();
            this.fileVersionColumn = new ColumnHeader();
            this.loadedAssemblyList = new ListView();
            this.switchesList = new ListView();
            this.appInfo = new TabPage();
            this.tabControl1 = new TabControl();
            this.asmNameColumn = new ColumnHeader();
            this.codeBaseColumn = new ColumnHeader();
            this.switchNameColumn = new ColumnHeader();
            this.displayNameColumn = new ColumnHeader();
            this.secInfo = new TabPage();
            this.bugReportInfo = new TabPage();
            this.bugReportLabel = new Label();
            this.switchInfo = new TabPage();
            this.closeButton = new Button();
            this.appProps = new PropertyGrid();
            this.securityLabel = new Label();
            this.bugReportDescription = new TextBox();
            this.includeSystemInformation = new CheckBox();
            this.includeApplicationInformation = new CheckBox();
            this.saveBugReport = new Button();
            this.submitBugReport = new Button();
            this.switchLabel = new Label();
            this.bugReportPanel = new Panel();


            //@design this.TrayLargeIcon = false;
            //@design this.TrayAutoArrange = true;
            //@design this.TrayHeight = 0;
            //@design this.GridSize = new System.Drawing.Size(4, 4);
            this.Text = SR.GetString(SR.SecurityAboutDialog);
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(442, 273);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ControlBox = false;
            this.CancelButton = closeButton;
            this.Font = new Font("Tahoma", 8);

            sysInfo.Size = new System.Drawing.Size(428, 210);
            sysInfo.TabIndex = 1;
            sysInfo.Text = SR.GetString(SR.SecurityAssembliesTab);

            versionColumn.Text = SR.GetString(SR.SecurityVersionColumn);
            versionColumn.Width = 100;
            versionColumn.TextAlign = HorizontalAlignment.Left;

            fileVersionColumn.Text = SR.GetString(SR.SecurityFileVersionColumn);
            fileVersionColumn.Width = 100;
            fileVersionColumn.TextAlign = HorizontalAlignment.Left;

            loadedAssemblyList.Size = new System.Drawing.Size(428, 210);
            loadedAssemblyList.Dock = DockStyle.Fill;
            loadedAssemblyList.TabIndex = 0;
            loadedAssemblyList.FullRowSelect = true;
            loadedAssemblyList.View = View.Details;
            loadedAssemblyList.Columns.Clear();
            loadedAssemblyList.Columns.AddRange(new ColumnHeader[] {asmNameColumn,
                fileVersionColumn,
                versionColumn,
                codeBaseColumn});

            switchLabel.Size = new Size(428, 25);
            switchLabel.Dock = DockStyle.Bottom;
            switchLabel.Text = SR.GetString(SR.SecuritySwitchLabel);

            switchesList.Size = new System.Drawing.Size(428, 210);
            switchesList.Dock = DockStyle.Fill;
            switchesList.ItemCheck += new ItemCheckEventHandler(SwitchChecked);
            switchesList.TabIndex = 0;
            switchesList.CheckBoxes = true;
            switchesList.FullRowSelect = true;
            switchesList.View = View.Details;
            switchesList.Columns.Clear();
            switchesList.Columns.AddRange(new ColumnHeader[] {switchNameColumn,
                displayNameColumn});

            appInfo.Size = new System.Drawing.Size(428, 210);
            appInfo.TabIndex = 2;
            appInfo.Text = SR.GetString(SR.SecurityApplication);
            appInfo.DockPadding.All = 2;

            switchInfo.Size = new System.Drawing.Size(428, 210);
            switchInfo.TabIndex = 4;
            switchInfo.Text = SR.GetString(SR.SecuritySwitchesTab);
            
            bugReportDescription.Multiline = true;
            bugReportDescription.Dock = DockStyle.Fill;
            bugReportDescription.WordWrap = true;

            bugReportLabel.Dock = DockStyle.Top;
            bugReportLabel.Size = new System.Drawing.Size(428, 36);
            bugReportLabel.Text = SR.GetString(SR.SecurityBugReportLabel);

            includeSystemInformation.Checked = true;
            includeSystemInformation.Dock = DockStyle.Bottom;
            includeSystemInformation.FlatStyle = FlatStyle.System;
            includeSystemInformation.Text = SR.GetString(SR.SecurityIncludeSysInfo);

            includeApplicationInformation.Checked = true;
            includeApplicationInformation.Dock = DockStyle.Bottom;
            includeApplicationInformation.FlatStyle = FlatStyle.System;
            includeApplicationInformation.Text = SR.GetString(SR.SecurityIncludeAppInfo);
            
            saveBugReport.Text = SR.GetString(SR.SecuritySaveBug);
            saveBugReport.Location = new Point(2, 2);
            saveBugReport.FlatStyle = FlatStyle.System;
            saveBugReport.Size = new Size(75, 23);
            saveBugReport.Click += new EventHandler(SaveBugReport);

            submitBugReport.Text = SR.GetString(SR.SecuritySubmitBug);
            submitBugReport.Location = new Point(79, 2);
            submitBugReport.FlatStyle = FlatStyle.System;
            submitBugReport.Size = new Size(75, 23);
            submitBugReport.Click += new EventHandler(SubmitBugReport);

            bugReportPanel.Dock = DockStyle.Bottom;
            bugReportPanel.Size = new Size(428, 27);

            appProps.Dock = DockStyle.Fill;
            appProps.ToolbarVisible = false;

            tabControl1.Location = new System.Drawing.Point(4, 4);
            tabControl1.Size = new System.Drawing.Size(436, 236);
            tabControl1.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            tabControl1.SelectedIndex = 0;
            tabControl1.TabIndex = 0;
            tabControl1.SelectedIndexChanged += new EventHandler(TabSelectionChanged);

            asmNameColumn.Text = SR.GetString(SR.SecurityAsmNameColumn);
            asmNameColumn.Width = 150;
            asmNameColumn.TextAlign = HorizontalAlignment.Left;

            switchNameColumn.Text = SR.GetString(SR.SecuritySwitchNameColumn);
            switchNameColumn.Width = 150;
            switchNameColumn.TextAlign = HorizontalAlignment.Left;

            displayNameColumn.Text = SR.GetString(SR.SecuritySwitchDescrColumn);
            displayNameColumn.Width = 300;
            displayNameColumn.TextAlign = HorizontalAlignment.Left;

            codeBaseColumn.Text = SR.GetString(SR.SecurityCodeBaseColumn);
            codeBaseColumn.Width = 400;
            codeBaseColumn.TextAlign = HorizontalAlignment.Left;

            secInfo.Size = new System.Drawing.Size(428, 210);
            secInfo.TabIndex = 0;
            secInfo.Text = SR.GetString(SR.SecurityInfoTab);

            bugReportInfo.Size = new System.Drawing.Size(428, 210);
            bugReportInfo.TabIndex = 0;
            bugReportInfo.Text = SR.GetString(SR.SecurityBugReportTab);

            securityLabel.Dock = DockStyle.Fill;

            closeButton.Size = new System.Drawing.Size(75, 23);
            closeButton.FlatStyle = FlatStyle.System;
            closeButton.TabIndex = 1;
            closeButton.Location = new System.Drawing.Point(344, 248);
            closeButton.Text = SR.GetString(SR.SecurityClose);
            closeButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            closeButton.DialogResult = DialogResult.OK;

            this.Controls.Add(closeButton);
            this.Controls.Add(tabControl1);
            switchInfo.Controls.Add(switchesList);
            switchInfo.Controls.Add(switchLabel);
            sysInfo.Controls.Add(loadedAssemblyList);
            secInfo.Controls.Add(securityLabel);
            appInfo.Controls.Add(appProps);
            bugReportPanel.Controls.Add(saveBugReport);
            bugReportPanel.Controls.Add(submitBugReport);
            bugReportInfo.Controls.Add(bugReportDescription);
            bugReportInfo.Controls.Add(bugReportLabel);
            bugReportInfo.Controls.Add(includeSystemInformation);
            bugReportInfo.Controls.Add(includeApplicationInformation);
            bugReportInfo.Controls.Add(bugReportPanel);
            tabControl1.Controls.Add(secInfo);
            tabControl1.Controls.Add(appInfo);
            tabControl1.Controls.Add(sysInfo);
            tabControl1.Controls.Add(switchInfo);
            tabControl1.Controls.Add(bugReportInfo);
        }

        [DefaultProperty("CompanyName")]
        public class AppInfo {
            AssemblyName assemblyName;

            public AppInfo() {
                assemblyName = Assembly.GetEntryAssembly().GetName();
            }

            [Category("Entry Assembly")]
            public Version Version {
                get {
                    return assemblyName.Version;
                }
            }

            [Category("Entry Assembly")]
            public string Name {
                get {
                    return assemblyName.Name;
                }
            }

            [Category("Entry Assembly")]
            public string CodeBase {
                get {
                    return assemblyName.CodeBase;
                }
            }

            [Category("Directories")]
            public string MyDocuments {
                get {
                    return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                }
            }

            [Category("Directories")]
            public string UserAppDataPath {
                get {
                    return Application.UserAppDataPath;
                }
            }

            [Category("Directories")]
            public string CommonUserAppDataPath {
                get {
                    return Application.CommonAppDataPath;
                }
            }

            [Category("Directories")]
            public string LocalUserAppDataPath {
                get {
                    return Application.LocalUserAppDataPath;
                }
            }

            [Category("Application")]
            public string CompanyName {
                get {
                    return Application.CompanyName;
                }
            }

            [Category("Directories")]
            public string AppBase {
                get {
                    return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                }
            }

            [Category("Application")]
            public string ConfigurationFile {
                get {
                    return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                }
            }

            [Category("Application")]
            public string ProductName {
                get {
                    return Application.ProductName;
                }
            }

            [Category("Application")]
            public string ProductVersion {
                get {
                    return Application.ProductVersion;
                }
            }

            [Category("Startup")]
            public string CommandLine {
                get {
                    return Environment.CommandLine;
                }
            }

            [Category("Startup")]
            public string[] CommandLineArgs {
                get {
                    return Environment.GetCommandLineArgs();
                }
            }

            [Category("Startup")]
            public string StartupPath {
                get {
                    return Application.StartupPath;
                }
            }

            [Category("Application")]
            public string ExecutablePath {
                get {
                    return Application.ExecutablePath;
                }
            }
        }


        class SwitchSorter : IComparer {
            public int Compare(object x, object y) {
                return String.Compare(((Switch)x).DisplayName, ((Switch)y).DisplayName, false, CultureInfo.InvariantCulture);
            }
        }

        class SwitchItem : ListViewItem {
            Switch value;

            internal SwitchItem(Switch value) : base(new string[] {value.DisplayName, value.Description}) {
                this.value = value;
            }

            public Switch Value {
                get {
                    return value;
                }
            }

        }
    }
}
#endif
