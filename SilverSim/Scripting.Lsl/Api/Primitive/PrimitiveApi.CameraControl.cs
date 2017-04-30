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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Camera;
using System;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_PITCH = 0;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET = 1;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET_X = 2;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET_Y = 3;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET_Z = 4;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_LAG = 5;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_LAG = 6;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_DISTANCE = 7;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_BEHINDNESS_ANGLE = 8;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_BEHINDNESS_LAG = 9;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_THRESHOLD = 10;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_THRESHOLD = 11;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_ACTIVE = 12;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION = 13;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_X = 14;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_Y = 15;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_Z = 16;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS = 17;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_X = 18;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_Y = 19;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_Z = 20;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_LOCKED = 21;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_LOCKED = 22;

        [APILevel(APIFlags.LSL, "llSetCameraAtOffset")]
        public void SetCameraAtOffset(ScriptInstance instance, Vector3 offset)
        {
            lock(instance)
            {
                instance.Part.CameraAtOffset = offset;
            }
        }

        [APILevel(APIFlags.LSL, "llSetLinkCamera")]
        public void SetLinkCamera(ScriptInstance instance, int link, Vector3 eye, Vector3 at)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part.ObjectGroup[link];
                part.CameraAtOffset = at;
                part.CameraEyeOffset = eye;
            }
        }

        [APILevel(APIFlags.LSL, "llSetCameraEyeOffset")]
        public void SetCameraEyeOffset(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                instance.Part.CameraEyeOffset = offset;
            }
        }

        [APILevel(APIFlags.LSL, "llReleaseCamera")]
        public void ReleaseCamera(ScriptInstance instance, LSLKey agent)
        {
            lock(instance)
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "llReleaseCameraDeprecated", "llReleaseCamera is deprecated and recognized for script compatibility\nUse llClearCameraParams instead."));
            }
        }

        [APILevel(APIFlags.LSL, "llClearCameraParams")]
        public void ClearCameraParams(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ControlCamera) != 0)
                {
                    ObjectGroup grp = instance.Part.ObjectGroup;
                    ClearFollowCamProperties followcamprops = new ClearFollowCamProperties();
                    followcamprops.ObjectID = grp.ID;
                    SceneInterface scene = grp.Scene;
                    IAgent agent;
                    if (scene.RootAgents.TryGetValue(grp.Owner.ID, out agent))
                    {
                        agent.SendMessageIfRootAgent(followcamprops, scene.ID);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetCameraParams")]
        public void SetCameraParams(ScriptInstance instance, AnArray rules)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ControlCamera) != 0)
                {
                    SortedDictionary<int, double> parameters = new SortedDictionary<int, double>();

                    int type;
                    int i = 0;
                    #region Rules Decoder
                    while(i < rules.Count)
                    {
                        try
                        {
                            type = rules[i++].AsInt;
                        }
                        catch
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidCameraParamType0", "Invalid camera param type {0}", rules[i - 1].ToString()));
                            return;
                        }

                        if(i >= rules.Count)
                        {
                            break;
                        }

                        switch(type)
                        {
                            case CAMERA_FOCUS:
                            case CAMERA_FOCUS_OFFSET:
                            case CAMERA_POSITION:
                                {
                                    Vector3 v;
                                    try
                                    {
                                        v = rules[i++].AsVector3;
                                    }
                                    catch (InvalidCastException)
                                    {
                                        switch (type)
                                        {
                                            case CAMERA_FOCUS:
                                                instance.ShoutError(new LocalizedScriptMessage(this, "llSetCameraParams0ExpectingAVectorParameter", "llSetCameraParams: {0}: Expecting a vector parameter", "CAMERA_FOCUS"));
                                                return;

                                            case CAMERA_FOCUS_OFFSET:
                                                instance.ShoutError(new LocalizedScriptMessage(this, "llSetCameraParams0ExpectingAVectorParameter", "llSetCameraParams: {0}: Expecting a vector parameter", "CAMERA_FOCUS_OFFSET"));
                                                return;

                                            case CAMERA_POSITION:
                                                instance.ShoutError(new LocalizedScriptMessage(this, "llSetCameraParams0ExpectingAVectorParameter", "llSetCameraParams: {0}: Expecting a vector parameter", "CAMERA_POSITION"));
                                                return;

                                            default:
                                                return;
                                        }
                                    }
                                    parameters[type + 1] = v.X;
                                    parameters[type + 2] = v.Y;
                                    parameters[type + 3] = v.Z;
                                }
                                break;

                            default:
                                try
                                {
                                    parameters[type] = rules[i++].AsReal;
                                }
                                catch(InvalidCastException)
                                {
                                    string typeName;
                                    switch (type)
                                    {
                                        case CAMERA_PITCH:
                                            typeName = "CAMERA_PITCH";
                                            break;
                                        case CAMERA_FOCUS_OFFSET_X:
                                            typeName = "CAMERA_FOCUS_OFFSET_X";
                                            break;
                                        case CAMERA_FOCUS_OFFSET_Y:
                                            typeName = "CAMERA_FOCUS_OFFSET_Y";
                                            break;
                                        case CAMERA_FOCUS_OFFSET_Z:
                                            typeName = "CAMERA_FOCUS_OFFSET_Z";
                                            break;
                                        case CAMERA_POSITION_LAG:
                                            typeName = "CAMERA_POSITION_LAG";
                                            break;
                                        case CAMERA_FOCUS_LAG:
                                            typeName = "CAMERA_FOCUS_LAG";
                                            break;
                                        case CAMERA_DISTANCE:
                                            typeName = "CAMERA_DISTANCE";
                                            break;
                                        case CAMERA_BEHINDNESS_ANGLE:
                                            typeName = "CAMERA_BEHINDNESS_ANGLE";
                                            break;
                                        case CAMERA_BEHINDNESS_LAG:
                                            typeName = "CAMERA_BEHINDNESS_LAG";
                                            break;
                                        case CAMERA_POSITION_THRESHOLD:
                                            typeName = "CAMERA_POSITION_THRESHOLD";
                                            break;
                                        case CAMERA_FOCUS_THRESHOLD:
                                            typeName = "CAMERA_FOCUS_THRESHOLD";
                                            break;
                                        case CAMERA_ACTIVE:
                                            typeName = "CAMERA_ACTIVE";
                                            break;
                                        case CAMERA_POSITION_X:
                                            typeName = "CAMERA_POSITION_X";
                                            break;
                                        case CAMERA_POSITION_Y:
                                            typeName = "CAMERA_POSITION_Y";
                                            break;
                                        case CAMERA_POSITION_Z:
                                            typeName = "CAMERA_POSITION_Z";
                                            break;
                                        case CAMERA_FOCUS_X:
                                            typeName = "CAMERA_FOCUS_X";
                                            break;
                                        case CAMERA_FOCUS_Y:
                                            typeName = "CAMERA_FOCUS_Y";
                                            break;
                                        case CAMERA_FOCUS_Z:
                                            typeName = "CAMERA_FOCUS_Z";
                                            break;
                                        case CAMERA_POSITION_LOCKED:
                                            typeName = "CAMERA_POSITION_LOCKED";
                                            break;
                                        case CAMERA_FOCUS_LOCKED:
                                            typeName = "CAMERA_FOCUS_LOCKED";
                                            break;
                                        default:
                                            typeName = string.Format("{0}", rules[i - 1].AsInt);
                                            break;
                                    }
                                    instance.ShoutError(new LocalizedScriptMessage(this, "llSetCameraParamsType0ParameterIsInvalid", "llSetCameraParams: {0}: Parameter is invalid", typeName));
                                    return;
                                }
                                break;
                        }
                    }
                    #endregion

                    if (parameters.Count > 0)
                    {
                        ObjectGroup grp = instance.Part.ObjectGroup;
                        SetFollowCamProperties followcamprops = new SetFollowCamProperties();
                        followcamprops.ObjectID = grp.ID;
                        foreach(KeyValuePair<int, double> kvp in parameters)
                        {
                            SetFollowCamProperties.CameraProperty prop = new SetFollowCamProperties.CameraProperty();
                            prop.Type = kvp.Key;
                            prop.Value = kvp.Value;
                            followcamprops.CameraProperties.Add(prop);
                        }
                        SceneInterface scene = grp.Scene;
                        IAgent agent;
                        if(scene.RootAgents.TryGetValue(grp.Owner.ID, out agent))
                        {
                            agent.SendMessageIfRootAgent(followcamprops, scene.ID);
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetCameraPos")]
        public Vector3 GetCameraPos(ScriptInstance instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {
                lock(instance)
                {
                    IAgent agent;
                    if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                    {
                        return agent.CameraPosition;
                    }
                }
            }
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL, "llGetCameraRot")]
        public Quaternion GetCameraRot(ScriptInstance instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {
                lock(instance)
                {
                    IAgent agent;
                    if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                    {
                        return agent.CameraRotation;
                    }
                }
            }
            return Quaternion.Identity;
        }
    }
}
