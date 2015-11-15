// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetMemoryLimit")]
        public int GetMemoryLimit(ScriptInstance instance)
        {
            /* Memory limit used for Mono scripts in SL */
            return 65536;
        }

        [APILevel(APIFlags.LSL, "llSetMemoryLimit")]
        public int SetMemoryLimit(ScriptInstance instance, int limit)
        {
            /* we are not doing anything with the provided value */
            return 0;
        }

        [APILevel(APIFlags.LSL, "llGetSPMaxMemory")]
        public int GetSPMaxMemory(ScriptInstance instance)
        {
            /* MaxMemory value used for Mono scripts in SL */
            return 65536;
        }

        [APILevel(APIFlags.LSL, "llGetUsedMemory")]
        public int GetUsedMemory(ScriptInstance instance)
        {
            /* we have no resource tracking */
            return 0;
        }

        [APILevel(APIFlags.LSL, "llGetFreeMemory")]
        public int GetFreeMemory(ScriptInstance instance)
        {
            /* we have no resource tracking */
            return 6556;
        }
    }
}
