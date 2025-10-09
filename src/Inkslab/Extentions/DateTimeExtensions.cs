#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// 日期精度。
    /// <para><see cref="None"/> -> yyyy-MM-dd 23:59:59.999</para>
    /// <para><see cref="MySql"/> -> yyyy-MM-dd 23:59:59.000</para>
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
        MySql = 1,
        /// <summary>
        /// SqlServer 数据库精度：yyyy-MM-dd 23:59:59.997。
        /// </summary>
        SqlServer = 2
    }

    /// <summary>
    /// 日期扩展。
    /// </summary>
    public static class DateTimeExtensions
    {
#if !NET6_0_OR_GREATER
        /// <summary>
        /// Unix纪元时间（1970年1月1日 00:00:00 UTC）。
        /// </summary>
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

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
                DatePrecision.MySql => date.Date.AddTicks(TimeSpan.TicksPerDay - TimeSpan.TicksPerSecond),
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
                return date.Date.AddDays(-(int)date.DayOfWeek);
            }

            // 对于Local时间，周一为一周的第一天
            var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
            return date.Date.AddDays(-daysSinceMonday);
        }

        /// <summary>
        /// 周末（当 <paramref name="date"/>.Kind 等于 <see cref="DateTimeKind.Utc"/> 时，周六作为一周的最后一天；否则，周日作为一周的最后一天），当 <paramref name="datePrecision"/> 为 <seealso cref="DatePrecision.MySql"/>时，返回：yyyy-MM-dd 23:59:59；否则，返回: yyyy-MM-dd 23:59:59.999。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <param name="datePrecision">日期精度。</param>
        /// <returns>周末时间。</returns>
        public static DateTime EndOfWeek(this DateTime date, DatePrecision datePrecision = DatePrecision.None)
        {
            if (date.Kind == DateTimeKind.Utc) //? 周六为一周的最后一天。
            {
                return date.AddDays(6 - (int)date.DayOfWeek).EndOfDay(datePrecision);
            }

            // 对于Local时间，周日为一周的最后一天
            var daysUntilSunday = (7 - (int)date.DayOfWeek) % 7;
            if (daysUntilSunday == 0) // 如果当前就是周日
            {
                return date.EndOfDay(datePrecision);
            }
            return date.AddDays(daysUntilSunday).EndOfDay(datePrecision);
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
                DatePrecision.MySql => new DateTime(year, month, month == 2 ? (IsLeapYear(year) ? 29 : 28) : ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, date.Kind),
                DatePrecision.SqlServer => new DateTime(year, month, month == 2 ? (IsLeapYear(year) ? 29 : 28) : ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, 997, date.Kind),
                _ => new DateTime(year, month, month == 2 ? (IsLeapYear(year) ? 29 : 28) : ((month & 1) == 0 ? month < 7 : month > 8) ? 30 : 31, 23, 59, 59, 999, date.Kind),
            };
        }

        /// <summary>
        /// 年初，返回: yyyy-01-01 00:00:00.000。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>年初时间。</returns>
        public static DateTime StartOfYear(this DateTime date) => new DateTime(date.Year, 1, 1, 0, 0, 0, date.Kind);

        /// <summary>
        /// 年末。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <param name="datePrecision">日期精度。</param>
        /// <returns>年末时间。</returns>
        public static DateTime EndOfYear(this DateTime date, DatePrecision datePrecision = DatePrecision.None)
        {
            return datePrecision switch
            {
                DatePrecision.MySql => new DateTime(date.Year, 12, 31, 23, 59, 59, date.Kind),
                DatePrecision.SqlServer => new DateTime(date.Year, 12, 31, 23, 59, 59, 997, date.Kind),
                _ => new DateTime(date.Year, 12, 31, 23, 59, 59, 999, date.Kind),
            };
        }

        /// <summary>
        /// 季度初，返回季度第一天的 00:00:00.000。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>季度初时间。</returns>
        public static DateTime StartOfQuarter(this DateTime date)
        {
            var quarterStartMonth = (date.Month - 1) / 3 * 3 + 1;
            return new DateTime(date.Year, quarterStartMonth, 1, 0, 0, 0, date.Kind);
        }

        /// <summary>
        /// 季度末。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <param name="datePrecision">日期精度。</param>
        /// <returns>季度末时间。</returns>
        public static DateTime EndOfQuarter(this DateTime date, DatePrecision datePrecision = DatePrecision.None)
        {
            var quarterEndMonth = ((date.Month - 1) / 3 + 1) * 3;
            var quarterEndDate = new DateTime(date.Year, quarterEndMonth, 1, 0, 0, 0, date.Kind);
            return quarterEndDate.EndOfMonth(datePrecision);
        }

        /// <summary>
        /// 获取指定日期是一年中的第几周（ISO 8601标准）。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>一年中的周数。</returns>
        public static int WeekOfYear(this DateTime date)
        {
            var jan1 = new DateTime(date.Year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek - 1;
            var firstMonday = jan1.AddDays(-daysOffset + (daysOffset > 3 ? 7 : 0));

            if (date < firstMonday)
            {
                return WeekOfYear(new DateTime(date.Year - 1, 12, 31));
            }

            return (date - firstMonday).Days / 7 + 1;
        }

        /// <summary>
        /// 判断指定日期是否为今天。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>是今天返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsToday(this DateTime date) => date.Date == DateTime.Today;

        /// <summary>
        /// 判断指定日期是否为昨天。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>是昨天返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsYesterday(this DateTime date) => date.Date == DateTime.Today.AddDays(-1);

        /// <summary>
        /// 判断指定日期是否为明天。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>是明天返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsTomorrow(this DateTime date) => date.Date == DateTime.Today.AddDays(1);

        /// <summary>
        /// 判断指定日期是否为工作日（周一到周五）。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>是工作日返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsWeekday(this DateTime date) => date.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;

        /// <summary>
        /// 判断指定日期是否为周末（周六或周日）。
        /// </summary>
        /// <param name="date">日期。</param>
        /// <returns>是周末返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsWeekend(this DateTime date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        /// <summary>
        /// 判断两个日期是否在同一天。
        /// </summary>
        /// <param name="date1">第一个日期。</param>
        /// <param name="date2">第二个日期。</param>
        /// <returns>在同一天返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsSameDay(this DateTime date1, DateTime date2) => date1.Date == date2.Date;

        /// <summary>
        /// 判断两个日期是否在同一周。
        /// </summary>
        /// <param name="date1">第一个日期。</param>
        /// <param name="date2">第二个日期。</param>
        /// <returns>在同一周返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsSameWeek(this DateTime date1, DateTime date2)
            => date1.StartOfWeek() == date2.StartOfWeek();

        /// <summary>
        /// 判断两个日期是否在同一月。
        /// </summary>
        /// <param name="date1">第一个日期。</param>
        /// <param name="date2">第二个日期。</param>
        /// <returns>在同一月返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsSameMonth(this DateTime date1, DateTime date2)
            => date1.Year == date2.Year && date1.Month == date2.Month;

        /// <summary>
        /// 判断两个日期是否在同一年。
        /// </summary>
        /// <param name="date1">第一个日期。</param>
        /// <param name="date2">第二个日期。</param>
        /// <returns>在同一年返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public static bool IsSameYear(this DateTime date1, DateTime date2) => date1.Year == date2.Year;

        /// <summary>
        /// 获取下一个指定星期几的日期。
        /// </summary>
        /// <param name="date">起始日期。</param>
        /// <param name="dayOfWeek">目标星期几。</param>
        /// <returns>下一个指定星期几的日期。</returns>
        public static DateTime NextWeekday(this DateTime date, DayOfWeek dayOfWeek)
        {
            var daysToAdd = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
            if (daysToAdd == 0)
            {
                daysToAdd = 7; // 如果是同一天，则获取下周的同一天
            }
            return date.Date.AddDays(daysToAdd);
        }

        /// <summary>
        /// 获取上一个指定星期几的日期。
        /// </summary>
        /// <param name="date">起始日期。</param>
        /// <param name="dayOfWeek">目标星期几。</param>
        /// <returns>上一个指定星期几的日期。</returns>
        public static DateTime PreviousWeekday(this DateTime date, DayOfWeek dayOfWeek)
        {
            var daysToSubtract = ((int)date.DayOfWeek - (int)dayOfWeek + 7) % 7;
            if (daysToSubtract == 0)
            {
                daysToSubtract = 7; // 如果是同一天，则获取上周的同一天
            }
            return date.Date.AddDays(-daysToSubtract);
        }

        /// <summary>
        /// 计算两个日期之间的工作日数量（不包括周末）。
        /// </summary>
        /// <param name="startDate">开始日期。</param>
        /// <param name="endDate">结束日期。</param>
        /// <returns>工作日数量。</returns>
        public static int WorkingDays(this DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                (endDate, startDate) = (startDate, endDate);
            }

            var workingDays = 0;
            var current = startDate.Date;

            while (current <= endDate.Date)
            {
                if (current.IsWeekday())
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }

            return workingDays;
        }

        /// <summary>
        /// 添加工作日（跳过周末）。
        /// </summary>
        /// <param name="date">起始日期。</param>
        /// <param name="workingDays">要添加的工作日数量。</param>
        /// <returns>添加工作日后的日期。</returns>
        public static DateTime AddWorkingDays(this DateTime date, int workingDays)
        {
            var result = date.Date;
            var daysAdded = 0;

            while (daysAdded < Math.Abs(workingDays))
            {
                result = result.AddDays(workingDays > 0 ? 1 : -1);

                if (result.IsWeekday())
                {
                    daysAdded++;
                }
            }

            return result.Add(date.TimeOfDay);
        }

        /// <summary>
        /// 将日期时间转换为Unix时间戳（秒）。
        /// </summary>
        /// <param name="dateTime">日期时间。</param>
        /// <returns>Unix时间戳。</returns>
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
#if NET6_0_OR_GREATER
            return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeSeconds();
