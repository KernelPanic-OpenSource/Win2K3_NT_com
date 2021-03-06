// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
namespace System.Globalization {
    
    using System;
    // 
    // This class implements the Julian calendar. In 48 B.C. Julius Caesar ordered a calendar reform, and this calendar
    // is called Julian calendar. It consisted of a solar year of twelve months and of 365 days with an extra day 
    // every fourth year.
    //
    //  Calendar support range:
    //      Calendar    Minimum     Maximum
    //      ==========  ==========  ==========
    //      Gregorian   0001/01/01   9999/12/31
    //      Julian      0001/01/03   9999/10/19
    /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar"]/*' />
    [Serializable]
    public class JulianCalendar : Calendar {

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.JulianEra"]/*' />
        public static readonly int JulianEra = 1;

        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;    

        // Number of days in a non-leap year
        private const int JulianDaysPerYear      = 365;
        // Number of days in 4 years
        private const int JulianDaysPer4Years    = JulianDaysPerYear * 4 + 1;
        
        internal static Calendar m_defaultInstance = null;

        internal static readonly int[] DaysToMonth365 = 
        {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
        };
        
        internal static readonly int[] DaysToMonth366 = 
        {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
        };

        // Gregorian Calendar 9999/12/31 = Julian Calendar 9999/10/19
        internal int MaxYear = 9999;

        /*=================================GetDefaultInstance==========================
        **Action: Internal method to provide a default intance of JulianCalendar.  Used by NLS+ implementation
        **       and other calendars.
        **Returns:
        **Arguments:
        **Exceptions:
        ============================================================================*/

        internal static Calendar GetDefaultInstance() {
            if (m_defaultInstance == null) {
                m_defaultInstance = new JulianCalendar();
            }
            return (m_defaultInstance);
        }
        
        // Construct an instance of gregorian calendar.
        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.JulianCalendar"]/*' />
        public JulianCalendar() {
            // There is no system setting of TwoDigitYear max, so set the value here.
            twoDigitYearMax = 2029;
        }

        internal override int ID {
            get {
                return (CAL_JULIAN);
            }
        }
        
