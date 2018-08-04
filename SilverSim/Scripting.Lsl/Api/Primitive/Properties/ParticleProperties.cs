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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Primitive.Properties
{
    [LSLImplementation]
    [ScriptApiName("ParticleProperties")]
    [Description("Particle Properties API")]
    public sealed class ParticleProperties : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Properties, "particlesystem")]
        [APIDisplayName("particlesystem")]
        [APIIsVariableType]
        [APIAccessibleMembers]
        [Serializable]
        public class ParticleSystemData
        {
            public bool IsBounce { get; set; }
            public bool IsEmissive { get; set; }
            public bool IsFollowSrc { get; set; }
            public bool IsFollowVelocity { get; set; }
            public bool IsInterpolatedColor { get; set; }
            public bool IsInterpolatedScale { get; set; }
            public bool IsRibbon { get; set; }
            public bool IsTargetLinear { get; set; }
            public bool IsTargetPos { get; set; }
            public bool IsWind { get; set; }

            public bool IsSrcPatternExplode { get; set; }
            public bool IsSrcPatternAngleCone { get; set; } = true;
            public bool IsSrcPatternAngle { get; set; }
            public bool IsSrcPatternDrop { get; set; } = true;
            public bool IsSrcPAtternAngleConeEmpty { get; set; }

            public double SrcBurstRadius { get; set; }
            public double SrcAngleBegin { get; set; }
            public double SrcAngleEnd { get; set; }

            public UUID TargetKey { get; set; } = UUID.Zero;
            public Vector3 StartColor { get; set; } = Vector3.Zero;

            private Vector3 m_EndColor = Vector3.Zero;
            public Vector3 EndColor
            {
                get
                {
                    return m_EndColor;
                }
                set
                {
                    m_EndColor = value;
                    IsInterpolatedColor = true;
                }
            }

            private double m_StartAlpha;
            public double StartAlpha
            {
                get
                {
                    return m_StartAlpha;
                }
                set
                {
                    m_StartAlpha = value.Clamp(0, 1);
                }
            }

            private double m_EndAlpha;
            public double EndAlpha
            {
                get
                {
                    return m_EndAlpha;
                }
                set
                {
                    m_EndAlpha = value.Clamp(0, 1);
                    IsInterpolatedColor = true;
                }
            }

            public Vector3 StartScale { get; set; } = Vector3.One;

            private Vector3 m_EndScale = Vector3.One;
            public Vector3 EndScale
            {
                get
                {
                    return m_EndScale;
                }
                set
                {
                    m_EndScale = value;
                    IsInterpolatedScale = true;
                }
            }

            public string Texture { get; set; }

            private double m_StartGlow;
            public double StartGlow
            {
                get
                {
                    return m_StartGlow;
                }
                set
                {
                    m_StartGlow = value.Clamp(0, 1);
                }
            }

            private double m_EndGlow;
            public double EndGlow
            {
                get
                {
                    return m_EndGlow;
                }
                set
                {
                    m_EndGlow = value.Clamp(0, 1);
                }
            }

            public int BlendFuncSrc { get; set; } = PrimitiveApi.PSYS_PART_BF_SOURCE_ALPHA;
            public int BlendFuncDest { get; set; } = PrimitiveApi.PSYS_PART_BF_ONE_MINUS_SOURCE_ALPHA;

            public double SrcMaxAge { get; set; }
            public double SrcPartMaxAge { get; set; }
            public double SrcBurstRate { get; set; } = 1;
            public int SrcBurstPartCount { get; set; } = 1;

            public Vector3 SrcAccel { get; set; }
            public Vector3 SrcOmega { get; set; } = Vector3.Zero;
            public double SrcBurstSpeedMin { get; } = 1;
            public double SrcBurstSpeedMax { get; } = 1;

            public static explicit operator AnArray(ParticleSystemData partsys)
            {
                var res = new AnArray();
                int partflags = 0;
                if(partsys.IsBounce)
                {
                    partflags |= PrimitiveApi.PSYS_PART_BOUNCE_MASK;
                }
                if(partsys.IsEmissive)
                {
                    partflags |= PrimitiveApi.PSYS_PART_EMISSIVE_MASK;
                }
                if(partsys.IsFollowSrc)
                {
                    partflags |= PrimitiveApi.PSYS_PART_FOLLOW_SRC_MASK;
                }
                if(partsys.IsFollowVelocity)
                {
                    partflags |= PrimitiveApi.PSYS_PART_FOLLOW_VELOCITY_MASK;
                }
                if(partsys.IsInterpolatedColor)
                {
                    partflags |= PrimitiveApi.PSYS_PART_INTERP_COLOR_MASK;
                }
                if(partsys.IsInterpolatedScale)
                {
                    partflags |= PrimitiveApi.PSYS_PART_INTERP_SCALE_MASK;
                }
                if(partsys.IsRibbon)
                {
                    partflags |= PrimitiveApi.PSYS_PART_RIBBON_MASK;
                }
                if(partsys.IsTargetLinear)
                {
                    partflags |= PrimitiveApi.PSYS_PART_TARGET_LINEAR_MASK;
                }
                if(partsys.IsTargetPos)
                {
                    partflags |= PrimitiveApi.PSYS_PART_TARGET_POS_MASK;
                }
                if(partsys.IsWind)
                {
                    partflags |= PrimitiveApi.PSYS_PART_WIND_MASK;
                }

                res.Add(PrimitiveApi.PSYS_PART_FLAGS);
                res.Add(partflags);

                res.Add(PrimitiveApi.PSYS_SRC_PATTERN);
                partflags = 0;
                if(partsys.IsSrcPatternExplode)
                {
                    partflags |= PrimitiveApi.PSYS_SRC_PATTERN_EXPLODE;
                }
                if(partsys.IsSrcPatternAngleCone)
                {
                    partflags |= PrimitiveApi.PSYS_SRC_PATTERN_ANGLE_CONE;
                }
                if(partsys.IsSrcPatternAngle)
                {
                    partflags |= PrimitiveApi.PSYS_SRC_PATTERN_ANGLE;
                }
                if(partsys.IsSrcPatternDrop)
                {
                    partflags |= PrimitiveApi.PSYS_SRC_PATTERN_DROP;
                }
                if(partsys.IsSrcPAtternAngleConeEmpty)
                {
                    partflags |= PrimitiveApi.PSYS_SRC_PATTERN_ANGLE_CONE_EMPTY;
                }
                res.Add(partflags);
                res.Add(PrimitiveApi.PSYS_SRC_BURST_RADIUS);
                res.Add(partsys.SrcBurstRadius);
                res.Add(PrimitiveApi.PSYS_SRC_ANGLE_BEGIN);
                res.Add(partsys.SrcAngleBegin);
                res.Add(PrimitiveApi.PSYS_SRC_ANGLE_END);
                res.Add(partsys.SrcAngleEnd);
                if (partsys.TargetKey != UUID.Zero)
                {
                    res.Add(PrimitiveApi.PSYS_SRC_TARGET_KEY);
                    res.Add(partsys.TargetKey);
                }

                res.Add(PrimitiveApi.PSYS_PART_START_COLOR);
                res.Add(partsys.StartColor);
                res.Add(PrimitiveApi.PSYS_PART_START_ALPHA);
                res.Add(partsys.StartAlpha);
                res.Add(PrimitiveApi.PSYS_PART_START_SCALE);
                res.Add(partsys.StartScale);
                res.Add(PrimitiveApi.PSYS_PART_START_GLOW);
                res.Add(partsys.StartGlow);

                if(partsys.IsInterpolatedColor)
                {
                    res.Add(PrimitiveApi.PSYS_PART_END_COLOR);
                    res.Add(partsys.EndColor);
                    res.Add(PrimitiveApi.PSYS_PART_END_ALPHA);
                    res.Add(partsys.EndAlpha);
                }
                if(partsys.IsInterpolatedScale)
                {
                    res.Add(PrimitiveApi.PSYS_PART_END_SCALE);
                    res.Add(partsys.EndScale);
                }
                res.Add(PrimitiveApi.PSYS_PART_END_GLOW);
                res.Add(partsys.EndGlow);
                res.Add(PrimitiveApi.PSYS_PART_BLEND_FUNC_SOURCE);
                res.Add(partsys.BlendFuncSrc);
                res.Add(PrimitiveApi.PSYS_PART_BLEND_FUNC_DEST);
                res.Add(partsys.BlendFuncDest);
                res.Add(PrimitiveApi.PSYS_SRC_MAX_AGE);
                res.Add(partsys.SrcMaxAge);
                res.Add(PrimitiveApi.PSYS_PART_MAX_AGE);
                res.Add(partsys.SrcPartMaxAge);
                res.Add(PrimitiveApi.PSYS_SRC_BURST_RATE);
                res.Add(partsys.SrcBurstRate);
                res.Add(PrimitiveApi.PSYS_SRC_BURST_PART_COUNT);
                res.Add(partsys.SrcBurstPartCount);
                res.Add(PrimitiveApi.PSYS_SRC_ACCEL);
                res.Add(partsys.SrcAccel);
                res.Add(PrimitiveApi.PSYS_SRC_OMEGA);
                res.Add(partsys.SrcOmega);
                res.Add(PrimitiveApi.PSYS_SRC_BURST_SPEED_MIN);
                res.Add(partsys.SrcBurstSpeedMin);
                res.Add(PrimitiveApi.PSYS_SRC_BURST_SPEED_MAX);
                res.Add(partsys.SrcBurstSpeedMax);
                return res;
            }
        }
    }
}
