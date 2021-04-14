//------------------------------------------------------------------------------
// <copyright file="XmlSchemaParticle.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Schema {

    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaParticle.uex' path='docs/doc[@for="XmlSchemaParticle"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class XmlSchemaParticle : XmlSchemaAnnotated {
        [Flags]
        enum Occurs {
            None,
            Min,
            Max
        };
        decimal minOccurs = decimal.One;
        decimal maxOccurs = decimal.One;
        Occurs flags = Occurs.None;
        
        /// <include file='doc\XmlSchemaParticle.uex' path='docs/doc[@for="XmlSchemaParticle.MinOccursString"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("minOccurs")]
        public string MinOccursString {
            get { 
                return (flags & Occurs.Min) == 0 ? null : XmlConvert.ToString(minOccurs); 
            }
            set {
                if (value == null) {
                    minOccurs = decimal.One;
                    flags &= ~Occurs.Min;
                }
                else {
                    minOccurs = XmlConvert.ToInteger(value);
                    if (minOccurs < decimal.Zero) {
                        throw new XmlSchemaException(Res.Sch_MinOccursInvalidXsd);
                    }
                    flags |= Occurs.Min;
                }
            }
        }
        
        /// <include file='doc\XmlSchemaParticle.uex' path='docs/doc[@for="XmlSchemaParticle.MaxOccursString"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("maxOccurs")]
        public string MaxOccursString {
            get { 
                return  (flags & Occurs.Max) == 0 ? null : (maxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(maxOccurs); 
            }
            set {
                if (value == null) {
                    maxOccurs = decimal.One;
                    flags &= ~Occurs.Max;
                }
                else {
                    if (value == "unbounded") {
                        maxOccurs = decimal.MaxValue;
                    }
                    else {
                        maxOccurs = XmlConvert.ToInteger(value); 
                        if (maxOccurs < decimal.Zero) {
                            throw new XmlSchemaException(Res.Sch_MaxOccursInvalidXsd);
                        }
                        else if (maxOccurs == decimal.Zero && (flags & Occurs.Min) == 0) {
                            minOccurs = decimal.Zero;
                        }
                    }
                    flags |= Occurs.Max;
                }
            }
        }
        
        /// <include file='doc\XmlSchemaParticle.uex' path='docs/doc[@for="XmlSchemaParticle.MinOccurs"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public decimal MinOccurs {
            get { return minOccurs; }
            set {
                if (value < decimal.Zero || value != decimal.Truncate(value)) {
                    throw new XmlSchemaException(Res.Sch_MinOccursInvalidXsd);
                }
                minOccurs = value; 
                flags |= Occurs.Min;
            }
        }
        
        /// <include file='doc\XmlSchemaParticle.uex' path='docs/doc[@for="XmlSchemaParticle.MaxOccurs"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public decimal MaxOccurs {
            get { return maxOccurs; }
            set { 
                if (value < decimal.Zero || value != decimal.Truncate(value)) {
                    throw new XmlSchemaException(Res.Sch_MaxOccursInvalidXsd);
                }
                maxOccurs = value; 
                if (maxOccurs == decimal.Zero && (flags & Occurs.Min) == 0) {
                    minOccurs = decimal.Zero;
                }
                flags |= Occurs.Max;
            }
        }

        internal virtual bool IsEmpty {
            get { return maxOccurs == decimal.Zero; }
        } 

        internal bool IsMultipleOccurrence {
            get { return maxOccurs > decimal.One; }
        }
        
        class EmptyParticle : XmlSchemaParticle {
            internal override bool IsEmpty {
                get { return true; }
            } 
        }

        internal static readonly XmlSchemaParticle Empty = new EmptyParticle();
    }
}
