//------------------------------------------------------------------------------
/// <copyright file="IHTMLElementCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// Microsoft.VisualStudio.Interop.Trident.IHTMLElementCollection.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Trident {
    
    using System;
    using System.Runtime.InteropServices;

    [ComVisible(true),Guid("3050F21F-98B5-11CF-BB82-00AA00BDCE0B"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    internal interface IHTMLElementCollection {

    	[return: MarshalAs(UnmanagedType.BStr)]
    	  string toString();

    	
    	 void SetLength(
    		 int p);

    	 int GetLength();

    	[return: MarshalAs(UnmanagedType.Interface)]
    	  object Get_newEnum();

    	[return: MarshalAs(UnmanagedType.Interface)]
    	  IHTMLElement Item(
    		  object name,
    		  object index);

    	[return: MarshalAs(UnmanagedType.Interface)]
    	  object Tags(
    		  object tagName);

    }
}