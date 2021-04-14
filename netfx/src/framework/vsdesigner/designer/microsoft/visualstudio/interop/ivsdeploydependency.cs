//------------------------------------------------------------------------------
/// <copyright file="IVsDeployDependency.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// IVsProject.cs
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

    [
    ComImport(),Guid("A086E870-AA0B-4EF9-8CF3-4A38267B9C7D"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
    CLSCompliant(false)
    ]
    interface IVsDeployDependency
    {
        void get_DeployDependencyURL(out string pbstrURL); // Location of dependency (local, UNC, or web)
    }
}

