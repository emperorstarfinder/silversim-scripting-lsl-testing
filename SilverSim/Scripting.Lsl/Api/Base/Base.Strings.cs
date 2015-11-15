// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llDeleteSubString")]
        public string DeleteSubString(ScriptInstance instance, string src, int start, int end)
        {
            if (start < 0)
            {
                start = src.Length - start;
            }
            if (end < 0)
            {
                end = src.Length - end;
            }

            if (start < 0)
            {
                start = 0;
            }
            else if (start > src.Length)
            {
                start = src.Length;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if (end > src.Length)
            {
                end = src.Length;
            }

            return (start > end) ?
                src.Substring(start, end - start + 1) :
                src.Substring(0, start + 1) + src.Substring(end);
        }

        [APILevel(APIFlags.LSL, "llToLower")]
        public string ToLower(ScriptInstance instance, string s)
        {
            return s.ToLower();
        }

        [APILevel(APIFlags.LSL, "llToUpper")]
        public string ToUpper(ScriptInstance instance, string s)
        {
            return s.ToUpper();
        }

        [APILevel(APIFlags.LSL, "llUnescapeURL")]
        public string UnescapeURL(ScriptInstance instance, string url)
        {
            return Uri.UnescapeDataString(url);
        }

        [APILevel(APIFlags.LSL, "llEscapeURL")]
        public string EscapeURL(ScriptInstance instance, string url)
        {
            return Uri.EscapeDataString(url);
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STRING_TRIM_HEAD = 0x1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STRING_TRIM_TAIL = 0x2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STRING_TRIM = 0x3;

        static readonly char[] trimchars = new char[] { ' ', '\t', '\r', '\n' };

        [APILevel(APIFlags.LSL, "llStringTrim")]
        public string StringTrim(ScriptInstance instance, string src, int type)
        {
            switch(type & STRING_TRIM)
            {
                case STRING_TRIM_HEAD:
                    src = src.TrimStart(trimchars);
                    break;
                case STRING_TRIM_TAIL:
                    src = src.TrimEnd(trimchars);
                    break;

                case STRING_TRIM:
                    src = src.Trim(trimchars);
                    break;

                default:
                    break;
            }

            return src;
        }

        [APILevel(APIFlags.LSL, "llStringLength")]
        public int StringLength(ScriptInstance instance, string src)
        {
            return src.Length;
        }

        [APILevel(APIFlags.LSL, "llSubStringIndex")]
        public int SubStringIndex(ScriptInstance instance, string source, string pattern)
        {
            return source.IndexOf(pattern);
        }

        [APILevel(APIFlags.LSL, "llGetSubString")]
        public string GetSubstring(ScriptInstance instance, string src, int start, int end)
        {
            if(start < 0)
            {
                start = src.Length - start;
            }
            if (end < 0)
            {
                end = src.Length - end;
            }

            if(start < 0)
            {
                start = 0;
            }
            else if(start > src.Length)
            {
                start = src.Length;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if(end > src.Length)
            {
                end = src.Length;
            }

            if(start <= end)
            {
                return src.Substring(start, end - start + 1);
            }
            else
            {
                string a = src.Substring(start);
                string b = src.Substring(0, end + 1);
                return b + a;
            }
        }

        [APILevel(APIFlags.LSL, "llMD5String")]
        public string MD5String(ScriptInstance instance, string src, int nonce)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] b = md5.ComputeHash(UTF8NoBOM.GetBytes(string.Format("{0}:{1}", src, nonce.ToString())));
                string s = string.Empty;
                for(int i = 0; i < b.Length; ++i)
                {
                    s += string.Format("{0:x2}", b[i]);
                }
                return s;
            }
        }

        [APILevel(APIFlags.LSL, "llSHA1String")]
        public string SHA1String(ScriptInstance instance, string src)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] b = sha1.ComputeHash(UTF8NoBOM.GetBytes(src));
                string s = string.Empty;
                for (int i = 0; i < b.Length; ++i)
                {
                    s += string.Format("{0:x2}", b[i]);
                }
                return s;
            }
        }

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
