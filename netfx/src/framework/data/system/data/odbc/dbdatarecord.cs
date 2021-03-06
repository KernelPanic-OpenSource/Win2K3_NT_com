//------------------------------------------------------------------------------
// <copyright file="DbDataRecord.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;            //Component
using System.Data;

namespace System.Data.Odbc
{
    sealed internal class DbSchemaInfo
    {
        //Data
        public      string  _name;
        public      string  _typename;
        public      Type    _type;
        public      object  _dbtype;
        public      object  _scale;
        public      object  _precision;
    }

    // ---------------------------------------------------------------------------
    // Cache
    //
    //  This is a on-demand cache, only caching what the user requests.
    //  The reational is that for ForwardOnly access (the default and LCD of drivers)
    //  we cannot obtain the data more than once, and even GetData(0) (to determine is-null)
    //  still obtains data for fixed lenght types.

    //  So simple code like:
    //      if(!rReader.IsDBNull(i))
    //          rReader.GetInt32(i)
    //
    //  Would fail, unless we cache on the IsDBNull call, and return the cached
    //  item for GetInt32.  This actually improves perf anyway, (even if the driver could
    //  support it), since we are not making a seperate interop call...

    //  We do not cache all columns, so reading out of order is still not
    //
    // ---------------------------------------------------------------------------
    sealed internal class DbCache
    {
        //Data

        private   DbSchemaInfo[]    _schema;
        private   object[]          _values;
        private   OdbcDataReader    _record;
        internal  int               _count;
        internal  bool              _randomaccess = true;

        //Constructor
        public   DbCache(OdbcDataReader record, int count)
        {
            _count = count;
            _record = record;
            _randomaccess = ((record.commandBehavior & CommandBehavior.SequentialAccess) == 0);
            _values = new object[count];

        }

        //Accessor
        internal  object            this[int i]
        {
            get { return Values[i];     }
            set { Values[i] = value;    }
        }

        internal  object            AccessIndex(int i)
        {
            //Note: We could put this directly in this[i], instead of having an explicit overload.
            //However that means that EVERY access into the cache takes the hit of checking, so
            //something as simple as the following code would take two hits.  It's nice not to
            //have to take the hit when you know what your doing.
            //
            //  if(cache[i] == null)
            //      ....
            //  return cache[i];

            object[] values = this.Values;
            if(_randomaccess)
            {
                //Random
                //Means that the user can ask for the values int any order (ie: out of order).
                //  In order to acheive this on a forward only stream, we need to actually
                //  retreive all the value in between so they can go back to values they've skipped
                for(int c=0; c<i; c++)
                {
                    if(values[c] == null)
                        values[c] = _record.GetValue(c);
                }
            }

            return values[i];
        }

        internal object[] Values {
            get {
                return _values;
            }
        }

        internal  DbSchemaInfo      GetSchema(int i)
        {
            if(_schema == null)
                _schema = new DbSchemaInfo[Count];
            if(_schema[i] == null)
                _schema[i] = new DbSchemaInfo();
            return _schema[i];
        }

        internal int Count {
            get {
                return _count;
            }
        }

        internal void FlushValues() {
            //Set all objects to null (to explcitly release them)
            //Note: SchemaInfo remains the same for all rows - no need to reget those...
            int count = _values.Length;
            for (int i = 0; i < count; ++i) {
                _values[i] = null;
            }
        }
    }
}
