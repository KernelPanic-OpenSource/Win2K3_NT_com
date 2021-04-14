//------------------------------------------------------------------------------
// <copyright file="CounterNameConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Diagnostics.Design {
    using System.Runtime.Serialization.Formatters;
    using System.Globalization;
    using System.Diagnostics;
    using System;
    using System.Windows.Forms.ComponentModel;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Collections;
    
    internal class CounterNameConverter : TypeConverter {
        
        /// <include file='doc\CounterNameConverter.uex' path='docs/doc[@for="CounterNameConverter.CounterNameConverter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the 'CounterNameConverter' class for the given type.
        ///    </para>
        /// </devdoc>
        public CounterNameConverter() {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                string text = ((string)value).Trim();
                return text;
            }
            return base.ConvertFrom(context, culture, value);
        }                 
         
        /// <include file='doc\CounterNameConverter.uex' path='docs/doc[@for="CounterNameConverter.GetStandardValues"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a collection of standard values for the data type this validator is
        ///       designed for.</para>
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {        
            PerformanceCounter counter = (context == null) ? null : context.Instance as PerformanceCounter;

            string machineName = ".";
            string categoryName = String.Empty;
            if (counter != null) {
                machineName = counter.MachineName;
                categoryName = counter.CategoryName;
            }

            try {
                PerformanceCounterCategory cat = new PerformanceCounterCategory(categoryName, machineName);
                string[] instanceNames = cat.GetInstanceNames();
                PerformanceCounter[] counters = null;
                if (instanceNames.Length == 0)
                    counters = cat.GetCounters();
                else
                    counters = cat.GetCounters(instanceNames[0]);                                            

                string[] retVal = new string[counters.Length];

                for (int i = 0; i < counters.Length; i++) {
                    retVal[i] = counters[i].CounterName;
                }

                Array.Sort(retVal, Comparer.Default);
                return new StandardValuesCollection(retVal);
            }
            catch (Exception) {
                ; // do nothing
            }
            
            return null;
        }

        /// <include file='doc\CounterNameConverter.uex' path='docs/doc[@for="CounterNameConverter.GetStandardValuesSupported"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a value indicating
        ///       whether this object
        ///       supports a standard set of values that can be picked
        ///       from a list using the specified context.</para>
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }
    }
}
