//------------------------------------------------------------------------------
// <copyright file="XmlBoundElement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


/**************************************************************************\
*
* Copyright (c) 1998-2002, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   XmlBoundElement.cs
*
* Abstract:
*
* Revision History:
*
\**************************************************************************/

namespace System.Xml {
    using System;
    using System.IO;
    using System.Collections;
    using System.Data;
    using System.Diagnostics;

    internal enum ElementState {
        None,
        Defoliated,
        WeakFoliation,
        StrongFoliation,
        Foliating,
        Defoliating,
    }

    internal class XmlBoundElement: XmlElement {
        private DataRow row;
        private ElementState state;

        internal XmlBoundElement( string prefix, string localName, string namespaceURI, XmlDocument doc )
        : base( prefix, localName, namespaceURI, doc ) {
            state = ElementState.None;
        }

        public override XmlAttributeCollection Attributes {
            get {
                AutoFoliate();
                return base.Attributes;
            }
        }

        public override bool HasAttributes {
            get { return Attributes.Count > 0; }
        }

        public override XmlNode FirstChild { 
            get { 
                AutoFoliate();
                return base.FirstChild;
            }
        }

        internal XmlNode SafeFirstChild { get { return base.FirstChild; } }

        public override XmlNode LastChild { 
            get { 
                AutoFoliate();
                return base.LastChild;
            }
        }

        internal XmlNode SafeLastChild { get { return base.LastChild; } }

        public override XmlNode PreviousSibling { 
            get { 
                XmlNode prev = base.PreviousSibling;
                if ( prev == null ) {
                    XmlBoundElement parent = ParentNode as XmlBoundElement;
                    if ( parent != null ) {
                        parent.AutoFoliate();
                        return base.PreviousSibling;
                    }
                }
                return prev;
            }
        }

        internal XmlNode SafePreviousSibling { get { return base.PreviousSibling; } }

        public override XmlNode NextSibling { 
            get { 
                XmlNode next = base.NextSibling;
                if ( next == null ) {
                    XmlBoundElement parent = ParentNode as XmlBoundElement;
                    if ( parent != null ) {
                        parent.AutoFoliate();
                        return base.NextSibling;
                    }
                }
                return next;
            } 
        }

        internal XmlNode SafeNextSibling { get { return base.NextSibling; } }

        public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild) {
            AutoFoliate();
            return base.InsertBefore( newChild, refChild );
        }

