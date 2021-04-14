// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly:AssemblyTitle("System.Management")]
[assembly:AssemblyDescription("This assembly contains the classes necessary to access management information from managed code")]
[assembly:AssemblyConfiguration("")]
[assembly:AssemblyCompany("Microsoft")]
[assembly:AssemblyProduct("WMI")]
[assembly:AssemblyCopyright("1995-2001")]
[assembly:AssemblyTrademark("")]
[assembly:AssemblyCulture("")]		

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Revision
//      Build Number
//
// You can specify all the value or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly:AssemblyVersion("1.0.5000.0")]

//
// In order to sign your assembly you must specify a key to use. Refer to the 
// COM+ 2.0 documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified - the assembly cannot be signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. 
//   (*) If the key file and a key name attributes are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP - that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the file is installed into the CSP and used.
//   (*) Delay Signing is an advanced option - see the COM+ 2.0 documentation for 
//       more information on this.
//
[assembly:AssemblyDelaySign(true)]
[assembly:AssemblyKeyFile("FinalPublicKey.snk")]
[assembly:AssemblyKeyName("")]

[assembly:CLSCompliant(true)]

// Do not surface these APIs to class COM clients
[assembly:ComVisible(false)]

// Request permissions up-front to avoid any code running if the client doesn't have enough permissions
[assembly:SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode=true)]