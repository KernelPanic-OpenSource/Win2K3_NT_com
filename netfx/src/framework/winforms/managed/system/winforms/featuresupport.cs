//------------------------------------------------------------------------------
// <copyright file="FeatureSupport.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms {
    using System.Configuration.Assemblies;

    using System.Diagnostics;

    using System;
    using System.Reflection;

    /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport"]/*' />
    /// <devdoc>
    /// <para>Provides <see langword='static'/> methods for retrieving feature information from the
    ///    current system.</para>
    /// </devdoc>
    public abstract class FeatureSupport : IFeatureSupport {

        /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport.IsPresent"]/*' />
        /// <devdoc>
        ///    <para>Determines whether any version of the specified feature
        ///       is installed in the system. This method is <see langword='static'/>.</para>
        /// </devdoc>
        public static bool IsPresent(string featureClassName, string featureConstName) {
            return IsPresent(featureClassName, featureConstName, new Version(0, 0, 0, 0));
        }

        /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport.IsPresent1"]/*' />
        /// <devdoc>
        ///    <para>Determines whether the specified or newer version of the specified feature is
        ///       installed in the system. This method is <see langword='static'/>.</para>
        /// </devdoc>
        public static bool IsPresent(string featureClassName, string featureConstName, Version minimumVersion) {
            object featureId = null;
            IFeatureSupport featureSupport = null;

            Type c = Type.GetType(featureClassName);
            if (c != null) {
                FieldInfo fi = c.GetField(featureConstName);

                if (fi != null) {
                    featureId = fi.GetValue(null);
                }
            }

            if (featureId != null) {
                featureSupport = (IFeatureSupport)Activator.CreateInstance(c);

                if (featureSupport != null) {
                    return featureSupport.IsPresent(featureId, minimumVersion);
                }
            }
            return false;
        }

        /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport.GetVersionPresent"]/*' />
        /// <devdoc>
        ///    <para>Gets the version of the specified feature that is available on the system.</para>
        /// </devdoc>
        public static Version GetVersionPresent(string featureClassName, string featureConstName) {
            object featureId = null;
            IFeatureSupport featureSupport = null;

            Type c = Type.GetType(featureClassName);
            if (c != null) {
                FieldInfo fi = c.GetField(featureConstName);

                if (fi != null) {
                    featureId = fi.GetValue(null);
                }
            }

            if (featureId != null) {
                featureSupport = (IFeatureSupport)Activator.CreateInstance(c);

                if (featureSupport != null) {
                    return featureSupport.GetVersionPresent(featureId);
                }
            }
            return null;
        }

        /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport.IsPresent2"]/*' />
        /// <devdoc>
        ///    <para>Determines whether any version of the specified feature
        ///       is installed in the system.</para>
        /// </devdoc>
        public virtual bool IsPresent(object feature) {
            return IsPresent(feature, new Version(0, 0, 0, 0));
        }

        /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport.IsPresent3"]/*' />
        /// <devdoc>
        ///    <para>Determines whether the specified or newer version of the
        ///       specified feature is installed in the system.</para>
        /// </devdoc>
        public virtual bool IsPresent(object feature, Version minimumVersion) {
            Version ver = GetVersionPresent(feature);

            if (ver != null) {
                return ver.CompareTo(minimumVersion) >= 0;
            }
            return false;
        }

        /// <include file='doc\FeatureSupport.uex' path='docs/doc[@for="FeatureSupport.GetVersionPresent1"]/*' />
        /// <devdoc>
        ///    <para>When overridden in a derived class, gets the version of the specified
        ///       feature that is available on the system.</para>
        /// </devdoc>
        public abstract Version GetVersionPresent(object feature);
    }


}