        internal void CheckYearEraRange(int year, int era) {
            if (era != CurrentEra && era != JulianEra) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));            
            }
            if (year <= 0 || year > MaxYear) {
                throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"),
                    1, MaxYear));            
            }
        }

        internal void CheckMonthRange(int month) {
            if (month < 1 || month > 12) {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
            }
        }

        /*=================================GetDefaultInstance==========================
        **Action: Check for if the day value is valid.
        **Returns:
        **Arguments:
        **Exceptions:
        **Notes:
        **  Before calling this method, call CheckYearEraRange()/CheckMonthRange() to make
        **  sure year/month values are correct.
        ============================================================================*/
        
        internal void CheckDayRange(int year, int month, int day) {
            if (year == 1 && month == 1)
            {
                // The mimimum supported Julia date is Julian 0001/01/03.
                if (day < 3) {
                    throw new ArgumentOutOfRangeException(
                        Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
                }
            }
            bool isLeapYear = (year % 4) == 0;
            int[] days = isLeapYear ? DaysToMonth366 : DaysToMonth365;
            int monthDays = days[month] - days[month - 1];
            if (day < 1 || day > monthDays) {
                throw new ArgumentOutOfRangeException("day", 
                    String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, monthDays));  
            }
        }
        
        
        // Returns a given date part of this DateTime. This method is used
        // to compute the year, day-of-year, month, or day part.
        internal int GetDatePart(long ticks, int part) 
        {
            // Gregorian 1/1/0001 is Julian 1/3/0001. Remember DateTime(0) is refered to Gregorian 1/1/0001.
            // The following line convert Gregorian ticks to Julian ticks.
            long julianTicks = ticks + TicksPerDay * 2;
            // n = number of days since 1/1/0001
            int n = (int)(julianTicks / TicksPerDay);
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / JulianDaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * JulianDaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / JulianDaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;        
            // If year was requested, compute and return it
            if (part == DatePartYear) 
            {
                return (y4 * 4 + y1 + 1);
            }
            // n = day number within year
            n -= y1 * JulianDaysPerYear;
            // If day-of-year was requested, return it
            if (part == DatePartDayOfYear) 
            {
                return (n + 1);
            }
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = (y1 == 3);
            int[] days = leapYear? DaysToMonth366: DaysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            int m = n >> 5 + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // If month was requested, return it
            if (part == DatePartMonth) return (m);
            // Return 1-based day-of-month
            return (n - days[m - 1] + 1);
        }        

        // Returns the tick count corresponding to the given year, month, and day.
        internal long DateToTicks(int year, int month, int day) 
        {
            int[] days = (year % 4 == 0)? DaysToMonth366: DaysToMonth365;
            int y = year - 1;
            int n = y * 365 + y / 4 + days[month - 1] + day - 1;
            // Gregorian 1/1/0001 is Julian 1/3/0001. n * TicksPerDay is the ticks in JulianCalendar.
            // Therefore, we subtract two days in the following to convert the ticks in JulianCalendar
            // to ticks in Gregorian calendar.
            return ((n - 2) * TicksPerDay);
        }
        
        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.AddMonths"]/*' />
        public override DateTime AddMonths(DateTime time, int months) 
        {
            if (months < -120000 || months > 120000) {
                throw new ArgumentOutOfRangeException(
                    "months", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    -120000, 120000));
            }
            int y = GetDatePart(time.Ticks, DatePartYear);
            int m = GetDatePart(time.Ticks, DatePartMonth);
            int d = GetDatePart(time.Ticks, DatePartDay);
            int i = m - 1 + months;
            if (i >= 0) {
                m = i % 12 + 1;
                y = y + i / 12;
            }
            else {
                m = 12 + (i + 1) % 12;
                y = y + (i - 11) / 12;
            }
            int[] daysArray = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) ? DaysToMonth366: DaysToMonth365;
            int days = (daysArray[m] - daysArray[m - 1]); 
            
            if (d > days) {
                d = days;
            }
            return (new DateTime(DateToTicks(y, m, d) + time.Ticks % TicksPerDay));
        }        

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.AddYears"]/*' />
        public override DateTime AddYears(DateTime time, int years) {
            return (AddMonths(time, years * 12));
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetDayOfMonth"]/*' />
        public override int GetDayOfMonth(DateTime time) {
            return (GetDatePart(time.Ticks, DatePartDay));
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetDayOfWeek"]/*' />
        public override DayOfWeek GetDayOfWeek(DateTime time) {
            return ((DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7));
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetDayOfYear"]/*' />
        public override int GetDayOfYear(DateTime time) {
            return (GetDatePart(time.Ticks, DatePartDayOfYear));
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetDaysInMonth"]/*' />
        public override int GetDaysInMonth(int year, int month, int era) {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            int[] days = (year % 4 == 0) ? DaysToMonth366: DaysToMonth365;
            return (days[month] - days[month - 1]);        
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetDaysInYear"]/*' />
        public override int GetDaysInYear(int year, int era) {
            // Year/Era range is done in IsLeapYear().
            return (IsLeapYear(year) ? 366:365);
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetEra"]/*' />
        public override int GetEra(DateTime time)
        {
            return (JulianEra);
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetMonth"]/*' />
        public override int GetMonth(DateTime time) 
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.Eras"]/*' />
        public override int[] Eras {
            get {
                return (new int[] {JulianEra});
            }
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetMonthsInYear"]/*' />
        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearEraRange(year, era);
            return (12);
        }    

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.GetYear"]/*' />
        public override int GetYear(DateTime time) 
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }    

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.IsLeapDay"]/*' />
        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            CheckMonthRange(month);
            // Year/Era range check is done in IsLeapYear().
            if (IsLeapYear(year, era)) {
                CheckDayRange(year, month, day);
                return (month == 2 && day == 29);
            }
            CheckDayRange(year, month, day);
            return (false);
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.IsLeapMonth"]/*' />
        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            return (false);            
        }    
    
        // Checks whether a given year in the specified era is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //
        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.IsLeapYear"]/*' />
        public override bool IsLeapYear(int year, int era)
        {
            CheckYearEraRange(year, era);
            return (year % 4 == 0);
        }

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.ToDateTime"]/*' />
        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            CheckDayRange(year, month, day);
            if (millisecond < 0 || millisecond >= MillisPerSecond) {
                throw new ArgumentOutOfRangeException("millisecond", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, MillisPerSecond - 1));
            }            
            
            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >=0 && second < 60)
            {
                return new DateTime(DateToTicks(year, month, day) + (new TimeSpan(0, hour, minute, second, millisecond)).Ticks);
            } else
            {
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond"));
            }
        }        

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.TwoDigitYearMax"]/*' />
        public override int TwoDigitYearMax {
            get {                
                return (twoDigitYearMax);
            }

            set {
                if (value < 100 || value > MaxYear) {
                    throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    100, MaxYear));

                }
                twoDigitYearMax = value;
            }
        } 

        /// <include file='doc\JulianCalendar.uex' path='docs/doc[@for="JulianCalendar.ToFourDigitYear"]/*' />
        public override int ToFourDigitYear(int year) {
            if (year > MaxYear) {
                throw new ArgumentOutOfRangeException("year",
                    String.Format(Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), 1, MaxYear)); 
            }
            return (base.ToFourDigitYear(year));
        }
    }

}
