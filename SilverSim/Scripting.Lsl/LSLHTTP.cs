// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.Scripting.Lsl
{
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    public sealed class LSLHTTP : IPlugin, IPluginShutdown
    {
        BaseHttpServer m_HttpServer;
        BaseHttpServer m_HttpsServer;
        readonly Timer m_HttpTimer;
        int m_TotalUrls = 15000;

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

            public HttpRequestData(HttpRequest req, UUID urlID)
            {
                Request = req;
                ContentType = "text/plain";
                ValidUntil = DateTime.UtcNow + TimeSpan.FromSeconds(25);
                UrlID = urlID;
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

            public URLData(UUID sceneID, UUID primID, UUID itemID, bool isSSL)
            {
                SceneID = sceneID;
                PrimID = primID;
                ItemID = itemID;
                IsSSL = isSSL;
            }
        }
        readonly RwLockedDictionary<UUID, URLData> m_UrlMap = new RwLockedDictionary<UUID, URLData>();

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
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/lslhttp/", LSLHttpRequestHandler);
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
            string[] parts = req.RawUrl.Substring(1).Split(new char[] {'/'}, 3);
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

            if(!UUID.TryParse(parts[1], out id))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if(!m_UrlMap.TryGetValue(id, out urlData))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            BaseHttpServer httpServer;
            if (parts[0] == "lslhttps")
            {
                if(!urlData.IsSSL)
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
            string pathinfo = req.RawUrl.Substring(45);
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

            UUID reqid = UUID.Random;
            HttpRequestData data = new HttpRequestData(req, id);

            string body = string.Empty;
            string method = data.Request.Method;
            if (method != "GET" && method != "DELETE")
            {
                int length;
                if(!int.TryParse(data.Request["Content-Length"], out length))
                {
                    req.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                    return;
                }
                byte[] buf = new byte[length];
                data.Request.Body.Read(buf, 0, length);
                body = buf.FromUTF8String();
            }

            try
            {
                m_HttpRequests.Add(reqid, data);
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.InternalServerError, "script access error");
                return;
            }

            HttpRequestEvent ev = new HttpRequestEvent();
            ev.RequestID = reqid;
            ev.Body = body;
            ev.Method = data.Request.Method;

            try
            {
                SceneInterface scene = SceneManager.Scenes[urlData.SceneID];
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
                byte[] b = body.ToUTF8String();
                HttpStatusCode httpStatus = (HttpStatusCode)status;
                reqdata.Request.SetConnectionClose();
                using (HttpResponse res = reqdata.Request.BeginResponse(httpStatus, httpStatus.ToString(), reqdata.ContentType))
                {
                    using (Stream s = res.GetOutputStream(b.LongLength))
                    {
                        s.Write(b, 0, b.Length);
                    }
                }
            }
        }

        readonly object m_ReqUrlLock = new object();

        public string RequestURL(ObjectPart part, ObjectPartInventoryItem item)
        {
            UUID newid;
            lock(m_ReqUrlLock)
            {
                if (m_UrlMap.Count >= m_TotalUrls)
                {
                    throw new InvalidOperationException("Too many URLs");
                }
                newid = UUID.Random;
                m_UrlMap.Add(newid, new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID, false));
            }
            return m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/lslhttp/" + newid.ToString();
        }

        public string RequestSecureURL(ObjectPart part, ObjectPartInventoryItem item)
        {
            if(null == m_HttpsServer)
            {
                throw new InvalidOperationException("No HTTPS support");
            }
            UUID newid;
            lock(m_ReqUrlLock)
            {
                if (m_UrlMap.Count >= m_TotalUrls)
                {
                    throw new InvalidOperationException("Too many URLs");
                }
                newid = UUID.Random;
                m_UrlMap.Add(newid, new URLData(part.ObjectGroup.Scene.ID, part.ID, item.ID, false));
            }
            return m_HttpsServer.Scheme + "://" + m_HttpsServer.ExternalHostName + ":" + m_HttpsServer.Port.ToString() + "/lslhttps/" + newid.ToString();
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
            if(parts.Length < 2 || (parts[0] != "lslhttp" && parts[0] != "lslhttps"))
            {
                return;
            }
            
            UUID urlid;
            if(!UUID.TryParse(parts[1], out urlid))
            {
                return;
            }

            URLData urlData;
            if(m_UrlMap.TryGetValue(urlid, out urlData) &&
                ((!urlData.IsSSL && parts[0] != "lslhttp") ||
                (urlData.IsSSL && parts[0] != "lslhttps")))
            {
                return;
            }

            if(m_UrlMap.Remove(urlid))
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
    }
}
