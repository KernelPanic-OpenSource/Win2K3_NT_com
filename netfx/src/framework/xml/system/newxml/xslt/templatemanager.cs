//------------------------------------------------------------------------------
// <copyright file="TemplateManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Xml.Xsl {
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Collections;

    internal class TemplateManager {
        private XmlQualifiedName mode;
        internal ArrayList       templates;
        private Stylesheet       stylesheet;    // Owning stylesheet

        private class TemplateComparer : IComparer {
            public int Compare(object x, object y) {
                Debug.Assert(x != null && x is TemplateAction);
                Debug.Assert(y != null && y is TemplateAction);

                TemplateAction tx = (TemplateAction) x;
                TemplateAction ty = (TemplateAction) y;

                Debug.Assert(! Double.IsNaN(tx.Priority));
                Debug.Assert(! Double.IsNaN(ty.Priority));

                if (tx.Priority == ty.Priority) {
                    Debug.Assert(tx.TemplateId != ty.TemplateId || tx == ty);
                    return tx.TemplateId - ty.TemplateId;
                }
                else {
                    return tx.Priority > ty.Priority ? 1 : -1;
                }
            }
        }

        private static TemplateComparer s_TemplateComparer = new TemplateComparer();

        internal XmlQualifiedName Mode {
            get { return this.mode; }
        }

        internal TemplateManager(Stylesheet stylesheet, XmlQualifiedName mode) {
            this.mode       = mode;
            this.stylesheet = stylesheet;
        }

        internal void AddTemplate(TemplateAction template) {
            Debug.Assert(template != null);
            Debug.Assert(
                ((object) this.mode == (object) template.Mode) ||
                (template.Mode == null && this.mode.Equals(XmlQualifiedName.Empty)) ||
                this.mode.Equals(template.Mode)
            );

            if (this.templates == null) {
                this.templates = new ArrayList();
            }

            Debug.Assert(this.templates != null);
            Debug.WriteLine("Adding Template #" + this.templates.Count + ", mode==\"" + template.Mode + "\" match=\"" + template.Match + "\"");

            this.templates.Add(template);
        }

        internal void ProcessTemplates() {
            if (this.templates != null) {
                this.templates.Sort(s_TemplateComparer);
            }
        }

        internal TemplateAction FindTemplate(Processor processor, XPathNavigator navigator) {
            if (this.templates == null) {
                return null;
            }

            Debug.Assert(this.templates != null);
            for (int templateIndex = this.templates.Count - 1; templateIndex >= 0 ; templateIndex --) {
                TemplateAction action = (TemplateAction) this.templates[templateIndex];
                int matchKey = action.MatchKey;

                if (matchKey != Compiler.InvalidQueryKey) {
                    if (processor.Matches(navigator, matchKey)) {
                        return action;
                    }
                }
            }

            Debug.WriteLine("Template not found");

            return null;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        internal void Trace(int tab) {
            Debug.Indent(tab);
            Debug.WriteLine("Template manager for mode \"" + this.mode + "\"");
            Debug.WriteLine("templates :");

            if (this.templates != null) {
                for (int template = 0; template < this.templates.Count; template ++) {
                    TemplateAction action = (TemplateAction) this.templates[template];
                    action.Trace(tab + 1);
                }
            }
        }
    }
}
