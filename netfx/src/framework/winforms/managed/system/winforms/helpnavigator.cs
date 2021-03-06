//------------------------------------------------------------------------------
// <copyright file="HelpNavigator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
* Copyright (c) 1997, Microsoft Corporation. All Rights Reserved.
* Information Contained Herein is Proprietary and Confidential.
*/

namespace System.Windows.Forms {
    using System;

    /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents the HTML 1.0 Help engine.
    ///    </para>
    /// </devdoc>
    public enum HelpNavigator {

        /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator.Topic"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the topic referenced by the topic referenced by
        ///       the specified Url.
        ///       This field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        Topic = unchecked((int)0x80000001),
        /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator.TableOfContents"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the contents of the 
        ///       HTML 1.0 Help file. This field is constant.
        ///    </para>
        /// </devdoc>
        TableOfContents = unchecked((int)0x80000002),
        /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator.Index"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the index of a specified
        ///       Url. This field is constant.
        ///    </para>
        /// </devdoc>
        Index = unchecked((int)0x80000003),
        /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator.Find"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the search page
        ///       of a specified Url. This field is constant.
        ///    </para>
        /// </devdoc>
        Find = unchecked((int)0x80000004),
        /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator.AssociateIndex"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the topic referenced by the topic referenced by
        ///       the specified Url.
        ///       This field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        AssociateIndex = unchecked((int)0x80000005),
        /// <include file='doc\HelpNavigator.uex' path='docs/doc[@for="HelpNavigator.KeywordIndex"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Displays the topic referenced by the topic referenced by
        ///       the specified Url.
        ///       This field is
        ///       constant.
        ///    </para>
        /// </devdoc>
        KeywordIndex = unchecked((int)0x80000006)
    }
}
