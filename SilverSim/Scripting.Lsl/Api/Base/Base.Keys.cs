// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGenerateKey")]
        public LSLKey GenerateKey(ScriptInstance instance)
        {
            return new LSLKey(UUID.Random);
        }

        #region osIsUUID
        [APILevel(APIFlags.OSSL, "osIsUUID")]
        public int IsUUID(ScriptInstance instance, string input)
        {
            Guid v;
            return Guid.TryParse(input, out v) ? TRUE : FALSE;
        }
        #endregion
    }
}
