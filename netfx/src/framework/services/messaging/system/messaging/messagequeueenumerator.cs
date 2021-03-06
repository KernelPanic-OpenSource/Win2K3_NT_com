//------------------------------------------------------------------------------
// <copyright file="MessageQueueEnumerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using INTPTR_INTPTRCAST = System.IntPtr;                    
                    
namespace System.Messaging {    
    using System.Runtime.InteropServices;    
    using System.Diagnostics;
    using System;    
    using System.Collections;    
    using System.Messaging.Interop;
    using System.Globalization;
    
    /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator"]/*' />
    /// <devdoc>
    ///    <para>Provides (forward-only) cursor semantics to enumerate the queues on a 
    ///       computer.</para>
    ///    <note type="rnotes">
    ///       I'm assuming all the queues have to
    ///       be
    ///       on the same computer. Is this the case? Do we want to translate this reference
    ///       to "cursor semantics" into English, or is it okay as it stands? Will the users
    ///       understand the concept of a cursor?
    ///    </note>
    /// </devdoc>
    public class MessageQueueEnumerator : MarshalByRefObject, IEnumerator, IDisposable {
        private MessageQueueCriteria criteria;
        private IntPtr locatorHandle;
        private MessageQueue currentMessageQueue;        
        private bool checkSecurity;
        private bool disposed;

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.MessageQueueEnumerator"]/*' />
        ///    <internalonly/>
        internal MessageQueueEnumerator(MessageQueueCriteria criteria) {
            this.criteria = criteria;            
            this.checkSecurity = true;
        }
        
        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.MessageQueueEnumerator1"]/*' />
        ///    <internalonly/>
        internal MessageQueueEnumerator(MessageQueueCriteria criteria, bool checkSecurity) {
            this.criteria = criteria;            
            this.checkSecurity = checkSecurity;
        }        

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.Current"]/*' />
        /// <devdoc>
        ///     Returns the current MessageQueue of the  enumeration. 
        ///     Before the first call to MoveNext and following a call to MoveNext that 
        ///     returned false an InvalidOperationException will be thrown. Multiple 
        ///     calls to Current with no intervening calls to MoveNext will return the 
        ///     same MessageQueue object.
        /// </devdoc>        
        public MessageQueue Current {
            get {
                if (this.currentMessageQueue == null)
                    throw new InvalidOperationException(Res.GetString(Res.NoCurrentMessageQueue));
                    
                return this.currentMessageQueue;
            }
        }
        
        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.IEnumerator.Current"]/*' />
        /// <internalonly/>
        object IEnumerator.Current {
            get {
                return this.Current;
            }   
        }

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.Close"]/*' />
        /// <devdoc>
        ///    <para>Frees the resources associated with the enumerator.</para>
        /// </devdoc>
        public void Close() {
            if (this.locatorHandle != (IntPtr)0) {
                SafeNativeMethods.MQLocateEnd(this.locatorHandle);
                this.locatorHandle = (IntPtr)0;
                this.currentMessageQueue = null;                
            }
        }         
         
        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.Dispose"]/*' />
        /// <devdoc>
        /// </devdoc>
        public void Dispose() {                        
            Dispose(true);
            GC.SuppressFinalize(this);                           
        }

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.Dispose1"]/*' />
        /// <devdoc>
        ///    <para>
        ///    </para>
        /// </devdoc>
        protected virtual void Dispose(bool disposing) {
            this.Close();                        
            this.disposed = true;
        }

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.Finalize"]/*' />
        /// <devdoc>
        /// </devdoc>
        ~MessageQueueEnumerator() {
            Dispose(false);
        }

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.LocatorHandle"]/*' />
        /// <devdoc>
        ///    <para>Indicates the native Message Queuing handle used to locate queues in a network. This
        ///       property is read-only.</para>
        /// </devdoc>
        public IntPtr LocatorHandle {
            get {
                if (this.locatorHandle == (IntPtr)0) {
                    //Cannot allocate the locatorHandle if the object has been disposed, since finalization has been suppressed.
                    if (this.disposed)
                        throw new ObjectDisposedException(GetType().Name);
                    
                    if (this.checkSecurity) {
                        MessageQueuePermission permission = new MessageQueuePermission(MessageQueuePermissionAccess.Browse, MessageQueuePermission.Any);
                        permission.Demand();
                    }                        
                    
                    Columns columns = new Columns(2);
                    IntPtr enumHandle;
                    columns.AddColumnId(NativeMethods.QUEUE_PROPID_PATHNAME);
                    //Adding the instance property avoids accessing the DS a second
                    //time, the formatName can be resolved by calling MQInstanceToFormatName
                    columns.AddColumnId(NativeMethods.QUEUE_PROPID_INSTANCE);
                    int status;
                    if (this.criteria != null)
                        status = UnsafeNativeMethods.MQLocateBegin(null, this.criteria.Reference, columns.GetColumnsRef(), null, out enumHandle);
                    else
                        status = UnsafeNativeMethods.MQLocateBegin(null, null, columns.GetColumnsRef(), null, out enumHandle);

                    if (MessageQueue.IsFatalError(status))
                        throw new MessageQueueException(status);

                    this.locatorHandle = enumHandle;
                }

                return this.locatorHandle;
            }
        }

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.MoveNext"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Advances the enumerator to the next queue of the enumeration, if one
        ///       is currently available.</para>
        /// </devdoc>
        public bool MoveNext() {
            int[] array = new int[8];
            int propertyCount;
            string currentItem;
            byte[] currentGuid = new byte[16];
            string machineName = null;

            if (this.criteria != null  && this.criteria.FilterMachine) {
                if (this.criteria.MachineName.CompareTo(".") == 0)
                    machineName = MessageQueue.ComputerName + "\\";
                else                    
                    machineName = this.criteria.MachineName + "\\";
            }                

            do {
                propertyCount = 2;
                int status;
                status = SafeNativeMethods.MQLocateNext(this.LocatorHandle, ref propertyCount, array);
                if (MessageQueue.IsFatalError(status))
                    throw new MessageQueueException(status);

                if (propertyCount != 2) {
                    this.currentMessageQueue = null;
                    return false;
                }

                //Using Unicode API even on Win9x
                currentItem = Marshal.PtrToStringUni((INTPTR_INTPTRCAST)array[2]);
                Marshal.Copy((INTPTR_INTPTRCAST)array[6], currentGuid, 0, 16);
                //MSMQ allocated this memory, lets free it.
                SafeNativeMethods.MQFreeMemory((INTPTR_INTPTRCAST)array[2]);
                SafeNativeMethods.MQFreeMemory((INTPTR_INTPTRCAST)array[6]);
            }
            while (machineName != null && (machineName.Length >= currentItem.Length ||
                                           String.Compare(machineName, 0, currentItem, 0, machineName.Length, true, CultureInfo.InvariantCulture) != 0));

            this.currentMessageQueue = new MessageQueue(currentItem, new Guid(currentGuid));
            return true;
        }        

        /// <include file='doc\MessageQueueEnumerator.uex' path='docs/doc[@for="MessageQueueEnumerator.Reset"]/*' />
        /// <devdoc>
        ///    <para>Resets the cursor, so it points to the head of the list..</para>
        /// </devdoc>
        public void Reset() {
            this.Close();
        }
    }
}        