#else
            return (long)(dateTime.ToUniversalTime() - _unixEpoch).TotalSeconds;
#endif
        }

        /// <summary>
        /// 将日期时间转换为Unix时间戳（毫秒）。
        /// </summary>
        /// <param name="dateTime">日期时间。</param>
        /// <returns>Unix时间戳（毫秒）。</returns>
        public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
        {
#if NET6_0_OR_GREATER
            return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
#else
            return (long)(dateTime.ToUniversalTime() - _unixEpoch).TotalMilliseconds;
#endif
        }

        /// <summary>
        /// 从Unix时间戳（秒）转换为DateTime。
        /// </summary>
        /// <param name="unixTimestamp">Unix时间戳（秒）。</param>
        /// <returns>DateTime对象。</returns>
        public static DateTime FromUnixTimestamp(this long unixTimestamp)
        {
#if NET6_0_OR_GREATER
            return DateTime.UnixEpoch.AddSeconds(unixTimestamp);
#else
            return _unixEpoch.AddSeconds(unixTimestamp);
#endif
        }

        /// <summary>
        /// 从Unix时间戳（毫秒）转换为DateTime。
        /// </summary>
        /// <param name="unixTimestampMilliseconds">Unix时间戳（毫秒）。</param>
        /// <returns>DateTime对象。</returns>
        public static DateTime FromUnixTimestampMilliseconds(this long unixTimestampMilliseconds)
        {
#if NET6_0_OR_GREATER
            return DateTime.UnixEpoch.AddMilliseconds(unixTimestampMilliseconds);
#else
            return _unixEpoch.AddMilliseconds(unixTimestampMilliseconds);
#endif
        }

        /// <summary>
        /// 获取指定日期的年龄（到当前日期）。
        /// </summary>
        /// <param name="birthDate">出生日期。</param>
        /// <param name="currentDate">当前日期，默认为今天。</param>
        /// <returns>年龄。</returns>
        public static int GetAge(this DateTime birthDate, DateTime? currentDate = null)
        {
            var today = currentDate ?? DateTime.Today;
            var age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        /// <summary>
        /// 舍入到最近的指定时间间隔。
        /// </summary>
        /// <param name="dateTime">日期时间。</param>
        /// <param name="interval">时间间隔。</param>
        /// <returns>舍入后的日期时间。</returns>
        public static DateTime RoundTo(this DateTime dateTime, TimeSpan interval)
        {
            var remainder = dateTime.Ticks % interval.Ticks;
            var halfInterval = interval.Ticks / 2;

            // 如果余数大于间隔的一半，向上舍入；小于等于一半时向下舍入
            if (remainder > halfInterval)
            {
                return new DateTime(dateTime.Ticks - remainder + interval.Ticks, dateTime.Kind);
            }
            else
            {
                return new DateTime(dateTime.Ticks - remainder, dateTime.Kind);
            }
        }

        /// <summary>
        /// 向下舍入到指定的时间间隔。
        /// </summary>
        /// <param name="dateTime">日期时间。</param>
        /// <param name="interval">时间间隔。</param>
        /// <returns>向下舍入后的日期时间。</returns>
        public static DateTime FloorTo(this DateTime dateTime, TimeSpan interval)
        {
            var ticks = dateTime.Ticks / interval.Ticks * interval.Ticks;
            return new DateTime(ticks, dateTime.Kind);
        }

        /// <summary>
        /// 向上舍入到指定的时间间隔。
        /// </summary>
        /// <param name="dateTime">日期时间。</param>
        /// <param name="interval">时间间隔。</param>
        /// <returns>向上舍入后的日期时间。</returns>
        public static DateTime CeilingTo(this DateTime dateTime, TimeSpan interval)
        {
            var ticks = (dateTime.Ticks + interval.Ticks - 1) / interval.Ticks * interval.Ticks;
            
            return new DateTime(ticks, dateTime.Kind);
        }
    }
}
