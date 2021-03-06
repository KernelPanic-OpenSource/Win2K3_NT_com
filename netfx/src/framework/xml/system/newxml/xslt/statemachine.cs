//------------------------------------------------------------------------------
// <copyright file="StateMachine.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Xsl {
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal class StateMachine {
        // Constants for the state table
        private  const int Init         = 0x000000;       // Initial state
        private  const int Elem         = 0x000001;       // Element was output
        private  const int AttrN        = 0x000002;       // Attribute name was output
        private  const int AttrV        = 0x000003;       // Attribute value was output (some more can follow)
        private  const int Attr         = 0x000004;       // Attribute was output
        private  const int InElm        = 0x000005;       // Filling in element, general state text
        private  const int EndEm        = 0x000006;       // After end element event - next end element doesn't generate token
        private  const int InCmt        = 0x000007;       // Adding text to a comment
        private  const int InPI         = 0x000008;       // Adding text to a processing instruction

        private  const int StateMask    = 0x00000F;       // State mask

        internal const int Error        = 0x000010;       // Invalid XML state

        private  const int Ignor        = 0x000020;       // Ignore this transition
        private  const int Assrt        = 0x000030;       // Assrt

        private  const int U            = 0x000100;       // Depth up
        private  const int D            = 0x000200;       // Depth down

        internal const int DepthMask    = 0x000300;       // Depth mask

        internal const int DepthUp      = U;
        internal const int DepthDown    = D;

        private  const int C            = 0x000400;       // BeginChild
        private  const int H            = 0x000800;       // HadChild
        private  const int M            = 0x001000;       // EmptyTag

        internal const int BeginChild   = C;
        internal const int HadChild     = H;
        internal const int EmptyTag     = M;

        private  const int B            = 0x002000;       // Begin Record
        private  const int E            = 0x004000;       // Record finished

        internal const int BeginRecord  = B;
        internal const int EndRecord    = E;

        private  const int S            = 0x008000;       // Push namespace scope
        private  const int P            = 0x010000;       // Pop current namepsace scope

        internal const int PushScope    = S;
        internal const int PopScope     = P;              // Next event must pop scope

        //
        // Runtime state
        //

        private int _State;

        internal StateMachine() {
            _State = Init;
        }

        internal int State {
            get {
                return _State;
            }

            set {
                // Hope you know what you are doing ...
                _State = value;
            }
        }

        internal void Reset() {
            _State = Init;
        }

        internal static int StateOnly(int state) {
            return state & StateMask;
        }

        internal int BeginOutlook(XPathNodeType nodeType) {
            int newState = s_BeginTransitions[(int)nodeType, _State];
            Debug.Assert(newState != Assrt);
            return newState;
        }

        internal int Begin(XPathNodeType nodeType) {
            int newState = s_BeginTransitions[(int)nodeType, _State];
            Debug.Assert(newState != Assrt);

            if (newState != Error && newState != Ignor) {
                _State = newState & StateMask;
            }
            return newState;
        }

        internal int EndOutlook(XPathNodeType nodeType) {
            int newState = s_EndTransitions[(int)nodeType, _State];
            Debug.Assert(newState != Assrt);
            return newState;
        }

        internal int End(XPathNodeType nodeType) {
            int newState = s_EndTransitions[(int)nodeType, _State];
            Debug.Assert(newState != Assrt);

            if (newState != Error && newState != Ignor) {
                _State = newState & StateMask;
            }
            return newState;
        }

        private static readonly int [,] s_BeginTransitions = {
            /*                             { Init,      Elem,          AttrN,   AttrV,   Attr,             InElm,            EndEm,            InCmt,   InPI  }, */
            /* Root                     */ { Error,     Error,         Error,   Error,   Error,            Error,            Error,            Error,   Error },
            /* Element                  */ { Elem |B|S, Elem |U|C|B|S, Error,   Error,   Elem |C|B|S,      Elem |B|S,        Elem |B|P|S,      Error,   Error },
            /* Attribute                */ { Error,     AttrN|U,       Error,   Error,   AttrN,            Error,            Error,            Error,   Error },
            /* Namespace                */ { Error,     AttrN|U,       Error,   Error,   AttrN,            Error,            Error,            Error,   Error },
            /* Text                     */ { InElm|B,   InElm|U|C|B,   AttrV|U, AttrV,   InElm|C|B,        InElm,            InElm|B|P,        InCmt,   InPI  },
            /* SignificantWhitespace    */ { InElm|B,   InElm|U|C|B,   AttrV|U, AttrV,   InElm|C|B,        InElm,            InElm|B|P,        InCmt,   InPI  },
            /* Whitespace               */ { InElm|B,   InElm|U|C|B,   AttrV|U, AttrV,   InElm|C|B,        InElm,            InElm|B|P,        InCmt,   InPI  },
            /* ProcessingInstruction    */ { InPI |B,   InPI |U|C|B,   Error,   Error,   InPI |C|B,        InPI |B,          InPI |B|P,        Error,   Error },
            /* Comment                  */ { InCmt|B,   InCmt|U|C|B,   Error,   Error,   InCmt|C|B,        InCmt|B,          InCmt|B|P,        Error,   Error },
            /* All                      */ { Error,     Error,         Error,   Error,   Error,            Error,            Error,            Error,   Error },
        };                                                             
                                                                       
        private static readonly int [,] s_EndTransitions = {           
            /*                             { Init,      Elem,          AttrN,   AttrV,   Attr,             InElm,            EndEm,            InCmt,   InPI   }, */
            /* Root                     */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
            /* Element                  */ { Assrt,     EndEm|B|E|P|M, Assrt,   Assrt,   EndEm|D|B|E|P|M,  EndEm|D|H|B|E|P,  EndEm|D|H|B|E|P,  Assrt,   Assrt  },
            /* Attribute                */ { Assrt,     Assrt,         Attr,    Attr |D, Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
            /* Namespace                */ { Assrt,     Assrt,         Attr,    Attr |D, Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
            /* Text                     */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
            /* SignificantWhitespace    */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
            /* Whitespace               */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
            /* ProcessingInstruction    */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            Assrt,   EndEm|E},
            /* Comment                  */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            EndEm|E, Assrt  },
            /* All                      */ { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,            Assrt,            Assrt,            Assrt,   Assrt  },
	};
    }

}
