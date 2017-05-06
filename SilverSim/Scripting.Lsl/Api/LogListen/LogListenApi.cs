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

using log4net.Core;
using SilverSim.Main.Common;
using SilverSim.Main.Common.Log;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.LogListen
{
    [ScriptApiName("LogListen")]
    [LSLImplementation]
    [Description("ASSL LogListen API")]
    public class LogListenApi : IScriptApi, IPlugin, IPluginShutdown
    {
        public class LogListenInfo
        {
            public UUID SceneID;
            public UUID PartID;
            public UUID ItemID;
            public int Channel;

            public LogListenInfo(UUID sceneID, UUID partID, UUID itemID, int channel)
            {
                SceneID = sceneID;
                PartID = partID;
                ItemID = itemID;
                Channel = channel;
            }
        }
        bool m_ShutdownHandlerThread;
        RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, LogListenInfo>> m_Listeners = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, LogListenInfo>>(delegate () { return new RwLockedDictionary<UUID, LogListenInfo>(); });
        SceneList m_Scenes;

        public LogListenApi()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionRemove += OnSceneRemove;
            LogController.Queues.Add(m_LogEventQueue);
            ThreadManager.CreateThread(LogThread).Start();
        }

        public void Shutdown()
        {
            LogController.Queues.Remove(m_LogEventQueue);
            m_Scenes.OnRegionRemove -= OnSceneRemove;
            m_ShutdownHandlerThread = true;
        }

        void OnSceneRemove(SceneInterface scene)
        {
            m_Listeners.Remove(scene.ID);
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutDatabase;
            }
        }

        readonly BlockingQueue<LoggingEvent> m_LogEventQueue = new BlockingQueue<LoggingEvent>();

        [APIExtension(APIExtension.Admin, "asLogListen")]
        public void LogListen(ScriptInstance instance, int onChannel, int enable)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectPartInventoryItem item = instance.Item;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                if (scene.IsSimConsoleAllowed(instance.Part.Owner) && enable != 0)
                {
                    m_Listeners[scene.ID][item.ID] = new LogListenInfo(scene.ID, part.ID, item.ID, onChannel);
                }
                else
                {
                    m_Listeners[scene.ID].Remove(item.ID);
                }
            }
        }

        void LogThread()
        {
            Thread.CurrentThread.Name = "asLogListen Thread";
            while (!m_ShutdownHandlerThread)
            {
                LoggingEvent logevent;
                try
                {
                    logevent = m_LogEventQueue.Dequeue(500);
                }
                catch
                {
                    continue;
                }

                string msg = string.Format("{0} - [{1}]: {2}",
                    logevent.TimeStamp.ToString(),
                    logevent.LoggerName,
                    logevent.RenderedMessage.ToString());
                foreach(RwLockedDictionary<UUID, LogListenInfo> listeners in m_Listeners.Values)
                {
                    foreach(KeyValuePair<UUID, LogListenInfo> kvp in listeners)
                    {
                        LogListenInfo li = kvp.Value;
                        SceneInterface scene;
                        ObjectPart part;
                        ObjectPartInventoryItem item;
                        if(m_Scenes.TryGetValue(li.SceneID, out scene) &&
                            scene.Primitives.TryGetValue(li.PartID, out part) &&
                            part.Inventory.TryGetValue(li.ItemID, out item))
                        {
                            ScriptInstance instance = item.ScriptInstance;
                            if(instance != null)
                            {
                                ListenEvent ev = new ListenEvent();
                                ev.Channel = li.Channel;
                                ev.ID = UUID.Zero;
                                ev.Name = "System Log";
                                ev.Message = msg;
                                ev.SourceType = ListenEvent.ChatSourceType.System;
                                instance.PostEvent(ev);
                            }
                            else
                            {
                                listeners.Remove(kvp.Key);
                            }
                        }
                    }
                }
            }
        }
    }
}
