using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Bee.Eee.Utility.Extensions.DateTimeExtensions
{
    public static class DateTimeExtensions
    {
        public static string ToStringSql(this DateTime value, bool quoted = false)
		{
			if (quoted)
				return String.Format("'{0}'", value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
			else
				return value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		}

		public static DateTime Round(this DateTime date, TimeSpan span)
        {
            if (date == DateTime.MaxValue) return date;
            long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }

        public static DateTime Floor(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks / span.Ticks);
            return new DateTime(ticks * span.Ticks);
        }

        public static DateTime Ceil(this DateTime date, TimeSpan span)
        {
            if( date == DateTime.MaxValue) return date;
            long ticks = (date.Ticks + span.Ticks - 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }

		/// <summary>
		/// Extension to return the start of day of current DateTime
		/// </summary>
		public static DateTime ToStartOfDay(this DateTime date)
		{
			return date.Subtract(date.TimeOfDay);
		}

		/// <summary>
		/// Extension to return the start of year of current DateTime
		/// </summary>
		public static DateTime ToYear(this DateTime date)
		{
			return new DateTime(date.Year, 1, 1);
		}

		/// <summary>
		/// Extension to return the start of quarter of current DateTime
		/// </summary>
		public static DateTime ToQuarter(this DateTime date)
		{
			// easier than doing a bunch of math...just a few compares
			var qm = (date.Month < 4 ? 1 : (date.Month < 7 ? 4 : (date.Month < 10 ? 7 : 10)));
			return new DateTime(date.Year, qm, 1);
		}

		/// <summary>
		/// Extension to return the start of month of current DateTime
		/// </summary>
		public static DateTime ToMonth(this DateTime date)
		{
			return new DateTime(date.Year, date.Month, 1);
		}

		/// <summary>
		/// Extension to return the start of week (Sunday) of current DateTime.
		/// </summary>
		public static DateTime ToWeek(this DateTime date)
		{
			// this cast results in Sunday being 0
			var dow = (int)date.DayOfWeek;
			return date.ToStartOfDay().AddDays(-dow);
		}

		/// <summary>
		/// Extension to return the start of hour of current DateTime
		/// </summary>
		public static DateTime ToHour(this DateTime date)
		{
			return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
		}

		/// <summary>
		/// Extension to return the start of the nearest half hour of current DateTime
		/// </summary>
		public static DateTime ToHalfHour(this DateTime date)
		{
			// easier than doing a bunch of math...just a few compares
			var hh = date.Minute < 30 ? 0 : 30;
			return new DateTime(date.Year, date.Month, date.Day, date.Hour, hh, 0);
		}

		/// <summary>
		/// Extension to return the start of the nearest quarter hour of current DateTime
		/// </summary>
		public static DateTime ToQuarterHour(this DateTime date)
		{
			// easier than doing a bunch of math...just a few compares
			var qh = (date.Minute < 15 ? 0 : (date.Minute < 30 ? 15 : (date.Minute < 45 ? 30 : 45)));
			return new DateTime(date.Year, date.Month, date.Day, date.Hour, qh, 0);
		}
	}
}
