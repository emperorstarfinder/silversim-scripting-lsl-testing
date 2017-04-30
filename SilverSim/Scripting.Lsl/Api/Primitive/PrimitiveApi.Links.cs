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
