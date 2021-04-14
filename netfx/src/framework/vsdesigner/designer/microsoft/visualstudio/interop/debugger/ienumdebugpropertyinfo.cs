//------------------------------------------------------------------------------
/// <copyright file="IEnumDebugPropertyInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IEnumDebugPropertyInfo.cs
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

    [ComImport(),System.Runtime.InteropServices.Guid("51973C51-CB0C-11D0-B5C9-00A0244A0E7A"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumDebugPropertyInfo {

    	
    	 void RemoteNext(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int celt,
    		[Out] 
    		  Microsoft.VisualStudio.Interop.Debugger.tagDebugPropertyInfo pinfo,
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		  int[] pcEltsfetched);

    	
    	 void RemoteNext(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int celt,
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.I4)] 
    		 int pinfo,
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		  int[] pcEltsfetched);

    	
    	 void Skip(
    		[System.Runtime.InteropServices.In,System.Runtime.InteropServices.MarshalAs(UnmanagedType.U4)] 
    		 int celt);

    	
    	 void Reset();

    	
    	 void Clone(
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		   Microsoft.VisualStudio.Interop.Debugger.IEnumDebugPropertyInfo[] ppepi);

    	
    	 void GetCount(
    		[System.Runtime.InteropServices.Out,System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] 
    		  int[] pcelt);


    }
}