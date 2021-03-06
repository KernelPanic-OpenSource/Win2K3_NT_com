//------------------------------------------------------------------------------
// <copyright file="XmlSchemaImport.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Schema {

    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaImport : XmlSchemaExternal {
        string ns;
        XmlSchemaAnnotation annotation;

        /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport.Namespace"]/*' />
        [XmlAttribute("namespace", DataType="anyURI")]
        public string Namespace {
            get { return ns; }
            set { ns = value; }
        }

        /// <include file='doc\XmlSchemaImport.uex' path='docs/doc[@for="XmlSchemaImport.Annotation"]/*' />
        [XmlElement("annotation", typeof(XmlSchemaAnnotation))]
        public XmlSchemaAnnotation Annotation {
            get { return annotation; }
            set { annotation = value; }
        }

        internal override void AddAnnotation(XmlSchemaAnnotation annotation) {
            this.annotation = annotation;
        }
    }
}
