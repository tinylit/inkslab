namespace System
{
    /// <summary>
    /// 日期精度。
    /// <para><see cref="None"/> -> yyyy-MM-dd 23:59:59.999</para>
    /// <para><see cref="MySQL"/> -> yyyy-MM-dd 23:59:59.000</para>
    /// <para><see cref="SqlServer"/> -> yyyy-MM-dd 23:59:59.997</para>
    /// </summary>
    public enum DatePrecision
    {
        /// <summary>
        /// 默认：yyyy-MM-dd 23:59:59.999。
        /// </summary>
        None = 0,
        /// <summary>
        /// MySQL 数据库精度：yyyy-MM-dd 23:59:59。
        /// </summary>
        MySQL = 1,
        /// <summary>
        /// SqlServer 数据库精度：yyyy-MM-dd 23:59:59.997。
        /// </summary>
        SqlServer = 2
    }

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
        /// 日末。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <param name="datePrecision">日期精度。</param>
        /// <returns>日末时间。</returns>
        public static DateTime EndOfDay(this DateTime date, DatePrecision datePrecision = DatePrecision.None)
        {
            return datePrecision switch
            {
                DatePrecision.MySQL => date.Date.AddTicks(TimeSpan.TicksPerDay - TimeSpan.TicksPerSecond),
                DatePrecision.SqlServer => date.Date.AddTicks(TimeSpan.TicksPerDay - 3L * TimeSpan.TicksPerMillisecond),
                _ => date.Date.AddTicks(TimeSpan.TicksPerDay - TimeSpan.TicksPerMillisecond),
            };
        }

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
        /// 周末（当 <paramref name="date"/>.Kind 等于 <see cref="DateTimeKind.Utc"/> 时，周六作为一周的最后一天；否则，周日作为一周的最后一天），当 <paramref name="datePrecision"/> 为 <seealso cref="DatePrecision.MySQL"/>时，返回：yyyy-MM-dd 23:59:59；否则，返回: yyyy-MM-dd 23:59:59.999。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <param name="datePrecision">日期精度。</param>
        /// <returns>周末时间。</returns>
        public static DateTime EndOfWeek(this DateTime date, DatePrecision datePrecision = DatePrecision.None)
        {
            if (date.Kind == DateTimeKind.Utc) //? 周六为一周的最后一天。
            {
                return date.AddDays(6 - date.DayOfWeek.GetHashCode()).EndOfDay(datePrecision);
            }

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                return date.EndOfDay();
            }

            return date.AddDays(7 - date.DayOfWeek.GetHashCode()).EndOfDay(datePrecision); //? 周日一为一周的最后一天。
        }

        /// <summary>
        /// 是否为闰年。
        /// </summary>
        /// <param name="year">年份。</param>
        /// <returns>是闰年则为 <see langword="true"/>， 否则为 <see langword="false"/>。</returns>
        public static bool IsLeapYear(this int year) => year % 400 == 0 || year % 4 == 0 && year % 100 > 0;

        /// <summary>
        /// 月初，返回: yyyy-MM-dd 00:00:00.000。。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>月初时间。</returns>
        public static DateTime StartOfMonth(this DateTime date) => new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind);

        /// <summary>
        /// 月末。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <param name="datePrecision">日期精度。</param>
        /// <returns>月末时间。</returns>
        public static DateTime EndOfMonth(this DateTime date, DatePrecision datePrecision = DatePrecision.None)
        {
            var year = date.Year;
            var month = date.Month;

            return datePrecision switch
            {
                DatePrecision.MySQL => new DateTime(year, month, month == 2 ? (IsLeapYear(year) ? 29 : 28) : ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, date.Kind),
                DatePrecision.SqlServer => new DateTime(year, month, month == 2 ? (IsLeapYear(year) ? 29 : 28) : ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, 997, date.Kind),
                _ => new DateTime(year, month, month == 2 ? (IsLeapYear(year) ? 29 : 28) : ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, 999, date.Kind),
            };
        }
    }
}
