//------------------------------------------------------------------------------
// <copyright file="TitleFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Web.UI.WebControls {
    
    /// <include file='doc\TitleFormat.uex' path='docs/doc[@for="TitleFormat"]/*' />
    /// <devdoc>
    ///    <para> Specifies the name format of the visible 
    ///       month for the <see cref='System.Web.UI.WebControls.Calendar'/>
    ///       control.</para>
    /// </devdoc>
    public enum TitleFormat {
        /// <include file='doc\TitleFormat.uex' path='docs/doc[@for="TitleFormat.Month"]/*' />
        /// <devdoc>
        ///    <para>The name format of the visible month contains the
        ///       month but not the year. For example, January.</para>
        /// </devdoc>
        Month = 0,
        /// <include file='doc\TitleFormat.uex' path='docs/doc[@for="TitleFormat.MonthYear"]/*' />
        /// <devdoc>
        ///    <para> The name format of the visible month contains both the
        ///       month and the year. For example, January 2000.</para>
        /// </devdoc>
        MonthYear = 1
    }
}
