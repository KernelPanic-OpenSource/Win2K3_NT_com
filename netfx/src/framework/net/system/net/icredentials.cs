//------------------------------------------------------------------------------
// <copyright file="ICredentials.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


namespace System.Net {

    //using System;
    //using System.Net;
    using System.Runtime.InteropServices;

    //
    // This is an extensible interface that authenticators
    // must implement to support credential lookup.
    // During execution of the protocol, if authentication
    // information is needed the GetCredential function will
    // be called with the host and realm information.
    //

    /// <include file='doc\ICredentials.uex' path='docs/doc[@for="ICredentials"]/*' />
    /// <devdoc>
    ///    <para>Provides the base authentication interface for Web client authentication.</para>
    /// </devdoc>
    public interface ICredentials {
        /// <include file='doc\ICredentials.uex' path='docs/doc[@for="ICredentials.GetCredential"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns a NetworkCredential object that
        ///       is associated with the supplied host, realm, and authentication type.
        ///    </para>
        /// </devdoc>

        //
        // CONVENTION:
        // returns null if no information is available
        // for the specified host&realm
        //
        NetworkCredential GetCredential(Uri uri, String authType);

    } // interface ICredentials


} // namespace System.Net