        public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild) {
            AutoFoliate();
            return base.InsertAfter( newChild, refChild );
        }

        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild) {
            AutoFoliate();
            return base.ReplaceChild( newChild, oldChild );
        }

        public override XmlNode AppendChild(XmlNode newChild) {
            AutoFoliate();
            return base.AppendChild( newChild );
        }

        internal void RemoveAllChildren() {           
            XmlNode child = FirstChild;
            XmlNode sibling = null;

            while ( child != null ) {
                sibling = child.NextSibling;
                RemoveChild( child );
                child = sibling;
            }
        }
        
        public override string InnerXml {
            get {
                return base.InnerXml;
            }
            set {
                
                RemoveAllChildren();
                
                XmlDataDocument doc = (XmlDataDocument) OwnerDocument;
                
                bool bOrigIgnoreXmlEvents = doc.IgnoreXmlEvents;
                bool bOrigIgnoreDataSetEvents = doc.IgnoreDataSetEvents;
                
                doc.IgnoreXmlEvents = true;
                doc.IgnoreDataSetEvents = true;
                
                base.InnerXml = value;

                doc.SyncTree( this );

                doc.IgnoreDataSetEvents = bOrigIgnoreDataSetEvents;
                doc.IgnoreXmlEvents = bOrigIgnoreXmlEvents;
            }
        }
        
        internal DataRow Row {
            get { return row;}
            set { row = value;}
        }

        internal bool IsFoliated {
            get { 
                while ( state == ElementState.Foliating || state == ElementState.Defoliating )
                    System.Threading.Thread.Sleep(0);
                //has to be sure that we are either foliated or defoliated when ask for IsFoliated.
                return state != ElementState.Defoliated;
            }
        }

        internal ElementState ElementState {
            get { return state;}
            set { state = value;}
        }

        internal void Foliate( ElementState newState ) {
            XmlDataDocument doc = (XmlDataDocument) OwnerDocument;
            if ( doc != null )
                doc.Foliate( this, newState );
        }

        // Foliate the node as a side effect of user calling functions on this node (like NextSibling) OR as a side effect of DataDocNav using nodes to do editing
        private void AutoFoliate() {
            XmlDataDocument doc = (XmlDataDocument) OwnerDocument;
            if ( doc != null )
                doc.Foliate( this, doc.AutoFoliationState );
        }

        public override XmlNode CloneNode(bool deep) {
            XmlDataDocument doc = (XmlDataDocument)(this.OwnerDocument);
            ElementState oldAutoFoliationState = doc.AutoFoliationState;
            doc.AutoFoliationState = ElementState.WeakFoliation;
            XmlElement element;
            try {
                Foliate( ElementState.WeakFoliation );
                element = (XmlElement)(base.CloneNode( deep ));
                // Clone should create a XmlBoundElement node
                Debug.Assert( element is XmlBoundElement );
            }
            finally {
                doc.AutoFoliationState = oldAutoFoliationState;
            }

            return element;
        }

        public override void WriteContentTo( XmlWriter w ) {
            DataPointer dp = new DataPointer( (XmlDataDocument)OwnerDocument, this );            
            WriteBoundElementContentTo( dp, w );            
        }

        public override void WriteTo( XmlWriter w ) {
            DataPointer dp = new DataPointer( (XmlDataDocument)OwnerDocument, this );            
            WriteRootBoundElementTo( dp, w );            
        }

        private void WriteRootBoundElementTo(DataPointer dp, XmlWriter w) {            
            Debug.Assert( dp.NodeType == XmlNodeType.Element );
            XmlDataDocument doc = (XmlDataDocument)OwnerDocument;
            w.WriteStartElement( dp.Prefix, dp.LocalName, dp.NamespaceURI );            
            int cAttr = dp.AttributeCount;
            bool bHasXSI = false;
            if ( cAttr > 0 ) {
                for ( int iAttr = 0; iAttr < cAttr; iAttr++ ) {
                    dp.MoveToAttribute( iAttr );
                    if ( dp.Prefix == "xmlns" && dp.LocalName == XmlDataDocument.XSI )
                        bHasXSI = true;
                    WriteTo( dp, w );
                    dp.MoveToOwnerElement();
                }
            }
            
            if ( !bHasXSI && doc.bLoadFromDataSet && doc.bHasXSINIL ) 
                w.WriteAttributeString( "xmlns", "xsi", "http://www.w3.org/2000/xmlns/", Keywords.XSINS );
            
            
            WriteBoundElementContentTo( dp, w );
            
            // Force long end tag when the elem is not empty, even if there are no children.
            if ( dp.IsEmptyElement )
                w.WriteEndElement();
            else
                w.WriteFullEndElement();
        }

        private static void WriteBoundElementTo( DataPointer dp, XmlWriter w ) {
            Debug.Assert( dp.NodeType == XmlNodeType.Element );
            w.WriteStartElement( dp.Prefix, dp.LocalName, dp.NamespaceURI );
            int cAttr = dp.AttributeCount;
            if ( cAttr > 0 ) {
                for ( int iAttr = 0; iAttr < cAttr; iAttr++ ) {
                    dp.MoveToAttribute( iAttr );
                    WriteTo( dp, w );
                    dp.MoveToOwnerElement();
                }
            }
            
            WriteBoundElementContentTo( dp, w );
            
            // Force long end tag when the elem is not empty, even if there are no children.
            if ( dp.IsEmptyElement )
                w.WriteEndElement();
            else
                w.WriteFullEndElement();
        }

        private static void WriteBoundElementContentTo( DataPointer dp, XmlWriter w ) {
            if ( !dp.IsEmptyElement && dp.MoveToFirstChild() ) {
                do {
                    WriteTo( dp, w );
                }
                while ( dp.MoveToNextSibling() );

                dp.MoveToParent();
            }
        }
        
        private static void WriteTo( DataPointer dp, XmlWriter w ) {
            switch ( dp.NodeType ) {
                case XmlNodeType.Attribute:
                    if ( !dp.IsDefault ) {
                        w.WriteStartAttribute( dp.Prefix, dp.LocalName, dp.NamespaceURI );

                        if ( dp.MoveToFirstChild() ) {
                            do {
                                WriteTo( dp, w );
                            }
                            while ( dp.MoveToNextSibling() );

                            dp.MoveToParent();
                        }

                        w.WriteEndAttribute();
                    }
                    break;

                case XmlNodeType.Element:
                    WriteBoundElementTo( dp, w );
                    break;

                case XmlNodeType.Text:
                    w.WriteString(dp.Value);
                    break;

                default:
                    Debug.Assert( ((IXmlDataVirtualNode)dp).IsOnColumn( null ) );
                    if ( dp.GetNode() != null )
                        dp.GetNode().WriteTo( w );
                    break;
            }
        }

    }
}
