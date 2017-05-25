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

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetTimestamp")]
        public string GetTimestamp(ScriptInstance instance) => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

        [APILevel(APIFlags.LSL, "llGetUnixTime")]
        public int GetUnixTime(ScriptInstance instance) => (int)Date.GetUnixTime();

        [APILevel(APIFlags.LSL, "llGetGMTclock")]
        public double GetGMTclock(ScriptInstance instance) => Date.GetUnixTime();

        [APILevel(APIFlags.LSL, "llGetTimeOfDay")]
        public double GetTimeOfDay(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.TimeOfDay;
            }
        }

        [APILevel(APIFlags.LSL, "llGetWallclock")]
        public double GetWallclock(ScriptInstance instance) => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Pacific Standard Time").TimeOfDay.TotalMilliseconds / 1000;

        [APILevel(APIFlags.LSL, "llGetDate")]
        public string GetDate(ScriptInstance instance) => DateTime.UtcNow.ToString("yyyy-MM-dd");

        [APILevel(APIFlags.OSSL, "osUnixTimeToTimestamp")]
        public string OsUnixTimeToTimestamp(ScriptInstance instance, int time)
        {
            const long baseTicks = 621355968000000000;
            const long tickResolution = 10000000;
            long epochTicks = (time * tickResolution) + baseTicks;
            var date = new DateTime(epochTicks);

            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }
    }
}
