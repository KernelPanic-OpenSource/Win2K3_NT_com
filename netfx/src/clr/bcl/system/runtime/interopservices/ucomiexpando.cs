// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*==========================================================================
**
** Interface:  UCOMIExpando
**
** Author: David Mortenson (DMortens)
**
** Purpose: 
** This interface is redefined here since the original IExpando interface 
** has all its methods marked as ecall's since it is a managed standard 
** interface. This interface is used from within the runtime to make a call 
** on the COM server directly when it implements the IExpando interface.
**
** Date:  January 18, 2000
** 
==========================================================================*/
namespace System.Runtime.InteropServices {
    
    using System;
    using System.Reflection;

    [Guid("AFBF15E6-C37C-11d2-B88E-00A0C9B471B8")]    
    internal interface UCOMIExpando : UCOMIReflect
    {
        FieldInfo AddField(String name);
        PropertyInfo AddProperty(String name);
        MethodInfo AddMethod(String name, Delegate method);
        void RemoveMember(MemberInfo m);
    }
}
