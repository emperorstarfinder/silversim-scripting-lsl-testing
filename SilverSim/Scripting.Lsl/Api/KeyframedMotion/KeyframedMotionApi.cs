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

#pragma warning disable IDE0018, RCS1029, RCS1163

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.ComponentModel;
using KeyframeMotionEntry = SilverSim.Scene.Types.KeyframedMotion.Keyframe;
using KeyframeMotionList = SilverSim.Scene.Types.KeyframedMotion.KeyframedMotion;

namespace SilverSim.Scripting.Lsl.Api.KeyframedMotion
{
    [ScriptApiName("KeyframedMotion")]
    [LSLImplementation]
    [Description("LSL KeyframedMotion API")]
    public class KeyframedMotionApi
    {
        [APILevel(APIFlags.LSL)]
        public const int KFM_COMMAND = 0;
        [APILevel(APIFlags.LSL)]
        public const int KFM_MODE = 1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_DATA = 2;

        [APILevel(APIFlags.LSL)]
        public const int KFM_CMD_PLAY = 0;
        [APILevel(APIFlags.LSL)]
        public const int KFM_CMD_STOP = 1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_CMD_PAUSE = 2;

        [APILevel(APIFlags.LSL)]
        public const int KFM_FORWARD = 0;
        [APILevel(APIFlags.LSL)]
        public const int KFM_LOOP = 1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_PING_PONG = 2;
        [APILevel(APIFlags.LSL)]
        public const int KFM_REVERSE = 3;

        [APILevel(APIFlags.LSL)]
        public const int KFM_ROTATION = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_TRANSLATION = 0x2;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llSetKeyframedMotion")]
        public void SetKeyframedMotion(ScriptInstance instance, AnArray keyframes, AnArray options)
        {
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                if(grp.IsAttached)
                {
                    return;
                }
                if(options.Count == 0)
                {
                    if(keyframes.Count == 0)
                    {
                        grp.KeyframedMotion = null;
                    }
                }
                else if(options[0].AsInt == KFM_COMMAND)
                {
                    if(options.Count != 2)
                    {
                        return;
                    }
                    switch(options[1].AsInt)
                    {
                        case KFM_CMD_PLAY:
                            grp.PlayKeyframedMotion();
                            break;

                        case KFM_CMD_PAUSE:
                            grp.PauseKeyframedMotion();
                            break;

                        case KFM_CMD_STOP:
                            grp.StopKeyframedMotion();
                            break;

                        default:
                            break;
                    }
                }
                else if(options.Count % 2 == 0)
                {
                    int mode = KFM_FORWARD;
                    int data = KFM_TRANSLATION | KFM_ROTATION;
                    for(int i = 0; i < options.Count; i += 2)
                    {
                        int type = options[i].AsInt;
                        int value = options[i + 1].AsInt;
                        switch(type)
                        {
                            case KFM_DATA:
                                data = value;
                                break;

                            case KFM_MODE:
                                mode = value;
                                break;

                            default:
                                return;
                        }
                    }

                    if(keyframes.Count == 0)
                    {
                        /* clear keyframe motion */
                        grp.KeyframedMotion = null;
                        return;
                    }

                    var newMotion = new KeyframeMotionList();
                    KeyframeMotionEntry entry;

                    switch(mode)
                    {
                        case KFM_LOOP:
                            newMotion.PlayMode = KeyframeMotionList.Mode.Loop;
                            break;

                        case KFM_PING_PONG:
                            newMotion.PlayMode = KeyframeMotionList.Mode.PingPong;
                            break;

                        case KFM_REVERSE:
                            newMotion.PlayMode = KeyframeMotionList.Mode.Reverse;
                            break;

                        default:
                            newMotion.PlayMode = KeyframeMotionList.Mode.Forward;
                            break;
                    }

                    newMotion.Flags = 0;
                    int div = 1;
                    if((data & KFM_TRANSLATION) != 0)
                    {
                        ++div;
                        newMotion.Flags |= KeyframeMotionList.DataFlags.Translation;
                    }
                    if ((data & KFM_ROTATION) != 0)
                    {
                        ++div;
                        newMotion.Flags |= KeyframeMotionList.DataFlags.Rotation;
                    }

                    if(div < 2)
                    {
                        return;
                    }

                    if(keyframes.Count % div != 0)
                    {
                        return;
                    }

                    for(int i = 0; i < keyframes.Count; i += div)
                    {
                        int j = i;
                        entry = new KeyframeMotionEntry();
                        if((data & KFM_TRANSLATION) != 0)
                        {
                            entry.TargetPosition = keyframes[j++].AsVector3;
                        }
                        if((data & KFM_ROTATION) != 0)
                        {
                            entry.TargetRotation = keyframes[j++].AsQuaternion;
                        }
                        entry.Duration = keyframes[j++].AsReal;
                    }
                    grp.KeyframedMotion = newMotion;
                }
            }
        }
    }
}
