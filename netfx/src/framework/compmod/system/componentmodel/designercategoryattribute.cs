//------------------------------------------------------------------------------
// <copyright file="DesignerCategoryAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.ComponentModel {
    
    using System;
    using System.ComponentModel;

    /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute"]/*' />
    /// <devdoc>
    ///    <para>Specifies that the designer for a class belongs to a certain
    ///       category.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed class DesignerCategoryAttribute : Attribute {
        private string category;
        private string typeId;
        
        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.Component"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category uses a
        ///       component designer. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Component = new DesignerCategoryAttribute("Component");
        
        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.Default"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category cannot use a visual
        ///       designer. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Default = new DesignerCategoryAttribute();
        
        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.Form"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category uses a form designer.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Form = new DesignerCategoryAttribute("Form");
        
        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.Generic"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category uses a generic designer.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Generic = new DesignerCategoryAttribute("Designer");
        
        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.DesignerCategoryAttribute"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with the
        ///       default category.
        ///    </para>
        /// </devdoc>
        public DesignerCategoryAttribute() {
            category = string.Empty;
        }

        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.DesignerCategoryAttribute1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with
        ///       the given category name.
        ///    </para>
        /// </devdoc>
        public DesignerCategoryAttribute(string category) {
            this.category = category;
        }

        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.Category"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the name of the category.
        ///    </para>
        /// </devdoc>
        public string Category {
            get {
                return category;
            }
        }

        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.TypeId"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       This defines a unique ID for this attribute type. It is used
        ///       by filtering algorithms to identify two attributes that are
        ///       the same type. For most attributes, this just returns the
        ///       Type instance for the attribute. DesignerAttribute overrides
        ///       this to include the name of the category
        ///    </para>
        /// </devdoc>
        public override object TypeId {
            get {
                if (typeId == null) {
                    typeId = GetType().FullName + Category;
                }
                return typeId;
            }
        }


        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.Equals"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        /// <internalonly/>
        public override bool Equals(object obj){
            if (obj == this) {
                return true;
            }

            DesignerCategoryAttribute other = obj as DesignerCategoryAttribute;
            return (other != null) && other.category == category;
        }
        
        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.GetHashCode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode() {
            return category.GetHashCode();
        }

        /// <include file='doc\DesignerCategoryAttribute.uex' path='docs/doc[@for="DesignerCategoryAttribute.IsDefaultAttribute"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        /// <internalonly/>
        public override bool IsDefaultAttribute() {
            return category.Equals(Default.Category);
        }
    }
}

