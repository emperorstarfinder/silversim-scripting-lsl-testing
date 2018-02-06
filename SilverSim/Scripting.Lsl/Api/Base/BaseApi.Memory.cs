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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetMemoryLimit")]
        public int GetMemoryLimit()
        {
            /* Memory limit used for Mono scripts in SL */
            return 65536;
        }

        [APILevel(APIFlags.LSL, "llSetMemoryLimit")]
        public int SetMemoryLimit(int limit) => 0; /* we are not doing anything with the provided value */

        [APILevel(APIFlags.LSL, "llGetSPMaxMemory")]
        public int GetSPMaxMemory() => 65536; /* MaxMemory value used for Mono scripts in SL */

        [APILevel(APIFlags.LSL, "llGetUsedMemory")]
        public int GetUsedMemory() => 0; /* we have no resource tracking */

        [APILevel(APIFlags.LSL, "llGetFreeMemory")]
        public int GetFreeMemory() => 65536; /* we have no resource tracking */

        [APILevel(APIFlags.LSL)]
        public const int PROFILE_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PROFILE_SCRIPT_MEMORY = 1;

        [APILevel(APIFlags.LSL, "llScriptProfiler")]
        public void ScriptProfiler(int flags)
        {
            /* no-operation */
        }
    }
}
