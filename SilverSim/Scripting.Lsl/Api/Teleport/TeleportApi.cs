// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.Teleport
{
    [ScriptApiName("Sound")]
    [LSLImplementation]
    public class TeleportApi : IScriptApi, IPlugin
    {
        public TeleportApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llTeleportAgentHome")]
        public void TeleportAgentHome(ScriptInstance instance, LSLKey avatar)
        {
            throw new NotImplementedException("llTeleportAgentHome(key)");
        }

        [APILevel(APIFlags.LSL, "llTeleportAgentGlobalCoords")]
        public void TeleportAgentGlobalCoords(ScriptInstance instance, LSLKey avatar, Vector3 globalCoordinates, Vector3 regionCoordinates, Vector3 lookAt)
        {
            throw new NotImplementedException("llTeleportAgentGlobalCoords(key, vector, vector, vector)");
        }

        [APILevel(APIFlags.LSL, "llTeleportAgent")]
        public void TeleportAgentLandmark(ScriptInstance instance, LSLKey avatar, string landmark, Vector3 position, Vector3 lookAt)
        {
            throw new NotImplementedException("llTeleportAgent(key, string, vector, vector)");
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        public void TeleportAgent(ScriptInstance instance, LSLKey agent, int regionX, int regionY, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgent", ScriptInstance.ThreatLevelType.VeryHigh);
            }
            throw new NotImplementedException("osTeleportAgent(key, integer, integer, vector, vector)");
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        public void TeleportAgent(ScriptInstance instance, LSLKey agent, string regionName, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgent", ScriptInstance.ThreatLevelType.VeryHigh);
            }
            throw new NotImplementedException("osTeleportAgent(key, string, vector, vector)");
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        public void TeleportAgent(ScriptInstance instance, LSLKey agent, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgent", ScriptInstance.ThreatLevelType.VeryHigh);
            }
            throw new NotImplementedException("osTeleportAgent(key, vector, vector)");
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwner")]
        public void TeleportOwner(ScriptInstance instance, int regionX, int regionY, Vector3 position, Vector3 lookAt)
        {
            throw new NotImplementedException("osTeleportOwner(integer, integer, vector, vector)");
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwner")]
        public void TeleportOwner(ScriptInstance instance, string regionName, Vector3 position, Vector3 lookAt)
        {
            throw new NotImplementedException("osTeleportOwner(string, vector, vector)");
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwner")]
        public void TeleportOwner(ScriptInstance instance, Vector3 position, Vector3 lookAt)
        {
            throw new NotImplementedException("osTeleportOwner(vector, vector)");
        }
    }
}
