//------------------------------------------------------------------------------
// <copyright file="ProcessPriorityClass.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Diagnostics {

    using System.Diagnostics;
    /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass"]/*' />
    /// <devdoc>
    ///     A category of priority for a process.  Threads within a process
    ///     run at a priority which is relative to the process priority class.
    /// </devdoc>
    public enum ProcessPriorityClass {
        /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass.Normal"]/*' />
        /// <devdoc>
        ///      Specify this class for a process with no special scheduling needs. 
        /// </devdoc>
        Normal = 0x20,
        
        /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass.Idle"]/*' />
        /// <devdoc>
        ///     Specify this class for a process whose threads run only when the system is idle. 
        ///     The threads of the process are preempted by the threads of any process running in 
        ///     a higher priority class. An example is a screen saver. The idle-priority class is 
        ///     inherited by child processes.
        /// </devdoc>
        Idle = 0x40,
        
        /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass.High"]/*' />
        /// <devdoc>
        ///     Specify this class for a process that performs time-critical tasks that must 
        ///     be executed immediately. The threads of the process preempt the threads of 
        ///     normal or idle priority class processes. An example is the Task List, which 
        ///     must respond quickly when called by the user, regardless of the load on the 
        ///     operating system. Use extreme care when using the high-priority class, because 
        ///     a high-priority class application can use nearly all available CPU time.
        /// </devdoc>
        High = 0x80,
        
        /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass.RealTime"]/*' />
        /// <devdoc>
        ///     Specify this class for a process that has the highest possible priority. 
        ///     The threads of the process preempt the threads of all other processes, 
        ///     including operating system processes performing important tasks. For example, 
        ///     a real-time process that executes for more than a very brief interval can cause 
        ///     disk caches not to flush or cause the mouse to be unresponsive.
        /// </devdoc>
        RealTime = 0x100,

        /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass.BelowNormal"]/*' />
        /// <devdoc>
        ///     Indicates a process that has priority above Idle but below Normal.
        /// </devdoc>
        BelowNormal = 0x4000,

        /// <include file='doc\ProcessPriorityClass.uex' path='docs/doc[@for="ProcessPriorityClass.AboveNormal"]/*' />
        /// <devdoc>
        ///     Indicates a process that has priority above Normal but below High.
        /// </devdoc>
        AboveNormal = 0x8000
    }
}
