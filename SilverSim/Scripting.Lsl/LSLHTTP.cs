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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Timers;

namespace SilverSim.Scripting.Lsl
{
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    [Description("LSL HTTP Server Support")]
    [ServerParam("LSL.MaxURLs", ParameterType = typeof(uint), DefaultValue = 15000)]
    public sealed class LSLHTTP : IPlugin, IPluginShutdown, IServerParamListener
    {
        BaseHttpServer m_HttpServer;
        BaseHttpServer m_HttpsServer;
        readonly Timer m_HttpTimer;
        int m_TotalUrls = 15000;
        SceneList m_Scenes;

        [ServerParam("LSL.MaxURLs")]
        public void MaxURLsUpdated(UUID regionID, string value)
        {
            int intval;
            if (value.Length == 0)
            {
                m_TotalUrls = 15000;
            }
            else if (int.TryParse(value, out intval))
            {
                m_TotalUrls = intval;
            }
        }

        public int TotalUrls
        {
            get
            {
                return m_TotalUrls;
            }
            set
            {
                if(value > 0)
                {
                    m_TotalUrls = value;
                }
            }
        }


        struct HttpRequestData
        {
            public DateTime ValidUntil;
            public string ContentType;
            public HttpRequest Request;
            public UUID UrlID;
            public string UrlName;
            public bool AllowXss;

            public HttpRequestData(HttpRequest req, UUID urlID, bool allowXss)
            {
                AllowXss = allowXss;
                Request = req;
                ContentType = "text/plain";
                ValidUntil = DateTime.UtcNow + TimeSpan.FromSeconds(25);
                UrlID = urlID;
                UrlName = string.Empty;
            }
            public HttpRequestData(HttpRequest req, string urlname, bool allowXss)
            {
                AllowXss = allowXss;
                Request = req;
                ContentType = "text/plain";
                ValidUntil = DateTime.UtcNow + TimeSpan.FromSeconds(25);
                UrlID = UUID.Zero;
                UrlName = urlname;
            }
        }

        readonly RwLockedDictionary<UUID, HttpRequestData> m_HttpRequests = new RwLockedDictionary<UUID, HttpRequestData>();

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
        struct URLData
        {
            public UUID SceneID;
            public UUID PrimID;
            public UUID ItemID;
            public bool IsSSL;
            public bool AllowXss;

            public URLData(UUID sceneID, UUID primID, UUID itemID, bool isSSL, bool allowXss)
            {
                SceneID = sceneID;
                PrimID = primID;
                ItemID = itemID;
                IsSSL = isSSL;
                AllowXss = allowXss;
            }
        }
        readonly RwLockedDictionary<UUID, URLData> m_UrlMap = new RwLockedDictionary<UUID, URLData>();
        readonly RwLockedDictionary<string, URLData> m_NamedUrlMap = new RwLockedDictionary<string, URLData>();

        public LSLHTTP()
        {
            m_HttpTimer = new Timer(1000);
            m_HttpTimer.Elapsed += TimerEvent;
            m_HttpTimer.Start();
        }

