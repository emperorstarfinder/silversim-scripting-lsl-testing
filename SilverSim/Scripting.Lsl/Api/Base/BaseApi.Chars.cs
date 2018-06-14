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

using System;
using System.Globalization;
using System.Linq;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APIExtension(APIExtension.CharacterType, "asIsAlpha")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsAlpha")]
        public int IsAlpha(char c) => char.IsLetter(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsAlnum")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsAlnum")]
        public int IsAlnum(char c) => char.IsLetterOrDigit(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsControl")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsControl")]
        public int IsControl(char c) => char.IsControl(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsDigit")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsDigit")]
        public int IsDigit(char c) => char.IsDigit(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsLower")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsLower")]
        public int IsLower(char c) => char.IsLower(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsPunct")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsPunct")]
        public int IsPunct(char c) => char.IsPunctuation(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsSpace")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsSpace")]
        public int IsSpace(char c) => char.IsWhiteSpace(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsUpper")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsUpper")]
        public int IsUpper(char c) => char.IsUpper(c).ToLSLBoolean();
        [APIExtension(APIExtension.CharacterType, "asIsXDigit")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsXDigit")]
        public int IsXDigit(char c) => (char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')).ToLSLBoolean();

        private static readonly UnicodeCategory[] nonRenderingCats = new UnicodeCategory[]
        {
            UnicodeCategory.Control,
            UnicodeCategory.OtherNotAssigned,
            UnicodeCategory.Surrogate
        };

        [APIExtension(APIExtension.CharacterType, "asIsPrint")]
        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "IsPrint")]
        public int IsPrint(char c) => (char.IsWhiteSpace(c) || !nonRenderingCats.Contains(Char.GetUnicodeCategory(c))).ToLSLBoolean();
    }
}
