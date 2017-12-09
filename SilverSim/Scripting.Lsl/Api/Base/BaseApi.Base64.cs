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
using System.Text;

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llIntegerToBase64")]
        public string IntegerToBase64(int number)
        {
            byte[] b = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL, "llBase64ToInteger")]
        public int Base64ToInteger(string s)
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
        public string StringToBase64(string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL, "llBase64ToString")]
        public string Base64ToString(string str)
        {
            byte[] b = Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(b);
        }

        [APILevel(APIFlags.LSL, "llXorBase64")]
        [APILevel(APIFlags.LSL, "llXorBase64StringsCorrect")]
        public string XorBase64(string str1, string str2)
        {
            byte[] a = Convert.FromBase64String(str1);
            byte[] b = Convert.FromBase64String(str2);
            var o = new byte[a.Length];

            for (int i = 0; i < a.Length; ++i)
            {
                o[i] = (byte)(a[i] ^ b[i % b.Length]);
            }
            return Convert.ToBase64String(o);
        }

        [APILevel(APIFlags.LSL, "llXorBase64Strings")]
        [ForcedSleep(0.3)]
        public string XorBase64Strings(string str1, string str2) =>
            XorBase64(str1, str2);
    }
}
