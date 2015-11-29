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
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llBreakLink")]
        public void BreakLink(ScriptInstance instance, int link)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llBreakAllLinks")]
        public void BreakAllLinks(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.OSSL, "osForceCreateLink")]
        public void ForceCreateLink(ScriptInstance instance, string target, int parent)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.OSSL, "osForceBreakLink")]
        public void ForceBreakLink(ScriptInstance instance, int link)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.OSSL, "osForceBreakAllLinks")]
        public void ForceBreakAllLinks(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }
    }
}
