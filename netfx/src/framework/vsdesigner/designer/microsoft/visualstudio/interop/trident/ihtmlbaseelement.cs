//------------------------------------------------------------------------------
/// <copyright file="IHTMLBaseElement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// Microsoft.VisualStudio.Interop.Trident.IHTMLBaseElement.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Trident {
    
    using System;
    using System.Runtime.InteropServices;

    [ComVisible(true),Guid("3050F204-98B5-11CF-BB82-00AA00BDCE0B"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    internal interface IHTMLBaseElement {

    	
    	 void SetHref(
    		[In,MarshalAs(UnmanagedType.BStr)] 
    		  string p);

    	[return: MarshalAs(UnmanagedType.BStr)]
    	  string GetHref();

    	
    	 void SetTarget(
    		[In,MarshalAs(UnmanagedType.BStr)] 
    		  string p);

    	[return: MarshalAs(UnmanagedType.BStr)]
    	  string GetTarget();

    }
}
