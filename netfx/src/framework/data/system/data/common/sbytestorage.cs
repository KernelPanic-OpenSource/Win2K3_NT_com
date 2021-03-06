//------------------------------------------------------------------------------
// <copyright file="SByteStorage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Data.Common {
    using System;
    using System.Xml;

    /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage"]/*' />
    /// <internalonly/>
    [
    Serializable,
    CLSCompliantAttribute(false)
    ]
    internal class SByteStorage : DataStorage {

        private const SByte defaultValue = 0;
        static private readonly Object defaultValueAsObject = defaultValue;

        private SByte[] values;

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.SByteStorage"]/*' />
        /// <internalonly/>
        public SByteStorage()
        : base(typeof(SByte)) {
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.DefaultValue"]/*' />
        /// <internalonly/>
        public override Object DefaultValue {
            get {
                return defaultValueAsObject;
            }
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.Aggregate"]/*' />
        /// <internalonly/>
        override public Object Aggregate(int[] records, AggregateType kind) {
            bool hasData = false;
            try {
                switch (kind) {
                    case AggregateType.Sum:
                        Int64 sum = defaultValue;
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
                        Int64 meanSum = (Int64)defaultValue;
                        int meanCount = 0;
                        foreach (int record in records) {
                            if (IsNull(record))
                                continue;
                            checked { meanSum += (Int64)values[record];}
                            meanCount++;
                            hasData = true;
                        }
                        if (hasData) {
                            SByte mean;
                            checked {mean = (SByte)(meanSum / meanCount);}
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
                        SByte min = SByte.MaxValue;
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
                        SByte max = SByte.MinValue;
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
                throw ExprException.Overflow(typeof(SByte));
            }
            throw ExceptionBuilder.AggregateException(kind.ToString(), DataType);
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.Compare"]/*' />
        /// <internalonly/>
        override public int Compare(int recordNo1, int recordNo2) {
            SByte valueNo1 = values[recordNo1];
            SByte valueNo2 = values[recordNo2];

            if (valueNo1.Equals(defaultValue) || valueNo2.Equals(defaultValue)) {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                    return bitCheck;
            }
            return valueNo1.CompareTo(valueNo2);
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.CompareToValue"]/*' />
        /// <internalonly/>
        override public int CompareToValue(int recordNo, Object value) {
            bool recordNull = IsNull(recordNo);

            if (recordNull && value == DBNull.Value)
                return 0;
            if (recordNull)
                return -1;
            if (value == DBNull.Value)
                return 1;

            SByte valueNo1 = values[recordNo];
            SByte valueNo2 = Convert.ToSByte(value);
            return valueNo1.CompareTo(valueNo2);
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.Copy"]/*' />
        /// <internalonly/>
        override public void Copy(int recordNo1, int recordNo2) {
            CopyBits(recordNo1, recordNo2);
            values[recordNo2] = values[recordNo1];
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.Get"]/*' />
        /// <internalonly/>
        override public Object Get(int record) {
            SByte value = values[record];
            if (!value.Equals(defaultValue)) {
                return value;
            }
            return GetBits(record);
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.Set"]/*' />
        /// <internalonly/>
        override public void Set(int record, Object value) {
            if (SetBits(record, value)) {
                values[record] = SByteStorage.defaultValue;
            }
            else {
                values[record] = Convert.ToSByte(value);
            }
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.SetCapacity"]/*' />
        /// <internalonly/>
        override public void SetCapacity(int capacity) {
            SByte[] newValues = new SByte[capacity];
            if (null != values) {
                Array.Copy(values, 0, newValues, 0, Math.Min(capacity, values.Length));
            }
            values = newValues;
            base.SetCapacity(capacity);
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.ConvertXmlToObject"]/*' />
        /// <internalonly/>
        override public object ConvertXmlToObject(string s) {
            return XmlConvert.ToSByte(s);
        }

        /// <include file='doc\SByteStorage.uex' path='docs/doc[@for="SByteStorage.ConvertObjectToXml"]/*' />
        /// <internalonly/>
        override public string ConvertObjectToXml(object value) {
            return XmlConvert.ToString((SByte)value);
        }
    }
}
