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
    }
}
