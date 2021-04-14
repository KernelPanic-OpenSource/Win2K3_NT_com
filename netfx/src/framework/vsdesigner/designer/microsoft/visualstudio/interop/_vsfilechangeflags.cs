//------------------------------------------------------------------------------
/// <copyright file="_VSFILECHANGEFLAGS.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// _VSFILECHANGEFLAGS.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Diagnostics;
    using System;
    
    using UnmanagedType = System.Runtime.InteropServices.UnmanagedType;

    [CLSCompliantAttribute(false)]
    internal class  _VSFILECHANGEFLAGS {

    	public const   int VSFILECHG_Attr = 0x1;
    	public const   int VSFILECHG_Time = 0x2;
    	public const   int VSFILECHG_Size = 0x4;
    	public const   int VSFILECHG_Del = 0x8;
    	public const   int VSFILECHG_Add = 0x10;
    }
}
