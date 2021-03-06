//------------------------------------------------------------------------------
// <copyright file="EncoderParameter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/**************************************************************************\
*
* Copyright (c) 1998-1999, Microsoft Corp.  All Rights Reserved.
*
* Module Name:
*
*   EncoderParameter.cs
*
* Abstract:
*
*   Native GDI+ EncoderParam structure.
*
* Revision History:
*
*   9/22/1999 ericvan
*       Created it.
*
\**************************************************************************/

namespace System.Drawing.Imaging {
    using System;
    using System.Text;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Drawing.Internal;

    /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class EncoderParameter : IDisposable {
         
        [MarshalAs(UnmanagedType.Struct)]
        Guid parameterGuid;                    // GUID of the parameter
        int numberOfValues;                    // Number of the parameter values  
        int parameterValueType;                // Value type, like ValueTypeLONG  etc.
        IntPtr parameterValue;                    // A pointer to the parameter values
        
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.Finalize"]/*' />
         ~EncoderParameter() {
             Dispose(false);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.Encoder"]/*' />
         /// <devdoc>
         ///    Gets/Sets the Encoder for the EncoderPameter.
         /// </devdoc>
         public Encoder Encoder {
            get {
                return new Encoder(parameterGuid);
            }
            set {
                parameterGuid = Encoder.Guid;
            }
         }
         
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.Type"]/*' />
         /// <devdoc>
         ///    Gets the EncoderParameterValueType object from the EncoderParameter.
         /// </devdoc>
         public EncoderParameterValueType Type {
            get { 
                return (EncoderParameterValueType)parameterValueType;
            }
         }
         
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.ValueType"]/*' />
         /// <devdoc>
         ///    Gets the EncoderParameterValueType object from the EncoderParameter.
         /// </devdoc>
         public EncoderParameterValueType ValueType {
            get { 
                return (EncoderParameterValueType)parameterValueType;
            }
         }
         
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.NumberOfValues"]/*' />
         /// <devdoc>
         ///    Gets the NumberOfValues from the EncoderParameter.
         /// </devdoc>
         public int NumberOfValues {
            get {
                return numberOfValues;
            }
         }
         
         IntPtr ValuePointer {
            get {
                return parameterValue;
            }
         }
         
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.Dispose"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public void Dispose() {
             Dispose(true);
             GC.KeepAlive(this);
             GC.SuppressFinalize(this);
         }

         void Dispose(bool disposing) {
             if (parameterValue != IntPtr.Zero)
                 Marshal.FreeHGlobal(parameterValue);
             parameterValue = IntPtr.Zero;
         }
         
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, byte value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeByte;
             numberOfValues = 1;
             parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Byte)));
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
                 
             Marshal.WriteByte(parameterValue, value);
             GC.KeepAlive(this);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter1"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, byte value, bool undefined)
         {
             parameterGuid = encoder.Guid;
             
             if (undefined == true)
                 parameterValueType = (int)EncoderParameterValueType.ValueTypeUndefined;
             else               
                 parameterValueType = (int)EncoderParameterValueType.ValueTypeByte;
             numberOfValues = 1;
             parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Byte)));
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
                 
             Marshal.WriteByte(parameterValue, value);
             GC.KeepAlive(this);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter2"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, short value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeShort;
             numberOfValues = 1;
             parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Int16)));
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             Marshal.WriteInt16(parameterValue, value);
             GC.KeepAlive(this);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter3"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, long value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeLong;
             numberOfValues = 1;
             parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Int32)));
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             Marshal.WriteInt32(parameterValue, (int)value);
             GC.KeepAlive(this);
         }
                  
         // Consider supporting a 'float' and converting to numerator/denominator                               
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter4"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, int numerator, int demoninator)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeRational;
             numberOfValues = 1;
             int size = Marshal.SizeOf(typeof(Int32));
             parameterValue = Marshal.AllocHGlobal(2*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             Marshal.WriteInt32(parameterValue, numerator);
             Marshal.WriteInt32(Add(parameterValue, size), demoninator);
             GC.KeepAlive(this);
         }
         
         // Consider supporting a 'float' and converting to numerator/denominator                               
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter5"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, long rangebegin, long rangeend)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeLongRange;
             numberOfValues = 1;
             int size = Marshal.SizeOf(typeof(Int32));
             parameterValue = Marshal.AllocHGlobal(2*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);

             Marshal.WriteInt32(parameterValue, (int)rangebegin);
             Marshal.WriteInt32(Add(parameterValue, size), (int)rangeend);
             GC.KeepAlive(this);
         }

         // Consider supporting a 'float' and converting to numerator/denominator                               
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter6"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, 
                                 int numerator1, int demoninator1,
                                 int numerator2, int demoninator2)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeRationalRange;
             numberOfValues = 1;
             int size = Marshal.SizeOf(typeof(Int32));
             parameterValue = Marshal.AllocHGlobal(4*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             Marshal.WriteInt32(parameterValue, numerator1);
             Marshal.WriteInt32(Add(parameterValue, size), demoninator1);
             Marshal.WriteInt32(Add(parameterValue, 2*size), numerator2);
             Marshal.WriteInt32(Add(parameterValue, 3*size), demoninator2);
             GC.KeepAlive(this);
         }
         
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter7"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, string value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeAscii;
             numberOfValues = value.Length;
             parameterValue = Marshal.StringToHGlobalAnsi(value);
             GC.KeepAlive(this);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter8"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, byte[] value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeByte;
             numberOfValues = value.Length;
             
             parameterValue = Marshal.AllocHGlobal(numberOfValues);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
                 
             Marshal.Copy(value, 0, parameterValue, numberOfValues);
             GC.KeepAlive(this);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter9"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, byte[] value, bool undefined)
         {
             parameterGuid = encoder.Guid;
             
             if (undefined == true)
                 parameterValueType = (int)EncoderParameterValueType.ValueTypeUndefined;
             else               
                 parameterValueType = (int)EncoderParameterValueType.ValueTypeByte;
                 
             numberOfValues = value.Length;
             parameterValue = Marshal.AllocHGlobal(numberOfValues);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
                 
             Marshal.Copy(value, 0, parameterValue, numberOfValues);
             GC.KeepAlive(this);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter10"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, short[] value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeShort;
             numberOfValues = value.Length;
             int size = Marshal.SizeOf(typeof(short));
             
             parameterValue = Marshal.AllocHGlobal(numberOfValues*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             Marshal.Copy(value, 0, parameterValue, numberOfValues);
             GC.KeepAlive(this);
         }

         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter11"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public unsafe EncoderParameter(Encoder encoder, long[] value)
         {
             parameterGuid = encoder.Guid;
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeLong;
             numberOfValues = value.Length;
             int size = Marshal.SizeOf(typeof(Int32));
             
             parameterValue = Marshal.AllocHGlobal(numberOfValues*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             int* dest = (int*)parameterValue;
             fixed (long* source = value) {
                 for (int i=0; i<value.Length; i++) {
                     dest[i] = (int)source[i];
                 }
             }
             GC.KeepAlive(this);
         }
                  
         // Consider supporting a 'float' and converting to numerator/denominator                               
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter12"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, int[] numerator, int[] denominator)
         {
             parameterGuid = encoder.Guid;
             
             if (numerator.Length != denominator.Length)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.InvalidParameter);
                                                                
             parameterValueType = (int)EncoderParameterValueType.ValueTypeRational;
             numberOfValues = numerator.Length;
             int size = Marshal.SizeOf(typeof(Int32));
             
             parameterValue = Marshal.AllocHGlobal(numberOfValues*2*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);

             for (int i=0; i<numberOfValues; i++)
             {             
                 Marshal.WriteInt32(Add(i*2*size, parameterValue), (int)numerator[i]);
                 Marshal.WriteInt32(Add((i*2+1)*size, parameterValue), (int)denominator[i]);
             }
             GC.KeepAlive(this);
         }
         
         // Consider supporting a 'float' and converting to numerator/denominator                               
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter13"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, long[] rangebegin, long[] rangeend)
         {
             parameterGuid = encoder.Guid;
             
             if (rangebegin.Length != rangeend.Length)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.InvalidParameter);
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeLongRange;
             numberOfValues = rangebegin.Length;
             int size = Marshal.SizeOf(typeof(Int32));
             
             parameterValue = Marshal.AllocHGlobal(numberOfValues*2*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);

             for (int i=0; i<numberOfValues; i++)
             {
                 Marshal.WriteInt32(Add(i*2*size, parameterValue), (int)rangebegin[i]);
                 Marshal.WriteInt32(Add((i*2+1)*size, parameterValue), (int)rangeend[i]);
             }
             GC.KeepAlive(this);
         }
      
         // Consider supporting a 'float' and converting to numerator/denominator                               
         /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter14"]/*' />
         /// <devdoc>
         ///    <para>[To be supplied.]</para>
         /// </devdoc>
         public EncoderParameter(Encoder encoder, 
                                 int[] numerator1, int[] denominator1,
                                 int[] numerator2, int[] denominator2)
         {
             parameterGuid = encoder.Guid;
             
             if (numerator1.Length != denominator1.Length ||
                 numerator1.Length != denominator2.Length ||
                 denominator1.Length != denominator2.Length)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.InvalidParameter);
             
             parameterValueType = (int)EncoderParameterValueType.ValueTypeRationalRange;
             numberOfValues = numerator1.Length;
             int size = Marshal.SizeOf(typeof(Int32));
             
             parameterValue = Marshal.AllocHGlobal(numberOfValues*4*size);
             
             if (parameterValue == IntPtr.Zero)
                 throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);
             
             for (int i=0; i<numberOfValues; i++)
             {
                 Marshal.WriteInt32(Add(parameterValue, 4*i*size), numerator1[i]);
                 Marshal.WriteInt32(Add(parameterValue, (4*i+1)*size), denominator1[i]);
                 Marshal.WriteInt32(Add(parameterValue, (4*i+2)*size), numerator2[i]);
                 Marshal.WriteInt32(Add(parameterValue, (4*i+3)*size), denominator2[i]);
             }
             GC.KeepAlive(this);
        }
        
        // Consider supporting a 'float' and converting to numerator/denominator                               
        /// <include file='doc\EncoderParameter.uex' path='docs/doc[@for="EncoderParameter.EncoderParameter15"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public EncoderParameter(Encoder encoder, int NumberOfValues, int Type, int Value)
        {
            int size;
            
            switch((EncoderParameterValueType)Type)
            {
            case EncoderParameterValueType.ValueTypeByte:
            case EncoderParameterValueType.ValueTypeAscii: size = 1; break;
            case EncoderParameterValueType.ValueTypeShort: size = 2; break;
            case EncoderParameterValueType.ValueTypeLong: size = 4; break;
            case EncoderParameterValueType.ValueTypeRational: 
            case EncoderParameterValueType.ValueTypeLongRange: size = 2*4; break;
            case EncoderParameterValueType.ValueTypeUndefined: size = 1; break;
            case EncoderParameterValueType.ValueTypeRationalRange: size = 2*2*4; break;
            default:
                throw SafeNativeMethods.StatusException(SafeNativeMethods.WrongState);
            }

            int bytes = size*NumberOfValues;
            
            parameterValue = Marshal.AllocHGlobal(bytes);
                        
            if (parameterValue == IntPtr.Zero)
                throw SafeNativeMethods.StatusException(SafeNativeMethods.OutOfMemory);

            for (int i=0; i<bytes; i++)
            {
                Marshal.WriteByte(Add(parameterValue, i), Marshal.ReadByte((IntPtr)(Value + i)));
            }
            
            parameterValueType = Type;
            numberOfValues = NumberOfValues;
            parameterGuid = encoder.Guid;              
            GC.KeepAlive(this);
        }

        private static IntPtr Add(IntPtr a, int b) {
            return (IntPtr) ((long)a + (long)b);
        }

        private static IntPtr Add(int a, IntPtr b) {
            return (IntPtr) ((long)a + (long)b);
        }
    }
}
