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
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llDeleteSubString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DeleteSubString")]
        [Pure]
        public string DeleteSubString(string src, int start, int end)
        {
            int srcLength = src.Length;
            if (start < 0)
            {
                start = srcLength + start;
            }
            if (end < 0)
            {
                end = srcLength + end;
            }

            if(start <= end)
            {
                if(end < 0 || start >= srcLength)
                {
                    return src;
                }

                start = Math.Max(0, start);
                end = Math.Min(end, srcLength - 1);

                return src.Remove(start, end - start + 1);
            }
            else if(start < 0 || end >= srcLength)
            {
                return string.Empty;
            }
            else if(end > 0)
            {
                if (start < srcLength)
                {
                    var sb = new StringBuilder(src);
                    sb.Remove(start, srcLength - start);
                    sb.Remove(0, end + 1);
                    return sb.ToString();
                }
                else
                {
                    return src.Substring(end + 1);
                }
            }
            else
            {
                return (start < srcLength) ? src.Substring(0, start) : src;
            }
        }

        [APILevel(APIFlags.LSL, "llToLower")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ToLower")]
        [Pure]
        public string ToLower(string s) => s.ToLower();

        [APILevel(APIFlags.LSL, "llToUpper")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ToUpper")]
        [Pure]
        public string ToUpper(string s) => s.ToUpper();

        [APILevel(APIFlags.LSL, "llUnescapeURL")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "UnescapeURL")]
        [Pure]
        public string UnescapeURL(string url) => Uri.UnescapeDataString(url);

        [APILevel(APIFlags.LSL, "llEscapeURL")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "EscapeURL")]
        [Pure]
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
        [Pure]
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
        [Pure]
        public string InsertString(string dest, int index, string src)
        {
            /* negative indexing here is non-LSL but we keep it since it makes sense to have it similarized to calls like llGetSubString and llDeleteSubString */
            int destLength = dest.Length;
            var res = new StringBuilder();
            if (index < 0)
            {
                index = destLength + index;
            }

            if (index > 0)
            {
                index = Math.Min(index, destLength);
                res.Append(dest, 0, index);
            }
            res.Append(src);
            if (index < destLength)
            {
                index = Math.Max(0, index);
                res.Append(dest, index, destLength - index);
            }
            return res.ToString();
        }

        [APILevel(APIFlags.LSL, "llStringLength")]
        [Pure]
        public int StringLength(string src) => src.Length;

        [APILevel(APIFlags.LSL, "llSubStringIndex")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "IndexOf")]
        [Pure]
        public int SubStringIndex(string source, string pattern) => source.IndexOf(pattern);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Contains")]
        [Pure]
        public int Contains(string source, string pattern) => source.Contains(pattern).ToLSLBoolean();

        [APILevel(APIFlags.LSL, "llGetSubString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Substring")]
        [Pure]
        public string GetSubstring(string src, int start, int end)
        {
            int srcLength = src.Length;
            if(srcLength == 0)
            {
                return src;
            }

            if(start < 0)
            {
                start = srcLength + start;
            }
            if (end < 0)
            {
                end = srcLength + end;
            }

            if(start <= end)
            {
                if(end < 0 || start >= srcLength)
                {
                    return string.Empty;
                }

                end = Math.Min(end, srcLength - 1);
                return (start < 0) ?
                    src.Substring(0, end + 1) :
                    src.Substring(start, (end + 1) - start);
            }
            else if( start < 0 || end >= srcLength)
            {
                return src;
            }
            else if(end < 0)
            {
                return (start < srcLength) ? src.Substring(start) : string.Empty;
            }
            else
            {
                string b = start < srcLength ? src.Substring(start) : string.Empty;
                return src.Substring(0, end + 1) + b;
            }
        }

        [APILevel(APIFlags.LSL, "llMD5String")]
        [Pure]
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
        [Pure]
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
        [Pure]
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
        [Pure]
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
        [Pure]
        public string OsFormatString(string fmt, AnArray list) => string.Format(fmt, list.ToArray());

        [APILevel(APIFlags.OSSL, "osRegexIsMatch")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "MatchesRegex")]
        [Pure]
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
        [Pure]
        public int StringStartsWith(string input, string startsWith) => input.StartsWith(startsWith).ToLSLBoolean();

        [APILevel(APIFlags.OSSL, "osStringEndsWith")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "EndsWith")]
        [Pure]
        public int StringEndsWith(string input, string endsWith) => input.EndsWith(endsWith).ToLSLBoolean();
    }
}
