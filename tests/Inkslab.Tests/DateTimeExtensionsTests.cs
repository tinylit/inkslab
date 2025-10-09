using System;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="DateTimeExtensions"/> 测试。
    /// </summary>
    public class DateTimeExtensionsTests
    {
        /// <summary>
        /// 测试日初方法。
        /// </summary>
        [Fact]
        public void StartOfDayTest()
        {
            // Arrange
            var date = new DateTime(2023, 12, 15, 14, 30, 45, 123);
            var expected = new DateTime(2023, 12, 15, 0, 0, 0, 0);

            // Act
            var result = date.StartOfDay();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试日末方法。
        /// </summary>
        [Theory]
        [InlineData(DatePrecision.None, 999)]
        [InlineData(DatePrecision.MySql, 0)]
        [InlineData(DatePrecision.SqlServer, 997)]
        public void EndOfDayTest(DatePrecision precision, int expectedMilliseconds)
        {
            // Arrange
            var date = new DateTime(2023, 12, 15, 14, 30, 45, 123);
            var expected = new DateTime(2023, 12, 15, 23, 59, 59, expectedMilliseconds);

            // Act
            var result = date.EndOfDay(precision);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试周初方法。
        /// </summary>
        [Theory]
        [InlineData("2023-12-15", DateTimeKind.Local, "2023-12-11")] // 周五 -> 周一
        [InlineData("2023-12-17", DateTimeKind.Local, "2023-12-11")] // 周日 -> 周一
        [InlineData("2023-12-15", DateTimeKind.Utc, "2023-12-10")]   // 周五 -> 周日
        [InlineData("2023-12-17", DateTimeKind.Utc, "2023-12-17")]   // 周日 -> 周日
        public void StartOfWeekTest(string dateStr, DateTimeKind kind, string expectedStr)
        {
            // Arrange
            var date = DateTime.SpecifyKind(DateTime.Parse(dateStr), kind);
            var expected = DateTime.SpecifyKind(DateTime.Parse(expectedStr), kind);

            // Act
            var result = date.StartOfWeek();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试周末方法。
        /// </summary>
        [Theory]
        [InlineData("2023-12-15", DateTimeKind.Local, "2023-12-17")] // 周五 -> 周日
        [InlineData("2023-12-11", DateTimeKind.Local, "2023-12-17")] // 周一 -> 周日
        [InlineData("2023-12-15", DateTimeKind.Utc, "2023-12-16")]   // 周五 -> 周六
        [InlineData("2023-12-10", DateTimeKind.Utc, "2023-12-16")]   // 周日 -> 周六
        public void EndOfWeekTest(string dateStr, DateTimeKind kind, string expectedDateStr)
        {
            // Arrange
            var date = DateTime.SpecifyKind(DateTime.Parse(dateStr), kind);
            var expectedDate = DateTime.SpecifyKind(DateTime.Parse(expectedDateStr), kind);
            var expected = expectedDate.EndOfDay();

            // Act
            var result = date.EndOfWeek();

            // Assert
            Assert.Equal(expected.Date, result.Date);
            Assert.Equal(23, result.Hour);
            Assert.Equal(59, result.Minute);
            Assert.Equal(59, result.Second);
        }

        /// <summary>
        /// 测试闰年判断。
        /// </summary>
        [Theory]
        [InlineData(2020, true)]   // 闰年
        [InlineData(2021, false)]  // 平年
        [InlineData(1900, false)]  // 世纪年但不是闰年
        [InlineData(2000, true)]   // 世纪年且是闰年
        [InlineData(2024, true)]   // 闰年
        public void IsLeapYearTest(int year, bool expected)
        {
            // Act
            var result = year.IsLeapYear();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试月初方法。
        /// </summary>
        [Fact]
        public void StartOfMonthTest()
        {
            // Arrange
            var date = new DateTime(2023, 12, 15, 14, 30, 45, 123);
            var expected = new DateTime(2023, 12, 1, 0, 0, 0, 0);

            // Act
            var result = date.StartOfMonth();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试月末方法。
        /// </summary>
        [Theory]
        [InlineData("2023-02-15", DatePrecision.None, "2023-02-28 23:59:59.999")] // 平年2月
        [InlineData("2024-02-15", DatePrecision.None, "2024-02-29 23:59:59.999")] // 闰年2月
        [InlineData("2023-04-15", DatePrecision.None, "2023-04-30 23:59:59.999")] // 30天月份
        [InlineData("2023-01-15", DatePrecision.None, "2023-01-31 23:59:59.999")] // 31天月份
        [InlineData("2023-12-15", DatePrecision.MySql, "2023-12-31 23:59:59.000")]
        public void EndOfMonthTest(string dateStr, DatePrecision precision, string expectedStr)
        {
            // Arrange
            var date = DateTime.Parse(dateStr);
            var expected = DateTime.Parse(expectedStr);

            // Act
            var result = date.EndOfMonth(precision);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试年初方法。
        /// </summary>
        [Fact]
        public void StartOfYearTest()
        {
            // Arrange
            var date = new DateTime(2023, 12, 15, 14, 30, 45, 123);
            var expected = new DateTime(2023, 1, 1, 0, 0, 0, 0);

            // Act
            var result = date.StartOfYear();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试年末方法。
        /// </summary>
        [Theory]
        [InlineData(DatePrecision.None, "2023-12-31 23:59:59.999")]
        [InlineData(DatePrecision.MySql, "2023-12-31 23:59:59.000")]
        [InlineData(DatePrecision.SqlServer, "2023-12-31 23:59:59.997")]
        public void EndOfYearTest(DatePrecision precision, string expectedStr)
        {
            // Arrange
            var date = new DateTime(2023, 6, 15, 14, 30, 45, 123);
            var expected = DateTime.Parse(expectedStr);

            // Act
            var result = date.EndOfYear(precision);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试季度初方法。
        /// </summary>
        [Theory]
        [InlineData("2023-01-15", "2023-01-01")] // Q1
        [InlineData("2023-04-15", "2023-04-01")] // Q2
        [InlineData("2023-07-15", "2023-07-01")] // Q3
        [InlineData("2023-10-15", "2023-10-01")] // Q4
        public void StartOfQuarterTest(string dateStr, string expectedStr)
        {
            // Arrange
            var date = DateTime.Parse(dateStr);
            var expected = DateTime.Parse(expectedStr);

            // Act
            var result = date.StartOfQuarter();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试季度末方法。
        /// </summary>
        [Theory]
        [InlineData("2023-01-15", "2023-03-31")] // Q1
        [InlineData("2023-04-15", "2023-06-30")] // Q2
        [InlineData("2023-07-15", "2023-09-30")] // Q3
        [InlineData("2023-10-15", "2023-12-31")] // Q4
        public void EndOfQuarterTest(string dateStr, string expectedDateStr)
        {
            // Arrange
            var date = DateTime.Parse(dateStr);
            var expectedDate = DateTime.Parse(expectedDateStr).EndOfDay();

            // Act
            var result = date.EndOfQuarter();

            // Assert
            Assert.Equal(expectedDate.Date, result.Date);
        }

        /// <summary>
        /// 测试获取年中第几周。
        /// </summary>
        [Theory]
        [InlineData("2023-01-01", 52)] // 2023年1月1日是2022年的第52周
        [InlineData("2023-01-02", 1)]  // 2023年1月2日是2023年的第1周
        [InlineData("2023-12-31", 52)] // 2023年12月31日是第52周
        public void GetWeekOfYearTest(string dateStr, int expectedWeek)
        {
            // Arrange
            var date = DateTime.Parse(dateStr);

            // Act
            var result = date.GetWeekOfYear();

            // Assert
            Assert.Equal(expectedWeek, result);
        }

        /// <summary>
        /// 测试今天判断。
        /// </summary>
        [Fact]
        public void IsTodayTest()
        {
            // Arrange
            var today = DateTime.Today;
            var yesterday = DateTime.Today.AddDays(-1);

            // Act & Assert
            Assert.True(today.IsToday());
            Assert.False(yesterday.IsToday());
        }

        /// <summary>
        /// 测试昨天判断。
        /// </summary>
        [Fact]
        public void IsYesterdayTest()
        {
            // Arrange
            var yesterday = DateTime.Today.AddDays(-1);
            var today = DateTime.Today;

            // Act & Assert
            Assert.True(yesterday.IsYesterday());
            Assert.False(today.IsYesterday());
        }

        /// <summary>
        /// 测试明天判断。
        /// </summary>
        [Fact]
        public void IsTomorrowTest()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var today = DateTime.Today;

            // Act & Assert
            Assert.True(tomorrow.IsTomorrow());
            Assert.False(today.IsTomorrow());
        }

        /// <summary>
        /// 测试工作日判断。
        /// </summary>
        [Theory]
        [InlineData("2023-12-11", true)]  // 周一
        [InlineData("2023-12-15", true)]  // 周五
        [InlineData("2023-12-16", false)] // 周六
        [InlineData("2023-12-17", false)] // 周日
        public void IsWeekdayTest(string dateStr, bool expected)
        {
            // Arrange
            var date = DateTime.Parse(dateStr);

            // Act
            var result = date.IsWeekday();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试周末判断。
        /// </summary>
        [Theory]
        [InlineData("2023-12-11", false)] // 周一
        [InlineData("2023-12-15", false)] // 周五
        [InlineData("2023-12-16", true)]  // 周六
        [InlineData("2023-12-17", true)]  // 周日
        public void IsWeekendTest(string dateStr, bool expected)
        {
            // Arrange
            var date = DateTime.Parse(dateStr);

            // Act
            var result = date.IsWeekend();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试同一天判断。
        /// </summary>
        [Fact]
        public void IsSameDayTest()
        {
            // Arrange
            var date1 = new DateTime(2023, 12, 15, 10, 30, 45);
            var date2 = new DateTime(2023, 12, 15, 20, 45, 30);
            var date3 = new DateTime(2023, 12, 16, 10, 30, 45);

            // Act & Assert
            Assert.True(date1.IsSameDay(date2));
            Assert.False(date1.IsSameDay(date3));
        }

        /// <summary>
        /// 测试同一周判断。
        /// </summary>
        [Fact]
        public void IsSameWeekTest()
        {
            // Arrange
            var monday = new DateTime(2023, 12, 11);    // 周一
            var friday = new DateTime(2023, 12, 15);    // 周五
            var nextMonday = new DateTime(2023, 12, 18); // 下周一

            // Act & Assert
            Assert.True(monday.IsSameWeek(friday));
            Assert.False(monday.IsSameWeek(nextMonday));
        }

        /// <summary>
        /// 测试同一月判断。
        /// </summary>
        [Fact]
        public void IsSameMonthTest()
        {
            // Arrange
            var date1 = new DateTime(2023, 12, 1);
            var date2 = new DateTime(2023, 12, 31);
            var date3 = new DateTime(2023, 11, 30);

            // Act & Assert
            Assert.True(date1.IsSameMonth(date2));
            Assert.False(date1.IsSameMonth(date3));
        }

        /// <summary>
        /// 测试同一年判断。
        /// </summary>
        [Fact]
        public void IsSameYearTest()
        {
            // Arrange
            var date1 = new DateTime(2023, 1, 1);
            var date2 = new DateTime(2023, 12, 31);
            var date3 = new DateTime(2024, 1, 1);

            // Act & Assert
            Assert.True(date1.IsSameYear(date2));
            Assert.False(date1.IsSameYear(date3));
        }

        /// <summary>
        /// 测试获取下一个指定星期几。
        /// </summary>
        [Fact]
        public void GetNextWeekdayTest()
        {
            // Arrange
            var friday = new DateTime(2023, 12, 15); // 周五
            var expectedNextMonday = new DateTime(2023, 12, 18); // 下周一

            // Act
            var result = friday.GetNextWeekday(DayOfWeek.Monday);

            // Assert
            Assert.Equal(expectedNextMonday, result);
        }

        /// <summary>
        /// 测试获取上一个指定星期几。
        /// </summary>
        [Fact]
        public void GetPreviousWeekdayTest()
        {
            // Arrange
            var friday = new DateTime(2023, 12, 15); // 周五
            var expectedPreviousMonday = new DateTime(2023, 12, 11); // 本周一

            // Act
            var result = friday.GetPreviousWeekday(DayOfWeek.Monday);

            // Assert
            Assert.Equal(expectedPreviousMonday, result);
        }

        /// <summary>
        /// 测试计算工作日数量。
        /// </summary>
        [Fact]
        public void GetWorkingDaysTest()
        {
            // Arrange
            var startDate = new DateTime(2023, 12, 11); // 周一
            var endDate = new DateTime(2023, 12, 15);   // 周五

            // Act
            var result = startDate.GetWorkingDays(endDate);

            // Assert
            Assert.Equal(5, result); // 周一到周五，5个工作日
        }

        /// <summary>
        /// 测试添加工作日。
        /// </summary>
        [Fact]
        public void AddWorkingDaysTest()
        {
            // Arrange
            var friday = new DateTime(2023, 12, 15, 10, 30, 0); // 周五 10:30
            var expectedTuesday = new DateTime(2023, 12, 19, 10, 30, 0); // 下周二 10:30

            // Act
            var result = friday.AddWorkingDays(2); // 添加2个工作日

            // Assert
            Assert.Equal(expectedTuesday, result);
        }

        /// <summary>
        /// 测试Unix时间戳转换。
        /// </summary>
        [Fact]
        public void UnixTimestampTest()
        {
            // Arrange
            var dateTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var expectedTimestamp = 1672531200L; // 2023-01-01 00:00:00 UTC

            // Act
            var timestamp = dateTime.ToUnixTimestamp();
            var convertedBack = timestamp.FromUnixTimestamp();

            // Assert
            Assert.Equal(expectedTimestamp, timestamp);
            Assert.Equal(dateTime, convertedBack);
        }

        /// <summary>
        /// 测试Unix时间戳毫秒转换。
        /// </summary>
        [Fact]
        public void UnixTimestampMillisecondsTest()
        {
            // Arrange
            var dateTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var expectedTimestamp = 1672531200000L; // 2023-01-01 00:00:00 UTC in milliseconds

            // Act
            var timestamp = dateTime.ToUnixTimestampMilliseconds();
            var convertedBack = timestamp.FromUnixTimestampMilliseconds();

            // Assert
            Assert.Equal(expectedTimestamp, timestamp);
            Assert.Equal(dateTime, convertedBack);
        }

        /// <summary>
        /// 测试年龄计算。
        /// </summary>
        [Theory]
        [InlineData("1990-01-01", "2023-01-01", 33)]
        [InlineData("1990-06-15", "2023-06-14", 32)] // 生日前一天
        [InlineData("1990-06-15", "2023-06-15", 33)] // 生日当天
        [InlineData("1990-06-15", "2023-06-16", 33)] // 生日后一天
        public void GetAgeTest(string birthDateStr, string currentDateStr, int expectedAge)
        {
            // Arrange
            var birthDate = DateTime.Parse(birthDateStr);
            var currentDate = DateTime.Parse(currentDateStr);

            // Act
            var age = birthDate.GetAge(currentDate);

            // Assert
            Assert.Equal(expectedAge, age);
        }

        /// <summary>
        /// 测试时间舍入。
        /// </summary>
        [Fact]
        public void RoundToTest()
        {
            // Arrange
            var dateTime = new DateTime(2023, 1, 1, 10, 37, 30); // 10:37:30
            var interval = TimeSpan.FromMinutes(15); // 15分钟间隔
            var expected = new DateTime(2023, 1, 1, 10, 30, 0); // 应该舍入到10:30

            // Act
            var result = dateTime.RoundTo(interval);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试向下舍入。
        /// </summary>
        [Fact]
        public void FloorToTest()
        {
            // Arrange
            var dateTime = new DateTime(2023, 1, 1, 10, 37, 30); // 10:37:30
            var interval = TimeSpan.FromMinutes(15); // 15分钟间隔
            var expected = new DateTime(2023, 1, 1, 10, 30, 0); // 应该向下舍入到10:30

            // Act
            var result = dateTime.FloorTo(interval);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试向上舍入。
        /// </summary>
        [Fact]
        public void CeilingToTest()
        {
            // Arrange
            var dateTime = new DateTime(2023, 1, 1, 10, 37, 30); // 10:37:30
            var interval = TimeSpan.FromMinutes(15); // 15分钟间隔
            var expected = new DateTime(2023, 1, 1, 10, 45, 0); // 应该向上舍入到10:45

            // Act
            var result = dateTime.CeilingTo(interval);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试DateTimeKind保持。
        /// </summary>
        [Fact]
        public void DateTimeKindPreservationTest()
        {
            // Arrange
            var utcDate = new DateTime(2023, 12, 15, 10, 30, 0, DateTimeKind.Utc);
            var localDate = new DateTime(2023, 12, 15, 10, 30, 0, DateTimeKind.Local);

            // Act & Assert
            Assert.Equal(DateTimeKind.Utc, utcDate.StartOfDay().Kind);
            Assert.Equal(DateTimeKind.Local, localDate.StartOfDay().Kind);
            Assert.Equal(DateTimeKind.Utc, utcDate.EndOfDay().Kind);
            Assert.Equal(DateTimeKind.Local, localDate.EndOfDay().Kind);
        }
    }
}