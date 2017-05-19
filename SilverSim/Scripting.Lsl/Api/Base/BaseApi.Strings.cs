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
        public string ToLower(ScriptInstance instance, string s) => s.ToLower();

        [APILevel(APIFlags.LSL, "llToUpper")]
        public string ToUpper(ScriptInstance instance, string s) => s.ToUpper();

        [APILevel(APIFlags.LSL, "llUnescapeURL")]
        public string UnescapeURL(ScriptInstance instance, string url) => Uri.UnescapeDataString(url);

        [APILevel(APIFlags.LSL, "llEscapeURL")]
        public string EscapeURL(ScriptInstance instance, string url) => Uri.EscapeDataString(url);

        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM_HEAD = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM_TAIL = 0x2;
        [APILevel(APIFlags.LSL)]
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

        [APILevel(APIFlags.LSL, "llInsertString")]
        public string InsertString(ScriptInstance instance, string dest, int index, string src)
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
        public int StringLength(ScriptInstance instance, string src) => src.Length;

        [APILevel(APIFlags.LSL, "llSubStringIndex")]
        public int SubStringIndex(ScriptInstance instance, string source, string pattern) => source.IndexOf(pattern);

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
        public string SHA1String(ScriptInstance instance, string src)
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
        public AnArray OsMatchString(ScriptInstance instance, string src, string pattern, int start)
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
        public string OsReplaceString(ScriptInstance instance, string src, string pattern, string replace, int count, int start)
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
        public string OsFormatString(ScriptInstance instance, string fmt, AnArray list) => string.Format(fmt, list.ToArray());

        [APILevel(APIFlags.OSSL, "osRegexIsMatch")]
        public int RegexIsMatch(ScriptInstance instance, string input, string pattern)
        {
            lock(instance)
            {
                try
                {
                    return Regex.IsMatch(input, pattern) ? 1 : 0;
                }
                catch (Exception)
                {
                    instance.ShoutError("Possible invalid regular expression detected.");
                    return 0;
                }
            }
        }
    }
}
