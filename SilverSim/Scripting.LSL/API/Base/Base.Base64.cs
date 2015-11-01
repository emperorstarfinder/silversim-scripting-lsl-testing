// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SilverSim.Scripting.LSL.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llIntegerToBase64")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string IntegerToBase64(ScriptInstance instance, int number)
        {
            byte[] b = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return System.Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL, "llBase64ToInteger")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        int Base64ToInteger(ScriptInstance instance, string s)
        {
            if (s.Length > 8)
            {
                return 0;
            }
            string i = s.PadRight(8, '=');
            byte[] b = System.Convert.FromBase64String(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b, 0, 4);
            }
            return BitConverter.ToInt32(b, 0);
        }

        [APILevel(APIFlags.LSL, "llStringToBase64")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string StringToBase64(ScriptInstance instance, string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            return System.Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL, "llBase64ToString")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string Base64ToString(ScriptInstance instance, string str)
        {
            byte[] b = System.Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(b);
        }

        [APILevel(APIFlags.LSL, "llXorBase64")]
        [ForcedSleep(0.3)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string XorBase64(ScriptInstance instance, string str1, string str2)
        {
            byte[] a = System.Convert.FromBase64String(str1);
            byte[] b = System.Convert.FromBase64String(str2);
            byte[] o = new byte[a.Length];

            for (int i = 0; i < a.Length; ++i)
            {
                o[i] = (byte)(a[i] ^ b[i % b.Length]);
            }
            return System.Convert.ToBase64String(o);
        }
    }
}
