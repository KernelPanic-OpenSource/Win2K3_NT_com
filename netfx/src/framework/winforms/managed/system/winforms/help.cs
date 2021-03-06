//------------------------------------------------------------------------------
// <copyright file="Help.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
* Copyright (c) 1997, Microsoft Corporation. All Rights Reserved.
* Information Contained Herein is Proprietary and Confidential.
*/

namespace System.Windows.Forms {
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Net;
    using System.Drawing;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using System.Globalization;
    
    /// <include file='doc\Help.uex' path='docs/doc[@for="Help"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents the HTML 1.0 Help engine.
    ///    </para>
    /// </devdoc>
    public class Help {
#if DEBUG        
        internal static readonly TraceSwitch WindowsFormsHelpTrace = new TraceSwitch("WindowsFormsHelpTrace", "Debug help system");
#else
        internal static readonly TraceSwitch WindowsFormsHelpTrace;
#endif

        private const int HH_DISPLAY_TOPIC        =0x0000;
        private const int HH_HELP_FINDER          =0x0000;  // WinHelp equivalent
        private const int HH_DISPLAY_TOC          =0x0001;  // not currently implemented
        private const int HH_DISPLAY_INDEX        =0x0002;  // not currently implemented
        private const int HH_DISPLAY_SEARCH       =0x0003;  // not currently implemented
        private const int HH_SET_WIN_TYPE         =0x0004;
        private const int HH_GET_WIN_TYPE         =0x0005;
        private const int HH_GET_WIN_HANDLE       =0x0006;
        private const int HH_ENUM_INFO_TYPE       =0x0007;  // Get Info type name, call repeatedly to enumerate, -1 at end
        private const int HH_SET_INFO_TYPE        =0x0008;  // Add Info type to filter.
        private const int HH_SYNC                 =0x0009;
        private const int HH_ADD_NAV_UI           =0x000A;  // not currently implemented
        private const int HH_ADD_BUTTON           =0x000B;  // not currently implemented
        private const int HH_GETBROWSER_APP       =0x000C;  // not currently implemented
        private const int HH_KEYWORD_LOOKUP       =0x000D;
        private const int HH_DISPLAY_TEXT_POPUP   =0x000E;  // display string resource id or text in a popup window
        private const int HH_HELP_CONTEXT         =0x000F;  // display mapped numeric value in dwData
        private const int HH_TP_HELP_CONTEXTMENU  =0x0010;  // text popup help, same as WinHelp HELP_CONTEXTMENU
        private const int HH_TP_HELP_WM_HELP      =0x0011;  // text popup help, same as WinHelp HELP_WM_HELP
        private const int HH_CLOSE_ALL            =0x0012;  // close all windows opened directly or indirectly by the caller
        private const int HH_ALINK_LOOKUP         =0x0013;  // ALink version of HH_KEYWORD_LOOKUP
        private const int HH_GET_LAST_ERROR       =0x0014;  // not currently implemented // See HHERROR.h
        private const int HH_ENUM_CATEGORY        =0x0015;   // Get category name, call repeatedly to enumerate, -1 at end
        private const int HH_ENUM_CATEGORY_IT     =0x0016;  // Get category info type members, call repeatedly to enumerate, -1 at end
        private const int HH_RESET_IT_FILTER      =0x0017;  // Clear the info type filter of all info types.
        private const int HH_SET_INCLUSIVE_FILTER =0x0018;  // set inclusive filtering method for untyped topics to be included in display
        private const int HH_SET_EXCLUSIVE_FILTER =0x0019;  // set exclusive filtering method for untyped topics to be excluded from display
        private const int HH_SET_GUID             =0x001A;  // For Microsoft Installer -- dwData is a pointer to the GUID string

        private const int HTML10HELP = 2;
        private const int HTMLFILE   = 3;

