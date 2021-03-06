//------------------------------------------------------------------------------
// <copyright file="ContractReference.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Web.Services.Discovery {

    using System;
    using System.Net;
    using System.Xml.Serialization;
    using System.Web.Services.Description;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using System.Web.Services.Protocols;
    using System.Text;
    using System.Collections;

    /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [XmlRoot("contractRef", Namespace=ContractReference.Namespace)]
    public class ContractReference : DiscoveryReference {

        /// <include file='doc\contractreference.uex' path='docs/doc[@for="contractreference.namespace"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public const string Namespace = "http://schemas.xmlsoap.org/disco/scl/";

        private string docRef;
        private string reference;

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.ContractReference"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ContractReference() {
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.ContractReference1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ContractReference(string href) {
            Ref = href;
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.ContractReference2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ContractReference(string href, string docRef) {
            Ref = href;
            DocRef = docRef;
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.Ref"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("ref")]
        public string Ref {
            get {
                return reference;
            }
            set {
                reference = value;
            }
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.DocRef"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("docRef")]
        public string DocRef {
            get {
                return docRef;
            }
            set {
                docRef = value;
            }
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.Url"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public override string Url {
            get {
                return Ref;
            }
            set {
                Ref = value;
            }
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.Contract"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public ServiceDescription Contract {
            get {
                if (ClientProtocol == null)
                    throw new InvalidOperationException(Res.GetString(Res.WebMissingClientProtocol));
                object document = ClientProtocol.Documents[Url];
                if (document == null) {
                    Resolve();
                    document = ClientProtocol.Documents[Url];
                }
                ServiceDescription contract = document as ServiceDescription;
                if (contract == null) {
                    throw new InvalidOperationException(Res.GetString(Res.WebInvalidDocType, 
                                                      typeof(ServiceDescription).FullName,
                                                      document == null ? string.Empty: document.GetType().FullName,
                                                      Url));
                }
                return contract;
            }
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.DefaultFilename"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public override string DefaultFilename {
            get {
                string fileName = Contract.Name;
                if (fileName == null || fileName.Length == 0)
                    fileName = FilenameFromUrl(Url);
                return Path.ChangeExtension(fileName, ".wsdl");
            }
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.WriteDocument"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override void WriteDocument(object document, Stream stream) {
            ((ServiceDescription) document).Write(new StreamWriter(stream, new UTF8Encoding(false)));
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.ReadDocument"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override object ReadDocument(Stream stream) {
            return ServiceDescription.Read(stream);
        }

        /// <include file='doc\ContractReference.uex' path='docs/doc[@for="ContractReference.Resolve"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected internal override void Resolve(string contentType, Stream stream) {
            if (ContentType.IsHtml(contentType))
                throw new InvalidContentTypeException(Res.GetString(Res.WebInvalidContentType, contentType), contentType);
            ServiceDescription serviceDescription = ClientProtocol.Documents[Url] as ServiceDescription;
            if( serviceDescription == null ) {
                serviceDescription = ServiceDescription.Read(stream);
                serviceDescription.RetrievalUrl = Url;
                ClientProtocol.Documents[Url] = serviceDescription;
            }

            ClientProtocol.References[Url] = this;
            
            ArrayList importUrls = new ArrayList();
            foreach (Import import in serviceDescription.Imports)
                if (import.Location != null)
                    importUrls.Add(import.Location);
            foreach (XmlSchema schema in serviceDescription.Types.Schemas)
                foreach (XmlSchemaObject includeObject in schema.Includes) {
                    XmlSchemaImport import = includeObject as XmlSchemaImport;
                    if (import != null) {
                        if (import.SchemaLocation != null)
                            importUrls.Add(import.SchemaLocation);
                        continue;
                    }
                    XmlSchemaInclude include = includeObject as XmlSchemaInclude;
                    if (include != null && include.SchemaLocation != null)
                        importUrls.Add(include.SchemaLocation);
                }                
                
            foreach (string urlFromImport in importUrls) {
                // make the (possibly) relative Uri in the contract fully qualified with respect to the contract URL
                string importUrl = new Uri(new Uri(Url, true), urlFromImport, true).ToString();
                if( ClientProtocol.Documents[importUrl] != null ) {
                    continue;
                }

                string oldUrl = importUrl;
                try {
                    stream = ClientProtocol.Download(ref importUrl, ref contentType);
                    try {
                        //Proceed only if not been here before
                        if( ClientProtocol.Documents[importUrl] == null ) {
                            XmlTextReader reader = new XmlTextReader(new StreamReader(stream, RequestResponseUtils.GetEncoding(contentType)));
                            reader.WhitespaceHandling = WhitespaceHandling.Significant;
                            reader.XmlResolver = null;
                            //Resolve on WSDL and XSD will go recursivelly
                            if (ServiceDescription.CanRead(reader)) {
                                ServiceDescription doc = ServiceDescription.Read(reader);
                                doc.RetrievalUrl = importUrl;
                                ClientProtocol.Documents[importUrl] = doc;
                                ContractReference contractReference = new ContractReference(importUrl, null);
                                contractReference.ClientProtocol = ClientProtocol;
                                try {
                                    contractReference.Resolve(contentType, stream);
                                }
                                catch {
                                    contractReference.Url = oldUrl;
                                }
                            }
                            else if (reader.IsStartElement("schema", XmlSchema.Namespace)) {
                                ClientProtocol.Documents[importUrl] = XmlSchema.Read(reader, null);
                                SchemaReference schemaReference = new SchemaReference(importUrl);
                                schemaReference.ClientProtocol = ClientProtocol;
                                try {
                                    schemaReference.Resolve(contentType, stream);
                                }
                                catch {
                                    schemaReference.Url = oldUrl;
                                }
                            }
                            // If it's not XML, or we don't know what kind of XML it is, skip the file.  The user 
                            // will have to download the dependent file(s) manually, but at least we will continue 
                            // to discover files instead of throwing an exception.
                        }
                    }
                    finally {
                        stream.Close();
                    }
                }
                catch (Exception e) {
                    throw new InvalidDocumentContentsException(Res.GetString(Res.TheWSDLDocumentContainsLinksThatCouldNotBeResolved, importUrl), e);
                }
            }
        }
    }
}
