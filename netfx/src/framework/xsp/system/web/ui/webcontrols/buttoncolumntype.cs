//------------------------------------------------------------------------------
// <copyright file="ButtonColumnType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.Web.UI.WebControls {

    using System;

    /// <include file='doc\ButtonColumnType.uex' path='docs/doc[@for="ButtonColumnType"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the values for the <see cref='System.Web.UI.WebControls.ButtonColumn.ButtonType'/> property on a <see cref='System.Web.UI.WebControls.ButtonColumn'/>
    ///       column.
    ///    </para>
    /// </devdoc>
    public enum ButtonColumnType {

        /// <include file='doc\ButtonColumnType.uex' path='docs/doc[@for="ButtonColumnType.LinkButton"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A
        ///       column of link buttons.
        ///    </para>
        /// </devdoc>
        LinkButton = 0,

        /// <include file='doc\ButtonColumnType.uex' path='docs/doc[@for="ButtonColumnType.PushButton"]/*' />
        /// <devdoc>
        ///    <para> A column of push buttons.</para>
        /// </devdoc>
        PushButton = 1
    }
}
