// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llIntegerToBase64")]
        public string IntegerToBase64(ScriptInstance instance, int number)
        {
            byte[] b = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL, "llBase64ToInteger")]
        public int Base64ToInteger(ScriptInstance instance, string s)
        {
            if (s.Length > 8)
            {
                return 0;
            }
            string i = s.PadRight(8, '=');
            byte[] b = Convert.FromBase64String(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b, 0, 4);
            }
            return BitConverter.ToInt32(b, 0);
        }

        [APILevel(APIFlags.LSL, "llStringToBase64")]
        public string StringToBase64(ScriptInstance instance, string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL, "llBase64ToString")]
        public string Base64ToString(ScriptInstance instance, string str)
        {
            byte[] b = Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(b);
        }

        [APILevel(APIFlags.LSL, "llXorBase64")]
        [APILevel(APIFlags.LSL, "llXorBase64StringsCorrect")]
        public string XorBase64(ScriptInstance instance, string str1, string str2)
        {
            byte[] a = Convert.FromBase64String(str1);
            byte[] b = Convert.FromBase64String(str2);
            byte[] o = new byte[a.Length];

            for (int i = 0; i < a.Length; ++i)
            {
                o[i] = (byte)(a[i] ^ b[i % b.Length]);
            }
            return Convert.ToBase64String(o);
        }

        [APILevel(APIFlags.LSL, "llXorBase64Strings")]
        [ForcedSleep(0.3)]
        public string XorBase64Strings(ScriptInstance instance, string str1, string str2)
        {
            return XorBase64(instance, str1, str2);
        }
    }
}
