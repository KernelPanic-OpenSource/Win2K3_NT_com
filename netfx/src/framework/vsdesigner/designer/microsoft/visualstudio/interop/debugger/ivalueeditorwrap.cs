//------------------------------------------------------------------------------
/// <copyright file="IValueEditorWrap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IValueEditorWrap.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Debugger {
    using System.Runtime.InteropServices;

    using System.Diagnostics;
    using System;
    
    using UnmanagedType = System.Runtime.InteropServices.UnmanagedType;

    [ComImport(),System.Runtime.InteropServices.Guid("C94D7A8A-5F06-11D2-B755-00C04F79E479"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IValueEditorWrap {

    	
    	 void GetStyle(
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		  int[] dwStyle);

    	
    	 void GetValues(
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   Microsoft.VisualStudio.Interop.Debugger.IEnumValues[] pIEnum);

    	
    	 void EditValue(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  Microsoft.VisualStudio.Interop.Debugger.IValueAccessWrap pIValueAccessWrap);

    	
    	 void PaintValue(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)] 
    		 int hdc,
    		[System.Runtime.InteropServices.In] 
    		  Microsoft.VisualStudio.Interop.Debugger.tagRECT rect,
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  object pIDispatchValue);

    	
    	 void ViewValue(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  object pIDispatchValue);

    	
    	 void GetTextFromValue(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  object pIDispatchValue,
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   String[] pbStrValue);

    	
    	 void GetValueFromText(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.BStr)] 
    		  string bstrValue,
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   Object[] pIDispatchValue);

    	
    	 void PaintValueEx(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)] 
    		 int hdc,
    		[System.Runtime.InteropServices.In] 
    		  Microsoft.VisualStudio.Interop.Debugger.tagRECT rect,
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  Microsoft.VisualStudio.Interop.Debugger.IValueAccessWrap pIValueAccessWrap);

    	
    	 void ViewValueEx(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  Microsoft.VisualStudio.Interop.Debugger.IValueAccessWrap pIValueAccessWrap);

    	
    	 void GetTextFromValueEx(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.Interface)] 
    		  Microsoft.VisualStudio.Interop.Debugger.IValueAccessWrap pIValueAccessWrap,
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   String[] pbStrValue);


    }
}
