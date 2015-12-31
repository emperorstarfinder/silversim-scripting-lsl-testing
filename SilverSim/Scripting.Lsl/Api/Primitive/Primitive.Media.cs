// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, "llClearLinkMedia")]
        public int ClearLinkMedia(ScriptInstance instance, int link, int face)
        {
            ObjectPart part;
            ObjectPart thisPart = instance.Part;
            if (link == 0)
            {
                link = LINK_ROOT;
            }
            if(LINK_THIS == link)
            {
                part = thisPart;
            }
            else if (!thisPart.ObjectGroup.TryGetValue(link, out part))
            {
                return STATUS_NOT_FOUND;
            }

            if(face < 0 || face >= part.NumberOfSides)
            {
                return STATUS_NOT_FOUND;
            }
            lock (instance)
            {
                Types.Primitive.PrimitiveMedia mediaList = part.Media;
                if (mediaList == null)
                {
                    return STATUS_OK;
                }

                if (mediaList.Count <= face)
                {
                    return STATUS_OK;
                }
                part.UpdateMediaFace(face, null, thisPart.Owner.ID);
                return STATUS_OK;
            }
        }

        [APILevel(APIFlags.LSL, "llClearPrimMedia")]
        [ForcedSleep(1.0)]
        public int ClearPrimMedia(ScriptInstance instance, int face)
        {
            return ClearLinkMedia(instance, LINK_THIS, face);
        }

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_ALT_IMAGE_ENABLE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CONTROLS = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CURRENT_URL = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_HOME_URL = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_LOOP = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_PLAY = 5;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_SCALE = 6;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_ZOOM = 7;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_FIRST_CLICK_INTERACT = 8;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_WIDTH_PIXELS = 9;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_HEIGHT_PIXELS = 10;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_WHITELIST_ENABLE = 11;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_WHITELIST = 12;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERMS_INTERACT = 13;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERMS_CONTROL = 14;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_NONE = 0x0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_OWNER = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_GROUP = 0x2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_ANYONE = 0x4;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CONTROLS_STANDARD = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CONTROLS_MINI = 1;

        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_OK = 0;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_MALFORMED_PARAMS = 1000;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_TYPE_MISMATCH = 1001;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_BOUNDS_ERROR = 1002;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_NOT_FOUND = 1003;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_NOT_SUPPORTED = 1004;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_INTERNAL_ERROR = 1999;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_WHITELIST_FAILED = 2001;

        [APILevel(APIFlags.LSL, "llGetLinkMedia")]
        public AnArray GetLinkMedia(ScriptInstance instance, int link, int face, AnArray param)
        {
            ObjectPart part;
            if(link == 0)
            {
                link = LINK_ROOT;
            }
            if(LINK_THIS == link)
            {
                part = instance.Part;
            }
            else if(!instance.Part.ObjectGroup.TryGetValue(link, out part))
            {
                return new AnArray();
            }
            if(face < 0 || face >= part.NumberOfSides)
            {
                return new AnArray();
            }
            lock (instance)
            {
                Types.Primitive.PrimitiveMedia mediaList = part.Media;
                if (mediaList == null)
                {
                    return new AnArray();
                }

                Types.Primitive.PrimitiveMedia.Entry entry;
                if (mediaList.Count <= face)
                {
                    return new AnArray();
                }
                entry = mediaList[face];
                if (null == entry)
                {
                    return new AnArray();
                }

                AnArray res = new AnArray();
                foreach (IValue iv in param)
                {
                    switch (iv.AsInt)
                    {
                        case PRIM_MEDIA_ALT_IMAGE_ENABLE:
                            res.Add(entry.IsAlternativeImageEnabled);
                            break;

                        case PRIM_MEDIA_CONTROLS:
                            res.Add((int)entry.Controls);
                            break;

                        case PRIM_MEDIA_CURRENT_URL:
                            res.Add(entry.CurrentURL);
                            break;

                        case PRIM_MEDIA_HOME_URL:
                            res.Add(entry.HomeURL);
                            break;

                        case PRIM_MEDIA_AUTO_LOOP:
                            res.Add(entry.IsAutoLoop);
                            break;

                        case PRIM_MEDIA_AUTO_PLAY:
                            res.Add(entry.IsAutoPlay);
                            break;

                        case PRIM_MEDIA_AUTO_SCALE:
                            res.Add(entry.IsAutoScale);
                            break;

                        case PRIM_MEDIA_AUTO_ZOOM:
                            res.Add(entry.IsAutoZoom);
                            break;

                        case PRIM_MEDIA_FIRST_CLICK_INTERACT:
                            res.Add(entry.IsInteractOnFirstClick);
                            break;

                        case PRIM_MEDIA_WIDTH_PIXELS:
                            res.Add(entry.Width);
                            break;

                        case PRIM_MEDIA_HEIGHT_PIXELS:
                            res.Add(entry.Height);
                            break;

                        case PRIM_MEDIA_WHITELIST_ENABLE:
                            res.Add(entry.IsWhiteListEnabled);
                            break;

                        case PRIM_MEDIA_WHITELIST:
                            {
                                string csv = string.Empty;
                                foreach (string whitelistEntry in entry.WhiteList)
                                {
                                    if (csv.Length != 0)
                                    {
                                        csv += ",";
                                    }
                                    csv += Uri.EscapeUriString(whitelistEntry);
                                }
                                res.Add(csv);
                            }
                            break;

                        case PRIM_MEDIA_PERMS_INTERACT:
                            res.Add((int)entry.InteractPermissions);
                            break;

                        case PRIM_MEDIA_PERMS_CONTROL:
                            res.Add((int)entry.ControlPermissions);
                            break;

                        default:
                            throw new ArgumentException(string.Format("Unknown media parameter {0}", iv.ToString()));
                    }
                }
                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llGetPrimMediaParams")]
        [ForcedSleep(1.0)]
        public AnArray GetPrimMediaParams(ScriptInstance instance, int face, AnArray param)
        {
            return GetLinkMedia(instance, LINK_THIS, face, param);
        }

        [APILevel(APIFlags.LSL)]
        public const int STATUS_OK = 0;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_MALFORMED_PARAMS = 1000;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_TYPE_MISMATCH = 1001;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_BOUNDS_ERROR = 1002;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_NOT_FOUND = 1003;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_NOT_SUPPORTED = 1004;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_INTERNAL_ERROR = 1999;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_WHITELIST_FAILED = 2001;

        [APILevel(APIFlags.LSL, "llSetLinkMedia")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public int SetLinkMedia(ScriptInstance instance, int link, int face, AnArray param)
        {
            ObjectPart part;
            if (link == 0)
            {
                link = LINK_ROOT;
            }
            if(LINK_THIS == link)
            {
                part = instance.Part;
            }
            else if (!instance.Part.ObjectGroup.TryGetValue(link, out part))
            {
                return STATUS_NOT_FOUND;
            }
            if (face < 0 || face >= part.NumberOfSides)
            {
                return STATUS_NOT_FOUND;
            }
            lock (instance)
            {
                Types.Primitive.PrimitiveMedia mediaList = part.Media;
                Types.Primitive.PrimitiveMedia.Entry entry;
                if (mediaList == null || mediaList.Count <= face)
                {
                    entry = new Types.Primitive.PrimitiveMedia.Entry();
                }
                else
                {
                    entry = mediaList[face];
                }

                int i, v;
                for (i = 0; i < param.Count; i += 2)
                {
                    switch(param[i].AsInt)
                    {
                        case PRIM_MEDIA_ALT_IMAGE_ENABLE:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsAlternativeImageEnabled = param[i + 1].AsBoolean;
                            break;

                        case PRIM_MEDIA_CONTROLS:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            v = param[i + 1].AsInt;
                            if(v > PRIM_MEDIA_CONTROLS_MINI || v < PRIM_MEDIA_CONTROLS_STANDARD)
                            {
                                return STATUS_BOUNDS_ERROR;
                            }
                            entry.Controls = (Types.Primitive.PrimitiveMediaControls)v;
                            break;

                        case PRIM_MEDIA_CURRENT_URL:
                            if(param[i + 1].LSL_Type != LSLValueType.String)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.CurrentURL = param[i + 1].ToString();
                            break;

                        case PRIM_MEDIA_HOME_URL:
                            if(param[i + 1].LSL_Type != LSLValueType.String)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.HomeURL = param[i + 1].ToString();
                            break;

                        case PRIM_MEDIA_AUTO_LOOP:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsAutoLoop = param[i + 1].AsBoolean;
                            break;

                            
                        case PRIM_MEDIA_AUTO_PLAY:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsAutoPlay = param[i + 1].AsBoolean;
                            break;

                        case PRIM_MEDIA_AUTO_SCALE:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsAutoScale = param[i + 1].AsBoolean;
                            break;

                        case PRIM_MEDIA_AUTO_ZOOM:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsAutoZoom = param[i + 1].AsBoolean;
                            break;

                        case PRIM_MEDIA_FIRST_CLICK_INTERACT:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsInteractOnFirstClick = param[i + 1].AsBoolean;
                            break;


                        case PRIM_MEDIA_WIDTH_PIXELS:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.Width = param[i + 1].AsInt;
                            if(entry.Width < 1)
                            {
                                return STATUS_BOUNDS_ERROR;
                            }
                            break;

                        case PRIM_MEDIA_HEIGHT_PIXELS:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.Height = param[i + 1].AsInt;
                            if(entry.Height < 1)
                            {
                                return STATUS_BOUNDS_ERROR;
                            }
                            break;

                        case PRIM_MEDIA_WHITELIST_ENABLE:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.IsWhiteListEnabled = param[i + 1].AsBoolean;
                            break;

                        case PRIM_MEDIA_WHITELIST:
                            if(param[i + 1].LSL_Type != LSLValueType.String)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            else
                            {
                                string[] parts = param[i + 1].ToString().Split(',');
                                string[] whitelist = new string[parts.Length];
                                for(int p = 0; p < parts.Length; ++p)
                                {
                                    whitelist[p] = Uri.UnescapeDataString(parts[p]);
                                }
                                entry.WhiteList = whitelist;
                            }
                            break;

                        case PRIM_MEDIA_PERMS_INTERACT:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.InteractPermissions = (Types.Primitive.PrimitiveMediaPermission)param[i + 1].AsInt;
                            break;

                        case PRIM_MEDIA_PERMS_CONTROL:
                            if(param[i + 1].LSL_Type != LSLValueType.Integer)
                            {
                                return STATUS_TYPE_MISMATCH;
                            }
                            entry.ControlPermissions = (Types.Primitive.PrimitiveMediaPermission)param[i + 1].AsInt;
                            break;

                        default:
                            return STATUS_NOT_SUPPORTED;
                    }
                }

#warning Implement white list checks
                part.UpdateMediaFace(face, entry, instance.Part.Owner.ID);
                return STATUS_OK;
            }
        }

        [APILevel(APIFlags.LSL, "llSetPrimMediaParams")]
        [ForcedSleep(1.0)]
        public int SetPrimMediaParams(ScriptInstance instance, int face, AnArray param)
        {
            return SetLinkMedia(instance, LINK_THIS, face, param);
        }

        [APILevel(APIFlags.LSL, "llSetPrimURL")]
        public void SetPrimURL(ScriptInstance instance, string url)
        {
            /* noop function */
        }

        [APILevel(APIFlags.LSL, "llRefreshPrimURL")]
        public void RefreshPrimURL(ScriptInstance instance)
        {
            /* noop function */
        }
    }
}
