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
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime StartOfDay(this DateTime date) => date.Date;

        /// <summary>
        /// 日末，返回: yyyy-MM-dd 23:59:59.fff。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns></returns>
        public static DateTime EndOfDay(this DateTime date) => date.Date.AddTicks(TimeSpan.TicksPerDay - TimeSpan.TicksPerMillisecond);

        /// <summary>
        /// 是否为闰年。
        /// </summary>
        /// <param name="year">年份。</param>
        /// <returns></returns>
        public static bool IsLeapYear(this int year) => year % 400 == 0 || year % 4 == 0 && year % 100 > 0;

        /// <summary>
        /// 月初。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns></returns>
        public static DateTime StartOfMonth(this DateTime date) => new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind);

        /// <summary>
        /// 月末。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns></returns>
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
