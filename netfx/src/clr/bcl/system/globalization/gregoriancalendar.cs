// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
namespace System.Globalization {
    //
    // N.B.:
    // A lot of this code is stolen directly from DateTime.  If you update that class,
    // update this one as well.
    // However, we still need these duplicated code because we will add era support
    // in this class.
    // @Consider: Check if DateTime should use the code here so we will not have duplicated code.
    //
    //

    // NOTE YSLin:
    // This class is also used as a base class by other Gregorian-based classes.
    // Therefore, methods like IsLeapYear(int year, int era) are often overriden by other classes.
    // Be sure NOT to call public virtual methods (like IsLeapYear()) in the implementation of this class.  
    // Otherwise, you may end up calling the overriden version in the child class.

    using System.Threading;
    using System;

    /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes"]/*' />
    [Serializable]
    public enum GregorianCalendarTypes {
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes.Localized"]/*' />
        Localized = Calendar.CAL_GREGORIAN,
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes.USEnglish"]/*' />
        USEnglish = Calendar.CAL_GREGORIAN_US,
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes.MiddleEastFrench"]/*' />
        MiddleEastFrench = Calendar.CAL_GREGORIAN_ME_FRENCH,
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes.Arabic"]/*' />
        Arabic = Calendar.CAL_GREGORIAN_ARABIC,
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes.TransliteratedEnglish"]/*' />
        TransliteratedEnglish = Calendar.CAL_GREGORIAN_XLIT_ENGLISH,
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendarTypes.TransliteratedFrench"]/*' />
        TransliteratedFrench = Calendar.CAL_GREGORIAN_XLIT_FRENCH,
    }

