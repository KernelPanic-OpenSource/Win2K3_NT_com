//------------------------------------------------------------------------------
// <copyright file="DBSchemaRow.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Data.OracleClient {

    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;

    /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow"]/*' />
    /// <devdoc>
    /// </devdoc>
    sealed internal class DBSchemaRow {
        DBSchemaTable schemaTable;
        DataRow dataRow;

        static internal DBSchemaRow[] GetSortedSchemaRows(DataTable dataTable) { // MDAC 60609
            DataColumn sortindex = new DataColumn("SchemaMapping Unsorted Index", typeof(Int32)); // MDAC 67050
            dataTable.Columns.Add(sortindex);
            int count = dataTable.Rows.Count;
            for (int i = 0; i < count; ++i) {
                dataTable.Rows[i][sortindex] = i;
            };
            DBSchemaTable schemaTable = new DBSchemaTable(dataTable);

            const DataViewRowState rowStates = DataViewRowState.Unchanged | DataViewRowState.Added | DataViewRowState.ModifiedCurrent;
            DataRow[] dataRows = dataTable.Select(null, "ColumnOrdinal ASC", rowStates);
            Debug.Assert(null != dataRows, "GetSchemaRows: unexpected null dataRows");

            DBSchemaRow[] schemaRows = new DBSchemaRow[dataRows.Length];

            for (int i = 0; i < dataRows.Length; ++i) {
                schemaRows[i] = new DBSchemaRow(schemaTable, dataRows[i]);
            }
            return schemaRows;
        }
        
        static internal DBSchemaRow[] GetSchemaRows(DataTable dataTable) {
            DBSchemaTable schemaTable = new DBSchemaTable(dataTable);

            const DataViewRowState rowStates = DataViewRowState.Unchanged | DataViewRowState.Added | DataViewRowState.ModifiedCurrent;
            DataRow[] dataRows = ADP.SelectRows(dataTable, rowStates);
            Debug.Assert(null != dataRows, "GetSchemaRows: unexpected null dataRows");

            DBSchemaRow[] schemaRows = new DBSchemaRow[dataRows.Length];

            for (int i = 0; i < dataRows.Length; ++i) {
                schemaRows[i] = new DBSchemaRow(schemaTable, dataRows[i]);
            }
            return schemaRows;
        }

        internal DBSchemaRow(DBSchemaTable schemaTable, DataRow dataRow) {
            this.schemaTable = schemaTable;
            this.dataRow = dataRow;
        }

        internal DataRow DataRow {
            get {
                return dataRow;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.ColumnName"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal string ColumnName {
            get {
                Debug.Assert(null != schemaTable.ColumnName, "no column ColumnName");
                object value = dataRow[schemaTable.ColumnName, DataRowVersion.Default];
                if (!Convert.IsDBNull(value)) {
                    return Convert.ToString(value);
                }
                return "";
            }
            set {
                Debug.Assert(null != schemaTable.ColumnName, "missing column ColumnName");
                dataRow[schemaTable.ColumnName] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.Ordinal"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal Int32 Ordinal {
            get {
                Debug.Assert(null != schemaTable.Ordinal, "no column Ordinal");
                return Convert.ToInt32(dataRow[schemaTable.Ordinal, DataRowVersion.Default]);
            }
            set {
                Debug.Assert(null != schemaTable.Ordinal, "missing column Ordinal");
                dataRow[schemaTable.Ordinal] = value;
            }

        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.Size"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal Int32 Size {
            get {
                Debug.Assert(null != schemaTable.Size, "no column Size");
                object value = dataRow[schemaTable.Size, DataRowVersion.Default];
                if (!Convert.IsDBNull(value)) {
                    return Convert.ToInt32(value);
                }
                return 0;
            }
            set {
                Debug.Assert(null != schemaTable.Size, "missing column Size");
                dataRow[schemaTable.Size] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.Precision"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal Int16 Precision {
            get {
                Debug.Assert(null != schemaTable.Precision, "no column Precision");
                object value = dataRow[schemaTable.Precision, DataRowVersion.Default];
                if (!Convert.IsDBNull(value)) {
                    return Convert.ToInt16(value);
                }
                return 0;
            }
            set {
                Debug.Assert(null != schemaTable.Precision, "missing column Precision");
                dataRow[schemaTable.Precision] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.Scale"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal Int16 Scale {
            get {
                Debug.Assert(null != schemaTable.Scale, "no column Scale");
                object value = dataRow[schemaTable.Scale, DataRowVersion.Default];
                if (!Convert.IsDBNull(value)) {
                    return Convert.ToInt16(value);
                }
                return 0;
            }
            set {
                Debug.Assert(null != schemaTable.Scale, "missing column Scale");
                dataRow[schemaTable.Scale] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.BaseColumnName"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal string BaseColumnName {
            get {
                if (null != schemaTable.BaseColumnName) {
                    object value = dataRow[schemaTable.BaseColumnName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToString(value);
                    }
                }
                return "";
            }
            set {
                Debug.Assert(null != schemaTable.BaseColumnName, "missing column BaseColumnName");
                dataRow[schemaTable.BaseColumnName] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.BaseServerName"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal string BaseServerName {
            get {
                if (null != schemaTable.BaseServerName) {
                    object value = dataRow[schemaTable.BaseServerName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToString(value);
                    }
                }
                return "";
            }
            set {
                Debug.Assert(null != schemaTable.BaseServerName, "missing column BaseServerName");
                dataRow[schemaTable.BaseServerName] = value;
            }
        }
        
    
        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.BaseCatalogName"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal string BaseCatalogName {
            get {
                if (null != schemaTable.BaseCatalogName) {
                    object value = dataRow[schemaTable.BaseCatalogName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToString(value);
                    }
                }
                return "";
            }
            set {
                Debug.Assert(null != schemaTable.BaseCatalogName, "missing column BaseCatalogName");
                dataRow[schemaTable.BaseCatalogName] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.BaseSchemaName"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal string BaseSchemaName {
            get {
                if (null != schemaTable.BaseSchemaName) {
                    object value = dataRow[schemaTable.BaseSchemaName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToString(value);
                    }
                }
                return "";
            }
            set {
                Debug.Assert(null != schemaTable.BaseSchemaName, "missing column BaseSchemaName");
                dataRow[schemaTable.BaseSchemaName] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.BaseTableName"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal string BaseTableName {
            get {
                if (null != schemaTable.BaseTableName) {
                    object value = dataRow[schemaTable.BaseTableName, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToString(value);
                    }
                }
                return "";
            }
            set {
                Debug.Assert(null != schemaTable.BaseTableName, "missing column BaseTableName");
                dataRow[schemaTable.BaseTableName] = value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.IsAutoIncrement"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal bool IsAutoIncrement {
            get {
                if (null != schemaTable.IsAutoIncrement) {
                    object value = dataRow[schemaTable.IsAutoIncrement, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsAutoIncrement, "missing column IsAutoIncrement");
                dataRow[schemaTable.IsAutoIncrement] = (bool)value;
            }
        }

        /// <include file='doc\DBSchemaRow.uex' path='docs/doc[@for="DBSchemaRow.IsUnique"]/*' />
        /// <devdoc>
        /// </devdoc>
        internal bool IsUnique {
            get {
                if (null != schemaTable.IsUnique) {
                    object value = dataRow[schemaTable.IsUnique, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsUnique, "missing column IsUnique");
                dataRow[schemaTable.IsUnique] = (bool)value;
            }
        }

        internal bool IsRowVersion {
            get {
                if (null != schemaTable.IsRowVersion) {
                    object value = dataRow[schemaTable.IsRowVersion, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsRowVersion, "missing column IsRowVersion");
                dataRow[schemaTable.IsRowVersion] = value;
            }
        }
        
        internal bool IsKey {
            get {
                if (null != schemaTable.IsKey) {
                    object value = dataRow[schemaTable.IsKey, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsKey, "missing column IsKey");
                dataRow[schemaTable.IsKey] = value;
            }
        }

        // consider:  just do comparison directly -> (object)(baseColumnName) == (object)(columnName)
        internal bool IsAliased {
            get {
                if (null != schemaTable.IsAliased) { // MDAC 62336
                    object value = dataRow[schemaTable.IsAliased, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsAliased, "missing column IsAliased");
                dataRow[schemaTable.IsAliased] = value;
            }
        }

        internal bool IsExpression {
            get {
                if (null != schemaTable.IsExpression) { // MDAC 62336
                    object value = dataRow[schemaTable.IsExpression, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsExpression, "missing column IsExpression");
                dataRow[schemaTable.IsExpression] = value;
            }
        }

        internal bool IsIdentity {
            get {
                if (null != schemaTable.IsIdentity) { // MDAC 62336
                    object value = dataRow[schemaTable.IsIdentity, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsIdentity, "missing column IsIdentity");
                dataRow[schemaTable.IsIdentity] = value;
            }
        }

        internal bool IsHidden {
            get {
                if (null != schemaTable.IsHidden) { // MDAC 62336
                    object value = dataRow[schemaTable.IsHidden, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsHidden, "missing column IsHidden");
                dataRow[schemaTable.IsHidden] = value;
            }
        }

        internal bool IsLong {
            get {
                if (null != schemaTable.IsLong) { // MDAC 62336
                    object value = dataRow[schemaTable.IsLong, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsLong, "missing column IsHidden");
                dataRow[schemaTable.IsLong] = value;
            }
        }

        internal bool IsReadOnly {
            get {
                if (null != schemaTable.IsReadOnly) { // MDAC 62336
                    object value = dataRow[schemaTable.IsReadOnly, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return false;
            }
            set {
                Debug.Assert(null != schemaTable.IsReadOnly, "missing column IsReadOnly");
                dataRow[schemaTable.IsReadOnly] = value;
            }
        }

        internal System.Type DataType {
            get {
                if (null != schemaTable.DataType) {
                    object value = dataRow[schemaTable.DataType, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return(System.Type) value;
                    }
                }
                Debug.Assert(false, "missing value DataType");
                return null;
            }
            set {
                Debug.Assert(null != schemaTable.DataType, "missing column DataType");
                dataRow[schemaTable.DataType] = value;
            }
        }

        internal bool AllowDBNull {
            get {
                if (null != schemaTable.AllowDBNull) {
                    object value = dataRow[schemaTable.AllowDBNull, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToBoolean(value);
                    }
                }
                return true;
            }
            set {
                Debug.Assert(null != schemaTable.AllowDBNull, "missing column MaybeNull");
                dataRow[schemaTable.AllowDBNull] = value;
            }
        }
        
        internal Int32 ProviderType {
            get {
                if (null != schemaTable.ProviderType) {
                    object value = dataRow[schemaTable.ProviderType, DataRowVersion.Default];
                    if (!Convert.IsDBNull(value)) {
                        return Convert.ToInt32(value);
                    }
                }
                return 0;
            }
            set {
                Debug.Assert(null != schemaTable.ProviderType, "missing column ProviderType");
                dataRow[schemaTable.ProviderType] = value;
            }
        }

        internal Int32 UnsortedIndex {
            get {
                return (Int32) dataRow[schemaTable.UnsortedIndex, DataRowVersion.Default];
            }
        }
    }
}
