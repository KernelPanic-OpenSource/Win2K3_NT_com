//------------------------------------------------------------------------------
// <copyright file="AvtEvent.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Xsl {
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Text;

    internal sealed class AvtEvent : TextEvent {
        private int key;

        public AvtEvent(int key) {
            Debug.Assert(key != Compiler.InvalidQueryKey);
            this.key = key;
        }

        public override bool Output(Processor processor, ActionFrame frame) {
            Debug.Assert(key != Compiler.InvalidQueryKey);
            return processor.TextEvent(processor.EvaluateString(frame, this.key));
        }

        public override string Evaluate(Processor processor, ActionFrame frame) {
            return processor.EvaluateString(frame, this.key);
        }
    }
}
