//------------------------------------------------------------------------------
/// <copyright file="__CommitChangeGestureFlags.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// __FRAMESHOW.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999; Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop {

    using System.Diagnostics;
    using System;
    
    using UnmanagedType = System.Runtime.InteropServices.UnmanagedType;

    [CLSCompliantAttribute(false)]
    internal class __ChangeCommitGestureFlags
    {
        public const int CCG_SAVE                        = 0x0001;
        public const int CCG_MULTILINE_CHANGE            = 0x0002;
        public const int CCG_CARET_ON_NEW_BUFFER_LINE    = 0x0004;
        public const int CCG_MASS_REPLACE                = 0x0008;
        public const int CCG_ENTER_COMMAND               = 0x0010;
        public const int CCG_FIND_STARTING               = 0x0020;
    } 
    
}