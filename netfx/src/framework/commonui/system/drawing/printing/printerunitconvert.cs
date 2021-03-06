//------------------------------------------------------------------------------
// <copyright file="PrinterUnitConvert.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Drawing.Printing {
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;    
    using System.Drawing;
    using System.ComponentModel;
    using Microsoft.Win32;

    /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies a series of conversion methods that are
    ///       useful when interoperating with the raw Win32 printing API.
    ///       This class cannot be inherited.
    ///    </para>
    /// </devdoc>
    public sealed class PrinterUnitConvert {
        private PrinterUnitConvert() {
        }

        /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert.Convert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the value, in fromUnit units, to toUnit units.
        ///    </para>
        /// </devdoc>
        public static double Convert(double value, PrinterUnit fromUnit, PrinterUnit toUnit) {
            double fromUnitsPerDisplay = UnitsPerDisplay(fromUnit);
            double toUnitsPerDisplay = UnitsPerDisplay(toUnit);
            return value * toUnitsPerDisplay / fromUnitsPerDisplay;
        }

        /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert.Convert1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the value, in fromUnit units, to toUnit units.
        ///    </para>
        /// </devdoc>
        public static int Convert(int value, PrinterUnit fromUnit, PrinterUnit toUnit) {
            return(int) Math.Round(Convert((double)value, fromUnit, toUnit));
        }

        /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert.Convert2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the value, in fromUnit units, to toUnit units.
        ///    </para>
        /// </devdoc>
        public static Point Convert(Point value, PrinterUnit fromUnit, PrinterUnit toUnit) {
            return new Point(
                            Convert(value.X, fromUnit, toUnit),
                            Convert(value.Y, fromUnit, toUnit)
                            );
        }

        /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert.Convert3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the value, in fromUnit units, to toUnit units.
        ///    </para>
        /// </devdoc>
        public static Size Convert(Size value, PrinterUnit fromUnit, PrinterUnit toUnit) {
            return new Size(
                           Convert(value.Width, fromUnit, toUnit),
                           Convert(value.Height, fromUnit, toUnit)
                           );
        }

        /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert.Convert4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the value, in fromUnit units, to toUnit units.
        ///    </para>
        /// </devdoc>
        public static Rectangle Convert(Rectangle value, PrinterUnit fromUnit, PrinterUnit toUnit) {
            return new Rectangle(
                                Convert(value.X, fromUnit, toUnit),
                                Convert(value.Y, fromUnit, toUnit),
                                Convert(value.Width, fromUnit, toUnit),
                                Convert(value.Height, fromUnit, toUnit)
                                );
        }

        /// <include file='doc\PrinterUnitConvert.uex' path='docs/doc[@for="PrinterUnitConvert.Convert5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the value, in fromUnit units, to toUnit units.
        ///    </para>
        /// </devdoc>
        public static Margins Convert(Margins value, PrinterUnit fromUnit, PrinterUnit toUnit) {
            Margins result = new Margins();

            result.Left = Convert(value.Left, fromUnit, toUnit);
            result.Right = Convert(value.Right, fromUnit, toUnit);
            result.Top = Convert(value.Top, fromUnit, toUnit);
            result.Bottom = Convert(value.Bottom, fromUnit, toUnit);

            return result;
        }

        private static double UnitsPerDisplay(PrinterUnit unit) {
            double result;
            switch (unit) {
                case PrinterUnit.Display:
                    result = 1.0;
                    break;
                case PrinterUnit.ThousandthsOfAnInch:
                    result = 10.0;
                    break;
                case PrinterUnit.HundredthsOfAMillimeter:
                    result = 25.4;
                    break;
                case PrinterUnit.TenthsOfAMillimeter:
                    result = 2.54;
                    break;
                default:
                    Debug.Fail("Unknown PrinterUnit " + unit);
                    result = 1.0;
                    break; 
            }

            return result;
        }
    }
}

