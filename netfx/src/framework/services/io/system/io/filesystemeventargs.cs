//------------------------------------------------------------------------------
// <copyright file="FileSystemEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.IO {

    using System.Diagnostics;
    using System.Security.Permissions;

    using System;

    /// <include file='doc\FileSystemEventArgs.uex' path='docs/doc[@for="FileSystemEventArgs"]/*' />
    /// <devdoc>
    /// <para>Provides data for the directory events: <see cref='System.IO.FileSystemWatcher.Changed'/>, <see cref='System.IO.FileSystemWatcher.Created'/>, <see cref='System.IO.FileSystemWatcher.Deleted'/>.</para>
    /// </devdoc>
    public class FileSystemEventArgs : EventArgs {
        private WatcherChangeTypes changeType;
        private string name;
        private string fullPath;

        /// <include file='doc\FileSystemEventArgs.uex' path='docs/doc[@for="FileSystemEventArgs.FileSystemEventArgs"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.IO.FileSystemEventArgs'/> class.</para>
        /// </devdoc>
        public FileSystemEventArgs(WatcherChangeTypes changeType, string directory, string name)
        {
            this.changeType = changeType;
            this.name = name;

            // Ensure that the directory name ends with a "\"
            if (!directory.EndsWith("\\")) {
                directory = directory + "\\";
            }

            this.fullPath = directory + name;
        }

        /// <include file='doc\FileSystemEventArgs.uex' path='docs/doc[@for="FileSystemEventArgs.ChangeType"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       one of the <see cref='System.IO.WatcherChangeTypes'/>
        ///       values.
        ///    </para>
        /// </devdoc>
        public WatcherChangeTypes ChangeType {
            get {
                return changeType;
            }
        }

        /// <include file='doc\FileSystemEventArgs.uex' path='docs/doc[@for="FileSystemEventArgs.FullPath"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the
        ///       fully qualifed path of the affected file or directory.
        ///    </para>
        /// </devdoc>
        public string FullPath {
            get {
                return fullPath;
            }
        }


        /// <include file='doc\FileSystemEventArgs.uex' path='docs/doc[@for="FileSystemEventArgs.Name"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       the name of the affected file or directory.
        ///    </para>
        /// </devdoc>
        public string Name {
            get {
                return name;
            }
        }
    }

}
