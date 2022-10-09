namespace System
{
    /// <summary>
    /// 日期扩展。
    /// </summary>
    public static class DateTimeExtentions
    {
        /// <summary>
        /// 日初，返回: yyyy-MM-dd 00:00:00.000。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>日初时间。</returns>
        public static DateTime StartOfDay(this DateTime date) => date.Date;

        /// <summary>
        /// 日末，返回: yyyy-MM-dd 23:59:59.999。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>日末时间。</returns>
        public static DateTime EndOfDay(this DateTime date) => date.Date.AddTicks(TimeSpan.TicksPerDay - TimeSpan.TicksPerMillisecond);

        /// <summary>
        /// 周初（当 <paramref name="date"/>.Kind 等于 <see cref="DateTimeKind.Utc"/> 时，周日作为一周的第一天；否则，周一作为一周的第一天），返回: yyyy-MM-dd 00:00:00.000。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>周初时间。</returns>
        public static DateTime StartOfWeek(this DateTime date)
        {
            if (date.Kind == DateTimeKind.Utc) //? 周日为一周的第一天。
            {
                return date.Date.AddDays(-date.DayOfWeek.GetHashCode());
            }

            return date.Date.AddDays(-date.DayOfWeek.GetHashCode() + 1); //? 周一为一周的第一天。
        }

        /// <summary>
        /// 周末（当 <paramref name="date"/>.Kind 等于 <see cref="DateTimeKind.Utc"/> 时，周六作为一周的最后一天；否则，周日作为一周的最后一天），返回: yyyy-MM-dd 23:59:59.999。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>周末时间。</returns>
        public static DateTime EndOfWeek(this DateTime date)
        {
            if (date.Kind == DateTimeKind.Utc) //? 周六为一周的最后一天。
            {
                return date.AddDays(6 - date.DayOfWeek.GetHashCode()).EndOfDay();
            }

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                return date.EndOfDay();
            }

            return date.AddDays(7 - date.DayOfWeek.GetHashCode()).EndOfDay(); //? 周日一为一周的最后一天。
        }

        /// <summary>
        /// 是否为闰年。
        /// </summary>
        /// <param name="year">年份。</param>
        /// <returns>是闰年则为 true， 否则为 false。</returns>
        public static bool IsLeapYear(this int year) => year % 400 == 0 || year % 4 == 0 && year % 100 > 0;

        /// <summary>
        /// 月初，返回: yyyy-MM-dd 00:00:00.000。。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>月初时间。</returns>
        public static DateTime StartOfMonth(this DateTime date) => new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind);

        /// <summary>
        /// 月末，返回: yyyy-MM-dd 23:59:59.999。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>月末时间。</returns>
        public static DateTime EndOfMonth(this DateTime date)
        {
            var year = date.Year;
            var month = date.Month;

            if (month == 2)
            {
                return new DateTime(year, month, IsLeapYear(year) ? 29 : 28, 23, 59, 59, 999, date.Kind);
            }

            return new DateTime(year, month, ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, 999, date.Kind);
        }
    }
}
