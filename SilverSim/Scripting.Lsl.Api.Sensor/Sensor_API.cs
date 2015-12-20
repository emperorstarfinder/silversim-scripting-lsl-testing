// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.Scripting.Lsl.Api.Sensor
{
    [ScriptApiName("Sensor")]
    [LSLImplementation]
    public class SensorApi : IScriptApi, IPlugin, IPluginShutdown
    {
        /* excerpt from http://wiki.secondlife.com/wiki/LlSensor

            * When searching for an avatar but not by name, it doesn't matter which AGENT flag is used.
            * Objects do not detect themselves, and attachments cannot detect their wearers (this includes HUD attachments).
            * Attachments cannot be detected by llSensor.
            * For an object to be detected, the center of its root prim (the same point it would report with llGetRootPosition) 
                must be within the sensor beam.
            * For an agent to be detected, a point near the pelvis must be inside the sensor beam (the same as llGetRootPosition would 
                report in a script attached to that avatar). This point is indicated by red crosshairs when Advanced>Character>Display Agent
                    Target is turned on.
                * If the agent is sitting on an object, the root prim of the sat upon object becomes a second sensor target for the agent 
                    (but not if the avatar is outside the sensor arc, see SVC-5145).
            * Sensors placed in the root prim of attachments will use the direction the avatar is facing as their forward vector. 
                In mouselook, this means that it will be wherever the avatar is looking, while out of mouselook, this means whichever 
                way the avatar is pointing. This does not include where the avatar's head is pointing, or what animation the avatar is 
                doing, just the direction the avatar would move in if you walked forward. This is the case, regardless of where the object
                is attached.
            * Sensors placed in prims other than the root prim of an attachment will have their forward direction offset relative to the 
                root prim's forward direction, e.g. a sensor in a prim whose +X direction is the reverse of the root +X will look backward.
            * llSensor does not detect objects or agents across region boundaries
            * If type is zero, the sensor will silently fail, neither sensor or no_sensor will be triggered.

         */

        public class SensorInfo
        {
            public readonly ScriptInstance Instance;
            public double Timeout;
            public double TimeoutToElapse;
            public bool IsRepeating;
            public UUID OwnObjectID;
            public UUID OwnerID;
            public bool IsAttached;
            public int SearchType;
            public string SearchName;
            public double SearchRadius;
            public double SearchArc;
            public Vector3 SensePoint;
            public Quaternion SenseRotation;
            public UUID SearchKey;

            public readonly RwLockedDictionary<UUID, DetectInfo> SensorHits = new RwLockedDictionary<UUID, DetectInfo>();

            public SensorInfo(ScriptInstance instance, bool isRepeating, double timeout, string sName, LSLKey sKey, int sType, double sRadius, double sArc)
            {
                Instance = instance;
                OwnerID = instance.Part.Owner.ID;
                IsAttached = instance.Part.ObjectGroup.IsAttached;
                Timeout = timeout;
                TimeoutToElapse = timeout;
                IsRepeating = isRepeating;
                SearchName = sName;
                SearchType = sType;
                SearchRadius = sRadius;
                SearchArc = sArc;
                SearchKey = (sKey.AsBoolean) ? sKey.AsUUID : UUID.Zero;
            }

            public void UpdateSenseLocation()
            {
                ObjectPart part = Instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                IsAttached = grp.IsAttached;
                IAgent agent;
                if (IsAttached && grp.Scene.RootAgents.TryGetValue(grp.Owner.ID, out agent))
                {
                    SensePoint = agent.GlobalPosition;
                    SenseRotation = agent.IsInMouselook ?
                        agent.CameraRotation : /* according to SL docs in Mouselook we have to use this rotation */
                        agent.BodyRotation;

                    SenseRotation *= part.LocalRotation;
                }
                else
                {
                    SensePoint = part.GlobalPosition;
                    SenseRotation = part.GlobalRotation;
                }
            }
        }

        public class SceneInfo : ISceneListener
        {
            private static readonly ILog m_Log = LogManager.GetLogger("LSL_SENSORS");

            public SceneInterface Scene;
            public readonly RwLockedDictionary<UUID, ObjectGroup> KnownObjects = new RwLockedDictionary<UUID, ObjectGroup>();
            public readonly RwLockedDictionary<ScriptInstance, SensorInfo> SensorRepeats = new RwLockedDictionary<ScriptInstance, SensorInfo>();
            public readonly System.Timers.Timer m_Timer = new System.Timers.Timer(1);
            public readonly object m_TimerLock = new object();
            /* when sensor repeats are active, these are the operating limits */
            int m_LastTickCount;
            const double MIN_SENSOR_INTERVAL = 0.2;
            const double MAX_SENSOR_INTERVAL = 3600;
            public Thread m_ObjectWorkerThread;
            public bool m_StopThread;
            readonly BlockingQueue<ObjectUpdateInfo> m_ObjectUpdates = new BlockingQueue<ObjectUpdateInfo>();

            public SceneInfo(SceneInterface scene)
            {
                Scene = scene;
                Scene.SceneListeners.Add(this);
                m_Timer.Elapsed += SensorRepeatTimer;
                m_ObjectWorkerThread = new Thread(SensorUpdateThread);
                m_ObjectWorkerThread.Start();
            }

            public void Stop()
            {
                m_StopThread = true;
                m_Timer.Stop();
                m_Timer.Elapsed -= SensorRepeatTimer;
                m_Timer.Dispose();
                Scene.SceneListeners.Remove(this);
                SensorRepeats.Clear();
                KnownObjects.Clear();
                Scene = null;
            }

            public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
            {
                m_ObjectUpdates.Enqueue(info);
            }

            void SensorUpdateThread()
            {
                Thread.CurrentThread.Name = "Sensor Repeat Thread for " + Scene.ID.ToString();
                while(!m_StopThread)
                {
                    ObjectUpdateInfo info;
                    try
                    {
                        info = m_ObjectUpdates.Dequeue(1000);
                    }
                    catch
                    {
                        continue;
                    }

                    if(m_StopThread)
                    {
                        break;
                    }

                    try
                    {
                        if (info.IsKilled || info.Part.LinkNumber != ObjectGroup.LINK_ROOT)
                        {
                            KnownObjects.Remove(info.Part.ID);
                            continue;
                        }
                        else if (info.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                        {
                            ObjectGroup grp = info.Part.ObjectGroup;
                            /* we can get the registering updates multiple times, so we process them that way */
                            KnownObjects[info.Part.ID] = grp;
                            foreach (KeyValuePair<ScriptInstance, SensorInfo> kvp in SensorRepeats)
                            {
                                if (kvp.Value.SensorHits.ContainsKey(grp.ID))
                                {
                                    continue;
                                }
                                AddIfSensed(kvp.Value, grp);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        /* never crash in this location */
                        m_Log.Debug("Unexpected exception", e);
                    }
                }
            }

            void SensorRepeatTimer(object o, EventArgs args)
            {
                List<IAgent> rootAgents = new List<IAgent>(Scene.RootAgents);
                int elapsedTimeInMsecs;
                lock (m_TimerLock)
                {
                    /* Stop timer when not needed */
                    if(SensorRepeats.Count == 0)
                    {
                        m_Timer.Stop();
                        return;
                    }
                    int newTickCount = Environment.TickCount;
                    elapsedTimeInMsecs = newTickCount - m_LastTickCount;
                    m_LastTickCount = newTickCount;
                }

                double elapsedTimeInSecs = elapsedTimeInMsecs / 1000f;

                foreach(KeyValuePair<ScriptInstance, SensorInfo> kvp in SensorRepeats)
                {
                    kvp.Value.TimeoutToElapse -= elapsedTimeInSecs;
                    if(kvp.Value.TimeoutToElapse <= 0)
                    {
                        try
                        {
                            kvp.Value.TimeoutToElapse += kvp.Value.Timeout;
                            if (kvp.Value.SensorHits.Count != 0)
                            {
                                SensorEvent ev = new SensorEvent();
                                ev.Data = new List<DetectInfo>(kvp.Value.SensorHits.Values);
                                kvp.Value.Instance.PostEvent(ev);
                            }
                            else
                            {
                                NoSensorEvent ev = new NoSensorEvent();
                                kvp.Value.Instance.PostEvent(ev);
                            }

                            /* re-evaluate sensor data */
                            kvp.Value.UpdateSenseLocation();
                            CleanRepeatSensor(kvp.Value);
                            if ((kvp.Value.SearchType & SENSE_AGENTS) != 0)
                            {
                                foreach (IAgent agent in rootAgents)
                                {
                                    AddIfSensed(kvp.Value, agent);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            /* do not loose sensors just of something happening in here */
                            m_Log.Debug("Unexpected exception", e);
                        }
                    }
                }
            }

            /* private constants */
            const int SENSE_AGENTS = 0x33;
            const int SENSE_OBJECTS = 0xE;
            const int AGENT = 0x01;
            const int ACTIVE = 0x02;
            const int PASSIVE = 0x04;
            const int SCRIPTED = 0x08;
            const int AGENT_BY_USERNAME = 0x10;
            const int NPC = 0x20;

            void CleanRepeatSensor(SensorInfo sensor)
            {
                List<UUID> removeList = new List<UUID>();

                /* it is a lot faster to re-check the detect list than going through the big object list.
                 * The nice improvement of that is that our repeat sensor does not need an initial scan after every interval.
                 */
                foreach (KeyValuePair<UUID, DetectInfo> kvp in sensor.SensorHits)
                {
                    if(!CheckIfSensed(sensor, kvp.Value.Object))
                    {
                        removeList.Add(kvp.Key);
                    }
                }
                
                foreach(UUID id in removeList)
                {
                    sensor.SensorHits.Remove(id);
                }
            }

            void EvalSensor(SensorInfo sensor)
            {
                if ((sensor.SearchType & SENSE_AGENTS) != 0)
                {
                    foreach (IAgent agent in Scene.RootAgents)
                    {
                        AddIfSensed(sensor, agent);
                    }
                }

                if ((sensor.SearchType & SENSE_OBJECTS) != 0)
                {
                    foreach (ObjectGroup grp in KnownObjects.Values)
                    {
                        AddIfSensed(sensor, grp);
                    }
                }
            }

            public void StartSensor(SensorInfo sensor)
            {
                sensor.UpdateSenseLocation();
                if(sensor.IsRepeating)
                {
                    SensorRepeats[sensor.Instance] = sensor;
                }

                if (sensor.SensorHits.Count != 0)
                {
                    SensorEvent ev = new SensorEvent();
                    ev.Data = new List<DetectInfo>(sensor.SensorHits.Values);
                    sensor.Instance.PostEvent(ev);
                }
                else
                {
                    NoSensorEvent ev = new NoSensorEvent();
                    sensor.Instance.PostEvent(ev);
                }

                if (sensor.IsRepeating)
                {
                    double mintimerreq = -1;
                    foreach(KeyValuePair<ScriptInstance, SensorInfo> kvp in SensorRepeats)
                    {
                        if(mintimerreq < 0 || kvp.Value.Timeout < mintimerreq)
                        {
                            mintimerreq = kvp.Value.Timeout;
                        }
                    }

                    mintimerreq = mintimerreq.Clamp(MIN_SENSOR_INTERVAL, MAX_SENSOR_INTERVAL);

                    lock(m_TimerLock)
                    {
                        if (mintimerreq < 0)
                        {
                            m_Timer.Stop();
                        }
                        else
                        {
                            m_Timer.Interval = mintimerreq;

                            if(!m_Timer.Enabled)
                            {
                                /* load a new value into LastTickCount, timer was disabled */
                                m_LastTickCount = Environment.TickCount;
                                m_Timer.Start();
                            }
                        }
                    }
                }
            }

            void AddIfSensed(SensorInfo sensor, IObject obj)
            {
                if(CheckIfSensed(sensor, obj))
                {
                    DetectInfo detInfo = new DetectInfo();
                    detInfo.Object = obj;
                    sensor.SensorHits[obj.ID] = detInfo;
                }
            }

            bool CheckArcAndRange(SensorInfo sensor, IObject obj)
            {
                Vector3 fromPos = sensor.SensePoint;
                Vector3 targetPos = obj.GlobalPosition;
                Vector3 object_direction = targetPos - fromPos;
                double distance = object_direction.Length;
                if(distance > sensor.SearchRadius)
                {
                    return false;
                }
                if(sensor.SearchArc < Math.PI && distance < double.Epsilon)
                {
                    Vector3 fwd_direction = Vector3.UnitX * sensor.SenseRotation;
                    double d = fwd_direction.Dot(object_direction);
                    double angleToObj = Math.Acos(d / distance);
                    return (angleToObj <= sensor.SearchArc);
                }
                else
                {
                    return true;
                }
            }

            bool CheckIfSensed(SensorInfo sensor, IObject obj)
            {
                if(sensor.SearchKey != UUID.Zero && sensor.SearchKey != obj.ID)
                {
                    return false;
                }
                if (obj.Owner.ID == sensor.OwnerID || obj.Owner.ID == sensor.OwnObjectID)
                {
                    return false;
                }

                ObjectGroup grp = obj as ObjectGroup;
                if(grp != null)
                {
                    if(grp.IsAttached)
                    {
                        /* ignore those */
                    }
                    else if (
                        ((sensor.SearchType & ACTIVE) != 0 && grp.IsPhysics) ||
                        ((sensor.SearchType & PASSIVE) != 0 && !grp.IsPhysics)
                        )
                    {
                        if(sensor.SearchName.Length != 0 && sensor.SearchName != obj.Name)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    return CheckArcAndRange(sensor, obj);
                }

                IAgent agent = obj as IAgent;
                if (agent != null)
                {
                    if((sensor.SearchType & SENSE_AGENTS) == 0)
                    {
                        return false;
                    }
                    if ((sensor.SearchType & AGENT) != 0 && sensor.SearchName.Length != 0)
                    {
                        if (sensor.SearchName != agent.Owner.FullName ||
                            (sensor.SearchName != agent.Owner.FirstName + " Resident" && agent.Owner.LastName.Length == 0))
                        {
                            return false;
                        }
                    }
                    else if ((sensor.SearchType & AGENT_BY_USERNAME) != 0 && sensor.SearchName.Length != 0)
                    {
                        if ((sensor.SearchName != (agent.Owner.FirstName + ".resident").ToLower() && agent.Owner.LastName.Length == 0) ||
                            (sensor.SearchName != agent.Owner.FullName.Replace(' ', '.') && agent.Owner.LastName.Length != 0))
                        {
                            return false;
                        }
                    }
                    else if ((sensor.SearchType & NPC) != 0 && sensor.SearchName.Length != 0)
                    {
                        if (sensor.SearchName != agent.Owner.FullName ||
                            (sensor.SearchName != agent.Owner.FirstName + " Resident" && agent.Owner.LastName.Length == 0))
                        {
                            return false;
                        }
                    }

                    if (agent.IsNpc && (sensor.SearchType & NPC) == 0)
                    {
                        return false;
                    }
                    if ((sensor.SearchType & SENSE_AGENTS) == 0)
                    {
                        if (agent.SittingOnObject != null && (sensor.SearchType & PASSIVE) == 0)
                        {
                            return false;
                        }
                        if (agent.SittingOnObject == null && (sensor.SearchType & ACTIVE) == 0)
                        {
                            return false;
                        }
                    }
                    return CheckArcAndRange(sensor, obj);
                }
                return false;
            }
        }

        readonly RwLockedDictionary<UUID, SceneInfo> m_Scenes = new RwLockedDictionary<UUID, SceneInfo>();

        public SensorApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            SceneManager.Scenes.OnRegionAdd += Scenes_OnRegionAdd;
            SceneManager.Scenes.OnRegionRemove += Scenes_OnRegionRemove;
        }

        public void Shutdown()
        {
            SceneManager.Scenes.OnRegionAdd -= Scenes_OnRegionAdd;
            SceneManager.Scenes.OnRegionRemove -= Scenes_OnRegionRemove;
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        void Scenes_OnRegionAdd(SceneInterface obj)
        {
            m_Scenes.Add(obj.ID, new SceneInfo(obj));
        }

        void Scenes_OnRegionRemove(SceneInterface obj)
        {
            SceneInfo sceneInfo;
            if(m_Scenes.Remove(obj.ID, out sceneInfo))
            {
                sceneInfo.Stop();
            }
        }

        [APILevel(APIFlags.LSL, "sensor")]
        [StateEventDelegate]
        public delegate void State_sensor(int num_detected);

        [APILevel(APIFlags.LSL, "no_sensor")]
        [StateEventDelegate]
        public delegate void State_no_sensor();

        [APILevel(APIFlags.LSL, "llSensor")]
        public void Sensor(ScriptInstance instance, string name, LSLKey id, int type, double radius, double arc)
        {
            if (type == 0)
            {
                return;
            }
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                UUID ownObjectID = grp.ID;
                SceneInfo sceneInfo;
                if (m_Scenes.TryGetValue(scene.ID, out sceneInfo))
                {
                    sceneInfo.StartSensor(new SensorInfo(instance, true, 0, name, id, type, radius, arc));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSensorRepeat")]
        public void SensorRepeat(ScriptInstance instance, string name, LSLKey id, int type, double range, double arc, double rate)
        {
            if (type == 0)
            {
                type = ~0;
            }
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                UUID ownObjectID = grp.ID;
                SceneInfo sceneInfo;
                if(m_Scenes.TryGetValue(scene.ID, out sceneInfo))
                {
                    sceneInfo.StartSensor(new SensorInfo(instance, true, rate, name, id, type, range, arc));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSensorRemove")]
        public void SensorRemove(ScriptInstance instance)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                SceneInfo sceneInfo;
                if (m_Scenes.TryGetValue(scene.ID, out sceneInfo))
                {
                    sceneInfo.SensorRepeats.Remove(instance);
                }
            }
        }

        [ExecutedOnScriptRemove]
        [ExecutedOnScriptReset]
        public void RemoveSensors(ScriptInstance instance)
        {
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            SceneInfo sceneInfo;
            if(m_Scenes.TryGetValue(scene.ID, out sceneInfo))
            {
                sceneInfo.SensorRepeats.Remove(instance);
            }
        }
    }
}
