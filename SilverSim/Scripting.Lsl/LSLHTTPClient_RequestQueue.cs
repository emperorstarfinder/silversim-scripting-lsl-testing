// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Web;

namespace SilverSim.Scripting.Lsl
{
    [Description("LSL HTTP Client Support")]
    [ServerParam("LSL.HTTPClient.WhiteList", Description = "List of URLs split by ;")]
    [ServerParam("LSL.HTTPClient.BlackList", Description = "List of URLs split by ;")]
    [ServerParam("LSL.HTTPClient.WhiteListOnly", ParameterType = typeof(bool))]
    public class LSLHTTPClient_RequestQueue : IPlugin, IPluginShutdown, IServerParamListener
    {
        public class LSLHttpRequest
        {
            public UUID RequestID = UUID.Random;
            public UUID SceneID;
            public UUID PrimID;
            public UUID ItemID;
            public string Url;
            public string Method = "GET";
            public string MimeType = "text/plain;charset=utf-8";
            public bool VerifyCert = true;
            public bool VerboseThrottle = true;
            public bool SendPragmaNoCache = true;
            public int MaxBodyLength = 2048;
            public string RequestBody = string.Empty;
            public Dictionary<string, string> Headers = new Dictionary<string, string>();

            public LSLHttpRequest()
            {
                Headers.Add("User-Agent", string.Format("{0} {1}", VersionInfo.ProductName, VersionInfo.Version));
                Headers.Add("X-SecondLife-Shard", VersionInfo.Shard);
            }
        }

        readonly RwLockedDictionary<UUID, BlockingQueue<LSLHttpRequest>> m_RequestQueues = new RwLockedDictionary<UUID, BlockingQueue<LSLHttpRequest>>();
        readonly SceneList m_Scenes;
        readonly RwLockedDictionary<UUID, string[]> m_BlackLists = new RwLockedDictionary<UUID, string[]>();
        readonly RwLockedDictionary<UUID, string[]> m_WhiteLists = new RwLockedDictionary<UUID, string[]>();
        readonly RwLockedDictionary<UUID, bool> m_WhiteListOnly = new RwLockedDictionary<UUID, bool>();

        [ServerParam("LSL.HTTPClient.WhiteListOnly", ParameterType = typeof(bool))]
        public void HandleWhiteListOnlyUpdated(UUID regionId, string value)
        {
            bool val;
            if (string.IsNullOrEmpty(value))
            {
                m_WhiteListOnly.Remove(regionId);
            }
            else
            {
                if (!bool.TryParse(value, out val))
                {
                    val = false;
                }
                m_WhiteListOnly[regionId] = val;
            }
        }

        [ServerParam("LSL.HTTPClient.WhiteList", Description = "List of URLs split by ;")]
        public void HandleWhiteListUpdated(UUID regionId, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                m_WhiteLists.Remove(regionId);
            }
            else
            {
                m_WhiteLists.Add(regionId, value.Split(';'));
            }
        }

        [ServerParam("LSL.HTTPClient.BlackList", Description = "List of URLs split by ;")]
        public void HandleBlackListUpdated(UUID regionId, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                m_BlackLists.Remove(regionId);
            }
            else
            {
                m_BlackLists.Add(regionId, value.Split(';'));
            }
        }

        bool IsURLAllowed(UUID regionId,  string url)
        {
            string[] blackList;
            if(m_BlackLists.TryGetValue(regionId, out blackList) ||
                m_BlackLists.TryGetValue(UUID.Zero, out blackList))
            {
                foreach(string u in blackList)
                {
                    if(url.StartsWith(u))
                    {
                        return false;
                    }
                }
            }

            string[] whiteList;
            if (m_BlackLists.TryGetValue(regionId, out whiteList) ||
                m_BlackLists.TryGetValue(UUID.Zero, out whiteList))
            {
                foreach (string u in blackList)
                {
                    if (url.StartsWith(u))
                    {
                        return true;
                    }
                }
            }

            /* check for WhiteList only */
            bool wOnly;
            if(!(m_WhiteListOnly.TryGetValue(regionId, out wOnly) ||
                m_WhiteListOnly.TryGetValue(UUID.Zero, out wOnly)))
            {
                wOnly = true;
            }
            return wOnly;
        }

        public LSLHTTPClient_RequestQueue(SceneList scenes)
        {
            m_Scenes = scenes;
            scenes.OnRegionAdd += RegionAdded;
            scenes.OnRegionRemove += RegionRemoved;
        }

        void RegionRemoved(SceneInterface scene)
        {
            m_RequestQueues.Remove(scene.ID);
        }

        void RegionAdded(SceneInterface scene)
        {
            int i;
            try
            {
                m_RequestQueues.Add(scene.ID, new BlockingQueue<LSLHttpRequest>());
            }
            catch
            {
                /* exception intentionally ignored should ever a queue already be added */
            }
            for(i = 0; i < 10; ++i)
            {
                Thread t = ThreadManager.CreateThread(ProcessThread);
                t.Name = "LSL:HTTPClient Processor for region " + scene.ID.ToString();
                t.Start(scene.ID);
            }
        }

        internal bool Enqueue(LSLHttpRequest req)
        {
            BlockingQueue<LSLHttpRequest> queue;
            if(m_RequestQueues.TryGetValue(req.SceneID, out queue))
            {
                queue.Enqueue(req);
                return true;
            }
            return false;
        }

        void ProcessThread(object o)
        {
            UUID id = (UUID)o;
            LSLHttpRequest req;
            for(;;)
            {
                BlockingQueue<LSLHttpRequest> reqqueue;

                if(!m_RequestQueues.TryGetValue(id, out reqqueue))
                {
                    /* terminate condition is deletion of the request queue */
                    break;
                }

                try
                {
                    req = reqqueue.Dequeue(1000);
                }
                catch(TimeoutException)
                {
                    continue;
                }

                HttpResponseEvent ev = new HttpResponseEvent();
                ev.RequestID = req.RequestID;
                if (IsURLAllowed(req.SceneID, req.Url))
                {
                    try
                    {
                        ev.Body = HttpClient.DoRequest(req.Method, req.Url, null, req.MimeType, req.RequestBody, false, 30000);
                        ev.Status = (int)HttpStatusCode.OK;
                    }
                    catch (HttpClient.BadHttpResponseException)
                    {
                        ev.Status = 499;
                    }
                    catch (HttpException e)
                    {
                        ev.Body = e.Message;
                        ev.Status = e.GetHttpCode();
                    }
                    catch
                    {
                        HttpResponseEvent e = new HttpResponseEvent();
                        e.Status = 499;
                    }
                }
                else
                {
                    HttpResponseEvent e = new HttpResponseEvent();
                    e.Status = 499;
                }

                SceneInterface scene;
                if(!m_Scenes.TryGetValue(req.SceneID, out scene))
                {
                    continue;
                }

                ObjectPart part;
                try
                {
                    part = scene.Primitives[req.PrimID];
                }
                catch
                {
                    continue;
                }

                part.PostEvent(ev);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
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
            m_RequestQueues.Clear();
        }
    }
}
