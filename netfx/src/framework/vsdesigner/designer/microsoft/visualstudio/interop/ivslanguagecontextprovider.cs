//------------------------------------------------------------------------------
/// <copyright file="IVsLanguageContextProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsLanguageContextProvider.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;

    [ComImport, ComVisible(true),Guid("19404D57-F8E4-42F4-9255-B8F889B0C50C"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsLanguageContextProvider {
        /*
        ProvideContext return values: 
            - S_OK: the implementor has added whatever context should be added
            - any other success code: forbidden (caller will assert)
            - any failure code: means the implementor is "passing" on this opportunity to provide context and the text editor will fall back to other mechanisms
        */

        [PreserveSig]
            int UpdateLanguageContext(
            _LanguageContextHint dwHint,
            IVsTextLines buffer,
            _TextSpan pTextSpan,
            IVsUserContext pUC);

    };
}

