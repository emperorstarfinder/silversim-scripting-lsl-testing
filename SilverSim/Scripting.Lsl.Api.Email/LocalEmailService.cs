// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SilverSim.Types;
using System.ComponentModel;
using SilverSim.Threading;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Object;
using System.Threading;
using SilverSim.Scene.Types.Script.Events;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    [Description("Local Email Service")]
    sealed class LocalEmailService : IEmailService, IPlugin, IPluginShutdown, ISceneListener
    {
        class Email
        {
            public Date Time = Date.Now;
            public string Address;
            public string Subject;
            public string Message;

            public Email(string address, string subject, string message)
            {
                Address = address;
                Subject = subject;
                Message = message;
            }
        }

        sealed class EmailQueue
        {
            readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();
            readonly List<Email> m_Emails = new List<Email>();

            public EmailQueue()
            {

            }

            public void Put(Email em)
            {
                m_RwLock.AcquireWriterLock(-1);
                try
                {
                    m_Emails.Add(em);
                }
                finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }

            public bool TryGetValue(string address, string subject, out Email em, out int remaining)
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    int c = m_Emails.Count;
                    for(int i = 0; i < c; ++i)
                    {
                        Email e = m_Emails[i];
                        if((string.IsNullOrEmpty(address) || e.Address == address) &&
                            (string.IsNullOrEmpty(subject) || e.Subject == subject))
                        {
                            em = e;
                            LockCookie lc = m_RwLock.UpgradeToWriterLock(-1);
                            try
                            {
                                m_Emails.RemoveAt(i);
                            }
                            finally
                            {
                                m_RwLock.DowngradeFromWriterLock(ref lc);
                            }
                            remaining = c - 1;
                            return true;
                        }
                    }
                    em = null;
                    remaining = c;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
                return false;
            }
        }

        string m_LocalDomain = "lsl.local";

        SceneList m_Scenes;
        readonly RwLockedDictionary<UUID, RwLockedDictionaryAutoAdd<UUID, EmailQueue>> m_Queues = new RwLockedDictionary<UUID, RwLockedDictionaryAutoAdd<UUID, EmailQueue>>();

        public LocalEmailService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionAdd += OnRegionAdd;
            m_Scenes.OnRegionRemove += OnRegionRemove;
        }

        public void Shutdown()
        {
            m_Scenes.OnRegionAdd -= OnRegionAdd;
            m_Scenes.OnRegionRemove -= OnRegionRemove;
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutDatabase;
            }
        }

        void ISceneListener.ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            if(info.IsKilled)
            {
                RwLockedDictionaryAutoAdd<UUID, EmailQueue> qList;
                if(m_Queues.TryGetValue(fromSceneID, out qList))
                {
                    qList.Remove(info.Part.ID);
                }
            }
        }

        void OnRegionAdd(SceneInterface scene)
        {
            m_Queues.Add(scene.ID, new RwLockedDictionaryAutoAdd<UUID, EmailQueue>(delegate () { return new EmailQueue(); }));
            scene.SceneListeners.Add(this);
        }

        void OnRegionRemove(SceneInterface scene)
        {
            scene.SceneListeners.Remove(this);
            m_Queues.Remove(scene.ID);
        }

        public void RequestNext(UUID sceneID, UUID objectID, string sender, string subject)
        {
            RwLockedDictionaryAutoAdd<UUID, EmailQueue> qList;
            EmailQueue eQueue;
            Email em;
            int remaining;
            EmailEvent ev;
            if (m_Queues.TryGetValue(sceneID, out qList) &&
                qList.TryGetValue(objectID, out eQueue) &&
                eQueue.TryGetValue(sender, subject, out em, out remaining))
            {
                ev = new EmailEvent();
                ev.Message = em.Message;
                ev.Subject = em.Subject;
                ev.Time = em.Time.AsULong.ToString();
                ev.NumberLeft = remaining;
                ev.Address = em.Address;
            }
            else
            {
                return;
            }

            SceneInterface scene;
            ObjectPart part;

            if(m_Scenes.TryGetValue(sceneID, out scene) &&
                scene.Primitives.TryGetValue(objectID, out part))
            {
                part.PostEvent(ev);
            }
            else
            {
                qList.Remove(objectID);
            }
        }

        public void Send(UUID fromSceneID, UUID fromObjectID, string toaddress, string subject, string body)
        {
            string[] parts = toaddress.Split(new char[] { '@' }, 2);
            if(parts.Length < 2)
            {
                return;
            }

            if (parts[1].ToLowerInvariant() != m_LocalDomain)
            {
                return;
            }

            UUID primid;

            if (!UUID.TryParse(parts[0], out primid))
            {
                return;
            }

            ObjectPart part = null;
            UUID sceneID = UUID.Zero;

            foreach(SceneInterface scene in m_Scenes.Values)
            {
                if(scene.Primitives.TryGetValue(primid, out part))
                {
                    sceneID = scene.ID;
                    break;
                }
            }

            if(null == part)
            {
                return;
            }

            RwLockedDictionaryAutoAdd<UUID, EmailQueue> qList;

            if(m_Queues.TryGetValue(sceneID, out qList))
            {
                qList[primid].Put(new Email(fromObjectID + "@lsl.local", subject, body));
            }
        }
    }
}
