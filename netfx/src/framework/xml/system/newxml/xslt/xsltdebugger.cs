//------------------------------------------------------------------------------
// <copyright file="XsltDebugger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Xsl.Debugger {
    using System;
    using System.Xml;
    using System.Xml.XPath;
    
    internal interface IStackFrame {
        XPathNavigator    Instruction     { get; }
        XPathNodeIterator NodeSet         { get; }
        // Variables:
        int               GetVariablesCount();
        XPathNavigator    GetVariable(int varIndex);
        object            GetVariableValue(int varIndex);
    }

    internal interface IXsltProcessor {
        int         StackDepth { get; }
        IStackFrame GetStackFrame(int depth);
    }

    internal interface IXsltDebugger {
        string GetBuiltInTemplatesUri();
        void   OnInstructionCompile(XPathNavigator styleSheetNavigator);
        void   OnInstructionExecute(IXsltProcessor xsltProcessor);
    }
}
