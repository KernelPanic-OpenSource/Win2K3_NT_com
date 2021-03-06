//------------------------------------------------------------------------------
// <copyright file="IgnoreSectionHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

#if !LIB

namespace System.Configuration {

    /// <include file='doc\IgnoreSectionHandler.uex' path='docs/doc[@for="IgnoreSectionHandler"]/*' />
    /// <devdoc>
    /// </devdoc>
    public class IgnoreSectionHandler : IConfigurationSectionHandler {
        /**
         * Create
         *
         * Given a partially composed config object (possibly null)
         * and some input from the config system, return a
         * further partially composed config object
         */
        /// <include file='doc\IgnoreSectionHandler.uex' path='docs/doc[@for="IgnoreSectionHandler.Create"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual object Create(Object parent, Object configContext, System.Xml.XmlNode section) {
            return null;
        }
    }
}

#endif

