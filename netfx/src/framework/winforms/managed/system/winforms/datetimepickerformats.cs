//------------------------------------------------------------------------------
// <copyright file="DateTimePickerFormats.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms {

    using System.Diagnostics;

    using System;
    using System.ComponentModel;
    using System.Drawing;
    using Microsoft.Win32;

    /// <include file='doc\DateTimePickerFormats.uex' path='docs/doc[@for="DateTimePickerFormat"]/*' />
    /// <devdoc>
    ///      Constants that specify how the date and time picker control displays
    ///      date and time information.
    /// </devdoc>
    public enum DateTimePickerFormat {

        /// <include file='doc\DateTimePickerFormats.uex' path='docs/doc[@for="DateTimePickerFormat.Long"]/*' />
        /// <devdoc>
        ///     Long format - produces output in the form "Wednesday, April 7, 1999"
        /// </devdoc>
        Long    = 0x0001,

        /// <include file='doc\DateTimePickerFormats.uex' path='docs/doc[@for="DateTimePickerFormat.Short"]/*' />
        /// <devdoc>
        ///     Short format - produces output in the form "4/7/99"
        /// </devdoc>
        Short   = 0x0002,

        /// <include file='doc\DateTimePickerFormats.uex' path='docs/doc[@for="DateTimePickerFormat.Time"]/*' />
        /// <devdoc>
        ///     Time format - produces output in time format
        /// </devdoc>
        Time    = 0x0004,

        /// <include file='doc\DateTimePickerFormats.uex' path='docs/doc[@for="DateTimePickerFormat.Custom"]/*' />
        /// <devdoc>
        ///     Custom format - produces output in custom format.
        /// </devdoc>
        Custom  = 0x0008,

    }
}