        void TimerEvent(object sender, ElapsedEventArgs e)
        {
            List<UUID> RemoveList = new List<UUID>();
            foreach(KeyValuePair<UUID, HttpRequestData> kvp in m_HttpRequests)
            {
                if(kvp.Value.ValidUntil < DateTime.UtcNow)
                {
                    RemoveList.Add(kvp.Key);
                }
            }

            HttpRequestData reqdata;
            foreach(UUID id in RemoveList)
            {
                if(m_HttpRequests.Remove(id, out reqdata))
                {
                    reqdata.Request.SetConnectionClose();
                    reqdata.Request.ErrorResponse(HttpStatusCode.InternalServerError, "Script timeout");
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/lslhttp/", LSLHttpRequestHandler);
            m_HttpServer.StartsWithUriHandlers.Add("/lslhttp-named/", LSLNamedHttpRequestHandler);
            try
            {
                m_HttpsServer = loader.HttpsServer;
            }
            catch(ConfigurationLoader.ServiceNotFoundException)
            {
                m_HttpsServer = null;
            }

            if(null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Add("/lslhttps/", LSLHttpRequestHandler);
                m_HttpsServer.StartsWithUriHandlers.Add("/lslhttps-named/", LSLNamedHttpRequestHandler);
            }

            IConfig lslConfig = loader.Config.Configs["LSL"];
            if(null != lslConfig)
            {
                m_TotalUrls = lslConfig.GetInt("MaxUrlsPerSimulator", 15000);
            }
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_HttpTimer.Stop();
            if(null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Remove("/lslhttps/");
            }
            m_HttpServer.StartsWithUriHandlers.Remove("/lslhttp/");

            HttpRequestData reqdata;
            foreach (UUID id in m_HttpRequests.Keys)
            {
                if (m_HttpRequests.Remove(id, out reqdata))
                {
                    reqdata.Request.SetConnectionClose();
                    reqdata.Request.ErrorResponse(HttpStatusCode.InternalServerError, "Script shutdown");
                }
            }

        }

        public int FreeUrls
        {
            get
            {
                if(m_TotalUrls < UsedUrls)
                {
                    return 0;
                }
                return m_TotalUrls - UsedUrls;
            }
        }
        public int UsedUrls
        {
            get
            {
                return m_UrlMap.Count;
            }
        }

        public void LSLHttpRequestHandler(HttpRequest req)
        {
            string[] parts = req.RawUrl.Substring(1).Split(new char[] { '/' }, 3);
            UUID id;
            URLData urlData;
            if (req.Method != "GET" && req.Method != "POST" && req.Method != "PUT" && req.Method != "DELETE")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            if (parts.Length < 2)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (!UUID.TryParse(parts[1], out id))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (!m_UrlMap.TryGetValue(id, out urlData))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            BaseHttpServer httpServer;
            if (parts[0] == "lslhttps")
            {
                if (!urlData.IsSSL)
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                httpServer = m_HttpsServer;
                req["x-script-url"] = httpServer.Scheme + "://" + httpServer.ExternalHostName + httpServer.Port.ToString() + "/lslhttps/" + id.ToString();
            }
            else
            {
                if (urlData.IsSSL)
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                httpServer = m_HttpServer;
                req["x-script-url"] = httpServer.Scheme + "://" + httpServer.ExternalHostName + httpServer.Port.ToString() + "/lslhttp/" + id.ToString();
            }
            string pathinfo = string.Empty;
            if (parts.Length > 2)
            {
                pathinfo = "/" + parts[2];
            }
            int pos = pathinfo.IndexOf('?');
            if (pos >= 0)
            {
                req["x-path-info"] = pathinfo.Substring(0, pos);
                req["x-query-string"] = req.RawUrl.Substring(pos + 1);
            }
            else
            {
                req["x-path-info"] = pathinfo;
            }
            req["x-remote-ip"] = req.CallerIP;

            HttpRequestData data = new HttpRequestData(req, id, urlData.AllowXss);
            LSLHttpRequestHandlerCommon(urlData, data);
        }

        public void LSLNamedHttpRequestHandler(HttpRequest req)
        {
            string[] parts = req.RawUrl.Substring(1).Split(new char[] { '/' }, 3);
            URLData urlData;
            if (req.Method != "GET" && req.Method != "POST" && req.Method != "PUT" && req.Method != "DELETE")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            if (parts.Length < 2)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (!IsValidNamedURL(parts[1]))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (!m_NamedUrlMap.TryGetValue(parts[1], out urlData))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            BaseHttpServer httpServer;
            if (parts[0] == "lslhttps-named")
            {
                if (!urlData.IsSSL)
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                httpServer = m_HttpsServer;
                req["x-script-url"] = httpServer.Scheme + "://" + httpServer.ExternalHostName + httpServer.Port.ToString() + "/lslhttps-named/" + parts[1];
            }
            else
            {
                if (urlData.IsSSL)
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                httpServer = m_HttpServer;
                req["x-script-url"] = httpServer.Scheme + "://" + httpServer.ExternalHostName + httpServer.Port.ToString() + "/lslhttp-named/" + parts[1];
            }
            string pathinfo = string.Empty;
            if(parts.Length > 2)
            {
                pathinfo = "/" + parts[2];
            }
            int pos = pathinfo.IndexOf('?');
            if (pos >= 0)
            {
                req["x-path-info"] = pathinfo.Substring(0, pos);
                req["x-query-string"] = req.RawUrl.Substring(pos + 1);
            }
            else
            {
                req["x-path-info"] = pathinfo;
            }
            req["x-remote-ip"] = req.CallerIP;

            HttpRequestData data = new HttpRequestData(req, parts[1], urlData.AllowXss);
            LSLHttpRequestHandlerCommon(urlData, data);
        }

        void LSLHttpRequestHandlerCommon(URLData urlData, HttpRequestData data)
        {
            UUID reqid = UUID.Random;
            string body = string.Empty;
            string method = data.Request.Method;
            if (method != "GET" && method != "DELETE")
            {
                int length;
                if(!int.TryParse(data.Request["Content-Length"], out length))
                {
                    data.Request.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                    return;
                }
                byte[] buf = new byte[length];
                data.Request.Body.Read(buf, 0, length);
                body = buf.FromUTF8Bytes();
            }

            try
            {
                m_HttpRequests.Add(reqid, data);
            }
            catch
            {
                data.Request.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                return;
            }

            HttpRequestEvent ev = new HttpRequestEvent();
            ev.RequestID = reqid;
            ev.Body = body;
            ev.Method = data.Request.Method;

            try
            {
                SceneInterface scene = m_Scenes[urlData.SceneID];
                ObjectPart part = scene.Primitives[urlData.ItemID];
                ObjectPartInventoryItem item = part.Inventory[urlData.ItemID];
                ScriptInstance instance = item.ScriptInstance;
                if (instance == null)
                {
                    throw new ArgumentException("item.ScriptInstance is null");
                }
                instance.PostEvent(ev);
            }
            catch
            {
                m_HttpRequests.Remove(reqid);
                data.Request.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                return;
            }
            throw new HttpResponse.DisconnectFromThreadException();
        }

        public string GetHttpHeader(UUID requestId, string header)
        {
            HttpRequestData reqdata;
            if (m_HttpRequests.TryGetValue(requestId, out reqdata) &&
                reqdata.Request.ContainsHeader(header))
            {
                return reqdata.Request[header];
            }
            return string.Empty;
        }

        public void SetContentType(UUID requestID, string contentType)
        {
            HttpRequestData reqdata;
            if(m_HttpRequests.TryGetValue(requestID, out reqdata))
            {
                reqdata.ContentType = contentType;
            }
        }

        public void HttpResponse(UUID requestID, int status, string body)
        {
            HttpRequestData reqdata;
            if(m_HttpRequests.Remove(requestID, out reqdata))
            {
                byte[] b = body.ToUTF8Bytes();
                HttpStatusCode httpStatus = (HttpStatusCode)status;
                reqdata.Request.SetConnectionClose();
                using (HttpResponse res = reqdata.Request.BeginResponse(httpStatus, httpStatus.ToString(), reqdata.ContentType))
                {
                    if (reqdata.AllowXss)
                    {
                        res.Headers.Add("Access-Control-Allow-Origin", "*");
                    }
                    using (Stream s = res.GetOutputStream(b.LongLength))
                    {
                        s.Write(b, 0, b.Length);
                    }
                }
            }
        }

        readonly object m_ReqUrlLock = new object();

        bool IsValidNamedURL(string name)
        {
            if(name.Length == 0)
            {
                return false;
            }

            foreach(char c in name)
            {
                if(!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        public string RequestURL(ObjectPart part, ObjectPartInventoryItem item, bool allowXss = false)
        {
            UUID newid;
            lock(m_ReqUrlLock)
            {
                if (m_UrlMap.Count + m_NamedUrlMap.Count >= m_TotalUrls)
                {
                    throw new InvalidOperationException("Too many URLs");
                }
                newid = UUID.Random;
                m_UrlMap.Add(newid, new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID, false, allowXss));
            }
            return m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/lslhttp/" + newid.ToString();
        }

        public string RequestURL(ObjectPart part, ObjectPartInventoryItem item, string name, bool allowXss = false)
        {
            if(!IsValidNamedURL(name))
            {
                throw new InvalidOperationException("Invalid name for predefined name");
            }
            lock(m_ReqUrlLock)
            {
                if (m_UrlMap.Count + m_NamedUrlMap.Count >= m_TotalUrls)
                {
                    throw new InvalidOperationException("Too many URLs");
                }
                m_NamedUrlMap[name] = new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID, false, allowXss);
            }
            return m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/lslhttp-named/" + name;
        }

        public string RequestSecureURL(ObjectPart part, ObjectPartInventoryItem item, bool allowXss = false)
        {
            if(null == m_HttpsServer)
            {
                throw new InvalidOperationException("No HTTPS support");
            }
            UUID newid;
            lock(m_ReqUrlLock)
            {
                if (m_UrlMap.Count + m_NamedUrlMap.Count >= m_TotalUrls)
                {
                    throw new InvalidOperationException("Too many URLs");
                }
                newid = UUID.Random;
                m_UrlMap.Add(newid, new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID, true, allowXss));
            }
            return m_HttpsServer.Scheme + "://" + m_HttpsServer.ExternalHostName + ":" + m_HttpsServer.Port.ToString() + "/lslhttps/" + newid.ToString();
        }

        public string RequestSecureURL(ObjectPart part, ObjectPartInventoryItem item, string name, bool allowXss = false)
        {
            if (!IsValidNamedURL(name))
            {
                throw new InvalidOperationException("Invalid name for predefined name");
            }
            if (null == m_HttpsServer)
            {
                throw new InvalidOperationException("No HTTPS support");
            }
            lock (m_ReqUrlLock)
            {
                if (m_UrlMap.Count + m_NamedUrlMap.Count >= m_TotalUrls)
                {
                    throw new InvalidOperationException("Too many URLs");
                }
                m_NamedUrlMap[name] = new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID, true, allowXss);
            }
            return m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/lslhttps-named/" + name;
        }

        public void ReleaseURL(string url)
        {
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                return;
            }

            string[] parts = uri.PathAndQuery.Substring(1).Split(new char[] { '/' }, 3);
            if (parts[0] == "lslhttp" || parts[0] == "lslhttps")
            {

                UUID urlid;
                if (!UUID.TryParse(parts[1], out urlid))
                {
                    return;
                }

                URLData urlData;
                if (m_UrlMap.TryGetValue(urlid, out urlData) &&
                    ((!urlData.IsSSL && parts[0] != "lslhttp") ||
                    (urlData.IsSSL && parts[0] != "lslhttps")))
                {
                    return;
                }

                if (m_UrlMap.Remove(urlid))
                {
                    List<UUID> RemoveList = new List<UUID>();
                    foreach (KeyValuePair<UUID, HttpRequestData> kvp in m_HttpRequests)
                    {
                        if (kvp.Value.UrlID == urlid)
                        {
                            RemoveList.Add(kvp.Key);
                        }
                    }

                    HttpRequestData reqdata;
                    foreach (UUID id in RemoveList)
                    {
                        if (m_HttpRequests.Remove(id, out reqdata))
                        {
                            reqdata.Request.SetConnectionClose();
                            reqdata.Request.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                        }
                    }
                }
            }
            else if(parts[0] == "lslhttp-named" || parts[0] == "lslhttps-named")
            {
                URLData urlData;
                if (m_NamedUrlMap.TryGetValue(parts[1], out urlData) &&
                    ((!urlData.IsSSL && parts[0] != "lslhttp-named") ||
                    (urlData.IsSSL && parts[0] != "lslhttps-named")))
                {
                    return;
                }

                if (m_NamedUrlMap.Remove(parts[1]))
                {
                    List<UUID> RemoveList = new List<UUID>();
                    foreach (KeyValuePair<UUID, HttpRequestData> kvp in m_HttpRequests)
                    {
                        if (kvp.Value.UrlName == parts[1])
                        {
                            RemoveList.Add(kvp.Key);
                        }
                    }

                    HttpRequestData reqdata;
                    foreach (UUID id in RemoveList)
                    {
                        if (m_HttpRequests.Remove(id, out reqdata))
                        {
                            reqdata.Request.SetConnectionClose();
                            reqdata.Request.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                        }
                    }
                }
            }
        }
    }
}
