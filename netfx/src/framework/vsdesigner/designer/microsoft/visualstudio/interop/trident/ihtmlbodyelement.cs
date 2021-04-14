//------------------------------------------------------------------------------
/// <copyright file="IHTMLBodyElement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// Microsoft.VisualStudio.Interop.Trident.IHTMLBodyElement.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Trident {
    
    using System;
    using System.Runtime.InteropServices;

    [ComVisible(true),Guid("3050F1D8-98B5-11CF-BB82-00AA00BDCE0B"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    internal interface IHTMLBodyElement {

    	
    	 void SetBackground(
    		[In,MarshalAs(UnmanagedType.BStr)]
    		  string p);

    	[return: MarshalAs(UnmanagedType.BStr)]
    	  string GetBackground();

    	
    	 void SetBgProperties(
    		[In,MarshalAs(UnmanagedType.BStr)]
    		  string p);

    	[return: MarshalAs(UnmanagedType.BStr)]
    	  string GetBgProperties();

    	
    	 void SetLeftMargin(
    		
    		  Object p);

    	
    	 Object GetLeftMargin();

    	
    	 void SetTopMargin(
    		
    		  Object p);

    	
    	 Object GetTopMargin();

    	
    	 void SetRightMargin(
    		
    		  Object p);

    	
    	 Object GetRightMargin();

    	
    	 void SetBottomMargin(
    		
    		  Object p);

    	
    	 Object GetBottomMargin();

    	
    	 void SetNoWrap(
    		
    		 bool p);

    	
    	 bool GetNoWrap();

    	
    	 void SetBgColor(
    		
    		  Object p);

    	
    	 Object GetBgColor();

    	
    	 void SetText(
    		
    		  Object p);

    	
    	 Object GetText();

    	
    	 void SetLink(
    		
    		  Object p);

    	
    	 Object GetLink();

    	
    	 void SetVLink(
    		
    		  Object p);

    	
    	 Object GetVLink();

    	
    	 void SetALink(
    		
    		  Object p);

    	
    	 Object GetALink();

    	
    	 void SetOnload(
    		
    		  Object p);

    	
    	 Object GetOnload();

    	
    	 void SetOnunload(
    		
    		  Object p);

    	
    	 Object GetOnunload();

    	
    	 void SetScroll(
    		[In,MarshalAs(UnmanagedType.BStr)]
    		  string p);

    	[return: MarshalAs(UnmanagedType.BStr)]
    	  string GetScroll();

    	
    	 void SetOnselect(
    		
    		  Object p);

    	
    	 Object GetOnselect();

    	
    	 void SetOnbeforeunload(
    		
    		  Object p);

    	
    	 Object GetOnbeforeunload();

    	[return: MarshalAs(UnmanagedType.Interface)]
    	  object CreateTextRange();

    }
}
