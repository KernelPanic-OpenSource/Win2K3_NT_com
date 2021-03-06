//------------------------------------------------------------------------------
// <copyright file="InternalBufferOverflowException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.IO {

    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System;

    /// <include file='doc\InternalBufferOverflowException.uex' path='docs/doc[@for="InternalBufferOverflowException"]/*' />
    /// <devdoc>
    ///    <para>The exception that is thrown when the internal buffer overflows.</para>
    /// </devdoc>
    [Serializable]
    public class InternalBufferOverflowException : SystemException {
        /// <include file='doc\InternalBufferOverflowException.uex' path='docs/doc[@for="InternalBufferOverflowException.InternalBufferOverflowException"]/*' />
        /// <devdoc>
        /// <para>Initializes a new default instance of the <see cref='System.IO.InternalBufferOverflowException'/> class.</para>
        /// </devdoc>
        public InternalBufferOverflowException() : base() {
            HResult = HResults.InternalBufferOverflow;
        }

        /// <include file='doc\InternalBufferOverflowException.uex' path='docs/doc[@for="InternalBufferOverflowException.InternalBufferOverflowException1"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.IO.InternalBufferOverflowException'/> class with the error
        ///    message to be displayed
        ///    specified.</para>
        /// </devdoc>
        public InternalBufferOverflowException(string message) : base(message) {
            HResult =HResults.InternalBufferOverflow;
        }

        /// <include file='doc\InternalBufferOverflowException.uex' path='docs/doc[@for="InternalBufferOverflowException.InternalBufferOverflowException2"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.IO.InternalBufferOverflowException'/>
        /// class with the message to be displayed and the generated inner exception specified.</para>
        /// </devdoc>
        public InternalBufferOverflowException(string message, Exception inner) : base(message, inner) {
            HResult = HResults.InternalBufferOverflow;
        }
        
        /// <internalonly/>             
        protected InternalBufferOverflowException(SerializationInfo info, StreamingContext context) : base (info, context) {            
        }

    }
}
