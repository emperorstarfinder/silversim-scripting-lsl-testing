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

using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Lsl.Api.ByteString;
using SilverSim.Types;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SilverSim.Scripting.Lsl.Api.Http
{
    public partial class HttpApi
    {
        [APILevel(APIFlags.LSL)]
        public const int HTTP_METHOD = 0;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_MIMETYPE = 1;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_BODY_MAXLENGTH = 2;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_VERIFY_CERT = 3;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_VERBOSE_THROTTLE = 4;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_CUSTOM_HEADER = 5;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_PRAGMA_NO_CACHE = 6;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_USER_AGENT = 7;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_ACCEPT = 8;
        [APIExtension(APIExtension.Properties)]
        public const int HTTP_USE_BYTEARRAY = 10000;

        private readonly string[] m_AllowedHttpHeaders =
        {
            "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language",
            "Accept-Ranges", "Age", "Allow", "Authorization", "Cache-Control",
            "Connection", "Content-Encoding", "Content-Language",
            "Content-Length", "Content-Location", "Content-MD5",
            "Content-Range", "Content-Type", "Date", "ETag", "Expect",
            "Expires", "From", "Host", "If-Match", "If-Modified-Since",
            "If-None-Match", "If-Range", "If-Unmodified-Since", "Last-Modified",
            "Location", "Max-Forwards", "Pragma", "Proxy-Authenticate",
            "Proxy-Authorization", "Range", "Referer", "Retry-After", "Server",
            "TE", "Trailer", "Transfer-Encoding", "Upgrade", "User-Agent",
            "Vary", "Via", "Warning", "WWW-Authenticate"
        };

        private static readonly Regex m_AuthRegex = new Regex(@"^(https?:\/\/)(\w+):(\w+)@(.*)$");

        [APILevel(APIFlags.LSL, "llHTTPRequest")]
        public LSLKey HTTPRequest(ScriptInstance instance, string url, AnArray parameters, string body) =>
            HTTPRequest(instance, url, parameters, body.ToUTF8Bytes());

        [APIExtension(APIExtension.ByteArray, "llHTTPRequest")]
        public LSLKey HTTPRequest(ScriptInstance instance, string url, AnArray parameters, ByteArrayApi.ByteArray body) =>
            HTTPRequest(instance, url, parameters, body.Data);

        private LSLKey HTTPRequest(ScriptInstance instance, string url, AnArray parameters, byte[] body)
        {
            var req = new LSLHTTPClient_RequestQueue.LSLHttpRequest();
            lock (instance)
            {
                req.SceneID = instance.Part.ObjectGroup.Scene.ID;
                req.PrimID = instance.Part.ID;
                req.ItemID = instance.Item.ID;
                req.RequestBody = body;
                req.Url = url;
            }

            if (url.Contains(' '))
            {
                lock (instance)
                {
                    var e = new HttpResponseEvent
                    {
                        RequestID = UUID.Random,
                        Status = 499
                    };
                    instance.Part.PostEvent(e);
                    return e.RequestID;
                }
            }

            int i = -1;
            while(++i < parameters.Count)
            {
                switch(parameters[i].AsInt)
                {
                    case HTTP_METHOD:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_METHOD"));
                                return UUID.Zero;
                            }
                        }

                        req.Method = parameters[++i].ToString();
                        break;

                    case HTTP_MIMETYPE:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_MIMEYPE"));
                                return UUID.Zero;
                            }
                        }

                        req.MimeType = parameters[++i].ToString();
                        break;

                    case HTTP_BODY_MAXLENGTH:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_METHOD"));
                                return UUID.Zero;
                            }
                        }

                        req.MaxBodyLength = parameters[++i].AsInt;
                        break;

                    case HTTP_VERIFY_CERT:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_VERIFY_CERT"));
                                return UUID.Zero;
                            }
                        }

                        req.VerifyCert = parameters[++i].AsBoolean;
                        break;

                    case HTTP_VERBOSE_THROTTLE:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_VERBOSE_THROTTLE"));
                                return UUID.Zero;
                            }
                        }

                        req.VerboseThrottle = parameters[++i].AsBoolean;
                        break;

                    case HTTP_USE_BYTEARRAY:
                        if (i + 1 >= parameters.Count)
                        {
                            lock (instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_USE_BYTEARRAY"));
                                return UUID.Zero;
                            }
                        }

                        req.RequestsByteResponse = parameters[++i].AsBoolean;
                        break;

                    case HTTP_CUSTOM_HEADER:
                        if(i + 2 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_CUSTOM_HEADER"));
                                return UUID.Zero;
                            }
                        }

                        string name = parameters[++i].ToString();
                        string value = parameters[++i].ToString();

                        if (!m_AllowedHttpHeaders.Contains(name))
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "CustomHeader0NotAllowed", "Custom header {0} not allowed", name));
                            return UUID.Zero;
                        }
                        try
                        {
                            req.Headers.Add(name, value);
                        }
                        catch
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "CustomHeader0AlreadyDefined", "Custom header {0} already defined", name));
                            return UUID.Zero;
                        }
                        break;

                    case HTTP_PRAGMA_NO_CACHE:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_PRAGMA_NO_CACHE"));
                                return UUID.Zero;
                            }
                        }

                        req.SendPragmaNoCache = parameters[++i].AsBoolean;
                        break;

                    case HTTP_USER_AGENT:
                        if (i + 1 >= parameters.Count)
                        {
                            lock (instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_USER_AGENT"));
                                return UUID.Zero;
                            }
                        }

                        string append = parameters[++i].ToString();
                        if(append.Contains(" "))
                        {
                            return UUID.Zero;
                        }

                        req.Headers["User-Agent"] += " " + append;
                        break;

                    case HTTP_ACCEPT:
                        if (i + 1 >= parameters.Count)
                        {
                            lock (instance)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "MissingParameterFor0", "Missing parameter for {0}", "HTTP_ACCEPT"));
                                return UUID.Zero;
                            }
                        }

                        req.Headers["Accept"] = parameters[++i].ToString();
                        break;

                    default:
                        lock(instance)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "UnknownParameter0forllHTTPRequest", "Unknown parameter {0} for llHTTPRequest", parameters[i].AsInt));
                            return UUID.Zero;
                        }
                }
            }

            lock (instance)
            {
                req.Headers.Add("X-SecondLife-Object-Name", instance.Part.ObjectGroup.Name);
                req.Headers.Add("X-SecondLife-Object-Key", (string)instance.Part.ObjectGroup.ID);
                req.Headers.Add("X-SecondLife-Region", instance.Part.ObjectGroup.Scene.Name);
                req.Headers.Add("X-SecondLife-Local-Position", string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", instance.Part.ObjectGroup.GlobalPosition.X, instance.Part.ObjectGroup.GlobalPosition.Y, instance.Part.ObjectGroup.GlobalPosition.Z));
                req.Headers.Add("X-SecondLife-Local-Velocity", string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", instance.Part.ObjectGroup.Velocity.X, instance.Part.ObjectGroup.Velocity.Y, instance.Part.ObjectGroup.Velocity.Z));
                req.Headers.Add("X-SecondLife-Local-Rotation", string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000}, {3:0.000000})", instance.Part.ObjectGroup.GlobalRotation.X, instance.Part.ObjectGroup.GlobalRotation.Y, instance.Part.ObjectGroup.GlobalRotation.Z, instance.Part.ObjectGroup.GlobalRotation.W));
                req.Headers.Add("X-SecondLife-Owner-Name", instance.Part.ObjectGroup.Scene.AvatarNameService.ResolveName(instance.Part.ObjectGroup.Owner).FullName);
                req.Headers.Add("X-SecondLife-Owner-Key", (string)instance.Part.ObjectGroup.Owner.ID);

                Match authMatch = m_AuthRegex.Match(url);
                if(authMatch.Success &&
                    authMatch.Groups.Count == 5)
                {
                    string authData = string.Format("{0}:{1}", authMatch.Groups[2].ToString(), authMatch.Groups[3].ToString());
                    byte[] authDataBinary = authData.ToUTF8Bytes();
                    req.Headers.Add("Authorization", string.Format("Basic {0}", Convert.ToBase64String(authDataBinary)));
                }

                return m_LSLHTTPClient.Enqueue(req) ?
                    req.RequestID :
                    UUID.Zero;
            }
        }
    }
}
