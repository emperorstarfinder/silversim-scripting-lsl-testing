// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using ThreadedClasses;

namespace SilverSim.Scripting.Lsl.Api.Sensor
{
    [ScriptApiName("Sensor")]
    [LSLImplementation]
    public class SensorApi : IScriptApi, IPlugin, IPluginShutdown
    {
        public class SensorInfo
        {
            public readonly ScriptInstance Instance;
            public double Timeout;
            public bool IsRepeating;

            public SensorInfo(ScriptInstance instance, bool isRepeating, double timeout)
            {
                Instance = instance;
                Timeout = timeout;
                IsRepeating = false;
            }
        }
        public class SceneInfo : ISceneListener
        {
            public SceneInterface Scene;
            public readonly RwLockedDictionary<UUID, ObjectGroup> KnownObjects = new RwLockedDictionary<UUID, ObjectGroup>();
            public readonly RwLockedDictionary<ScriptInstance, SensorInfo> Sensors = new RwLockedDictionary<ScriptInstance, SensorInfo>();

            public SceneInfo(SceneInterface scene)
            {
                Scene = scene;
                Scene.SceneListeners.Add(this);
            }

            public void Remove()
            {
                Scene.SceneListeners.Remove(this);
            }

            public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
            {
                if(info.IsKilled || info.Part.LinkNumber != ObjectGroup.LINK_ROOT)
                {
                    KnownObjects.Remove(info.Part.ID);
                }
                else if(info.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                {
                    KnownObjects.Add(info.Part.ID, info.Part.ObjectGroup);
                }
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
                sceneInfo.KnownObjects.Clear();
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
            throw new NotImplementedException("llSensor(string, key, integer, float, float)");
        }

        [APILevel(APIFlags.LSL, "llSensorRepeat")]
        public void SensorRepeat(ScriptInstance instance, string name, LSLKey id, int type, double range, double arc, double rate)
        {
            /* there is only one repeating sensor per script */
            throw new NotImplementedException("llSensorRepeat(string, key, integer, float, float, float)");
        }

        [APILevel(APIFlags.LSL, "llSensorRemove")]
        public void SensorRemove(ScriptInstance instance)
        {
            throw new NotImplementedException("llSensorRemove()");
        }

        [ExecutedOnScriptRemove]
        [ExecutedOnScriptReset]
        public void RemoveSensors(ScriptInstance instance)
        {
            /* to be filled when implementing sensors */
        }
    }
}
