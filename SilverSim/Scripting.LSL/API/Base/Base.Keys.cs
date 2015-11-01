// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGenerateKey")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        LSLKey GenerateKey(ScriptInstance instance)
        {
            return new LSLKey(UUID.Random);
        }

        #region osIsUUID
        [APILevel(APIFlags.OSSL, "osIsUUID")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        int IsUUID(ScriptInstance instance, string input)
        {
            Guid v;
            return Guid.TryParse(input, out v) ? TRUE : FALSE;
        }
        #endregion
    }
}
