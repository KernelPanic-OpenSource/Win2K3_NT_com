//------------------------------------------------------------------------------
// <copyright file="FOrExpr.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

#if StreamFilter
namespace System.Xml.XPath {
    internal sealed class FOrExpr :IFQuery {
        private IFQuery _opnd1;
        private IFQuery _opnd2;

        FOrExpr() {
        }
        internal FOrExpr( IFQuery  opnd1, IFQuery  opnd2) {
            _opnd1 = opnd1;
            _opnd2 = opnd2;
        }
        internal override  bool MatchNode( XmlReader  qyContext) {
            if (_opnd1.MatchNode(qyContext))
                return true;
            return _opnd2.MatchNode(qyContext);
        }

        internal override  XPathResultType ReturnType() {
            return XPathResultType.Boolean;
        }
    }
}

#endif
