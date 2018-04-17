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

#pragma warning disable IDE0018, RCS1029, IDE0019

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Lsl.Api.Primitive;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace SilverSim.Scripting.Lsl
{
    public static class ExtensionMethods
    {
        private static readonly Dictionary<string, UUID> m_InternalAnimations = new Dictionary<string, UUID>();

        static ExtensionMethods()
        {
            m_InternalAnimations.Add("express_afraid", "6b61c8e8-4747-0d75-12d7-e49ff207a4ca");
            m_InternalAnimations.Add("aim_r_bazooka", "b5b4a67d-0aee-30d2-72cd-77b333e932ef");
            m_InternalAnimations.Add("aim_l_bow", "46bb4359-de38-4ed8-6a22-f1f52fe8f506");
            m_InternalAnimations.Add("aim_r_handgun", "3147d815-6338-b932-f011-16b56d9ac18b");
            m_InternalAnimations.Add("aim_r_rifle", "ea633413-8006-180a-c3ba-96dd1d756720");
            m_InternalAnimations.Add("express_anger", "5747a48e-073e-c331-f6f3-7c2149613d3e");
            m_InternalAnimations.Add("away", "fd037134-85d4-f241-72c6-4f42164fedee");
            m_InternalAnimations.Add("backflip", "c4ca6188-9127-4f31-0158-23c4e2f93304");
            m_InternalAnimations.Add("express_laugh", "18b3a4b5-b463-bd48-e4b6-71eaac76c515");
            m_InternalAnimations.Add("blowkiss", "db84829b-462c-ee83-1e27-9bbee66bd624");
            m_InternalAnimations.Add("express_bored", "b906c4ba-703b-1940-32a3-0c7f7d791510");
            m_InternalAnimations.Add("bow", "82e99230-c906-1403-4d9c-3889dd98daba");
            m_InternalAnimations.Add("brush", "349a3801-54f9-bf2c-3bd0-1ac89772af01");
            m_InternalAnimations.Add("busy", "efcf670c-2d18-8128-973a-034ebc806b67");
            m_InternalAnimations.Add("clap", "9b0c1c4e-8ac7-7969-1494-28c874c4f668");
            m_InternalAnimations.Add("courtbow", "9ba1c942-08be-e43a-fb29-16ad440efc50");
            m_InternalAnimations.Add("crouch", "201f3fdf-cb1f-dbec-201f-7333e328ae7c");
            m_InternalAnimations.Add("crouchwalk", "47f5f6fb-22e5-ae44-f871-73aaaf4a6022");
            m_InternalAnimations.Add("express_cry", "92624d3e-1068-f1aa-a5ec-8244585193ed");
            m_InternalAnimations.Add("turn_180", "038fcec9-5ebd-8a8e-0e2e-6e71a0a1ac53");
            m_InternalAnimations.Add("turnback_180", "6883a61a-b27b-5914-a61e-dda118a9ee2c");
            m_InternalAnimations.Add("dance1", "b68a3d7c-de9e-fc87-eec8-543d787e5b0d");
            m_InternalAnimations.Add("dance2", "928cae18-e31d-76fd-9cc9-2f55160ff818");
            m_InternalAnimations.Add("dance3", "30047778-10ea-1af7-6881-4db7a3a5a114");
            m_InternalAnimations.Add("dance4", "951469f4-c7b2-c818-9dee-ad7eea8c30b7");
            m_InternalAnimations.Add("dance5", "4bd69a1d-1114-a0b4-625f-84e0a5237155");
            m_InternalAnimations.Add("dance6", "cd28b69b-9c95-bb78-3f94-8d605ff1bb12");
            m_InternalAnimations.Add("dance7", "a54d8ee2-28bb-80a9-7f0c-7afbbe24a5d6");
            m_InternalAnimations.Add("dance8", "b0dc417c-1f11-af36-2e80-7e7489fa7cdc");
            m_InternalAnimations.Add("dead", "57abaae6-1d17-7b1b-5f98-6d11a6411276");
            m_InternalAnimations.Add("drink", "0f86e355-dd31-a61c-fdb0-3a96b9aad05f");
            m_InternalAnimations.Add("express_embarrased", "514af488-9051-044a-b3fc-d4dbf76377c6");
            m_InternalAnimations.Add("express_afraid_emote", "aa2df84d-cf8f-7218-527b-424a52de766e");
            m_InternalAnimations.Add("express_anger_emote", "1a03b575-9634-b62a-5767-3a679e81f4de");
            m_InternalAnimations.Add("express_bored_emote", "214aa6c1-ba6a-4578-f27c-ce7688f61d0d");
            m_InternalAnimations.Add("express_cry_emote", "d535471b-85bf-3b4d-a542-93bea4f59d33");
            m_InternalAnimations.Add("express_disdain", "d4416ff1-09d3-300f-4183-1b68a19b9fc1");
            m_InternalAnimations.Add("express_embarrassed_emote", "0b8c8211-d78c-33e8-fa28-c51a9594e424");
            m_InternalAnimations.Add("express_frown", "fee3df48-fa3d-1015-1e26-a205810e3001");
            m_InternalAnimations.Add("express_kiss", "1e8d90cc-a84e-e135-884c-7c82c8b03a14");
            m_InternalAnimations.Add("express_laugh_emote", "62570842-0950-96f8-341c-809e65110823");
            m_InternalAnimations.Add("express_open_mouth", "d63bc1f9-fc81-9625-a0c6-007176d82eb7");
            m_InternalAnimations.Add("express_repulsed_emote", "f76cda94-41d4-a229-2872-e0296e58afe1");
            m_InternalAnimations.Add("express_sad_emote", "eb6ebfb2-a4b3-a19c-d388-4dd5c03823f7");
            m_InternalAnimations.Add("express_shrug_emote", "a351b1bc-cc94-aac2-7bea-a7e6ebad15ef");
            m_InternalAnimations.Add("express_smile", "b7c7c833-e3d3-c4e3-9fc0-131237446312");
            m_InternalAnimations.Add("express_surprise_emote", "728646d9-cc79-08b2-32d6-937f0a835c24");
            m_InternalAnimations.Add("express_tongue_out", "835965c6-7f2f-bda2-5deb-2478737f91bf");
            m_InternalAnimations.Add("express_toothsmile", "b92ec1a5-e7ce-a76b-2b05-bcdb9311417e");
            m_InternalAnimations.Add("express_wink_emote", "da020525-4d94-59d6-23d7-81fdebf33148");
            m_InternalAnimations.Add("express_worry_emote", "9c05e5c7-6f07-6ca4-ed5a-b230390c3950");
            m_InternalAnimations.Add("falldown", "666307d9-a860-572d-6fd4-c3ab8865c094");
            m_InternalAnimations.Add("female_walk", "f5fc7433-043d-e819-8298-f519a119b688");
            m_InternalAnimations.Add("angry_fingerwag", "c1bc7f36-3ba0-d844-f93c-93be945d644f");
            m_InternalAnimations.Add("fist_pump", "7db00ccd-f380-f3ee-439d-61968ec69c8a");
            m_InternalAnimations.Add("fly", "aec4610c-757f-bc4e-c092-c6e9caf18daf");
            m_InternalAnimations.Add("flyslow", "2b5a38b2-5e00-3a97-a495-4c826bc443e6");
            m_InternalAnimations.Add("hello", "9b29cd61-c45b-5689-ded2-91756b8d76a9");
            m_InternalAnimations.Add("hold_r_bazooka", "ef62d355-c815-4816-2474-b1acc21094a6");
            m_InternalAnimations.Add("hold_l_bow", "8b102617-bcba-037b-86c1-b76219f90c88");
            m_InternalAnimations.Add("hold_r_handgun", "efdc1727-8b8a-c800-4077-975fc27ee2f2");
            m_InternalAnimations.Add("hold_r_rifle", "3d94bad0-c55b-7dcc-8763-033c59405d33");
            m_InternalAnimations.Add("hold_throw_r", "7570c7b5-1f22-56dd-56ef-a9168241bbb6");
            m_InternalAnimations.Add("hover", "4ae8016b-31b9-03bb-c401-b1ea941db41d");
            m_InternalAnimations.Add("hover_down", "20f063ea-8306-2562-0b07-5c853b37b31e");
            m_InternalAnimations.Add("hover_up", "62c5de58-cb33-5743-3d07-9e4cd4352864");
            m_InternalAnimations.Add("impatient", "5ea3991f-c293-392e-6860-91dfa01278a3");
            m_InternalAnimations.Add("jump", "2305bd75-1ca9-b03b-1faa-b176b8a8c49e");
            m_InternalAnimations.Add("jumpforjoy", "709ea28e-1573-c023-8bf8-520c8bc637fa");
            m_InternalAnimations.Add("kissmybutt", "19999406-3a3a-d58c-a2ac-d72e555dcf51");
            m_InternalAnimations.Add("land", "7a17b059-12b2-41b1-570a-186368b6aa6f");
            m_InternalAnimations.Add("laugh_short", "ca5b3f14-3194-7a2b-c894-aa699b718d1f");
            m_InternalAnimations.Add("soft_land", "f4f00d6e-b9fe-9292-f4cb-0ae06ea58d57");
            m_InternalAnimations.Add("motorcycle_sit", "08464f78-3a8e-2944-cba5-0c94aff3af29");
            m_InternalAnimations.Add("musclebeach", "315c3a41-a5f3-0ba4-27da-f893f769e69b");
            m_InternalAnimations.Add("no_head", "5a977ed9-7f72-44e9-4c4c-6e913df8ae74");
            m_InternalAnimations.Add("no_unhappy", "d83fa0e5-97ed-7eb2-e798-7bd006215cb4");
            m_InternalAnimations.Add("nyanya", "f061723d-0a18-754f-66ee-29a44795a32f");
            m_InternalAnimations.Add("punch_onetwo", "eefc79be-daae-a239-8c04-890f5d23654a");
            m_InternalAnimations.Add("peace", "b312b10e-65ab-a0a4-8b3c-1326ea8e3ed9");
            m_InternalAnimations.Add("point_me", "17c024cc-eef2-f6a0-3527-9869876d7752");
            m_InternalAnimations.Add("point_you", "ec952cca-61ef-aa3b-2789-4d1344f016de");
            m_InternalAnimations.Add("prejump", "7a4e87fe-de39-6fcb-6223-024b00893244");
            m_InternalAnimations.Add("punch_l", "f3300ad9-3462-1d07-2044-0fef80062da0");
            m_InternalAnimations.Add("punch_r", "c8e42d32-7310-6906-c903-cab5d4a34656");
            m_InternalAnimations.Add("express_repulsed", "36f81a92-f076-5893-dc4b-7c3795e487cf");
            m_InternalAnimations.Add("kick_roundhouse_r", "49aea43b-5ac3-8a44-b595-96100af0beda");
            m_InternalAnimations.Add("rps_countdown", "35db4f7e-28c2-6679-cea9-3ee108f7fc7f");
            m_InternalAnimations.Add("rps_paper", "0836b67f-7f7b-f37b-c00a-460dc1521f5a");
            m_InternalAnimations.Add("rps_rock", "42dd95d5-0bc6-6392-f650-777304946c0f");
            m_InternalAnimations.Add("rps_scissors", "16803a9f-5140-e042-4d7b-d28ba247c325");
            m_InternalAnimations.Add("run", "05ddbff8-aaa9-92a1-2b74-8fe77a29b445");
            m_InternalAnimations.Add("express_sad", "0eb702e2-cc5a-9a88-56a5-661a55c0676a");
            m_InternalAnimations.Add("salute", "cd7668a6-7011-d7e2-ead8-fc69eff1a104");
            m_InternalAnimations.Add("shoot_l_bow", "e04d450d-fdb5-0432-fd68-818aaf5935f8");
            m_InternalAnimations.Add("shout", "6bd01860-4ebd-127a-bb3d-d1427e8e0c42");
            m_InternalAnimations.Add("express_shrug", "70ea714f-3a97-d742-1b01-590a8fcd1db5");
            m_InternalAnimations.Add("sit", "1a5fe8ac-a804-8a5d-7cbd-56bd83184568");
            m_InternalAnimations.Add("sit_female", "b1709c8d-ecd3-54a1-4f28-d55ac0840782");
            m_InternalAnimations.Add("sit_ground", "1c7600d6-661f-b87b-efe2-d7421eb93c86");
            m_InternalAnimations.Add("sit_ground_constrained", "1a2bd58e-87ff-0df8-0b4c-53e047b0bb6e");
            m_InternalAnimations.Add("sit_generic", "245f3c54-f1c0-bf2e-811f-46d8eeb386e7");
            m_InternalAnimations.Add("sit_to_stand", "a8dee56f-2eae-9e7a-05a2-6fb92b97e21e");
            m_InternalAnimations.Add("sleep", "f2bed5f9-9d44-39af-b0cd-257b2a17fe40");
            m_InternalAnimations.Add("smoke_idle", "d2f2ee58-8ad1-06c9-d8d3-3827ba31567a");
            m_InternalAnimations.Add("smoke_inhale", "6802d553-49da-0778-9f85-1599a2266526");
            m_InternalAnimations.Add("smoke_throw_down", "0a9fb970-8b44-9114-d3a9-bf69cfe804d6");
            m_InternalAnimations.Add("snapshot", "eae8905b-271a-99e2-4c0e-31106afd100c");
            m_InternalAnimations.Add("stand", "2408fe9e-df1d-1d7d-f4ff-1384fa7b350f");
            m_InternalAnimations.Add("standup", "3da1d753-028a-5446-24f3-9c9b856d9422");
            m_InternalAnimations.Add("stand_1", "15468e00-3400-bb66-cecc-646d7c14458e");
            m_InternalAnimations.Add("stand_2", "370f3a20-6ca6-9971-848c-9a01bc42ae3c");
            m_InternalAnimations.Add("stand_3", "42b46214-4b44-79ae-deb8-0df61424ff4b");
            m_InternalAnimations.Add("stand_4", "f22fed8b-a5ed-2c93-64d5-bdd8b93c889f");
            m_InternalAnimations.Add("stretch", "80700431-74ec-a008-14f8-77575e73693f");
            m_InternalAnimations.Add("stride", "1cb562b0-ba21-2202-efb3-30f82cdf9595");
            m_InternalAnimations.Add("surf", "41426836-7437-7e89-025d-0aa4d10f1d69");
            m_InternalAnimations.Add("express_surprise", "313b9881-4302-73c0-c7d0-0e7a36b6c224");
            m_InternalAnimations.Add("sword_strike_r", "85428680-6bf9-3e64-b489-6f81087c24bd");
            m_InternalAnimations.Add("talk", "5c682a95-6da4-a463-0bf6-0f5b7be129d1");
            m_InternalAnimations.Add("angry_tantrum", "11000694-3f41-adc2-606b-eee1d66f3724");
            m_InternalAnimations.Add("throw_r", "aa134404-7dac-7aca-2cba-435f9db875ca");
            m_InternalAnimations.Add("tryon_shirt", "83ff59fe-2346-f236-9009-4e3608af64c1");
            m_InternalAnimations.Add("turnleft", "56e0ba0d-4a9f-7f27-6117-32f2ebbf6135");
            m_InternalAnimations.Add("turnright", "2d6daa51-3192-6794-8e2e-a15f8338ec30");
            m_InternalAnimations.Add("type", "c541c47f-e0c0-058b-ad1a-d6ae3a4584d9");
            m_InternalAnimations.Add("walk", "6ed24bd8-91aa-4b12-ccc7-c97c857ab4e0");
            m_InternalAnimations.Add("whisper", "7693f268-06c7-ea71-fa21-2b30d6533f8f");
            m_InternalAnimations.Add("whistle", "b1ed7982-c68e-a982-7561-52a88a5298c0");
            m_InternalAnimations.Add("express_wink", "869ecdad-a44b-671e-3266-56aef2e3ac2e");
            m_InternalAnimations.Add("wink_hollywood", "c0c4030f-c02b-49de-24ba-2331f43fe41c");
            m_InternalAnimations.Add("express_worry", "9f496bd2-589a-709f-16cc-69bf7df1d36c");
            m_InternalAnimations.Add("yes_head", "15dd911d-be82-2856-26db-27659b142875");
            m_InternalAnimations.Add("yes_happy", "b8c8b2a3-9008-1771-3bfc-90924955ab2d");
            m_InternalAnimations.Add("yoga_float", "42ecd00b-9947-a97c-400a-bbc9174c7aeb");
        }

        public static bool TryGetBinaryMask(this int constant, out int mask)
        {
            mask = 0;
            if (constant != 0)
            {
                while ((constant & 1) == 0)
                {
                    mask = (mask << 1) | 1;
                    constant >>= 1;
                }
            }
            return constant == 1 && mask != -2147483648;
        }

        public static bool TryGetBinaryMask(this long constant, out long mask)
        {
            mask = 0;
            if (constant != 0)
            {
                while ((constant & 1) == 0)
                {
                    mask = (mask << 1) | 1;
                    constant >>= 1;
                }
            }
            return constant == 1;
        }

        public static bool FillDetectInfoFromObject(ref DetectInfo detectInfo, IObject obj)
        {
            var agent = obj as IAgent;
            if (agent != null)
            {
                detectInfo.ObjType = agent.DetectedType;
                detectInfo.Name = agent.Name;
                detectInfo.Key = agent.ID;
                detectInfo.Position = agent.GlobalPosition;
                detectInfo.Rotation = agent.GlobalRotation;
                detectInfo.Group = agent.Group;
                detectInfo.Velocity = agent.Velocity;
                return true;
            }

            var grp = obj as ObjectGroup;
            if (grp != null)
            {
                detectInfo.ObjType = obj.DetectedType;
                detectInfo.Name = grp.Name;
                detectInfo.Key = grp.ID;
                detectInfo.Position = grp.GlobalPosition;
                detectInfo.Rotation = grp.GlobalRotation;
                detectInfo.Group = grp.Group;
                detectInfo.Velocity = grp.Velocity;
                return true;
            }

            var part = obj as ObjectPart;
            if (part != null)
            {
                detectInfo.ObjType = part.DetectedType;
                detectInfo.Name = part.Name;
                detectInfo.Key = part.ID;
                detectInfo.Position = part.GlobalPosition;
                detectInfo.Rotation = part.GlobalRotation;
                detectInfo.Group = part.Group;
                detectInfo.Velocity = part.Velocity;
                return true;
            }
            return false;
        }

        public static int ToLSLBoolean(this bool v) => v ? 1 : 0;

        public static object ReadTypedValue(this XmlTextReader reader)
        {
            string type = string.Empty;
            bool isEmptyElement = reader.IsEmptyElement;
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    switch (reader.Name)
                    {
                        case "type":
                            type = reader.Value;
                            break;

                        default:
                            break;
                    }
                } while (reader.MoveToNextAttribute());
            }

            string data = string.Empty;
            if (!isEmptyElement)
            {
                data = reader.ReadElementValueAsString("ListItem");
            }
            switch(type)
            {
                case "System.Boolean":
                    return bool.Parse(data);

                case "System.Single":
                case "System.Double":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                    return double.Parse(data, NumberStyles.Float, CultureInfo.InvariantCulture);

                case "System.String":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                    return data;

                case "System.Int32":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                    return int.Parse(data);

                case "System.Int64":
                    return long.Parse(data);

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                    return Vector3.Parse(data);

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                    return Quaternion.Parse(data);

                case "OpenMetaverse.UUID":
                    return UUID.Parse(data);

                default:
                    throw new ArgumentException("Unknown type \"" + type + "\" in serialization");
            }
        }

        public static void WriteTypedValue(this XmlTextWriter writer, string tagname, object o)
        {
            if(o is bool)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Boolean");
                writer.WriteValue(o.ToString());
            }
            else if(o is double)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Single");
                writer.WriteValue(((float)(double)o).ToString(CultureInfo.InvariantCulture));
            }
            else if(o is string)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.String");
                writer.WriteValue(o.ToString());
            }
            else if(o is int)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Int32");
                writer.WriteValue(o.ToString());
            }
            else if (o is long)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Int64");
                writer.WriteValue(o.ToString());
            }
            else if(o is Vector3)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                writer.WriteValue(o.ToString());
            }
            else if(o is Quaternion)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                writer.WriteValue(o.ToString());
            }
            else if (o is UUID)
            {
                writer.WriteStartElement(tagname);
                /* people want compatibility with their creations so we have to understand to what
                 * these types map in the OpenSim serialization.
                 */
                writer.WriteAttributeString("type", "OpenMetaverse.UUID");
                writer.WriteValue(o.ToString());
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(o));
            }
            writer.WriteEndElement();
        }

        public static string FindInventoryName(this ScriptInstance instance, AssetType assetType, UUID assetID)
        {
            if (assetID != UUID.Zero)
            {
                foreach (ObjectPartInventoryItem item in instance.Part.Inventory.Values)
                {
                    if (item.AssetType == assetType && item.AssetID == assetID)
                    {
                        return item.Name;
                    }
                }
            }
            return assetID.ToString();
        }

        public static bool IsInternalAnimationID(this UUID id) => m_InternalAnimations.ContainsValue(id);

        public static UUID GetAnimationAssetID(this ScriptInstance instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                bool checkViewerBuiltin = true;
                /* must be an inventory item */
                lock (instance)
                {
                    ObjectPartInventoryItem i;
                    if (!instance.Part.Inventory.TryGetValue(item, out i))
                    {
                        /* intentionally left empty */
                    }
                    else if (i.InventoryType != InventoryType.Animation)
                    {
                        throw new LocalizedScriptErrorException(instance, "InventoryItem0IsNotAnAnimation", "Inventory item {0} is not an animation", item);
                    }
                    else
                    {
                        assetID = i.AssetID;
                        checkViewerBuiltin = false;
                    }
                }

                if (checkViewerBuiltin && !m_InternalAnimations.TryGetValue(item, out assetID))
                {
                    throw new LocalizedScriptErrorException(instance, "InventoryItem0NotFound", "Inventory item {0} not found", item);
                }
            }
            return assetID;
        }

        public static UUID GetLandmarkAssetID(this ScriptInstance instance, string item)
        {
            UUID assetID;
            /* must be an inventory item */
            lock (instance)
            {
                ObjectPartInventoryItem i;
                if (instance.Part.Inventory.TryGetValue(item, out i))
                {
                    if (i.InventoryType != InventoryType.Landmark)
                    {
                        throw new LocalizedScriptErrorException(instance, "InventoryItem0IsNotALandmark", "Inventory item {0} is not a landmark", item);
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(instance, "InventoryItem0NotFound", "Inventory item {0} not found", item);
                }
                assetID = i.AssetID;
            }
            return assetID;
        }

        public static UUID GetNotecardAssetID(this ScriptInstance instance, string item)
        {
            UUID assetID;
            /* must be an inventory item */
            lock (instance)
            {
                ObjectPartInventoryItem i;
                if (instance.Part.Inventory.TryGetValue(item, out i))
                {
                    if (i.InventoryType != InventoryType.Notecard)
                    {
                        throw new LocalizedScriptErrorException(instance, "InventoryItem0IsNotANotecard", "Inventory item {0} is not a notecard", item);
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(instance, "InventoryItem0NotFound", "Inventory item {0} not found", item);
                }
                assetID = i.AssetID;
            }
            return assetID;
        }

        public static UUID GetSoundAssetID(this ScriptInstance instance, string item, int inventorylink = PrimitiveApi.LINK_THIS)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                /* must be an inventory item */
                lock (instance)
                {
                    ObjectPartInventoryItem i;
                    ObjectPart part = instance.Part;
                    if(inventorylink != PrimitiveApi.LINK_THIS)
                    {
                        if(inventorylink == PrimitiveApi.LINK_UNLINKED_ROOT)
                        {
                            inventorylink = PrimitiveApi.LINK_ROOT;
                        }

                        if(!part.ObjectGroup.TryGetValue(inventorylink, out part))
                        {
                            throw new LocalizedScriptErrorException(instance, "InventoryItem0IsNotASoundUnknownLink1", "Inventory item {0} is not a sound due to unknown link {1}.", item, inventorylink);
                        }
                    }

                    if (instance.Part.Inventory.TryGetValue(item, out i))
                    {
                        if (i.InventoryType != InventoryType.Sound)
                        {
                            throw new LocalizedScriptErrorException(instance, "InventoryItem0IsNotASound", "Inventory item {0} is not a sound", item);
                        }
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(instance, "InventoryItem0NotFound", "Inventory item {0} not found", item);
                    }
                    assetID = i.AssetID;
                }
            }
            return assetID;
        }

        public static UUID GetTextureAssetID(this ScriptInstance instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                /* must be an inventory item */
                lock (instance)
                {
                    ObjectPartInventoryItem i;
                    if (instance.Part.Inventory.TryGetValue(item, out i))
                    {
                        if (i.InventoryType != InventoryType.Texture)
                        {
                            throw new LocalizedScriptErrorException(instance, "InventoryItem0IsNotATexture", "Inventory item {0} is not a texture", item);
                        }
                        assetID = i.AssetID;
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(instance, "InventoryItem0NotFound", "Inventory item {0} not found", item);
                    }
                }
            }
            else
            {
                lock (instance)
                {
                    ObjectGroup grp = instance.Part.ObjectGroup;
                    SceneInterface scene = grp.Scene;
                    if (grp.IsAttached && !scene.AssetService.Exists(assetID))
                    {
                        IAgent agent;
                        if (scene.RootAgents.TryGetValue(grp.Owner.ID, out agent))
                        {
                            AssetData data;
                            if (agent.AssetService.TryGetValue(assetID, out data))
                            {
                                scene.AssetService.Store(data);
                            }
                        }
                    }
                }
            }
            return assetID;
        }

        internal static PropertyInfo GetMemberProperty(this Type t, LSLCompiler.CompileState cs, string name)
        {
            try
            {
                return t.GetProperty(name);
            }
            catch(AmbiguousMatchException)
            {
                PropertyInfo pInfo = null;
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.Name != name)
                    {
                        continue;
                    }

                    if (cs.IsValidType(prop.PropertyType))
                    {
                        if(pInfo != null)
                        {
                            throw new AmbiguousMatchException();
                        }
                        pInfo = prop;
                    }
                }
                return pInfo;
            }
        }

        public static long LslConvertToLong(this IValue iv)
        {
            if (iv is Real)
            {
                return LSLCompiler.ConvToLong((Real)iv);
            }
            else if (iv is AString || iv is LSLKey)
            {
                return LSLCompiler.ConvToLong(iv.ToString());
            }
            else
            {
                try
                {
                    return iv.AsLong;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static int LslConvertToInt(this IValue iv)
        {
            if (iv is Real)
            {
                return LSLCompiler.ConvToInt((Real)iv);
            }
            else if (iv is AString || iv is LSLKey)
            {
                return LSLCompiler.ConvToInt(iv.ToString());
            }
            else
            {
                try
                {
                    return iv.AsInteger;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static LSLKey LslConvertToKey(this IValue val, bool singlePrecision = false)
        {
            Type t = val.GetType();
            if (t == typeof(Real))
            {
                return singlePrecision ?
                    LSLCompiler.SinglePrecision.TypecastFloatToString(val.AsReal) :
                    LSLCompiler.TypecastDoubleToString(val.AsReal);
            }
            else if (t == typeof(Vector3))
            {
                return singlePrecision ?
                    LSLCompiler.SinglePrecision.TypecastVectorToString6Places((Vector3)val) :
                    LSLCompiler.TypecastVectorToString6Places((Vector3)val);
            }
            else if (t == typeof(Quaternion))
            {
                return singlePrecision ?
                    LSLCompiler.SinglePrecision.TypecastRotationToString6Places((Quaternion)val) :
                    LSLCompiler.TypecastRotationToString6Places((Quaternion)val);
            }
            else
            {
                return val.ToString();
            }
        }

        public static Quaternion LslConvertToRot(this IValue iv)
        {
            try
            {
                return iv.AsQuaternion;
            }
            catch
            {
                return Quaternion.Identity;
            }
        }

        public static Vector3 LslConvertToVector(this IValue iv)
        {
            try
            {
                return iv.AsVector3;
            }
            catch
            {
                return Vector3.Zero;
            }
        }

        public static string LslConvertToString(this IValue val, bool singlePrecision = false)
        {
            Type t = val.GetType();
            if (t == typeof(Real))
            {
                return singlePrecision ?
                    LSLCompiler.SinglePrecision.TypecastFloatToString(val.AsReal) :
                    LSLCompiler.TypecastDoubleToString(val.AsReal);
            }
            else if (t == typeof(Vector3))
            {
                return singlePrecision ?
                    LSLCompiler.SinglePrecision.TypecastVectorToString6Places((Vector3)val) :
                    LSLCompiler.TypecastVectorToString6Places((Vector3)val);
            }
            else if (t == typeof(Quaternion))
            {
                return singlePrecision ?
                    LSLCompiler.SinglePrecision.TypecastRotationToString6Places((Quaternion)val) :
                    LSLCompiler.TypecastRotationToString6Places((Quaternion)val);
            }
            else
            {
                return val.ToString();
            }
        }

        public static double LslConvertToFloat(this IValue iv)
        {
            try
            {
                return iv.AsReal;
            }
            catch
            {
                return 0;
            }
        }

        public static bool TryGetLink(this ScriptInstance instance, int link, out ObjectPart linkedpart)
        {
            ObjectPart part = instance.Part;
            ObjectGroup grp = part.ObjectGroup;
            if (PrimitiveApi.LINK_THIS == link)
            {
                linkedpart = part;
                return true;
            }
            else if (link == PrimitiveApi.LINK_ROOT || link == PrimitiveApi.LINK_UNLINKED_ROOT)
            {
                linkedpart = grp.RootPart;
                return true;
            }
            else
            {
                return grp.TryGetValue(link, out linkedpart);
            }
        }


        public static List<ObjectPart> GetLinkTargets(this ScriptInstance instance, int link)
        {
            var list = new List<ObjectPart>();
            ObjectPart thisPart = instance.Part;
            ObjectGroup thisGroup = thisPart.ObjectGroup;
            switch(link)
            {
                case PrimitiveApi.LINK_THIS:
                    list.Add(thisPart);
                    break;

                case PrimitiveApi.LINK_UNLINKED_ROOT:
                case PrimitiveApi.LINK_ROOT:
                    list.Add(thisGroup.RootPart);
                    break;

                case PrimitiveApi.LINK_SET:
                    list.AddRange(thisGroup.Values);
                    break;

                case PrimitiveApi.LINK_ALL_OTHERS:
                    foreach (ObjectPart part in thisGroup.Values)
                    {
                        if (part != instance.Part)
                        {
                            list.Add(part);
                        }
                    }
                    break;

                default:
                    list.Add(thisGroup[link]);
                    break;
            }

            return list;
        }
    }
}
