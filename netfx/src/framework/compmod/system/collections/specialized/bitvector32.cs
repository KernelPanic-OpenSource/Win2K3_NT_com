//------------------------------------------------------------------------------
// <copyright file="BitVector32.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Collections.Specialized {

    using System.Diagnostics;
    using System.Text;
    using System;
    using Microsoft.Win32;

    /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32"]/*' />
    /// <devdoc>
    ///    <para>Provides a simple light bit vector with easy integer or Boolean access to
    ///       a 32 bit storage.</para>
    /// </devdoc>
    public struct BitVector32 {
        private uint data;

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.BitVector32"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector32 structure with the specified internal data.</para>
        /// </devdoc>
        public BitVector32(int data) {
            this.data = (uint)data;
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.BitVector321"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector32 structure with the information in the specified 
        ///    value.</para>
        /// </devdoc>
        public BitVector32(BitVector32 value) {
            this.data = value.data;
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.this"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether all the specified bits are set.</para>
        /// </devdoc>
        public bool this[int bit] {
            get {
                return (data & bit) == bit;
            }
            set {
                if (value) {
                    data |= (uint)bit;
                }
                else {
                    data &= ~(uint)bit;
                }
            }
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.this1"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the value for the specfied section.</para>
        /// </devdoc>
        public int this[Section section] {
            get {
                return (int)((data & (uint)(section.Mask << section.Offset)) >> section.Offset);
            }
            set {
#if DEBUG
                if ((value & section.Mask) != value) {
                    Debug.Fail("Value out of bounds on BitVector32 Section Set!");
                }
#endif
                value <<= section.Offset;
                int offsetMask = (0xFFFF & (int)section.Mask) << section.Offset;
                data = (data & ~(uint)offsetMask) | ((uint)value & (uint)offsetMask);
            }
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Data"]/*' />
        /// <devdoc>
        ///    returns the raw data stored in this bit vector...
        /// </devdoc>
        public int Data {
            get {
                return (int)data;
            }
        }

        private static short CountBitsSet(short mask) {

            // yes, I know there are better algorythms, however, we know the
            // bits are always right aligned, with no holes (i.e. always 00000111,
            // never 000100011), so this is just fine...
            //
            short value = 0;
            while ((mask & 0x1) != 0) {
                value++;
                mask >>= 1;
            }
            return value;
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.CreateMask"]/*' />
        /// <devdoc>
        ///    <para> Creates the first mask in a series.</para>
        /// </devdoc>
        public static int CreateMask() {
            return CreateMask(0);
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.CreateMask1"]/*' />
        /// <devdoc>
        ///     Creates the next mask in a series.
        /// </devdoc>
        public static int CreateMask(int previous) {
            if (previous == 0) {
                return 1;
            }

            if (previous == unchecked((int)0x80000000)) {
                throw new InvalidOperationException(SR.GetString(SR.BitVectorFull));
            }

            return previous << 1;
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.CreateMaskFromHighValue"]/*' />
        /// <devdoc>
        ///     Given a highValue, creates the mask
        /// </devdoc>
        private static short CreateMaskFromHighValue(short highValue) {
            short required = 16;
            while ((highValue & 0x8000) == 0) {
                required--;
                highValue <<= 1;
            }

            ushort value = 0;
            while (required > 0) {
                required--;
                value <<= 1;
                value |= 0x1;
            }

            return unchecked((short) value);
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.CreateSection"]/*' />
        /// <devdoc>
        ///    <para>Creates the first section in a series, with the specified maximum value.</para>
        /// </devdoc>
        public static Section CreateSection(short maxValue) {
            return CreateSectionHelper(maxValue, 0, 0);
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.CreateSection1"]/*' />
        /// <devdoc>
        ///    <para>Creates the next section in a series, with the specified maximum value.</para>
        /// </devdoc>
        public static Section CreateSection(short maxValue, Section previous) {
            return CreateSectionHelper(maxValue, previous.Mask, previous.Offset);
        }

        private static Section CreateSectionHelper(short maxValue, short priorMask, short priorOffset) {
            if (maxValue < 1) {
                throw new ArgumentException("maxValue");
            }
#if DEBUG
            int maskCheck = CreateMaskFromHighValue(maxValue);
            int offsetCheck = priorOffset + CountBitsSet(priorMask);
            Debug.Assert(maskCheck <= short.MaxValue && offsetCheck < 32, "Overflow on BitVector32");
#endif
            short offset = (short)(priorOffset + CountBitsSet(priorMask));
            if (offset >= 32) {
                throw new InvalidOperationException(SR.GetString(SR.BitVectorFull));
            }
            return new Section(CreateMaskFromHighValue(maxValue), offset);
        }
        
        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Equals"]/*' />
        public override bool Equals(object o) {
            if (!(o is BitVector32)) {
                return false;
            }
            
            return data == ((BitVector32)o).data;
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.GetHashCode"]/*' />
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public static string ToString(BitVector32 value) {
            StringBuilder sb = new StringBuilder(/*"BitVector32{".Length*/12 + /*32 bits*/32 + /*"}".Length"*/1);
            sb.Append("BitVector32{");
            int locdata = (int)value.data;
            for (int i=0; i<32; i++) {
                if ((locdata & 0x80000000) != 0) {
                    sb.Append("1");
                }
                else {
                    sb.Append("0");
                }
                locdata <<= 1;
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.ToString1"]/*' />
        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override string ToString() {
            return BitVector32.ToString(this);
        }

        /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Section"]/*' />
        /// <devdoc>
        ///    <para> 
        ///       Represents an section of the vector that can contain a integer number.</para>
        /// </devdoc>
        public struct Section {
            private readonly short mask;
            private readonly short offset;

            internal Section(short mask, short offset) {
                this.mask = mask;
                this.offset = offset;
            }

            /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Section.Mask"]/*' />
            /// <internalonly/>
            public short Mask {
                get {
                    return mask;
                }
            }

            /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Section.Offset"]/*' />
            /// <internalonly/>
            public short Offset {
                get {
                    return offset;
                }
            }
            
            /// <include file='doc\BitVector32.uex' path='docs/doc[@for="Section.Equals"]/*' />
            /// <internalonly/>
            public override bool Equals(object o) {
                if (!(o is Section)) {
                    return false;
                }
                
                Section s = (Section)o;
                return mask == s.mask && offset == s.offset;
            }
            
            /// <include file='doc\BitVector32.uex' path='docs/doc[@for="Section.GetHashCode"]/*' />
            /// <internalonly/>
            public override int GetHashCode() {
                return base.GetHashCode();
            }

            /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Section.ToString"]/*' />
            /// <internalonly/>
            /// <devdoc>
            /// </devdoc>
            public static string ToString(Section value) {
                return "Section{0x" + Convert.ToString(value.Mask, 16) + ", 0x" + Convert.ToString(value.Offset, 16) + "}";
            }

            /// <include file='doc\BitVector32.uex' path='docs/doc[@for="BitVector32.Section.ToString1"]/*' />
            /// <internalonly/>
            /// <devdoc>
            /// </devdoc>
            public override string ToString() {
                return Section.ToString(this);
            }

        }
    }
 }
