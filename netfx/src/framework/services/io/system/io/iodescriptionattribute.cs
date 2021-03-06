//------------------------------------------------------------------------------
// <copyright file="IODescriptionAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.IO {


    using System;
    using System.ComponentModel;   

    /// <include file='doc\IODescriptionAttribute.uex' path='docs/doc[@for="IODescriptionAttribute"]/*' />
    /// <devdoc>
    ///     DescriptionAttribute marks a property, event, or extender with a
    ///     description. Visual designers can display this description when referencing
    ///     the member.
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public class IODescriptionAttribute : DescriptionAttribute {

        private bool replaced = false;

        /// <include file='doc\IODescriptionAttribute.uex' path='docs/doc[@for="IODescriptionAttribute.IODescriptionAttribute"]/*' />
        /// <devdoc>
        ///     Constructs a new sys description.
        /// </devdoc>
        public IODescriptionAttribute(string description) : base(description) {
        }

        /// <include file='doc\IODescriptionAttribute.uex' path='docs/doc[@for="IODescriptionAttribute.Description"]/*' />
        /// <devdoc>
        ///     Retrieves the description text.
        /// </devdoc>
        public override string Description {
            get {
                if (!replaced) {
                    replaced = true;
                    DescriptionValue = SR.GetString(base.Description);
                }
                return base.Description;
            }
        }
    }
}
