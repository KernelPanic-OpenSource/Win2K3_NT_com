//------------------------------------------------------------------------------
/// <copyright file="IDebugProperty.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IDebugProperty.cs
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

    [ComImport(),System.Runtime.InteropServices.Guid("51973C50-CB0C-11D0-B5C9-00A0244A0E7A"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDebugProperty {

    	
    	 void GetPropertyInfo(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int dwFieldSpec,
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int nRadix,
    		[Out] 
    		  Microsoft.VisualStudio.Interop.Debugger.tagDebugPropertyInfo pPropertyInfo);

    	
    	 void GetExtendedInfo(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int cInfos,
    		[System.Runtime.InteropServices.In] 
    		  ref Guid rgguidExtendedInfo,
    		[In, Out] 
    		  ref Object rgvar);

    	
    	 void SetValueAsString(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.BStr)] 
    		  string pszValue,
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int nRadix);

    	
    	 void EnumMembers(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int dwFieldSpec,
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int nRadix,
    		[System.Runtime.InteropServices.In] 
    		  ref Guid refiid,
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   Microsoft.VisualStudio.Interop.Debugger.IEnumDebugPropertyInfo[] ppepi);

    	
    	 void GetParent(
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   Microsoft.VisualStudio.Interop.Debugger.IDebugProperty[] ppDebugProp);


    }
}
