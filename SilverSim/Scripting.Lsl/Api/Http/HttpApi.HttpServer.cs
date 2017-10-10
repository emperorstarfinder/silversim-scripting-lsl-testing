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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Net.Sockets;

namespace SilverSim.Scripting.Lsl.Api.Http
{
    public partial class HttpApi
    {
        [APILevel(APIFlags.LSL, "llGetFreeURLs")]
        public int GetFreeURLs(ScriptInstance instance)
        {
            lock(instance)
            {
                int freeurls = m_HTTPHandler.FreeUrls;
                if(freeurls < 0)
                {
                    freeurls = 0;
                }

                return freeurls;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestURL")]
        public LSLKey RequestURL(ScriptInstance instance)
        {
            lock(instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    string urlID = m_HTTPHandler.RequestURL(instance.Part, instance.Item);
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_GRANTED,
                        Body = urlID
                    });
                }
                catch
                {
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_DENIED,
                        Body = string.Empty
                    });
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.OSSL, "osRequestURL")]
        public LSLKey RequestURL(ScriptInstance instance, AnArray options)
        {
            bool allowXss = false;
            foreach(IValue iv in options)
            {
                allowXss = iv.ToString() == "allowXss";
            }

            lock (instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    string urlID = m_HTTPHandler.RequestURL(instance.Part, instance.Item, allowXss);
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_GRANTED,
                        Body = urlID
                    });
                }
                catch
                {
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_DENIED,
                        Body = string.Empty
                    });
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.ASSL, "asRequestURL")]
        public LSLKey RequestURL(ScriptInstance instance, string itemname)
        {
            lock (instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    string urlID = m_HTTPHandler.RequestURL(instance.Part, instance.Item, itemname);
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_GRANTED,
                        Body = urlID
                    });
                }
                catch
                {
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_DENIED,
                        Body = string.Empty
                    });
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.LSL, "llReleaseURL")]
        public void ReleaseURL(ScriptInstance instance, string url)
        {
            lock (instance)
            {
                m_HTTPHandler.ReleaseURL(url);
            }
        }

        [APILevel(APIFlags.LSL, "llRequestSecureURL")]
        public LSLKey RequestSecureURL(ScriptInstance instance)
        {
            lock (instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    string urlID = m_HTTPHandler.RequestSecureURL(instance.Part, instance.Item);
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_GRANTED,
                        Body = urlID
                    });
                }
                catch
                {
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_DENIED,
                        Body = string.Empty
                    });
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.OSSL, "osRequestSecureURL")]
        public LSLKey RequestSecureURL(ScriptInstance instance, AnArray options)
        {
            bool allowXss = false;
            foreach (IValue iv in options)
            {
                allowXss = iv.ToString() == "allowXss";
            }

            lock (instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    string urlID = m_HTTPHandler.RequestSecureURL(instance.Part, instance.Item, allowXss);
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_GRANTED,
                        Body = urlID
                    });
                }
                catch
                {
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_DENIED,
                        Body = string.Empty
                    });
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.ASSL, "asRequestSecureURL")]
        public LSLKey RequestSecureURL(ScriptInstance instance, string itemname)
        {
            lock (instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    string urlID = m_HTTPHandler.RequestSecureURL(instance.Part, instance.Item, itemname);
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_GRANTED,
                        Body = urlID
                    });
                }
                catch
                {
                    instance.PostEvent(new HttpRequestEvent
                    {
                        RequestID = reqID,
                        Method = URL_REQUEST_DENIED,
                        Body = string.Empty
                    });
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.LSL, "llGetHTTPHeader")]
        public string GetHTTPHeader(ScriptInstance instance, LSLKey requestID, string header)
        {
            lock (instance)
            {
                return m_HTTPHandler.GetHttpHeader(requestID, header);
            }
        }

        [APILevel(APIFlags.LSL, "llHTTPResponse")]
        public void HTTPResponse(ScriptInstance instance, LSLKey requestID, int status, string body)
        {
            lock(instance)
            {
                try
                {
                    m_HTTPHandler.HttpResponse(requestID, status, body);
                }
                catch(SocketException)
                {
                    /* ignore this one */
                }
                catch (HttpResponse.ConnectionCloseException)
                {
                    /* ignore this one */
                }
                catch
#if DEBUG
                (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Exception in llHTTPResponse", e);
#endif
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_TEXT = 0;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_HTML = 1;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_XML = 2;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_XHTML = 3;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_ATOM = 4;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_JSON = 5;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_LLSD = 6;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_FORM = 7;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_RSS = 8;

        [APILevel(APIFlags.LSL, "llSetContentType")]
        public void SetContentType(ScriptInstance instance, LSLKey requestID, int contenttype)
        {
            lock(instance)
            {
                switch(contenttype)
                {
                    default:
                        m_HTTPHandler.SetContentType(requestID, "text/plain");
                        break;
                    case CONTENT_TYPE_HTML:
                        m_HTTPHandler.SetContentType(requestID, "text/html");
                        break;
                    case CONTENT_TYPE_XML:
                        m_HTTPHandler.SetContentType(requestID, "application/xml");
                        break;
                    case CONTENT_TYPE_XHTML:
                        m_HTTPHandler.SetContentType(requestID, "application/xhtml+xml");
                        break;
                    case CONTENT_TYPE_ATOM:
                        m_HTTPHandler.SetContentType(requestID, "application/atom+xml");
                        break;
                    case CONTENT_TYPE_JSON:
                        m_HTTPHandler.SetContentType(requestID, "application/json");
                        break;
                    case CONTENT_TYPE_LLSD:
                        m_HTTPHandler.SetContentType(requestID, "application/llsd+xml");
                        break;
                    case CONTENT_TYPE_FORM:
                        m_HTTPHandler.SetContentType(requestID, "application/x-www-form-urlencoded");
                        break;
                    case CONTENT_TYPE_RSS:
                        m_HTTPHandler.SetContentType(requestID, "application/rss+xml ");
                        break;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osSetContentType")]
        public void SetContentType(ScriptInstance instance, LSLKey id, string type)
        {
            lock(instance)
            {
                m_HTTPHandler.SetContentType(id, type);
            }
        }
    }
}
