//------------------------------------------------------------------------------
// <copyright file="GPStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Drawing.Internal {

    using System;
    using System.IO;
    using System.Drawing;
    using System.Runtime.InteropServices;

    internal class GPStream : UnsafeNativeMethods.IStream {
        protected Stream dataStream;

        // to support seeking ahead of the stream length...
        long virtualPosition = -1;

        internal GPStream(Stream stream) {
            if (!stream.CanSeek) {
                const int ReadBlock = 256;
                byte[] bytes = new byte[ReadBlock];
                int readLen;
                int current = 0;
                do {
                    if (bytes.Length < current + ReadBlock) {
                        byte[] newData = new byte[bytes.Length * 2];
                        Array.Copy(bytes, newData, bytes.Length);
                        bytes = newData;
                    }
                    readLen = stream.Read(bytes, current, ReadBlock);
                    current += readLen;
                } while (readLen != 0);

                dataStream = new MemoryStream(bytes);
            }
            else {
                dataStream = stream;
            }
        }

        private void ActualizeVirtualPosition() {
            if (virtualPosition == -1) return;

            if (virtualPosition > dataStream.Length)
                dataStream.SetLength(virtualPosition);

            dataStream.Position = virtualPosition;

            virtualPosition = -1;
        }

        public virtual UnsafeNativeMethods.IStream Clone() {
            NotImplemented();
            return null;
        }

        public virtual void Commit(int grfCommitFlags) {
            dataStream.Flush();
            // Extend the length of the file if needed.
            ActualizeVirtualPosition();
        }

        public virtual long CopyTo(UnsafeNativeMethods.IStream pstm, long cb, long[] pcbRead) {
            int bufsize = 4096; // one page
            IntPtr buffer = Marshal.AllocHGlobal(bufsize);
            if (buffer == IntPtr.Zero) throw new OutOfMemoryException();
            long written = 0;
            try {
                while (written < cb) {
                    int toRead = bufsize;
                    if (written + toRead > cb) toRead  = (int) (cb - written);
                    int read = Read(buffer, toRead);
                    if (read == 0) break;
                    if (pstm.Write(buffer, read) != read) {
                        throw EFail("Wrote an incorrect number of bytes");
                    }
                    written += read;
                }
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
            if (pcbRead != null && pcbRead.Length > 0) {
                pcbRead[0] = written;
            }

            return written;
        }

        public virtual Stream GetDataStream() {
            return dataStream;
        }

        public virtual void LockRegion(long libOffset, long cb, int dwLockType) {
        }

        protected static ExternalException EFail(string msg) {
            ExternalException e = new ExternalException(msg, SafeNativeMethods.E_FAIL);
            throw e;
        }

        protected static void NotImplemented() {
            ExternalException e = new ExternalException("Not implemented.", SafeNativeMethods.E_NOTIMPL);
            throw e;
        }

        public virtual int Read(IntPtr buf, /* cpr: int offset,*/  int length) {
            //        System.Text.Out.WriteLine("IStream::Read(" + length + ")");
            byte[] buffer = new byte[length];
            int count = Read(buffer, length);
            Marshal.Copy(buffer, 0, buf, length);
            return count;
        }

        public virtual int Read(byte[] buffer, /* cpr: int offset,*/  int length) {
            ActualizeVirtualPosition();
            return dataStream.Read(buffer, 0, length);
        }

        public virtual void Revert() {
            NotImplemented();
        }

        public virtual long Seek(long offset, int origin) {
            // Console.WriteLine("IStream::Seek("+ offset + ", " + origin + ")");
            long pos = virtualPosition;
            if (virtualPosition == -1) {
                pos = dataStream.Position;
            }
            long len = dataStream.Length;
            switch (origin) {
                case SafeNativeMethods.StreamConsts.STREAM_SEEK_SET:
                    if (offset <= len) {
                        dataStream.Position = offset;
                        virtualPosition = -1;
                    }
                    else {
                        virtualPosition = offset;
                    }
                    break;
                case SafeNativeMethods.StreamConsts.STREAM_SEEK_END:
                    if (offset <= 0) {
                        dataStream.Position = len + offset;
                        virtualPosition = -1;
                    }
                    else {
                        virtualPosition = len + offset;
                    }
                    break;
                case SafeNativeMethods.StreamConsts.STREAM_SEEK_CUR:
                    if (offset+pos <= len) {
                        dataStream.Position = pos + offset;
                        virtualPosition = -1;
                    }
                    else {
                        virtualPosition = offset + pos;
                    }
                    break;
            }
            if (virtualPosition != -1) {
                return virtualPosition;
            }
            else {
                return dataStream.Position;
            }
        }

        public virtual void SetSize(long value) {
            dataStream.SetLength(value);
        }

        public void Stat(IntPtr pstatstg, int grfStatFlag) {
            STATSTG stats = new STATSTG();
            stats.cbSize = dataStream.Length;
            Marshal.StructureToPtr(stats, pstatstg, true);
        }

        public virtual void UnlockRegion(long libOffset, long cb, int dwLockType) {
        }

        public virtual int Write(IntPtr buf, /* cpr: int offset,*/ int length) {
            byte[] buffer = new byte[length];
            Marshal.Copy(buf, buffer, 0, length);
            return Write(buffer, length);
        }

        public virtual int Write(byte[] buffer, /* cpr: int offset,*/ int length) {
            ActualizeVirtualPosition();
            dataStream.Write(buffer, 0, length);
            return length;
        }
    
        [StructLayout(LayoutKind.Sequential)]
        public class STATSTG {
            public   IntPtr pwcsName;
            public   int type = 0;
            [MarshalAs(UnmanagedType.I8)]
            public   long cbSize;
            [MarshalAs(UnmanagedType.I8)]
            public   long mtime;
            [MarshalAs(UnmanagedType.I8)]
            public   long ctime;
            [MarshalAs(UnmanagedType.I8)]
            public   long atime;
            [MarshalAs(UnmanagedType.I4)]
            public   int grfMode;
            [MarshalAs(UnmanagedType.I4)]
            public   int grfLocksSupported;
            
            public   int clsid_data1;
            [MarshalAs(UnmanagedType.I2)]
            public   short clsid_data2;
            [MarshalAs(UnmanagedType.I2)]
            public   short clsid_data3;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b0;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b1;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b2;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b3;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b4;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b5;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b6;
            [MarshalAs(UnmanagedType.U1)]
            public   byte clsid_b7;
            [MarshalAs(UnmanagedType.I4)]
            public   int grfStateBits;
            [MarshalAs(UnmanagedType.I4)]
            public   int reserved;
        }
    }
}
