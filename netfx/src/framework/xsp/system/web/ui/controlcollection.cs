//------------------------------------------------------------------------------
// <copyright file="ControlCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace System.Web.UI {
    using System.Runtime.InteropServices;

    using System;
    using System.Collections;
    using System.Security.Permissions;

    /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection"]/*' />
    /// <devdoc>
    ///    <para>
    ///       The <see langword='ControlCollection'/> class provides a
    ///       collection container that enables a control to maintain a
    ///       list of its child controls.
    ///
    ///       For performance reasons, this is internally strongly typed. Most implementation is copied from
    ///       ArrayList.cs
    ///    </para>
    /// </devdoc>
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class ControlCollection : ICollection {
        private Control _owner;
        private Control[] _controls;
        private int _size;
        private int _version;
        private string _readOnlyErrorMsg = null;

        private const int _defaultCapacity = 5;
        private const int _growthFactor = 4;

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.ControlCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ControlCollection(Control owner) {
            if (owner == null) {
                throw new ArgumentNullException("owner");
            }
            _owner = owner;
        }

        /*
         * Adds a child control to this control.
         */
        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.Add"]/*' />
        /// <devdoc>
        /// <para>Adds the specified <see cref='System.Web.UI.Control'/> object
        ///    to the collection. The new control is logically added to the end of an ordinal
        ///    index array.</para>
        /// </devdoc>
        public virtual void Add(Control child) {
            // Note: duplication of code with AddAt is deliberate for performance reasons. This is the more common form of the call.

            // Check arguments
            if (child == null)
                throw new ArgumentNullException("child");

            if (_readOnlyErrorMsg != null) {
                throw new HttpException(HttpRuntime.FormatResourceString(_readOnlyErrorMsg ));
            }

            // Make sure we have room
            if (_controls == null) {
                _controls = new Control[_defaultCapacity];
            }
            else if (_size >= _controls.Length) {
                Control[] newArray = new Control[_controls.Length * _growthFactor ];
                Array.Copy(_controls, newArray, _controls.Length);
                _controls = newArray;
            }

            // Add the control
            int index = _size;
            _controls[index] = child;
            _size++;
            _version++;

            // Notify Owner
            _owner.AddedControl(child, index);
        }

        /*
         * Adds a child control to this control at a certain index
         */
        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.AddAt"]/*' />
        /// <devdoc>
        /// <para>Adds the specified <see cref='System.Web.UI.Control'/> object to the collection. The new control is added
        ///    to the array at the specified index location.</para>
        /// </devdoc>
        public virtual void AddAt(int index, Control child) {

            // For compatability, we must support this.
            if (index == -1) {
                Add(child);
                return;
            }

            // Check Arguments
            if (child == null) {
                throw new ArgumentNullException("child");
            }
            if (index < 0 || index > _size) {
                throw new ArgumentOutOfRangeException("index");
            }

            if (_readOnlyErrorMsg != null) {
                throw new HttpException(HttpRuntime.FormatResourceString(_readOnlyErrorMsg ));
            }

            // Make sure we have room
            if (_controls == null) {
                _controls = new Control[_defaultCapacity];
            }
            else if (_size >= _controls.Length) {
                Control[] newArray = new Control[_controls.Length * _growthFactor ];
                Array.Copy(_controls, newArray, _controls.Length);
                _controls = newArray;
            }

            // Insert the control
            if (index < _size) {
                Array.Copy(_controls, index, _controls, index + 1, _size - index);
            }
            _controls[index] = child;
            _size++;
            _version++;

            _owner.AddedControl(child, index);
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.Clear"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Removes all controls in the collection.
        ///    </para>
        /// </devdoc>
        public virtual void Clear() {
            if (_controls != null) {
                // ASURT 123965: This used to call RemoveAt(0), which was an n^2 operation.  Removing from the end of the array now.
                for (int i = _size - 1; i >= 0; i--) {
                    RemoveAt(i);
                }

                if (_owner is INamingContainer)
                    _owner.ClearNamingContainer();
            }
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Indicates whether the collection contains a specific object
        ///    </para>
        /// </devdoc>
        public virtual bool Contains(Control c) {
            if (_controls == null || c == null)
                return false;

            for (int i = 0; i < _size; i++) {
                if (Object.ReferenceEquals(c, _controls[i])) {
                    return true;
                }
            }
            return false;
        }

        /*
         * Retrieves the number of child controls.
         */
        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.Count"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the number of child controls in the collection.
        ///    </para>
        /// </devdoc>
        public int Count {
            get {
                return _size;
                }
                }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.Owner"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected Control Owner {
            get {
                return _owner;
            }
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns the index of a specified <see cref='System.Web.UI.Control'/>
        ///       object
        ///       in the collection.
        ///    </para>
        /// </devdoc>
        public virtual int IndexOf(Control value) {
            if (_controls == null)
                return -1;

            return Array.IndexOf(_controls, value, 0, _size);
            }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.GetEnumerator"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns an enumerator of all controls in the collection.
        ///    </para>
        /// </devdoc>
        public IEnumerator GetEnumerator() {
            return new ControlCollectionEnumerator(this);
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>Copies the specified child control to a location in the collection.</para>
        /// </devdoc>
        public void CopyTo(Array array, int index) {
            if (_controls == null)
                return;
            if ((array != null) && (array.Rank != 1))
                throw new HttpException(HttpRuntime.FormatResourceString(SR.InvalidArgumentValue, "array"));

            Array.Copy(_controls, 0, array, index, _size);
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.SyncRoot"]/*' />
        /// <devdoc>
        ///    <para>Gets the parent control of the control collection.</para>
        /// </devdoc>
        public Object SyncRoot {
            get { return this;}
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.IsReadOnly"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether the collection is read-only.</para>
        /// </devdoc>
        public bool IsReadOnly {
            get { return (_readOnlyErrorMsg != null); }
        }

        // Setting an error message makes the control collection read only.  If the user tries to modify
        // the collection, we look up the error message in the resources and throw an exception.
        // Set errorMsg to null to make the collection not read only.
        internal string SetCollectionReadOnly(string errorMsg) {
            string olderror = _readOnlyErrorMsg;
            _readOnlyErrorMsg = errorMsg;
            return olderror;
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.IsSynchronized"]/*' />
        /// <devdoc>
        ///    <para> Gets a value indicating whether the collection
        ///       is synchronized.</para>
        /// </devdoc>
        public bool IsSynchronized {
            get { return false;}
        }


        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.this"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a reference to an ordinal-indexed control in the collection.
        ///    </para>
        /// </devdoc>
        virtual public Control this[int index]
        {
            get {
                if (index < 0 || index >= _size) {
                    throw new ArgumentOutOfRangeException("index");
                }
                return _controls[index];
            }
        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.RemoveAt"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Removes the specified child control from the collection.
        ///    </para>
        /// </devdoc>
        public virtual void RemoveAt(int index) {

            if (_readOnlyErrorMsg != null) {
                throw new HttpException(HttpRuntime.FormatResourceString(_readOnlyErrorMsg ));
            }

            Control child = this[index];
            _size--;
            if (index < _size) {
                Array.Copy(_controls, index + 1, _controls, index, _size - index);
            }
            _controls[_size] = null;
            _version++;
            _owner.RemovedControl(child);

        }

        /// <include file='doc\ControlCollection.uex' path='docs/doc[@for="ControlCollection.Remove"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Removes the specified
        ///       child control object from the collection.
        ///    </para>
        /// </devdoc>
        public virtual void Remove(Control value) {
            int index = IndexOf(value);
            if (index >=0)
                RemoveAt(index);
        }

        // This is a copy of the ArrayListEnumeratorSimple in ArrayList.cs
        private class ControlCollectionEnumerator : IEnumerator
        {
            private ControlCollection list;
            private int index;
            private int version;
            private Control currentElement;

            internal ControlCollectionEnumerator(ControlCollection list) {
                this.list = list;
                this.index = -1;
                version = list._version;
            }

            public bool MoveNext() {
                if (index < (list.Count-1)) {
                    if (version != list._version)
                        throw new InvalidOperationException(SR.GetString(SR.ListEnumVersionMismatch));
                    index++;
                    currentElement = list[index];
                    return true;
                }
                else
                    index = list.Count;
                return false;
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public Control Current {
                get {
                    if (index == -1)
                        throw new InvalidOperationException(SR.GetString(SR.ListEnumCurrentOutOfRange));
                    if (index >= list.Count)
                        throw new InvalidOperationException(SR.GetString(SR.ListEnumCurrentOutOfRange));
                    return currentElement;
                }
            }

            public void Reset() {
                if (version != list._version)
                    throw new InvalidOperationException(SR.GetString(SR.ListEnumVersionMismatch));
                currentElement = null;
                index = -1;
            }
        }

    }
}
