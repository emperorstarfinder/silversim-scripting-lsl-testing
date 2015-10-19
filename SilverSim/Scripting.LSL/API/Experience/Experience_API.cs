// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Experience
{
    [ScriptApiName("Experience")]
    [LSLImplementation]
    public partial class Experience_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Experience_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_THROTTLED = 1;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCES_DISABLED = 2;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_INVALID_PARAMETERS = 3;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_PERMITTED = 4;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NO_EXPERIENCE = 5;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_FOUND = 6;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_INVALID_EXPERIENCE = 7;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCE_DISABLED = 8;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCE_SUSPENDED = 9;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_UNKNOWN_ERROR = 10;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_QUOTA_EXCEEDED = 11;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_STORE_DISABLED = 12;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_STORAGE_EXCEPTION = 13;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_KEY_NOT_FOUND = 14;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_RETRY_UPDATE = 15;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_MATURITY_EXCEEDED = 16;

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("experience_permissions")]
        public delegate void State_experience_permissions(LSLKey agent_id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("experience_permissions_denied")]
        public delegate void State_experience_permissions_denied(LSLKey agent_id, int reason);

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llAgentInExperience")]
        public int AgentInExperience(ScriptInstance instance, LSLKey agent)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llCreateKeyValue")]
        public LSLKey CreateKeyValue(ScriptInstance instance, string k, string v)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDataSizeKeyValue")]
        public LSLKey DataSizeKeyValue(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDeleteKeyValue")]
        public LSLKey DeleteKeyValue(ScriptInstance instance, string k)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetExperienceDetails")]
        public AnArray GetExperienceDetails(ScriptInstance instance, LSLKey experience_id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetExperienceErrorMessage")]
        public string GetExperienceErrorMessage(ScriptInstance instance, int error)
        {
            switch(error)
            {
                case XP_ERROR_NONE: return "no error";
                case XP_ERROR_THROTTLED: return "exceeded throttle";
                case XP_ERROR_EXPERIENCES_DISABLED: return "experiences are disabled";
                case XP_ERROR_INVALID_PARAMETERS: return "invalid parameters";
                case XP_ERROR_NOT_PERMITTED: return "operation not permitted";
                case XP_ERROR_NO_EXPERIENCE: return "script not associated with an experience";
                case XP_ERROR_NOT_FOUND: return "not found";
                case XP_ERROR_INVALID_EXPERIENCE: return "invalid experience";
                case XP_ERROR_EXPERIENCE_DISABLED: return "experience is disabled";
                case XP_ERROR_EXPERIENCE_SUSPENDED: return "experience is suspended";
                case XP_ERROR_UNKNOWN_ERROR: return "unknown error";
                case XP_ERROR_QUOTA_EXCEEDED: return "experience data quota exceeded";
                case XP_ERROR_STORE_DISABLED: return "key-value store is disabled";
                case XP_ERROR_STORAGE_EXCEPTION: return "key-value store communication failed";
                case XP_ERROR_KEY_NOT_FOUND: return "key doesn't exist";
                case XP_ERROR_RETRY_UPDATE: return "retry update";
                case XP_ERROR_MATURITY_EXCEEDED: return "experience content rating too high";
                default: return "unknown error";
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llKeyCountKeyValue")]
        public LSLKey KeyCountKeyValue(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llKeysKeyValue")]
        public LSLKey KeysKeyValue(ScriptInstance instance, int first, int count)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llReadKeyValue")]
        public LSLKey ReadKeyValue(ScriptInstance instance, string k)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llRequestExperiencePermissions")]
        public void RequestExperiencePermissions(ScriptInstance instance, LSLKey agent, string name /* unused */)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llUpdateKeyValue")]
        public LSLKey UpdateKeyValue(ScriptInstance instance, string k, string v, int checked_orig, string original_value)
        {
            throw new NotImplementedException();
        }
    }
}
