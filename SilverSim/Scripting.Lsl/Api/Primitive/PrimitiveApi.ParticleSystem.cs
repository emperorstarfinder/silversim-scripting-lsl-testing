﻿// SilverSim is distributed under the terms of the
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

#pragma warning disable IDE0018, RCS1029

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Api.Primitive.Properties;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_INTERP_COLOR_MASK = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_INTERP_SCALE_MASK = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BOUNCE_MASK = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_WIND_MASK = 8;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_FOLLOW_SRC_MASK = 16;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_FOLLOW_VELOCITY_MASK = 32;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_TARGET_POS_MASK = 64;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_TARGET_LINEAR_MASK = 128;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_EMISSIVE_MASK = 256;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_RIBBON_MASK = 1024;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_FLAGS = 0;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_COLOR = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_ALPHA = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_COLOR = 3;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_ALPHA = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_SCALE = 5;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_SCALE = 6;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_MAX_AGE = 7;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_ACCEL = 8;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN = 9;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_INNERANGLE = 10;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_OUTERANGLE = 11;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_TEXTURE = 12;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_RATE = 13;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_PART_COUNT = 15;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_RADIUS = 16;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_SPEED_MIN = 17;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_BURST_SPEED_MAX = 18;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_MAX_AGE = 19;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_TARGET_KEY = 20;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_OMEGA = 21;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_ANGLE_BEGIN = 22;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_ANGLE_END = 23;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BLEND_FUNC_SOURCE = 24;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BLEND_FUNC_DEST = 25;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_START_GLOW = 26;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_END_GLOW = 27;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ZERO = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_DEST_COLOR = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_SOURCE_COLOR = 3;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE_MINUS_DEST_COLOR = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE_MINUS_SOURCE_COLOR = 5;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_SOURCE_ALPHA = 7;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_PART_BF_ONE_MINUS_SOURCE_ALPHA = 9;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_DROP = 1;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_EXPLODE = 2;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_ANGLE = 4;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_ANGLE_CONE = 8;
        [APILevel(APIFlags.LSL)]
        public const int PSYS_SRC_PATTERN_ANGLE_CONE_EMPTY = 16;

        private static float ValidParticleScale(double value)
        {
            return (float)value.Clamp(0f, 4f);
        }

        [APILevel(APIFlags.ASSL, "asClearAllParticleSystems")]
        public void ClearAllParticleSystems(ScriptInstance instance) =>
            ClearAllParticleSystems(instance, LINK_THIS);

        [APILevel(APIFlags.ASSL, "asLinkClearAllParticleSystems")]
        public void ClearAllParticleSystems(ScriptInstance instance, int link)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    foreach(ObjectPartLocalizedInfo localization in part.NamedLocalizations)
                    {
                        localization.ParticleSystem = null;
                    }
                    part.ParticleSystem = null;
                }
            }
        }

        [APIExtension(APIExtension.Properties, "llLinkParticleSystem")]
        [APIExtension(APIExtension.Properties, "asLinkParticleSystem")]
        public void LinkParticleSystem(ScriptInstance instance, int link, ParticleProperties.ParticleSystemData data) =>
            LinkParticleSystem(instance, link, (AnArray)data);

        [APIExtension(APIExtension.Properties, "asLinkParticleSystem")]
        public void LinkParticleSystem(ScriptInstance instance, int link, string langname, ParticleProperties.ParticleSystemData data) =>
            LinkParticleSystem(instance, link, langname, (AnArray)data);

        [APILevel(APIFlags.LSL, "llLinkParticleSystem")]
        [APILevel(APIFlags.ASSL, "asLinkParticleSystem")]
        public void LinkParticleSystem(ScriptInstance instance, int link, AnArray rules) =>
            LinkParticleSystem(instance, link, string.Empty, rules);

        [APILevel(APIFlags.ASSL, "asLinkParticleSystem")]
        public void LinkParticleSystem(ScriptInstance instance, int link, string langname, AnArray rules)
        {
            ParticleSystem ps = null;

            float tempf = 0;
            int tmpi = 0;
            Vector3 tempv;

            if(rules.Count != 0)
            {
                ps = new ParticleSystem();
            }

            for (int i = 0; i < rules.Count; i += 2)
            {
                int psystype;
                try
                {
                    psystype = rules[i].AsInteger;
                }
                catch (InvalidCastException)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InvalidTypeParameterAtIndex0", "Invalid type parameter at index {0}", i));
                    return;
                }
                IValue value = rules[i + 1];
                switch (psystype)
                {
                    case PSYS_PART_FLAGS:
                        try
                        {
                            ps.PartDataFlags = (ParticleSystem.ParticleDataFlags)value.AsUInt;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_FLAGS", i + 1, "integer"));
                            return;
                        }
                        break;

                    case PSYS_PART_START_COLOR:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_START_COLOR", i + 1, "vector"));
                            return;
                        }
                        ps.PartStartColor.R = tempv.X;
                        ps.PartStartColor.G = tempv.Y;
                        ps.PartStartColor.B = tempv.Z;
                        break;

                    case PSYS_PART_START_ALPHA:
                        try
                        {
                            ps.PartStartColor.A = value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_START_ALPHA", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_PART_END_COLOR:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_END_COLOR", i + 1, "vector"));
                            return;
                        }
                        ps.PartEndColor.R = tempv.X;
                        ps.PartEndColor.G = tempv.Y;
                        ps.PartEndColor.B = tempv.Z;
                        break;

                    case PSYS_PART_END_ALPHA:
                        try
                        {
                            ps.PartEndColor.A = value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_END_ALPHA", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_PART_START_SCALE:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_START_SCALE", i + 1, "vector"));
                            return;
                        }
                        ps.PartStartScaleX = ValidParticleScale(tempv.X);
                        ps.PartStartScaleY = ValidParticleScale(tempv.Y);
                        break;

                    case PSYS_PART_END_SCALE:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_END_SCALE", i + 1, "vector"));
                            return;
                        }
                        ps.PartEndScaleX = ValidParticleScale(tempv.X);
                        ps.PartEndScaleY = ValidParticleScale(tempv.Y);
                        break;

                    case PSYS_PART_MAX_AGE:
                        try
                        {
                            ps.PartMaxAge = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_MAX_AGE", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_SRC_ACCEL:
                        try
                        {
                            tempv = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_ACCEL", i + 1, "vector"));
                            return;
                        }
                        ps.PartAcceleration.X = tempv.X;
                        ps.PartAcceleration.Y = tempv.Y;
                        ps.PartAcceleration.Z = tempv.Z;
                        break;

                    case PSYS_SRC_PATTERN:
                        try
                        {
                            ps.Pattern = (ParticleSystem.SourcePattern)value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_PATTERN", i + 1, "integer"));
                            return;
                        }
                        break;

                    // PSYS_SRC_INNERANGLE and PSYS_SRC_ANGLE_BEGIN use the same variables. The
                    // PSYS_SRC_OUTERANGLE and PSYS_SRC_ANGLE_END also use the same variable. The
                    // client tells the difference between the two by looking at the 0x02 bit in
                    // the PartFlags variable.
                    case PSYS_SRC_INNERANGLE:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_INNERANGLE", i + 1, "float"));
                            return;
                        }
                        ps.InnerAngle = tempf;
                        ps.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                        break;

                    case PSYS_SRC_OUTERANGLE:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_OUTERANGLE", i + 1, "float"));
                            return;
                        }
                        ps.OuterAngle = tempf;
                        ps.PartFlags &= 0xFFFFFFFD; // Make sure new angle format is off.
                        break;

                    case PSYS_PART_BLEND_FUNC_SOURCE:
                        try
                        {
                            tmpi = value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_BLEND_FUNC_SOURCE", i + 1, "integer"));
                            return;
                        }
                        ps.BlendFuncSource = (ParticleSystem.BlendFunc)tmpi;
                        break;

                    case PSYS_PART_BLEND_FUNC_DEST:
                        try
                        {
                            tmpi = value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_BLEND_FUNC_DEST", i + 1, "integer"));
                            return;
                        }
                        ps.BlendFuncDest = (ParticleSystem.BlendFunc)tmpi;
                        break;

                    case PSYS_PART_START_GLOW:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_START_GLOW", i + 1, "float"));
                            return;
                        }
                        ps.PartStartGlow = tempf;
                        break;

                    case PSYS_PART_END_GLOW:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_PART_END_GLOW", i + 1, "float"));
                            return;
                        }
                        ps.PartEndGlow = tempf;
                        break;

                    case PSYS_SRC_TEXTURE:
                        string textureName = value.ToString();
                        if (string.IsNullOrEmpty(textureName))
                        {
                            ps.Texture = UUID.Zero;
                        }
                        else
                        {
                            try
                            {
                                ps.Texture = instance.GetTextureAssetID(textureName);
                            }
                            catch (InvalidOperationException)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "ParameterFor0AtIndex1MustBeReferingATexture", "PSYS_SRC_TEXTURE", i + 1));
                                return;
                            }
                            catch (InvalidCastException)
                            {
                                instance.ShoutError(new LocalizedScriptMessage(this, "ParameterFor0AtIndex1MustBeReferingATexture", "PSYS_SRC_TEXTURE", i + 1));
                                return;
                            }
                        }
                        break;

                    case PSYS_SRC_BURST_RATE:
                        try
                        {
                            ps.BurstRate = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_BURST_RATE", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_PART_COUNT:
                        try
                        {
                            ps.BurstPartCount = (byte)value.AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_BURST_PART_COUNT", i + 1, "integer"));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_RADIUS:
                        try
                        {
                            ps.BurstRadius = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_BURST_RADIUS", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_SPEED_MIN:
                        try
                        {
                            ps.BurstSpeedMin = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_BURST_SPEED_MIN", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_SRC_BURST_SPEED_MAX:
                        try
                        {
                            ps.BurstSpeedMax = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_BURST_SPEED_MAX", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_SRC_MAX_AGE:
                        try
                        {
                            ps.MaxAge = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_MAX_AGE", i + 1, "float"));
                            return;
                        }
                        break;

                    case PSYS_SRC_TARGET_KEY:
                        UUID key = UUID.Zero;
                        ps.Target = (UUID.TryParse(value.ToString(), out key)) ?
                            key :
                            instance.Part.ID;
                        break;

                    case PSYS_SRC_OMEGA:
                        try
                        {
                            ps.AngularVelocity = value.AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_OMEGA", i + 1, "vector"));
                            return;
                        }
                        break;

                    case PSYS_SRC_ANGLE_BEGIN:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_ANGLE_BEGIN", i + 1, "float"));
                            return;
                        }
                        ps.InnerAngle = tempf;
                        ps.PartFlags |= 0x02; // Set new angle format.
                        break;

                    case PSYS_SRC_ANGLE_END:
                        try
                        {
                            tempf = (float)value.AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InvalidParameterFor0At1MustBeType2", "Invalid parameter for {0} at index {1}. It must be {2}.", "PSYS_SRC_ANGLE_END", i + 1, "float"));
                            return;
                        }
                        ps.OuterAngle = tempf;
                        ps.PartFlags |= 0x02; // Set new angle format.
                        break;

                    default:
                        instance.ShoutError(new LocalizedScriptMessage(this, "UnknownTypeParameter1AtIndex0", "Unknown type parameter {1} at index {0}", i, psystype));
                        return;
                }
            }
            if (ps != null)
            {
                ps.CRC = 1;
            }

            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    if (!string.IsNullOrEmpty(langname))
                    {
                        part.GetOrCreateLocalization(langname).ParticleSystem = ps;
                    }
                    else
                    {
                        part.ParticleSystem = ps;
                    }
                }
            }
        }

        [APIExtension(APIExtension.Properties, "llParticleSystem")]
        [APIExtension(APIExtension.Properties, "asParticleSystem")]
        public void ParticleSystem(ScriptInstance instance, ParticleProperties.ParticleSystemData data) =>
            ParticleSystem(instance, (AnArray)data);

        [APIExtension(APIExtension.Properties, "asParticleSystem")]
        public void ParticleSystem(ScriptInstance instance, string langname, ParticleProperties.ParticleSystemData data) =>
            ParticleSystem(instance, langname, (AnArray)data);

        [APILevel(APIFlags.LSL, "llParticleSystem")]
        [APILevel(APIFlags.ASSL, "asParticleSystem")]
        public void ParticleSystem(ScriptInstance instance, AnArray rules)
        {
            LinkParticleSystem(instance, LINK_THIS, rules);
        }

        [APILevel(APIFlags.ASSL, "asParticleSystem")]
        public void ParticleSystem(ScriptInstance instance, string langname, AnArray rules)
        {
            LinkParticleSystem(instance, LINK_THIS, langname, rules);
        }

        [APILevel(APIFlags.LSL, "llMakeExplosion")]
        [ForcedSleep(0.1)]
        public void MakeExplosion(ScriptInstance instance, int particles, double scale, double vel, double lifetime, double arc, string texture, Vector3 offset) =>
            /* except offset all are used as given in SL wiki for emulation */
            ParticleSystem(instance, new AnArray
            {
                { PSYS_PART_FLAGS }, { PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_EMISSIVE_MASK | PSYS_PART_WIND_MASK },
                { PSYS_SRC_PATTERN }, { PSYS_SRC_PATTERN_ANGLE_CONE },
                { PSYS_PART_START_COLOR }, { Vector3.One },
                { PSYS_PART_END_COLOR }, { Vector3.One },
                { PSYS_PART_START_ALPHA }, { 0.50 },
                { PSYS_PART_END_ALPHA }, { 0.25 },
                { PSYS_PART_START_SCALE }, { new Vector3(scale, scale, 0.0) },
                { PSYS_PART_END_SCALE }, { new Vector3(scale * 2 + lifetime, scale * 2 + lifetime, 0.0) },
                { PSYS_PART_MAX_AGE }, { lifetime },
                { PSYS_SRC_ACCEL }, { Vector3.Zero },
                { PSYS_SRC_TEXTURE }, { texture },
                { PSYS_SRC_BURST_RATE }, { 1.0 },
                { PSYS_SRC_ANGLE_BEGIN }, { 0.0 },
                { PSYS_SRC_ANGLE_END }, { arc * Math.PI },
                { PSYS_SRC_BURST_PART_COUNT }, { particles / 2 },
                { PSYS_SRC_BURST_RADIUS }, { 0.0 },
                { PSYS_SRC_BURST_SPEED_MIN }, { vel / 3 },
                { PSYS_SRC_BURST_SPEED_MAX }, { vel * 2/3 },
                { PSYS_SRC_MAX_AGE }, { lifetime / 2 },
                { PSYS_SRC_OMEGA },{Vector3.Zero }
            });

        [APILevel(APIFlags.LSL, "llMakeFountain")]
        [ForcedSleep(0.1)]
        public void MakeFountain(ScriptInstance instance, int particles, double scale, double vel, double lifetime, double arc, int bounce, string texture, Vector3 offset, double bounce_offset) =>
            /* bounce, bounce_offset and local_offset is ignored. All other parameters are emulated according to SL wiki */
            ParticleSystem(instance, new AnArray
            {
                { PSYS_PART_FLAGS }, { PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_WIND_MASK | PSYS_PART_BOUNCE_MASK | PSYS_PART_EMISSIVE_MASK },
                { PSYS_SRC_PATTERN }, { PSYS_SRC_PATTERN_ANGLE_CONE },
                { PSYS_PART_START_COLOR }, { Vector3.One },
                { PSYS_PART_END_COLOR }, { Vector3.One },
                { PSYS_PART_START_ALPHA }, { 0.50 },
                { PSYS_PART_END_ALPHA }, { 0.25 },
                { PSYS_PART_START_SCALE }, { new Vector3(scale/1.5, scale/1.5, 0.0) },
                { PSYS_PART_END_SCALE }, { Vector3.Zero },
                { PSYS_PART_MAX_AGE }, { 3.0 },
                { PSYS_SRC_ACCEL }, { new Vector3(1.0, 0.0, -4) },
                { PSYS_SRC_TEXTURE }, { texture },
                { PSYS_SRC_BURST_RATE }, { 5/particles },
                { PSYS_SRC_ANGLE_BEGIN }, { 0.0 },
                { PSYS_SRC_ANGLE_END }, { arc * Math.PI },
                { PSYS_SRC_BURST_PART_COUNT }, { 1 },
                { PSYS_SRC_BURST_RADIUS }, { 0.0 },
                { PSYS_SRC_BURST_SPEED_MIN }, { vel },
                { PSYS_SRC_BURST_SPEED_MAX }, { vel },
                { PSYS_SRC_MAX_AGE }, { lifetime/2 },
                { PSYS_SRC_OMEGA }, { Vector3.Zero }
            });

        [APILevel(APIFlags.LSL, "llMakeSmoke")]
        [ForcedSleep(0.1)]
        public void MakeSmoke(ScriptInstance instance, int particles, double scale, double vel, double lifetime, double arc, string texture, Vector3 offset) =>
            /* offset is ignored and remaining parameters are emulated according to SL wiki */
            ParticleSystem(instance, new AnArray
            {
                { PSYS_PART_FLAGS }, { PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_EMISSIVE_MASK | PSYS_PART_WIND_MASK },
                { PSYS_SRC_PATTERN }, { PSYS_SRC_PATTERN_ANGLE_CONE },
                { PSYS_PART_START_COLOR }, { Vector3.One },
                { PSYS_PART_END_COLOR }, { Vector3.One },
                { PSYS_PART_START_ALPHA }, { 1.00 },
                { PSYS_PART_END_ALPHA }, { 0.05 },
                { PSYS_PART_START_SCALE }, { new Vector3(scale, scale, 0.0) },
                { PSYS_PART_END_SCALE }, { new Vector3(10, 10, 0.0) },
                { PSYS_PART_MAX_AGE }, { 3.0 },
                { PSYS_SRC_ACCEL }, {Vector3.Zero },
                { PSYS_SRC_TEXTURE }, { texture },
                { PSYS_SRC_BURST_RATE }, { 10.0 / particles },
                { PSYS_SRC_ANGLE_BEGIN }, { 0.0 },
                { PSYS_SRC_ANGLE_END }, { arc * Math.PI },
                { PSYS_SRC_BURST_PART_COUNT }, { 1 },
                { PSYS_SRC_BURST_RADIUS }, { 0.0 },
                { PSYS_SRC_BURST_SPEED_MIN }, { vel },
                { PSYS_SRC_BURST_SPEED_MAX }, { vel },
                { PSYS_SRC_MAX_AGE }, { lifetime / 2 },
                { PSYS_SRC_OMEGA }, {Vector3.Zero }
            });

        [APILevel(APIFlags.LSL, "llMakeFire")]
        [ForcedSleep(0.1)]
        public void MakeFire(ScriptInstance instance, int particles, double scale, double vel, double lifetime, double arc, string texture, Vector3 offset) =>
            /* offset is ignored and remaining parameters are emulated according to SL wiki */
            ParticleSystem(instance, new AnArray
            {
                { PSYS_PART_FLAGS }, { PSYS_PART_INTERP_COLOR_MASK | PSYS_PART_INTERP_SCALE_MASK | PSYS_PART_EMISSIVE_MASK | PSYS_PART_WIND_MASK },
                { PSYS_SRC_PATTERN }, { PSYS_SRC_PATTERN_ANGLE_CONE },
                { PSYS_PART_START_COLOR }, { Vector3.One },
                { PSYS_PART_END_COLOR }, { Vector3.One },
                { PSYS_PART_START_ALPHA }, { 0.50 },
                { PSYS_PART_END_ALPHA }, { 0.10 },
                { PSYS_PART_START_SCALE }, { new Vector3(scale/2, scale/2, 0.0) },
                { PSYS_PART_END_SCALE }, { new Vector3(scale, scale, 0.0) },
                { PSYS_PART_MAX_AGE }, { 0.5 },
                { PSYS_SRC_ACCEL }, { Vector3.Zero },
                { PSYS_SRC_TEXTURE }, { texture },
                { PSYS_SRC_BURST_RATE }, { 5 / particles },
                { PSYS_SRC_ANGLE_BEGIN }, { 0.0 },
                { PSYS_SRC_ANGLE_END }, { arc * Math.PI },
                { PSYS_SRC_BURST_PART_COUNT }, { 1 },
                { PSYS_SRC_BURST_RADIUS }, { 0.0 },
                { PSYS_SRC_BURST_SPEED_MIN }, { vel },
                { PSYS_SRC_BURST_SPEED_MAX }, { vel },
                { PSYS_SRC_MAX_AGE }, { lifetime / 2 },
                { PSYS_SRC_OMEGA }, { Vector3.Zero }
            });
    }
}
