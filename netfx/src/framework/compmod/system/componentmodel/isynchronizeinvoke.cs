//------------------------------------------------------------------------------
// <copyright file="ISynchronizeInvoke.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.ComponentModel {
    using System; 
         
    /// <include file='doc\ISynchronizeInvoke.uex' path='docs/doc[@for="ISynchronizeInvoke"]/*' />
    /// <devdoc>
    ///    <para>Provides a way to synchronously or asynchronously execute a delegate.</para>
    /// </devdoc>
    public interface ISynchronizeInvoke {
    
        /// <include file='doc\ISynchronizeInvoke.uex' path='docs/doc[@for="ISynchronizeInvoke.InvokeRequired"]/*' />
        /// <devdoc>
        /// <para>Gets a value indicating whether the caller must call <see cref='System.ComponentModel.ISynchronizeInvoke.Invoke'/> when calling an object that implements 
        ///    this interface.</para>
        /// </devdoc>
        bool InvokeRequired{get;}
                
        /// <include file='doc\ISynchronizeInvoke.uex' path='docs/doc[@for="ISynchronizeInvoke.BeginInvoke"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Executes the given delegate on the main thread that this object executes on.</para>
        /// </devdoc>
        IAsyncResult BeginInvoke(Delegate method, object[] args);            
        
        /// <include file='doc\ISynchronizeInvoke.uex' path='docs/doc[@for="ISynchronizeInvoke.EndInvoke"]/*' />
        /// <devdoc>
        ///    <para>Waits until the process you started by 
        ///       calling <see cref='System.ComponentModel.ISynchronizeInvoke.BeginInvoke'/> completes, and then returns
        ///       the value generated by the process.</para>
        /// </devdoc>
        object EndInvoke(IAsyncResult result);                      
        
        /// <include file='doc\ISynchronizeInvoke.uex' path='docs/doc[@for="ISynchronizeInvoke.Invoke"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Executes the given delegate on the main thread that this object
        ///       executes on.</para>
        /// </devdoc>
        object Invoke(Delegate method, object[] args);        
    }
}
