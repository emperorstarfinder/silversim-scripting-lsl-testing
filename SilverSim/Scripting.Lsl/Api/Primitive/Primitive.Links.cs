// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, "llCreateLink")]
        public void CreateLink(ScriptInstance instance, LSLKey key, int parent)
        {
            throw new NotImplementedException("llCreateLink(key, integer)");
        }

        [APILevel(APIFlags.LSL, "llBreakLink")]
        public void BreakLink(ScriptInstance instance, int link)
        {
            throw new NotImplementedException("llBreakLink(integer)");
        }

        [APILevel(APIFlags.LSL, "llBreakAllLinks")]
        public void BreakAllLinks(ScriptInstance instance)
        {
            throw new NotImplementedException("llBreakAllLinks()");
        }

        [APILevel(APIFlags.OSSL, "osForceCreateLink")]
        public void ForceCreateLink(ScriptInstance instance, LSLKey target, int parent)
        {
            throw new NotImplementedException("osForceCreateLink(key, integer)");
        }

        [APILevel(APIFlags.OSSL, "osForceBreakLink")]
        public void ForceBreakLink(ScriptInstance instance, int link)
        {
            throw new NotImplementedException("osForceBreakLink(integer)");
        }

        [APILevel(APIFlags.OSSL, "osForceBreakAllLinks")]
        public void ForceBreakAllLinks(ScriptInstance instance)
        {
            throw new NotImplementedException("osForceBreakAllLinks()");
        }
    }
}
