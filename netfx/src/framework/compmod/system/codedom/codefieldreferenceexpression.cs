//------------------------------------------------------------------------------
// <copyright file="CodeFieldReferenceExpression.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.CodeDom {

    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Runtime.InteropServices;

    /// <include file='doc\CodeFieldReferenceExpression.uex' path='docs/doc[@for="CodeFieldReferenceExpression"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents a reference to a field.
    ///    </para>
    /// </devdoc>
    [
        ClassInterface(ClassInterfaceType.AutoDispatch),
        ComVisible(true),
        Serializable,
    ]
    public class CodeFieldReferenceExpression : CodeExpression {
        private CodeExpression targetObject;
        private string fieldName;

        /// <include file='doc\CodeFieldReferenceExpression.uex' path='docs/doc[@for="CodeFieldReferenceExpression.CodeFieldReferenceExpression"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeFieldReferenceExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeFieldReferenceExpression() {
        }

        /// <include file='doc\CodeFieldReferenceExpression.uex' path='docs/doc[@for="CodeFieldReferenceExpression.CodeFieldReferenceExpression1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeFieldReferenceExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeFieldReferenceExpression(CodeExpression targetObject, string fieldName) {
            TargetObject = targetObject;
            FieldName = fieldName;
        }

        /// <include file='doc\CodeFieldReferenceExpression.uex' path='docs/doc[@for="CodeFieldReferenceExpression.TargetObject"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the target object.
        ///    </para>
        /// </devdoc>
        public CodeExpression TargetObject {
            get {
                return targetObject;
            }
            set {
                targetObject = value;
            }
        }

        /// <include file='doc\CodeFieldReferenceExpression.uex' path='docs/doc[@for="CodeFieldReferenceExpression.FieldName"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the field name.
        ///    </para>
        /// </devdoc>
        public string FieldName {
            get {
                return (fieldName == null) ? string.Empty : fieldName;
            }
            set {
                fieldName = value;
            }
        }
    }
}
