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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llDeleteSubString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DeleteSubString")]
        public string DeleteSubString(string src, int start, int end)
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
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ToLower")]
        public string ToLower(string s) => s.ToLower();

        [APILevel(APIFlags.LSL, "llToUpper")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ToUpper")]
        public string ToUpper(string s) => s.ToUpper();

        [APILevel(APIFlags.LSL, "llUnescapeURL")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "UnescapeURL")]
        public string UnescapeURL(string url) => Uri.UnescapeDataString(url);

        [APILevel(APIFlags.LSL, "llEscapeURL")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "EscapeURL")]
        public string EscapeURL(string url) => Uri.EscapeDataString(url);

        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM_HEAD = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM_TAIL = 0x2;
        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM = 0x3;

        private static readonly char[] trimchars = new char[] { ' ', '\t', '\r', '\n' };

        [APILevel(APIFlags.LSL, "llStringTrim")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Trim")]
        public string StringTrim(string src, int type)
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

        [APILevel(APIFlags.LSL, "llInsertString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Insert")]
        public string InsertString(string dest, int index, string src)
        {
            if (index < 0)
            {
                index = dest.Length + index;

                if (index < 0)
                {
                    return src + dest;
                }
            }

            if (index >= dest.Length)
            {
                return dest + src;
            }

            return dest.Substring(0, index) + src + dest.Substring(index);
        }

        [APILevel(APIFlags.LSL, "llStringLength")]
        public int StringLength(string src) => src.Length;

        [APILevel(APIFlags.LSL, "llSubStringIndex")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Index")]
        public int SubStringIndex(string source, string pattern) => source.IndexOf(pattern);

        [APILevel(APIFlags.LSL, "llGetSubString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Substring")]
        public string GetSubstring(string src, int start, int end)
        {
            if(src.Length == 0)
            {
                return src;
            }

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
                string a = start < src.Length ? src.Substring(start) : string.Empty;
                string b = src.Substring(0, end + 1);
                return b + a;
            }
        }

        [APILevel(APIFlags.LSL, "llMD5String")]
        public string MD5String(string src, int nonce)
        {
            using (var md5 = MD5.Create())
            {
                byte[] b = md5.ComputeHash(string.Format("{0}:{1}", src, nonce.ToString()).ToUTF8Bytes());
                var s = new StringBuilder();
                for(int i = 0; i < b.Length; ++i)
                {
                    s.Append(string.Format("{0:x2}", b[i]));
                }
                return s.ToString();
            }
        }

        [APILevel(APIFlags.LSL, "llSHA1String")]
        public string SHA1String(string src)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] b = sha1.ComputeHash(src.ToUTF8Bytes());
                var s = new StringBuilder();
                for (int i = 0; i < b.Length; ++i)
                {
                    s.Append(string.Format("{0:x2}", b[i]));
                }
                return s.ToString();
            }
        }

        [APILevel(APIFlags.OSSL, "osMatchString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Match")]
        public AnArray OsMatchString(string src, string pattern, int start)
        {
            var result = new AnArray();

            /* Normalize indices (if negative). */
            if (start < 0)
            {
                start = src.Length + start;
            }

            if (start < 0 || start >= src.Length)
            {
                return result;  // empty list
            }

            // Find matches beginning at start position
            var matcher = new Regex(pattern);
            Match match = matcher.Match(src, start);
            while (match.Success)
            {
                foreach (Group g in match.Groups)
                {
                    if (g.Success)
                    {
                        result.Add(g.Value);
                        result.Add(g.Index);
                    }
                }

                match = match.NextMatch();
            }

            return result;
        }

        [APILevel(APIFlags.OSSL, "osReplaceString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Replace")]
        public string OsReplaceString(string src, string pattern, string replace, int count, int start)
        {
            if (start < 0)
            {
                start = src.Length + start;
            }

            if (start < 0 || start >= src.Length)
            {
                return src;
            }

            var matcher = new Regex(pattern);
            return matcher.Replace(src, replace, count, start);
        }

        [APILevel(APIFlags.OSSL, "osFormatString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Format")]
        public string OsFormatString(string fmt, AnArray list) => string.Format(fmt, list.ToArray());

        [APILevel(APIFlags.OSSL, "osRegexIsMatch")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "MatchesRegex")]
        public int RegexIsMatch(ScriptInstance instance, string input, string pattern)
        {
            try
            {
                return Regex.IsMatch(input, pattern) ? 1 : 0;
            }
            catch (Exception)
            {
                lock (instance)
                {
                    instance.ShoutError("Possible invalid regular expression detected.");
                }
                return 0;
            }
        }

        [APILevel(APIFlags.OSSL, "osStringStartsWith")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "StartsWith")]
        public int StringStartsWith(string input, string startsWith) => input.StartsWith(startsWith).ToLSLBoolean();

        [APILevel(APIFlags.OSSL, "osStringEndsWith")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "EndsWith")]
        public int StringEndsWith(string input, string endsWith) => input.EndsWith(endsWith).ToLSLBoolean();
    }
}