    // 
    // This class implements the Gregorian calendar. In 1582, Pope Gregory XIII made 
    // minor changes to the solar Julian or "Old Style" calendar to make it more 
    // accurate. Thus the calendar became known as the Gregorian or "New Style" 
    // calendar, and adopted in Catholic countries such as Spain and France. Later 
    // the Gregorian calendar became popular throughout Western Europe because it 
    // was accurate and convenient for international trade. Scandinavian countries 
    // adopted it in 1700, Great Britain in 1752, the American colonies in 1752 and 
    // India in 1757. China adopted the same calendar in 1911, Russia in 1918, and 
    // some Eastern European countries as late as 1940.
    // 
    // This calendar recognizes two era values:
    // 0 CurrentEra (AD) 
    // 1 BeforeCurrentEra (BC) 
    /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar"]/*' />
    [Serializable()] public class GregorianCalendar : Calendar
    {
        /*
            A.D. = anno Domini (after the birth of Jesus Christ)
         */
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.ADEra"]/*' />
        public const int ADEra = 1;

        
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;    

        //
        // This is the max Gregorian year can be represented by DateTime class.  The limitation
        // is derived from DateTime class.
        // 
        internal const int MaxYear = 9999;

        internal GregorianCalendarTypes m_type;
        
        internal static readonly int[] DaysToMonth365 = 
        {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
        };
        
        internal static readonly int[] DaysToMonth366 = 
        {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
        };

        internal static Calendar m_defaultInstance = null;

        /*=================================GetDefaultInstance==========================
        **Action: Internal method to provide a default intance of GregorianCalendar.  Used by NLS+ implementation
        **       and other calendars.
        **Returns:
        **Arguments:
        **Exceptions:
        ============================================================================*/

        internal static Calendar GetDefaultInstance() {
            if (m_defaultInstance == null) {
                m_defaultInstance = new GregorianCalendar();
            }
            return (m_defaultInstance);
        }
    
        // Construct an instance of gregorian calendar.
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GregorianCalendar"]/*' />
        public GregorianCalendar() :
            this(GregorianCalendarTypes.Localized) {
        }

        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GregorianCalendar1"]/*' />
        public GregorianCalendar(GregorianCalendarTypes type) {
            this.m_type = type;
        }

        // BUGBUG YSLin:
        // So there is a bug here.  This makes other calendar derived from Gregorian get this as well, but
        // this is not what we want.  How do I solve this?  Can I hide this in other derived classes?
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.CalendarType"]/*' />
        public virtual GregorianCalendarTypes CalendarType {
            get {
                return (m_type);
            }

            set {
                m_type = value;
            }
        }
        
        internal override int ID {
            get {
                // By returning different ID for different variations of GregorianCalendar, 
                // we can support the Transliterated Gregorian calendar.
                // DateTimeFormatInfo will use this ID to get formatting information about
                // the calendar.
                return ((int)m_type);
            }
        }

    
        // Returns a given date part of this DateTime. This method is used
        // to compute the year, day-of-year, month, or day part.
        internal virtual int GetDatePart(long ticks, int part) 
        {
            // n = number of days since 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;        
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;        
            // If year was requested, compute and return it
            if (part == DatePartYear) 
            {
                return (y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1);
            }
            // n = day number within year
            n -= y1 * DaysPerYear;
            // If day-of-year was requested, return it
            if (part == DatePartDayOfYear) 
            {
                return (n + 1);
            }
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = (y1 == 3 && (y4 != 24 || y100 == 3));
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

        /*=================================GetAbsoluteDate==========================
        **Action: Gets the absolute date for the given Gregorian date.  The absolute date means
        **       the number of days from January 1st, 1 A.D.
        **Returns:  the absolute date
        **Arguments:
        **      year    the Gregorian year
        **      month   the Gregorian month
        **      day     the day
        **Exceptions:
        **      ArgumentOutOfRangException  if year, month, day value is valid.
        **Note:
        **      This is an internal method used by DateToTicks() and the calculations of Hijri and Hebrew calendars.
        **      Number of Days in Prior Years (both common and leap years) +
        **      Number of Days in Prior Months of Current Year +
        **      Number of Days in Current Month
        **
        ============================================================================*/

        internal static long GetAbsoluteDate(int year, int month, int day) {
            if (year >= 1 && year <= MaxYear && month >= 1 && month <= 12) 
            {
                int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0))) ? DaysToMonth366: DaysToMonth365;
                if (day >= 1 && (day <= days[month] - days[month - 1])) {
                    int y = year - 1;
                    int absoluteDate = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
                    return (absoluteDate);
                }
            }
            throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
        }        

        // Returns the tick count corresponding to the given year, month, and day.
        // Will check the if the parameters are valid.
        internal virtual long DateToTicks(int year, int month, int day) {
            return (GetAbsoluteDate(year, month,  day)* TicksPerDay);
        }
            
        // Returns the DateTime resulting from adding the given number of
        // months to the specified DateTime. The result is computed by incrementing
        // (or decrementing) the year and month parts of the specified DateTime by
        // value months, and, if required, adjusting the day part of the
        // resulting date downwards to the last day of the resulting month in the
        // resulting year. The time-of-day part of the result is the same as the
        // time-of-day part of the specified DateTime.
        //
        // In more precise terms, considering the specified DateTime to be of the
        // form y / m / d + t, where y is the
        // year, m is the month, d is the day, and t is the
        // time-of-day, the result is y1 / m1 / d1 + t,
        // where y1 and m1 are computed by adding value months
        // to y and m, and d1 is the largest value less than
        // or equal to d that denotes a valid day in month m1 of year
        // y1.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.AddMonths"]/*' />
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
            if (i >= 0) 
            {
                m = i % 12 + 1;
                y = y + i / 12;
            }
            else 
            {
                m = 12 + (i + 1) % 12;
                y = y + (i - 11) / 12;
            }
            int[] daysArray = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) ? DaysToMonth366: DaysToMonth365;
            int days = (daysArray[m] - daysArray[m - 1]); 
            
            if (d > days) 
            {
                d = days;
            }
            return (new DateTime(DateToTicks(y, m, d) + time.Ticks % TicksPerDay));
        }
    
        // Returns the DateTime resulting from adding a number of
        // weeks to the specified DateTime. The
        // value argument is permitted to be negative.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.AddWeeks"]/*' />
        public override DateTime AddWeeks(DateTime time, int weeks)
        {
            return (AddDays(time, weeks * 7));
        }
        
        // Returns the DateTime resulting from adding the given number of
        // years to the specified DateTime. The result is computed by incrementing
        // (or decrementing) the year part of the specified DateTime by value
        // years. If the month and day of the specified DateTime is 2/29, and if the
        // resulting year is not a leap year, the month and day of the resulting
        // DateTime becomes 2/28. Otherwise, the month, day, and time-of-day
        // parts of the result are the same as those of the specified DateTime.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.AddYears"]/*' />
        public override DateTime AddYears(DateTime time, int years) 
        {
            return (AddMonths(time, years * 12));
        }
    
        // Returns the day-of-month part of the specified DateTime. The returned
        // value is an integer between 1 and 31.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetDayOfMonth"]/*' />
        public override int GetDayOfMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDay));
        }
    
        // Returns the day-of-week part of the specified DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetDayOfWeek"]/*' />
        public override DayOfWeek GetDayOfWeek(DateTime time) 
        {
            return ((DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7));
        }
    
        // Returns the day-of-year part of the specified DateTime. The returned value
        // is an integer between 1 and 366.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetDayOfYear"]/*' />
        public override int GetDayOfYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDayOfYear));
        }

        // Returns the number of days in the month given by the year and
        // month arguments.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetDaysInMonth"]/*' />
        public override int GetDaysInMonth(int year, int month, int era) {
            if (era == CurrentEra || era == ADEra) {
                if (month < 1 || month > 12) {
                    throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
                }
                int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366: DaysToMonth365);
                return (days[month] - days[month - 1]);        
            }
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
        }
    
        // Returns the number of days in the year given by the year argument for the current era.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetDaysInYear"]/*' />
        public override int GetDaysInYear(int year, int era)
        {
            if (era == CurrentEra || era == ADEra) {
                if (year >= 1 && year <= MaxYear) {
                    return ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? 366:365);
                }
                throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, MaxYear));
            }
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
        }
    
        // Returns the era for the specified DateTime value.
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetEra"]/*' />
        public override int GetEra(DateTime time)
        {
            return (ADEra);
        }

        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.Eras"]/*' />
        public override int[] Eras {
            get {
                return (new int[] {ADEra} );
            }
        }

            
        // Returns the month part of the specified DateTime. The returned value is an
        // integer between 1 and 12.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetMonth"]/*' />
        public override int GetMonth(DateTime time) 
        {
    		return (GetDatePart(time.Ticks, DatePartMonth));
        }
    
        // Returns the number of months in the specified year and era.
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetMonthsInYear"]/*' />
        public override int GetMonthsInYear(int year, int era)
        {
            if (era == CurrentEra || era == ADEra) {
                if (year >= 1 && year <= MaxYear)    
                {
                    return (12);
                }
                throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, MaxYear));
            }
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
        }
                
        // Returns the year part of the specified DateTime. The returned value is an
        // integer between 1 and 9999.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.GetYear"]/*' />
        public override int GetYear(DateTime time) 
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }    
    
        // Checks whether a given day in the specified era is a leap day. This method returns true if
        // the date is a leap day, or false if not.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.IsLeapDay"]/*' />
        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            if (era != CurrentEra && era != ADEra) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
            }
            if (year < 1 || year > MaxYear) {
                throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }            
            
            if (month < 1 || month > 12) {
                throw new ArgumentOutOfRangeException("month", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, 12));                
            }
            if (day < 1 || day > GetDaysInMonth(year, month)) {
                throw new ArgumentOutOfRangeException("day", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, GetDaysInMonth(year, month)));                
            }
            if (!IsLeapYear(year)) {
                return (false);
            }
            if (month == 2 && day == 29) {
                return (true);
            }
            return (false);
        }
    
        // Checks whether a given month in the specified era is a leap month. This method returns true if
        // month is a leap month, or false if not.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.IsLeapMonth"]/*' />
        public override bool IsLeapMonth(int year, int month, int era)
        {
            if (era != CurrentEra && era != ADEra) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
            }            
            if (year < 1 || year > MaxYear) {
                throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }                        
            if (month < 1 || month > 12) {
                throw new ArgumentOutOfRangeException("month", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, 12));                
            }
            return (false);

        }    
    
        // Checks whether a given year in the specified era is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.IsLeapYear"]/*' />
        public override bool IsLeapYear(int year, int era) {
            if (era == CurrentEra || era == ADEra) {
                if (year >= 1 && year <= MaxYear) {
                    return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));
                }
                throw new ArgumentOutOfRangeException("year", String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 
                    1, MaxYear));
            }
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
        }
    
        // Returns the date and time converted to a DateTime value.  Throws an exception if the n-tuple is invalid.
        //
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.ToDateTime"]/*' />
        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            if (era == CurrentEra || era == ADEra) {
                return new DateTime(year, month, day, hour, minute, second, millisecond);
            }
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEraValue"));
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 2029;            
        
        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.TwoDigitYearMax"]/*' />
        public override int TwoDigitYearMax
        {
            get {
                if (twoDigitYearMax == -1) {
                    twoDigitYearMax = GetSystemTwoDigitYearSetting(ID, DEFAULT_TWO_DIGIT_YEAR_MAX);
                }
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

        /// <include file='doc\GregorianCalendar.uex' path='docs/doc[@for="GregorianCalendar.ToFourDigitYear"]/*' />
        public override int ToFourDigitYear(int year) {
            if (year > MaxYear) {
                throw new ArgumentOutOfRangeException("year",
                    String.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear)); 
            }
            return (base.ToFourDigitYear(year));
        }
    }
}
