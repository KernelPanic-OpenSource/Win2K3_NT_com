//------------------------------------------------------------------------------
// <copyright file="SafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Web.UI.Design.MobileControls {
    using System.Runtime.InteropServices;
    using System;
    using System.Security.Permissions;
    using System.Collections;
    using System.IO;
    using System.Text;

    [
    System.Runtime.InteropServices.ComVisible(false), 
    System.Security.SuppressUnmanagedCodeSecurityAttribute()
    ]
    internal class SafeNativeMethods {
        [DllImport(ExternDll.User32, ExactSpelling=true, CharSet=System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool MessageBeep(int type);
    }
}
