// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetTimestamp")]
        public string GetTimestamp(ScriptInstance instance)
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        [APILevel(APIFlags.LSL, "llGetUnixTime")]
        public int GetUnixTime(ScriptInstance instance)
        {
            return (int)Date.GetUnixTime();
        }

        [APILevel(APIFlags.LSL, "llGetGMTclock")]
        public double GetGMTclock(ScriptInstance instance)
        {
            return Date.GetUnixTime();
        }

        [APILevel(APIFlags.LSL, "llGetTimeOfDay")]
        public double GetTimeOfDay(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.TimeOfDay;
            }
        }

        [APILevel(APIFlags.LSL, "llGetWallclock")]
        public double GetWallclock(ScriptInstance instance)
        {
            /* function is defined as returning PST, so we do that */
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Pacific Standard Time").TimeOfDay.TotalMilliseconds / 1000;
        }

        [APILevel(APIFlags.LSL, "llGetDate")]
        public string GetDate(ScriptInstance instance)
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        [APILevel(APIFlags.OSSL, "osUnixTimeToTimestamp")]
        public string OsUnixTimeToTimestamp(ScriptInstance instance, int time)
        {
            long baseTicks = 621355968000000000;
            long tickResolution = 10000000;
            long epochTicks = (time * tickResolution) + baseTicks;
            DateTime date = new DateTime(epochTicks);

            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

    }
}
