//------------------------------------------------------------------------------
// <copyright file="ToolBarButtonClickEventHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


/**************************************************************************\
*
* Copyright (c) 1998-2002, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   ToolBarButtonClickEventHandler.cs
*
* Abstract:
*
* Revision History:
*
\**************************************************************************/
namespace System.Windows.Forms {

    using System.Diagnostics;

    using System;


    /// <include file='doc\ToolBarButtonClickEventHandler.uex' path='docs/doc[@for="ToolBarButtonClickEventHandler"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents the method that will handle the <see cref='System.Windows.Forms.ToolBar.ButtonClick'/> event of a
    ///    <see cref='System.Windows.Forms.ToolBar'/> .
    ///    </para>
    /// </devdoc>
    public delegate void ToolBarButtonClickEventHandler(object sender, ToolBarButtonClickEventArgs e);
}