        // not creatable
        //
        private Help() {
        }        

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHelp"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays
        ///       the contents of the Help file at located at a specified Url.
        ///    </para>
        /// </devdoc>
        public static void ShowHelp(Control parent, string url) {
            ShowHelp(parent, url, HelpNavigator.TableOfContents, null);
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHelp1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the contents of
        ///       the Help
        ///       file for a specific topic found at the specified Url.
        ///    </para>
        /// </devdoc>
        public static void ShowHelp(Control parent, string url, HelpNavigator navigator) {
            ShowHelp(parent, url, navigator, null);
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHelp2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the contents of
        ///       the Help
        ///       file for a specific topic found at the specified Url.
        ///    </para>
        /// </devdoc>
        public static void ShowHelp(Control parent, string url, string keyword) {
            if (keyword != null && keyword.Length != 0) {
                ShowHelp(parent, url, HelpNavigator.Topic, keyword);
            }
            else {
                ShowHelp(parent, url, HelpNavigator.TableOfContents, null);
            }
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHelp3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the contents of the Help file located at
        ///       the Url
        ///       supplied by the
        ///       user.
        ///    </para>
        /// </devdoc>
        public static void ShowHelp(Control parent, string url, HelpNavigator command, object param) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: ShowHelp");

            switch (GetHelpFileType(url)) {
                case HTML10HELP:
                    ShowHTML10Help(parent, url, command, param);
                    break;
                case HTMLFILE:
                    ShowHTMLFile(parent, url, command, param);
                    break;
            }
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHelpIndex"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the index of the specified file.
        ///    </para>
        /// </devdoc>
        public static void ShowHelpIndex(Control parent, string url) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: ShowHelpIndex");

            ShowHelp(parent, url, HelpNavigator.Index, null);
        }
 
        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowPopup"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays a Help pop-up window.
        ///    </para>
        /// </devdoc>
        public static void ShowPopup(Control parent, string caption, Point location) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: ShowPopup");

            NativeMethods.HH_POPUP pop = new NativeMethods.HH_POPUP();
 
            // ASURT 148069
            // HtmlHelpW overwrites the pszText pointer in the HH_POPUP structure
            // so we've got to explicitly allocate and free the memory ourselves
            // rather than depending on the marshaller.
	    IntPtr pszText = Marshal.StringToCoTaskMemAuto(caption);
	    
	    try {
               pop.pszText = pszText;
               pop.idString = 0;
               pop.pt = new NativeMethods.POINT(location.X, location.Y);

               // ASURT 108580
               // Looks like a windows bug causes the -1 value for clrBackground to not
               // do the right thing for High Contrast Black color scheme (and probably others)
               //
               pop.clrBackground = Color.FromKnownColor(KnownColor.Window).ToArgb() & 0x00ffffff;
          
               ShowHTML10Help(parent, null, HelpNavigator.Topic, pop);
            }
            finally {
               Marshal.FreeCoTaskMem(pszText);
            }
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHTML10Help"]/*' />
        /// <devdoc>
        ///     Displays HTML 1.0 Help with the specified parameters
        /// </devdoc>
        /// <internalonly/>
        private static void ShowHTML10Help(Control parent, string url, HelpNavigator command, object param) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: ShowHTML10Help:: " + url + ", " + command.ToString("G") + ", " + param);

            object htmlParam;
            if (param is string) {
                int htmlCommand = MapCommandToHTMLCommand(command, (string)param, out htmlParam);
                
                if (htmlParam is string) {
                    SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, htmlCommand, (string)htmlParam);
                }
                else if (htmlParam is NativeMethods.HH_FTS_QUERY) {
                    SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, htmlCommand, (NativeMethods.HH_FTS_QUERY)htmlParam);
                }
                else if (htmlParam is NativeMethods.HH_AKLINK) {
                    // According to MSDN documentation, we have to ensure that the help window is up
                    // before we call ALINK lookup.
                    //
                    SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, HH_DISPLAY_TOPIC, (string)null);
                    SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, htmlCommand, (NativeMethods.HH_AKLINK)htmlParam);
                }
                else {
                    Debug.Fail("Cannot handle HTML parameter of type: " + htmlParam.GetType());
                    SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, htmlCommand, (string)param);
                }
            }
            else if (param == null) {
                SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, MapCommandToHTMLCommand(command, null, out htmlParam), 0);
            }
            else if (param is NativeMethods.HH_POPUP) {
                SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, HH_DISPLAY_TEXT_POPUP, (NativeMethods.HH_POPUP)param);
            }
            else if (param.GetType() == typeof(Int32)) {
                SafeNativeMethods.HtmlHelp(new HandleRef(parent, parent.Handle), url, MapCommandToHTMLCommand(command, null, out htmlParam), (int)param);
            }
        }


        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.ShowHTMLFile"]/*' />
        /// <devdoc>
        ///     Displays HTMLFile with the specified parameters
        /// </devdoc>
        /// <internalonly/>
        private static void ShowHTMLFile(Control parent, string url, HelpNavigator command, object param) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: ShowHTMLHelp:: " + url + ", " + command.ToString("G") + ", " + param);

            Uri file = Resolve(url);

            if (file == null) {
                throw new ArgumentException(SR.GetString(SR.HelpInvalidURL, url), "url");
            }

            switch (file.Scheme) {
                case "http":
                case "https":
                    Debug.WriteLineIf(IntSecurity.SecurityDemand.TraceVerbose, "WebPermission Demanded");
                    new WebPermission(NetworkAccess.Connect, url).Demand();
                    break;
                default:
                    Debug.WriteLineIf(IntSecurity.SecurityDemand.TraceVerbose, "UnmanagedCode Demanded");
                    IntSecurity.UnmanagedCode.Demand();
                    break;
            }

            switch (command) {
                case HelpNavigator.TableOfContents:
                case HelpNavigator.Find:
                case HelpNavigator.Index:
                    // nothing needed...
                    //
                    break;
                case HelpNavigator.Topic:
                    if (param != null && param is string) {
                        file = new Uri(file.ToString() + "#" + (string)param);
                    }
                    break;
            }

            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "\tExecuting '" + file.ToString() + "'");
            UnsafeNativeMethods.ShellExecute_NoBFM(new HandleRef(parent, parent.Handle), null, file.ToString(), null, null, NativeMethods.SW_NORMAL);
        }

        private static Uri Resolve(string partialUri) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: Resolve " + partialUri);
            Debug.Indent();

            Uri file = null;
            try {
                file = new Uri(partialUri);
            }
            catch {
                // eat URI parse exceptions...
                //
            }

            if (file != null && file.Scheme == "file") {
                string localPath = NativeMethods.GetLocalPath(partialUri);
                Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "file, check for existence");
                new FileIOPermission(FileIOPermissionAccess.Read, localPath).Assert();
                try {
                    if (!File.Exists(localPath)) {
                        // nuke, and try relative to AppBase...
                        //
                        file = null;
                    }
                }
                finally {
                    CodeAccessPermission.RevertAssert();
                }
            }

            if (file == null) {
                Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "try appbase relative");
                try {
                    // try relative to AppBase...
                    //
                    file = new Uri(new Uri(AppDomain.CurrentDomain.SetupInformation.ApplicationBase), 
                                   partialUri);
                }
                catch {
                    // eat URI parse exceptions...
                    //
                }

                if (file != null && file.Scheme == "file") {
                    string localPath = file.LocalPath + file.Fragment;
                    Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "file, check for existence");
                    new FileIOPermission(FileIOPermissionAccess.Read, localPath).Assert();
                    try {
                        if (!File.Exists(localPath)) {
                            // nuke - file isn't there...
                            //
                            file = null;
                        }
                    }
                    finally {
                        CodeAccessPermission.RevertAssert();
                    }
                }
            }



            Debug.Unindent();
            return file;
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.GetHelpFileType"]/*' />
        /// <internalonly/>
        private static int GetHelpFileType(string url) {
            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "Help:: GetHelpFileType " + url);

            if (url == null) {
                Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "\tnull, must be Html File");
                return HTMLFILE;
            }

            Uri file = Resolve(url);

            if (file == null || file.Scheme == "file") {
                Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "\tfile");

                string ext = Path.GetExtension(file == null ? url : file.LocalPath + file.Fragment).ToLower(CultureInfo.InvariantCulture);
                if (ext == ".chm" || ext == ".col") {
                    Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "\tchm or col, HtmlHelp 1.0 file");
                    return HTML10HELP;
                }
            }

            Debug.WriteLineIf(Help.WindowsFormsHelpTrace.TraceVerbose, "\tnot file, or odd extension, but be HTML");
            return HTMLFILE;
        }

        /// <include file='doc\Help.uex' path='docs/doc[@for="Help.MapCommandToHTMLCommand"]/*' />
        /// <devdoc>
        ///     Maps one of the COMMAND_* constants to the HTML 1.0 Help equivalent.
        /// </devdoc>
        /// <internalonly/>
        private static int MapCommandToHTMLCommand(HelpNavigator command, string param, out object htmlParam) {
            htmlParam = param;

            if ((param == null || param == "") &&
                (command == HelpNavigator.AssociateIndex || command == HelpNavigator.KeywordIndex)) {
                return HH_DISPLAY_INDEX;
            }

            switch (command) {
                case HelpNavigator.Topic:
                    return HH_DISPLAY_TOPIC;
                
                case HelpNavigator.TableOfContents:
                    return HH_DISPLAY_TOC;
                
                case HelpNavigator.Index:
                    return HH_DISPLAY_INDEX;
                
                case HelpNavigator.Find: {
                    NativeMethods.HH_FTS_QUERY ftsQuery = new NativeMethods.HH_FTS_QUERY();
                    ftsQuery.pszSearchQuery = param;
                    htmlParam = ftsQuery;
                    return HH_DISPLAY_SEARCH;
                }
                
                case HelpNavigator.KeywordIndex:
                case HelpNavigator.AssociateIndex: {
                    NativeMethods.HH_AKLINK alink = new NativeMethods.HH_AKLINK();
                    alink.pszKeywords = param;
                    alink.fIndexOnFail = true;
                    htmlParam = alink;
                    return (command == HelpNavigator.KeywordIndex) ? HH_KEYWORD_LOOKUP : HH_ALINK_LOOKUP;
                }
                
                default:
                    return (int)command;
            }
        }
    }
}
