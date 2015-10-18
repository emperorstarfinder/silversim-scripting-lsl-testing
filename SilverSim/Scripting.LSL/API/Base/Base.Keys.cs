// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;
using SilverSim.Scripting.Common;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGenerateKey")]
        public LSLKey GenerateKey(ScriptInstance Instance)
        {
            return new LSLKey(UUID.Random);
        }

        #region osIsUUID
        [APILevel(APIFlags.OSSL)]
        [ScriptFunctionName("osIsUUID")]
        public int IsUUID(ScriptInstance Instance, string input)
        {
            Guid v;
            return Guid.TryParse(input, out v) ? TRUE : FALSE;
        }
        #endregion
    }
}
