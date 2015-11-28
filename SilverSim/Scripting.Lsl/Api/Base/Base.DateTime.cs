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
            return DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
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
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetWallclock")]
        public double GetWallclock(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetDate")]
        public string GetDate(ScriptInstance instance)
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
    }
}
