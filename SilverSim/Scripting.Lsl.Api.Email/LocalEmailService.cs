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

#pragma warning disable RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    [Description("Local Email Service")]
    [PluginName("LSL_Email_LocalOnlyService")]
    public sealed class LocalEmailService : IEmailService, IPlugin, IPluginShutdown, ISceneListener
    {
        private class Email
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

        private sealed class EmailQueue
        {
            private readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();
            private readonly List<Email> m_Emails = new List<Email>();

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
                        if((address?.Length == 0 || e.Address == address) &&
                            (subject?.Length == 0 || e.Subject == subject))
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

        private readonly string m_LocalDomain = "lsl.local";

        private SceneList m_Scenes;
        private readonly RwLockedDictionary<UUID, RwLockedDictionaryAutoAdd<UUID, EmailQueue>> m_Queues = new RwLockedDictionary<UUID, RwLockedDictionaryAutoAdd<UUID, EmailQueue>>();

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

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutDatabase;

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

        private void OnRegionAdd(SceneInterface scene)
        {
            m_Queues.Add(scene.ID, new RwLockedDictionaryAutoAdd<UUID, EmailQueue>(() => new EmailQueue()));
            scene.SceneListeners.Add(this);
        }

        private void OnRegionRemove(SceneInterface scene)
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
                ev = new EmailEvent()
                {
                    Message = em.Message,
                    Subject = em.Subject,
                    Time = em.Time.AsULong.ToString(),
                    NumberLeft = remaining,
                    Address = em.Address
                };
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

            if(part == null)
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
