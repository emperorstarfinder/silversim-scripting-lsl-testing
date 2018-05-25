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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Diagnostics.Contracts;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL)]
        public const double PI = 3.14159274f;
        [APILevel(APIFlags.LSL)]
        public const double TWO_PI = 6.28318548f;
        [APILevel(APIFlags.LSL)]
        public const double PI_BY_TWO = 1.57079637f;
        [APILevel(APIFlags.LSL)]
        public const double DEG_TO_RAD = 0.01745329238f;
        [APILevel(APIFlags.LSL)]
        public const double RAD_TO_DEG = 57.29578f;
        [APILevel(APIFlags.LSL)]
        public const double SQRT2 = 1.414213538f;

        [APILevel(APIFlags.OSSL, "osMax")]
        public static readonly LSLCompiler.InlineApiMethodInfo OsMax = new LSLCompiler.InlineApiMethodInfo("Max",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "a", typeof(double)),
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "b", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Max", new Type[] { typeof(double), typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.OSSL, "osMin")]
        public static readonly LSLCompiler.InlineApiMethodInfo OsMin = new LSLCompiler.InlineApiMethodInfo("Min",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "a", typeof(double)),
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "b", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Min", new Type[] { typeof(double), typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.OSSL, "osRound")]
        [Pure]
        public double Round(double value, int ndigits) => Math.Round(value, ndigits.Clamp(0, 15), MidpointRounding.AwayFromZero);

        [APILevel(APIFlags.OSSL, "osVecMagSquare")]
        public static readonly LSLCompiler.InlineApiMethodInfo VecMagSquare = new LSLCompiler.InlineApiMethodInfo("VecMagSquare",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "v", typeof(Vector3))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Vector3).GetProperty("LengthSquared").GetGetMethod());
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.OSSL, "osVecDistSquare")]
        public double VecDistSquare(Vector3 a, Vector3 b) => (a - b).LengthSquared;

        [APILevel(APIFlags.OSSL, "osAngleBetween")]
        public double OsAngleBetween(Vector3 a, Vector3 b) => Math.Atan2(a.Cross(b).Length, a.Dot(b));

        [APILevel(APIFlags.LSL, "llAbs")]
        public static readonly LSLCompiler.InlineApiMethodInfo Abs = new LSLCompiler.InlineApiMethodInfo("Abs",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(int))
            },
            typeof(int),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Abs", new Type[] { typeof(int) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llAcos")]
        public static readonly LSLCompiler.InlineApiMethodInfo Acos = new LSLCompiler.InlineApiMethodInfo("Acos",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Acos", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llAsin")]
        public static readonly LSLCompiler.InlineApiMethodInfo Asin = new LSLCompiler.InlineApiMethodInfo("Asin",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Asin", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llAtan2")]
        public static readonly LSLCompiler.InlineApiMethodInfo Atan2 = new LSLCompiler.InlineApiMethodInfo("Atan2",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "y", typeof(double)),
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "x", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Atan2", new Type[] { typeof(double), typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llCos")]
        public static readonly LSLCompiler.InlineApiMethodInfo Cos = new LSLCompiler.InlineApiMethodInfo("Cos",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Cos", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llFabs")]
        public static readonly LSLCompiler.InlineApiMethodInfo Fabs = new LSLCompiler.InlineApiMethodInfo("Fabs",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Abs", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llLog")]
        public static readonly LSLCompiler.InlineApiMethodInfo Log = new LSLCompiler.InlineApiMethodInfo("Log",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Log", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llLog10")]
        public static readonly LSLCompiler.InlineApiMethodInfo Log10 = new LSLCompiler.InlineApiMethodInfo("Log10",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Log10", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llPow")]
        public static readonly LSLCompiler.InlineApiMethodInfo Pow = new LSLCompiler.InlineApiMethodInfo("Pow",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "bas", typeof(double)),
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "exponent", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llSin")]
        public static readonly LSLCompiler.InlineApiMethodInfo Sin = new LSLCompiler.InlineApiMethodInfo("Sin",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Sin", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llSqrt")]
        public static readonly LSLCompiler.InlineApiMethodInfo Sqrt = new LSLCompiler.InlineApiMethodInfo("Sqrt",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Sqrt", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llTan")]
        public static readonly LSLCompiler.InlineApiMethodInfo Tan = new LSLCompiler.InlineApiMethodInfo("Tan",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "value", typeof(double))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Math).GetMethod("Tan", new Type[] { typeof(double) }));
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llVecDist")]
        [Pure]
        public double VecDist(Vector3 a, Vector3 b) => (a - b).Length;

        [APILevel(APIFlags.LSL, "llVecMag")]
        public static readonly LSLCompiler.InlineApiMethodInfo VecMag = new LSLCompiler.InlineApiMethodInfo("VecMag",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "v", typeof(Vector3))
            },
            typeof(double),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Vector3).GetProperty("Length").GetGetMethod());
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llVecNorm")]
        [Pure]
        public Vector3 VecNorm(Vector3 v) => (v.Length == 0.0) ? Vector3.Zero : (v / v.Length);

        [APILevel(APIFlags.LSL, "llModPow")]
        [ForcedSleep(1)]
        [Pure]
        public int ModPow(int a, int b, int c) => ((int)Math.Pow(a, b)) % c;

        [APILevel(APIFlags.LSL, "llRot2Euler")]
        [Pure]
        public Vector3 Rot2Euler(Quaternion q)
        {
            double roll;
            double pitch;
            double yaw;

            q.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(roll, pitch, yaw);
        }

        [APILevel(APIFlags.LSL, "llRot2Angle")]
        [Pure]
        public double Rot2Angle(Quaternion r)
        {
            /* based on http://wiki.secondlife.com/wiki/LlRot2Angle */
            double s2 = r.Z * r.Z; // square of the s-element
            double v2 = (r.X * r.X) + (r.Y * r.Y) + (r.Z * r.Z); // sum of the squares of the v-elements

            if (s2 < v2)
            {   // compare the s-component to the v-component
                return 2.0d * Math.Acos(Math.Sqrt(s2 / (s2 + v2))); // use arccos if the v-component is dominant
            }
            if (Math.Abs(v2) >= Double.Epsilon)
            {   // make sure the v-component is non-zero
                return 2.0d * Math.Asin(Math.Sqrt(v2 / (s2 + v2))); // use arcsin if the s-component is dominant
            }

            return 0.0; // argument is scaled too small to be meaningful, or it is a zero rotation, so return zero
        }

        [APILevel(APIFlags.LSL, "llRot2Axis")]
        [Pure]
        public Vector3 Rot2Axis(Quaternion q) => VecNorm(new Vector3(q.X, q.Y, q.Z)) * Math.Sign(q.W);

        [APILevel(APIFlags.LSL, "llAxisAngle2Rot")]
        [Pure]
        public Quaternion AxisAngle2Rot(Vector3 axis, double angle) => Quaternion.CreateFromAxisAngle(axis, angle);

        [APILevel(APIFlags.LSL, "llEuler2Rot")]
        [Pure]
        public Quaternion Euler2Rot(Vector3 v) => Quaternion.CreateFromEulers(v);

        [APILevel(APIFlags.LSL, "llAngleBetween")]
        [Pure]
        public double AngleBetween(ScriptInstance instance, Quaternion a, Quaternion b)
        {   /* based on http://wiki.secondlife.com/wiki/LlAngleBetween */
            Quaternion r = b / a;
            double s2 = r.W * r.W;
            double v2 = (r.X * r.X) + (r.Y * r.Y) + (r.Z * r.Z);
            if (s2 < v2)
            {
                return 2.0 * Math.Acos(Math.Sqrt(s2 / (s2 + v2)));
            }
            else if (v2 > double.Epsilon)
            {
                return 2.0 * Math.Asin(Math.Sqrt(v2 / (s2 + v2)));
            }
            return 0f;
        }

        [APILevel(APIFlags.LSL, "llAxes2Rot")]
        [Pure]
        public Quaternion Axes2Rot(Vector3 fwd, Vector3 left, Vector3 up) => Quaternion.Axes2Rot(fwd, left, up);

        [APILevel(APIFlags.LSL, "llRot2Fwd")]
        public static readonly LSLCompiler.InlineApiMethodInfo Rot2Fwd = new LSLCompiler.InlineApiMethodInfo("Rot2Fwd",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "r", typeof(Quaternion))
            },
            typeof(Vector3),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("FwdAxis").GetGetMethod());
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llRot2Left")]
        public static readonly LSLCompiler.InlineApiMethodInfo Rot2Left = new LSLCompiler.InlineApiMethodInfo("Rot2Left",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "r", typeof(Quaternion))
            },
            typeof(Vector3),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("LeftAxis").GetGetMethod());
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llRot2Up")]
        public static readonly LSLCompiler.InlineApiMethodInfo Rot2Up = new LSLCompiler.InlineApiMethodInfo("Rot2Up",
            new LSLCompiler.InlineApiMethodInfo.ParameterInfo[]
            {
                new LSLCompiler.InlineApiMethodInfo.ParameterInfo( "r", typeof(Quaternion))
            },
            typeof(Vector3),
            (ilgen) =>
            {
                ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("UpAxis").GetGetMethod());
            })
        {
            IsPure = true
        };

        [APILevel(APIFlags.LSL, "llRotBetween")]
        [Pure]
        public Quaternion RotBetween(Vector3 a, Vector3 b) => Quaternion.RotBetween(a, b);

        [APILevel(APIFlags.LSL, "llFloor")]
        [Pure]
        public int Floor(double f) => LSLCompiler.ConvToInt(Math.Floor(f));

        [APILevel(APIFlags.LSL, "llCeil")]
        [Pure]
        public int Ceil(double f) => LSLCompiler.ConvToInt(Math.Ceiling(f));

        [APILevel(APIFlags.LSL, "llRound")]
        [Pure]
        public int Round(double f) => (int)Math.Round(f, MidpointRounding.AwayFromZero);

        private readonly Random random = new Random();
        [APILevel(APIFlags.LSL, "llFrand")]
        public double Frand(ScriptInstance instance, double mag)
        {
            lock(instance)
            {
                lock(random)
                {
                    return random.NextDouble() * mag;
                }
            }
        }
    }
}
