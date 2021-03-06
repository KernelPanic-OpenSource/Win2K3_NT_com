//------------------------------------------------------------------------------
// <copyright file="UInt64Storage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Data.Common {
    using System;
    using System.Xml;

    /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage"]/*' />
    /// <internalonly/>
    [
    Serializable,
    CLSCompliantAttribute(false)
    ]
    internal class UInt64Storage : DataStorage {

        private static readonly UInt64 defaultValue = UInt64.MinValue;
        static private readonly Object defaultValueAsObject = defaultValue;

        private UInt64[] values;

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.UInt64Storage"]/*' />
        /// <internalonly/>
        public UInt64Storage()
        : base(typeof(UInt64)) {
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.DefaultValue"]/*' />
        /// <internalonly/>
        public override Object DefaultValue {
            get {
                return defaultValueAsObject;
            }
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.Aggregate"]/*' />
        /// <internalonly/>
        override public Object Aggregate(int[] records, AggregateType kind) {
            bool hasData = false;
            try {
                switch (kind) {
                    case AggregateType.Sum:
                        UInt64 sum = defaultValue;
                        foreach (int record in records) {
                            if (IsNull(record))
                                continue;
                            checked { sum += values[record];}
                            hasData = true;
                        }
                        if (hasData) {
                            return sum;
                        }
                        return DBNull.Value;

                    case AggregateType.Mean:
                        Decimal meanSum = (Decimal)defaultValue;
                        int meanCount = 0;
                        foreach (int record in records) {
                            if (IsNull(record))
                                continue;
                            checked { meanSum += (Decimal)values[record];}
                            meanCount++;
                            hasData = true;
                        }
                        if (hasData) {
                            UInt64 mean;
                            checked {mean = (UInt64)(Decimal)(meanSum / (Decimal)meanCount);}
                            return mean;
                        }
                        return DBNull.Value;

                    case AggregateType.Var:
                    case AggregateType.StDev:
                        int count = 0;
                        double var = (double)defaultValue;
                        double prec = (double)defaultValue;
                        double dsum = (double)defaultValue;
                        double sqrsum = (double)defaultValue;

                        foreach (int record in records) {
                            if (IsNull(record))
                                continue;
                            dsum += (double)values[record];
                            sqrsum += (double)values[record]*(double)values[record];
                            count++;
                        }

                        if (count > 1) {
                            var = ((double)count * sqrsum - (dsum * dsum));
                            prec = var / (dsum * dsum);
                            
                            // we are dealing with the risk of a cancellation error
                            // double is guaranteed only for 15 digits so a difference 
                            // with a result less than 1e-15 should be considered as zero

                            if ((prec < 1e-15) || (var <0))
                                var = 0;
                            else
                                var = var / (count * (count -1));
                            
                            if (kind == AggregateType.StDev) {
                                return Math.Sqrt(var);
                            }
                            return var;
                        }
                        return DBNull.Value;

                    case AggregateType.Min:
                        UInt64 min = UInt64.MaxValue;
                        for (int i = 0; i < records.Length; i++) {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            min=Math.Min(values[record], min);
                            hasData = true;
                        }
                        if (hasData) {
                            return min;
                        }
                        return DBNull.Value;

                    case AggregateType.Max:
                        UInt64 max = UInt64.MinValue;
                        for (int i = 0; i < records.Length; i++) {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            max=Math.Max(values[record], max);
                            hasData = true;
                        }
                        if (hasData) {
                            return max;
                        }
                        return DBNull.Value;

                    case AggregateType.First:
                        if (records.Length > 0) {
                            return values[records[0]];
                        }
                        return null;

                    case AggregateType.Count:
                        return base.Aggregate(records, kind);

                }
            }
            catch (OverflowException) {
                throw ExprException.Overflow(typeof(UInt64));
            }
            throw ExceptionBuilder.AggregateException(kind.ToString(), DataType);
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.Compare"]/*' />
        /// <internalonly/>
        override public int Compare(int recordNo1, int recordNo2) {
            UInt64 valueNo1 = values[recordNo1];
            UInt64 valueNo2 = values[recordNo2];

            if (valueNo1.Equals(defaultValue) || valueNo2.Equals(defaultValue)) {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                    return bitCheck;
            }
            return valueNo1.CompareTo(valueNo2);
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.CompareToValue"]/*' />
        /// <internalonly/>
        override public int CompareToValue(int recordNo, Object value) {
            bool recordNull = IsNull(recordNo);

            if (recordNull && value == DBNull.Value)
                return 0;
            if (recordNull)
                return -1;
            if (value == DBNull.Value)
                return 1;

            UInt64 valueNo1 = values[recordNo];
            UInt64 valueNo2 = Convert.ToUInt64(value);
            return valueNo1.CompareTo(valueNo2);
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.Copy"]/*' />
        /// <internalonly/>
        override public void Copy(int recordNo1, int recordNo2) {
            CopyBits(recordNo1, recordNo2);
            values[recordNo2] = values[recordNo1];
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.Get"]/*' />
        /// <internalonly/>
        override public Object Get(int record) {
            UInt64 value = values[record];
            if (!value.Equals(defaultValue)) {
                return value;
            }
            return GetBits(record);
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.Set"]/*' />
        /// <internalonly/>
        override public void Set(int record, Object value) {
            if (SetBits(record, value)) {
                values[record] = UInt64Storage.defaultValue;
            }
            else {
                values[record] = Convert.ToUInt64(value);
            }
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.SetCapacity"]/*' />
        /// <internalonly/>
        override public void SetCapacity(int capacity) {
            UInt64[] newValues = new UInt64[capacity];
            if (null != values) {
                Array.Copy(values, 0, newValues, 0, Math.Min(capacity, values.Length));
            }
            values = newValues;
            base.SetCapacity(capacity);
        }

        /// <include file='doc\UInt64Storage.uex' path='docs/doc[@for="UInt64Storage.ConvertXmlToObject"]/*' />
        /// <internalonly/>
        override public object ConvertXmlToObject(string s) {
            return XmlConvert.ToUInt64(s);
        }

        /// <include file='doc\UInt32Storage.uex' path='docs/doc[@for="UInt32Storage.ConvertObjectToXml"]/*' />
        /// <internalonly/>
        override public string ConvertObjectToXml(object value) {
            return XmlConvert.ToString((UInt64)value);
        }
    }
}
