// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llDeleteSubString")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public string ToLower(ScriptInstance instance, string s)
        {
            return s.ToLower();
        }

        [APILevel(APIFlags.LSL, "llToUpper")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public string ToUpper(ScriptInstance instance, string s)
        {
            return s.ToUpper();
        }

        [APILevel(APIFlags.LSL, "llUnescapeURL")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public string UnescapeURL(ScriptInstance instance, string url)
        {
            return Uri.UnescapeDataString(url);
        }

        [APILevel(APIFlags.LSL, "llEscapeURL")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public int StringLength(ScriptInstance instance, string src)
        {
            return src.Length;
        }

        [APILevel(APIFlags.LSL, "llSubStringIndex")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public int SubStringIndex(ScriptInstance instance, string source, string pattern)
        {
            return source.IndexOf(pattern);
        }

        [APILevel(APIFlags.LSL, "llGetSubstring")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
    }
}
