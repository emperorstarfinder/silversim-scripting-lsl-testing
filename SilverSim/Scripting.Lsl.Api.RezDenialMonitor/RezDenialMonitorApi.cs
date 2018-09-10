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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.RezDenialMonitor
{
    [PluginName("LSL_RezDenialMonitor")]
    [Description("Rezzing denial monitor")]
    public class RezDenialMonitorApi : IScriptApi, IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("REZ DENIAL MONITOR");
        private SceneList m_Scenes;
        private readonly BlockingQueue<RezDeniedEvent> m_Queue = new BlockingQueue<RezDeniedEvent>();
        private Thread m_WorkerThread;

        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID /* itemid */, UUID /* object id */>> m_RegisteredScripts = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, UUID>>(() => new RwLockedDictionary<UUID, UUID>());

        [APIExtension("RezDenialMonitor")]
        public const int REZ_DENIAL_REASON_BLACKLISTED = 1;
        [APIExtension("RezDenialMonitor")]
        public const int REZ_DENIAL_REASON_PARCEL_NOT_ALLOWED = 2;
        [APIExtension("RezDenialMonitor")]
        public const int REZ_DENIAL_REASON_PARCEL_NOT_FOUND = 3;

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        [APIExtension("RezDenialMonitor", "rez_denied")]
        [StateEventDelegate]
        public delegate void RezDeniedDelegate(LSLKey rezzerID, LSLKey ownerID, LSLKey rezzingScriptAssetID, LSLKey rezzedObjectAssetID, int rezDenialReason, AnArray extraParams);

        [TranslatedScriptEvent("rez_denied")]
        private class RezDeniedEvent : IScriptEvent
        {
            public UUID SceneID;

            [TranslatedScriptEventParameter(0)]
            public LSLKey RezzerID;
            [TranslatedScriptEventParameter(1)]
            public LSLKey OwnerID;
            [TranslatedScriptEventParameter(2)]
            public LSLKey RezzingScriptAssetID;
            [TranslatedScriptEventParameter(3)]
            public LSLKey RezzedObjectAssetID;
            [TranslatedScriptEventParameter(4)]
            public int RezDenialReason;
            [TranslatedScriptEventParameter(5)]
            public AnArray ExtraParams = new AnArray();
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionAdd += RegionAdded;
            m_Scenes.OnRegionRemove += RegionRemoved;
            m_WorkerThread = ThreadManager.CreateThread(BackgroundThread);
            m_WorkerThread.Start();
        }

        private void RegionAdded(SceneInterface scene)
        {
            scene.OnRezzingDenied += RezzingDenied;
        }

        private void RegionRemoved(SceneInterface scene)
        {
            scene.OnRezzingDenied -= RezzingDenied;
            m_RegisteredScripts.Remove(scene.ID);
        }

        private void RezzingDenied(UUID sceneID, UGUI agentID, UUID rezzerID, RezDenialReason reason, UUID rezzingscriptassetid, UUID rezzedobjectassetid)
        {
            m_Queue.Enqueue(new RezDeniedEvent
            {
                SceneID = sceneID,
                RezzerID = rezzerID,
                OwnerID = agentID.ID,
                RezzingScriptAssetID = rezzingscriptassetid,
                RezzedObjectAssetID = rezzedobjectassetid,
                RezDenialReason = (int)reason
            });
        }

        private bool m_ShutdownThreads;

        private void BackgroundThread()
        {
            Thread.CurrentThread.Name = "Rez Denial Monitor Queue";
            m_Log.Info("Started");
            while (!m_ShutdownThreads)
            {
                RezDeniedEvent ev;
                try
                {
                    ev = m_Queue.Dequeue(1000);
                }
                catch(TimeoutException)
                {
                    continue;
                }

                SceneInterface scene;
                RwLockedDictionary<UUID, UUID> registeredScriptsInScene;
                if(!m_Scenes.TryGetValue(ev.SceneID, out scene))
                {
                    m_RegisteredScripts.Remove(ev.SceneID);
                }
                else if(m_RegisteredScripts.TryGetValue(ev.SceneID, out registeredScriptsInScene))
                {
                    List<UUID> keys = new List<UUID>(registeredScriptsInScene.Keys);
                    UUID partid;
                    ObjectPart part;
                    foreach(UUID scriptitemid in keys)
                    {
                        if(registeredScriptsInScene.TryGetValue(scriptitemid, out partid) &&
                            scene.Primitives.TryGetValue(partid, out part))
                        {
                            ObjectPartInventoryItem item;
                            if(part.Inventory.TryGetValue(scriptitemid, out item))
                            {
                                item.ScriptInstance?.PostEvent(ev);
                            }
                            else
                            {
                                registeredScriptsInScene.Remove(scriptitemid);
                            }
                        }
                    }
                }
            }
            m_Log.Info("Stopped");
        }

        public void Shutdown()
        {
            m_ShutdownThreads = true;
            if (m_WorkerThread != null)
            {
                if (!m_WorkerThread.Join(10000))
                {
                    m_WorkerThread.Abort();
                }
            }
        }
    }
}
