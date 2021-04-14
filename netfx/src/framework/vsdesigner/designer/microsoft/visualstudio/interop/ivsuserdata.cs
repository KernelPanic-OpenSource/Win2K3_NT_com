//------------------------------------------------------------------------------
/// <copyright file="IVsUserData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsUserData.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {
    
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    
    [ComImport(),Guid("978A8E17-4DF8-432A-9623-D530A26452BC"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsUserData {
    	
    	 object GetData(
    		  ref Guid riidKey);

    	
    	 void SetData(
    		  ref Guid riidKey,
    		  object vtData);
    }
}
