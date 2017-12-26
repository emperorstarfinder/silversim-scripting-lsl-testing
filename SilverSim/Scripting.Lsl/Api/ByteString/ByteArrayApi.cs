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

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace SilverSim.Scripting.Lsl.Api.ByteString
{
    [LSLImplementation]
    [ScriptApiName("ByteArray")]
    [Description("ByteArray API")]
    public sealed class ByteArrayApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.ByteArray, "bytearray")]
        [APIDisplayName("bytearray")]
        [APIIsVariableType]
        [APICloneOnAssignment]
        [APIAccessibleMembers("Length")]
        [Serializable]
        public class ByteArray
        {
            public byte[] Data = new byte[0];

            public ByteArray()
            {
                /* intentionally left empty */
            }

            public ByteArray(ByteArray src)
            {
                Data = new byte[src.Data.Length];
                Buffer.BlockCopy(src.Data, 0, Data, 0, src.Data.Length);
            }

            public ByteArray(byte[] src)
            {
                Data = src;
            }

            public int this[int index]
            {
                get
                {
                    return Data[index];
                }
                set
                {
                    Data[index] = (byte)value;
                }
            }

            public ByteArray this[int start, int count]
            {
                get
                {
                    if(start < 0 || start >= Data.Length || count < 0)
                    {
                        return new ByteArray();
                    }
                    byte[] resdata;
                    if(count > Data.Length - start)
                    {
                        resdata = new byte[Data.Length - start];
                        Buffer.BlockCopy(Data, start, resdata, 0, resdata.Length);
                    }
                    else
                    {
                        resdata = new byte[count];
                        Buffer.BlockCopy(Data, start, resdata, 0, count);
                    }
                    return new ByteArray(resdata);
                }
                set
                {
                    if (start < 0 || start >= Data.Length || count < 0)
                    {
                        return;
                    }
                    if(count > value.Length)
                    {
                        count = value.Length;
                    }
                    if (count > Data.Length - start)
                    {
                        Buffer.BlockCopy(value.Data, 0, Data, start, count);
                    }
                    else
                    {
                        Buffer.BlockCopy(value.Data, 0, Data, start, count);
                    }
                }
            }

            public static ByteArray operator+(ByteArray a, ByteArray b)
            {
                byte[] resdata = new byte[a.Length + b.Length];
                Buffer.BlockCopy(a.Data, 0, resdata, 0, a.Length);
                Buffer.BlockCopy(b.Data, 0, resdata, a.Length, b.Length);
                return new ByteArray(resdata);
            }

            [XmlIgnore]
            public int Length => Data.Length;
        }

        [APIExtension(APIExtension.ByteArray, "baSHA1")]
        public ByteArray CalcSHA1(ByteArray data)
        {
            using (var sha = SHA1.Create())
            {
                return new ByteArray(sha.ComputeHash(data.Data));
            }
        }

        [APIExtension(APIExtension.ByteArray, "baMD5")]
        public ByteArray CalcMD5(ByteArray data)
        {
            using (var md5 = MD5.Create())
            {
                return new ByteArray(md5.ComputeHash(data.Data));
            }
        }

        [APIExtension(APIExtension.ByteArray, "ByteArray")]
        public ByteArray CreateByteArray(int size) => new ByteArray(new byte[size]);

        [APIExtension(APIExtension.ByteArray, "baResize")]
        public ByteArray Resize(ByteArray byteArray, int size)
        {
            byte[] resdata = new byte[size];
            Buffer.BlockCopy(byteArray.Data, 0, resdata, 0, Math.Min(byteArray.Data.Length, size));
            return new ByteArray(resdata);
        }

        [APIExtension(APIExtension.ByteArray, "baFromBase64")]
        public ByteArray FromBase64(string base64) => new ByteArray(Convert.FromBase64String(base64));

        [APIExtension(APIExtension.ByteArray, "baToBase64")]
        public string ToBase64(ByteArray byteArray) => Convert.ToBase64String(byteArray.Data);

        [APIExtension(APIExtension.ByteArray, "baFromUTF8")]
        public ByteArray FromUTF8(string s) => new ByteArray(s.ToUTF8Bytes());

        [APIExtension(APIExtension.ByteArray, "baToUTF8")]
        public string ToUTF8(ByteArray byteArray) => byteArray.Data.FromUTF8Bytes();

        [APIExtension(APIExtension.ByteArray, "baFromHex")]
        public ByteArray FromHex(string s) => new ByteArray(s.FromHexStringToByteArray());

        [APIExtension(APIExtension.ByteArray, "baToHex")]
        public string ToHex(ByteArray byteArray) => byteArray.Data.ToHexString();
    }
}
