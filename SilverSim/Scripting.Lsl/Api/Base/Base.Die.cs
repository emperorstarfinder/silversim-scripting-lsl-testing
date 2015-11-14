// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llDie")]
        public void Die(ScriptInstance instance)
        {
            instance.AbortBegin();
            instance.Part.ObjectGroup.Scene.Remove(instance.Part.ObjectGroup, instance);
            throw new ScriptAbortException();
        }
    }
}
