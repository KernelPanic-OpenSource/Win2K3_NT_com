//------------------------------------------------------------------------------
// <copyright file="ResultPropertyCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.DirectoryServices {

    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Globalization;
    
    /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection"]/*' />
    /// <devdoc>
    /// <para>Contains the properties on a <see cref='System.DirectoryServices.SearchResult'/>.</para>
    /// </devdoc>
    public class ResultPropertyCollection : DictionaryBase {
        
        internal ResultPropertyCollection() {
        }

        /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection.this"]/*' />
        /// <devdoc>
        ///    <para>Gets the property with the given name.</para>
        /// </devdoc>
        public ResultPropertyValueCollection this[string name] {
            get {
                object objectName = name.ToLower(CultureInfo.InvariantCulture);
                return (ResultPropertyValueCollection) InnerHashtable[objectName];
            }
        }

        /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection.PropertyNames"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ICollection PropertyNames {
            get { return Dictionary.Keys; }
        }

        /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection.Values"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ICollection Values {
            get {
                return Dictionary.Values;
            }
        }                                               
                                               
        internal void Add(string name, ResultPropertyValueCollection value) {
            Dictionary.Add(name.ToLower(CultureInfo.InvariantCulture), value);            
        }        
        
        /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(string propertyName) {
            object objectName = propertyName.ToLower(CultureInfo.InvariantCulture);
            return Dictionary.Contains(objectName);
        }
        
        /// <include file='doc\ResultPropertyCollection.uex' path='docs/doc[@for="ResultPropertyCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(ResultPropertyValueCollection[] array, int index) {
            Dictionary.Values.CopyTo((Array)array, index);
        }        
    }
}
