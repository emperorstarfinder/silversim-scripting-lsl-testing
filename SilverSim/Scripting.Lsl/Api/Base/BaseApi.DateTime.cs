// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable RCS1163

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Globalization;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetTimestamp")]
        public string GetTimestamp() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

        [APILevel(APIFlags.LSL, "llGetUnixTime")]
        public int GetUnixTime() => (int)Date.GetUnixTime();

        [APILevel(APIFlags.LSL, "llGetGMTclock")]
        public double GetGMTclock() => Date.GetUnixTime();

        [APILevel(APIFlags.LSL, "llGetTimeOfDay")]
        public double GetTimeOfDay(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.TimeOfDay;
            }
        }

        private const long EPOCH_BASE_TICKS = 621355968000000000;

        private long UnixTimeToTicks(long time) => (time * TimeSpan.TicksPerSecond) + EPOCH_BASE_TICKS;

        [APILevel(APIFlags.LSL, "llGetWallclock")]
        public double GetWallclock() => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Pacific Standard Time").TimeOfDay.TotalMilliseconds / 1000;

        [APILevel(APIFlags.LSL, "llGetDate")]
        public string GetDate() => DateTime.UtcNow.ToString("yyyy-MM-dd");

        [APILevel(APIFlags.OSSL, "osUnixTimeToTimestamp")]
        [IsPure]
        public string OsUnixTimeToTimestamp(int time)
        {
            var date = new DateTime(UnixTimeToTicks(time));

            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_SUNDAY = 0;
        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_MONDAY = 1;
        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_TUESDAY = 2;
        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_WEDNESDAY = 3;
        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_THURSDAY = 4;
        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_FRIDAY = 5;
        [APIExtension(APIExtension.DateTime)]
        public const int DAYOFWEEK_SATURDAY = 6;

        [APIExtension(APIExtension.DateTime, "datetime")]
        [APIDisplayName("datetime")]
        [APIIsVariableType]
        [APIAccessibleMembers]
        [Serializable]
        [APICloneOnAssignment]
        public class DateTimeContainer
        {
            private readonly DateTime m_DateTime;

            public DateTimeContainer()
            {
                m_DateTime = new DateTime();
            }

            public DateTimeContainer(DateTime datetime)
            {
                m_DateTime = datetime;
            }

            public DateTimeContainer(DateTimeContainer src)
            {
                m_DateTime = src.m_DateTime;
            }

            public long Ticks => m_DateTime.Ticks;
            public DateTimeContainer Date => new DateTimeContainer(m_DateTime.Date);
            public int Month => m_DateTime.Month;
            public int Minute => m_DateTime.Minute;
            public int Millisecond => m_DateTime.Millisecond;
            public int Hour => m_DateTime.Hour;
            public int DayOfYear => m_DateTime.DayOfYear;
            public int DayOfWeek => (int)m_DateTime.DayOfWeek;
            public int Day => m_DateTime.Day;
            public int Second => m_DateTime.Second;
            public TimeSpanContainer TimeOfDay => new TimeSpanContainer(m_DateTime.TimeOfDay);
            public int Year => m_DateTime.Year;
            public int IsDaylightSavingTime => m_DateTime.IsDaylightSavingTime().ToLSLBoolean();
            public int IsLeapYear => DateTime.IsLeapYear(m_DateTime.Year).ToLSLBoolean();
            public long UnixTime => (m_DateTime.Ticks - EPOCH_BASE_TICKS) / TimeSpan.TicksPerSecond;

            public DateTimeContainer Add(TimeSpanContainer value) => new DateTimeContainer(m_DateTime.Add(value.m_TimeSpan));
            public DateTimeContainer AddDays(double value) => new DateTimeContainer(m_DateTime.AddDays(value));
            public DateTimeContainer AddHours(double value) => new DateTimeContainer(m_DateTime.AddHours(value));
            public DateTimeContainer AddMilliseconds(double value) => new DateTimeContainer(m_DateTime.AddMilliseconds(value));
            public DateTimeContainer AddMinutes(double value) => new DateTimeContainer(m_DateTime.AddMinutes(value));
            public DateTimeContainer AddMonths(int months) => new DateTimeContainer(m_DateTime.AddMonths(months));
            public DateTimeContainer AddSeconds(double value) => new DateTimeContainer(m_DateTime.AddSeconds(value));
            public DateTimeContainer AddTicks(long value) => new DateTimeContainer(m_DateTime.AddTicks(value));
            public DateTimeContainer AddYears(int value) => new DateTimeContainer(m_DateTime.AddYears(value));
            public override bool Equals(object obj) => m_DateTime.Equals((obj as DateTimeContainer)?.m_DateTime);
            public bool Equals(DateTimeContainer value) => m_DateTime.Equals(value.m_DateTime);
            public override int GetHashCode() => m_DateTime.GetHashCode();
            public static DateTimeContainer operator +(DateTimeContainer d, TimeSpanContainer t) => new DateTimeContainer(d.m_DateTime + t.m_TimeSpan);
            public static TimeSpanContainer operator -(DateTimeContainer d1, DateTimeContainer d2) => new TimeSpanContainer(d1.m_DateTime - d2.m_DateTime);
            public static DateTimeContainer operator -(DateTimeContainer d, TimeSpanContainer t) => new DateTimeContainer(d.m_DateTime - t.m_TimeSpan);
            public static bool operator ==(DateTimeContainer d1, DateTimeContainer d2) => d1.m_DateTime == d2.m_DateTime;
            public static bool operator !=(DateTimeContainer d1, DateTimeContainer d2) => d1.m_DateTime != d2.m_DateTime;
            public static bool operator <(DateTimeContainer t1, DateTimeContainer t2) => t1.m_DateTime < t2.m_DateTime;
            public static bool operator >(DateTimeContainer t1, DateTimeContainer t2) => t1.m_DateTime > t2.m_DateTime;
            public static bool operator <=(DateTimeContainer t1, DateTimeContainer t2) => t1.m_DateTime <= t2.m_DateTime;
            public static bool operator >=(DateTimeContainer t1, DateTimeContainer t2) => t1.m_DateTime >= t2.m_DateTime;

            public string ToString(string format, IFormatProvider provider) => m_DateTime.ToString(format, provider);
            public string ToString(IFormatProvider provider) => m_DateTime.ToString(provider);
            public string ToString(string format) => m_DateTime.ToString(format);
            public override string ToString() => m_DateTime.ToString();

            public static explicit operator string(DateTimeContainer datetime) => datetime.ToString(CultureInfo.InvariantCulture);
            public static explicit operator DateTimeContainer(string input) => new DateTimeContainer(DateTime.Parse(input, CultureInfo.InvariantCulture));
        }

        [APIExtension(APIExtension.DateTime, "Now")]
        public DateTimeContainer DateTimeNow => UnixTimeToDateTime((long)Date.GetUnixTime());

        [APIExtension(APIExtension.DateTime, "dtFromUnixTime")]
        [IsPure]
        public DateTimeContainer UnixTimeToDateTime(long input) => new DateTimeContainer(new DateTime(UnixTimeToTicks(input)));
        [APIExtension(APIExtension.DateTime, "DateTime")]
        [IsPure]
        public DateTimeContainer ParseDateTime(string input) => new DateTimeContainer(DateTime.Parse(input, CultureInfo.InvariantCulture));
        [APIExtension(APIExtension.DateTime, "DateTime")]
        [IsPure]
        public DateTimeContainer ParseDateTime(string input, string format) =>
            new DateTimeContainer(DateTime.ParseExact(input, format, CultureInfo.InvariantCulture));
        [APIExtension(APIExtension.DateTime, "DateTime")]
        [IsPure]
        public DateTimeContainer CreateDateTime(long ticks) =>
            new DateTimeContainer(new DateTime(ticks));
        [APIExtension(APIExtension.DateTime, "DateTime")]
        [IsPure]
        public DateTimeContainer CreateDateTime(int year, int month, int day) =>
            new DateTimeContainer(new DateTime(year, month, day));
        [APIExtension(APIExtension.DateTime, "DateTime")]
        [IsPure]
        public DateTimeContainer CreateDateTime(int year, int month, int day, int hour, int minute, int second) =>
            new DateTimeContainer(new DateTime(year, month, day, hour, minute, second));
        [APIExtension(APIExtension.DateTime, "DateTime")]
        [IsPure]
        public DateTimeContainer CreateDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) =>
            new DateTimeContainer(new DateTime(year, month, day, hour, minute, second, millisecond));
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction, "ToString")]
        [IsPure]
        public string TimeSpanToString(DateTimeContainer datetime, string format) => datetime.ToString(format, CultureInfo.InvariantCulture);

        [APIExtension(APIExtension.DateTime, "dtDaysInMonth")]
        [IsPure]
        public int DaysInMonth(int year, int month) => DateTime.DaysInMonth(year, month);
        [APIExtension(APIExtension.DateTime, "dtIsLeapYear")]
        [IsPure]
        public int IsLeapYear(int year) => DateTime.IsLeapYear(year).ToLSLBoolean();

        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddDays(DateTimeContainer datetime, double value) => datetime.AddDays(value);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddHours(DateTimeContainer datetime, double value) => datetime.AddHours(value);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddMilliseconds(DateTimeContainer datetime, double value) => datetime.AddMilliseconds(value);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddMinutes(DateTimeContainer datetime, double value) => datetime.AddMinutes(value);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddMonths(DateTimeContainer datetime, int months) => datetime.AddMonths(months);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddSeconds(DateTimeContainer datetime, double value) => datetime.AddSeconds(value);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddTicks(DateTimeContainer datetime, long value) => datetime.AddTicks(value);
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction)]
        [IsPure]
        public DateTimeContainer AddYears(DateTimeContainer datetime, int value) => datetime.AddYears(value);

        [APIExtension(APIExtension.DateTime)]
        public const long TICKS_PER_MILLISECOND = 10000;
        [APIExtension(APIExtension.DateTime)]
        public const long TICKS_PER_SECOND = 10000000;
        [APIExtension(APIExtension.DateTime)]
        public const long TICKS_PER_MINUTE = 600000000;
        [APIExtension(APIExtension.DateTime)]
        public const long TICKS_PER_HOUR = 36000000000;
        [APIExtension(APIExtension.DateTime)]
        public const long TICKS_PER_DAY = 864000000000;

        [APIExtension(APIExtension.DateTime, "timespan")]
        [APIDisplayName("timespan")]
        [APIIsVariableType]
        [APIAccessibleMembers]
        [Serializable]
        [APICloneOnAssignment]
        public sealed class TimeSpanContainer
        {
            internal readonly TimeSpan m_TimeSpan;

            public TimeSpanContainer()
            {
                m_TimeSpan = new TimeSpan();
            }

            public TimeSpanContainer(TimeSpan src)
            {
                m_TimeSpan = src;
            }

            public TimeSpanContainer(TimeSpanContainer src)
            {
                m_TimeSpan = src.m_TimeSpan;
            }

            public double TotalMilliseconds => m_TimeSpan.TotalMilliseconds;
            public double TotalHours => m_TimeSpan.TotalHours;
            public double TotalDays => m_TimeSpan.TotalDays;
            public int Seconds => m_TimeSpan.Seconds;
            public int Minutes => m_TimeSpan.Minutes;
            public int Milliseconds => m_TimeSpan.Milliseconds;
            public int Hours => m_TimeSpan.Hours;
            public int Days => m_TimeSpan.Days;
            public long Ticks => m_TimeSpan.Ticks;
            public double TotalMinutes => m_TimeSpan.TotalMinutes;
            public double TotalSeconds => m_TimeSpan.TotalSeconds;

            public bool Equals(TimeSpanContainer obj) => m_TimeSpan.Equals(obj.m_TimeSpan);
            public override bool Equals(object obj) => m_TimeSpan.Equals((obj as TimeSpanContainer)?.m_TimeSpan);
            public override int GetHashCode() => m_TimeSpan.GetHashCode();
            public TimeSpanContainer Negate() => new TimeSpanContainer(m_TimeSpan.Negate());
            public TimeSpanContainer Subtract(TimeSpanContainer ts) => new TimeSpanContainer(m_TimeSpan - ts.m_TimeSpan);
            public string ToString(string format, IFormatProvider formatProvider) => m_TimeSpan.ToString(format, formatProvider);
            public string ToString(string format) => m_TimeSpan.ToString(format);
            public override string ToString() => m_TimeSpan.ToString();
            public static TimeSpanContainer operator +(TimeSpanContainer t) => new TimeSpanContainer(t);
            public static TimeSpanContainer operator +(TimeSpanContainer t1, TimeSpanContainer t2) => new TimeSpanContainer(t1.m_TimeSpan + t2.m_TimeSpan);
            public static TimeSpanContainer operator -(TimeSpanContainer t) => new TimeSpanContainer(-t.m_TimeSpan);
            public static TimeSpanContainer operator -(TimeSpanContainer t1, TimeSpanContainer t2) => new TimeSpanContainer(t1.m_TimeSpan - t2.m_TimeSpan);
            public static bool operator ==(TimeSpanContainer t1, TimeSpanContainer t2) => t1.m_TimeSpan == t2.m_TimeSpan;
            public static bool operator !=(TimeSpanContainer t1, TimeSpanContainer t2) => t1.m_TimeSpan != t2.m_TimeSpan;
            public static bool operator <(TimeSpanContainer t1, TimeSpanContainer t2) => t1.m_TimeSpan < t2.m_TimeSpan;
            public static bool operator >(TimeSpanContainer t1, TimeSpanContainer t2) => t1.m_TimeSpan > t2.m_TimeSpan;
            public static bool operator <=(TimeSpanContainer t1, TimeSpanContainer t2) => t1.m_TimeSpan <= t2.m_TimeSpan;
            public static bool operator >=(TimeSpanContainer t1, TimeSpanContainer t2) => t1.m_TimeSpan >= t2.m_TimeSpan;

            public static explicit operator string(TimeSpanContainer datetime) => datetime.ToString();
            public static explicit operator TimeSpanContainer(string input) => new TimeSpanContainer(TimeSpan.Parse(input, CultureInfo.InvariantCulture));
            public static implicit operator bool(TimeSpanContainer datetime) => datetime.m_TimeSpan != TimeSpan.Zero;
        }

        [APIExtension(APIExtension.DateTime, "TimeSpan")]
        [IsPure]
        public TimeSpanContainer ParseTimeSpan(string input) =>
            new TimeSpanContainer(TimeSpan.Parse(input, CultureInfo.InvariantCulture));
        [APIExtension(APIExtension.DateTime, "TimeSpan")]
        [IsPure]
        public TimeSpanContainer ParseTimeSpan(string input, string format) =>
            new TimeSpanContainer(TimeSpan.ParseExact(input, format, CultureInfo.InvariantCulture));
        [APIExtension(APIExtension.DateTime, "TimeSpan")]
        [IsPure]
        public TimeSpanContainer CreateTimeSpan(long ticks) =>
            new TimeSpanContainer(new TimeSpan(ticks));
        [APIExtension(APIExtension.DateTime, "TimeSpan")]
        [IsPure]
        public TimeSpanContainer CreateTimeSpan(int hours, int minutes, int seconds) =>
            new TimeSpanContainer(new TimeSpan(hours, minutes, seconds));
        [APIExtension(APIExtension.DateTime, "TimeSpan")]
        [IsPure]
        public TimeSpanContainer CreateTimeSpan(int days, int hours, int minutes, int seconds) =>
            new TimeSpanContainer(new TimeSpan(days, hours, minutes, seconds));
        [APIExtension(APIExtension.DateTime, "TimeSpan")]
        [IsPure]
        public TimeSpanContainer CreateTimeSpan(int days, int hours, int minutes, int seconds, int milliseconds) =>
            new TimeSpanContainer(new TimeSpan(days, hours, minutes, seconds, milliseconds));
        [APIExtension(APIExtension.DateTime, APIUseAsEnum.MemberFunction, "ToString")]
        [IsPure]
        public string TimeSpanToString(TimeSpanContainer timespan, string format) => timespan.ToString(format, CultureInfo.InvariantCulture);
    }
}
