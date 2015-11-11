// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const double PI = 3.14159274f;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const double TWO_PI = 6.28318548f;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const double PI_BY_TWO = 1.57079637f;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const double DEG_TO_RAD = 0.01745329238f;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const double RAD_TO_DEG = 57.29578f;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const double SQRT2 = 1.414213538f;

        [APILevel(APIFlags.LSL, "llAbs")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int Abs(ScriptInstance instance, int v)
        {
            return (v < 0) ? -v : v;
        }

        [APILevel(APIFlags.LSL, "llAcos")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Acos(ScriptInstance instance, double v)
        {
            return Math.Acos(v);
        }

        [APILevel(APIFlags.LSL, "llAsin")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Asin(ScriptInstance instance, double v)
        {
            return Math.Asin(v);
        }

        [APILevel(APIFlags.LSL, "llAtan2")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Atan2(ScriptInstance instance, double y, double x)
        {
            return Math.Atan2(y, x);
        }

        [APILevel(APIFlags.LSL, "llCos")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Cos(ScriptInstance instance, double v)
        {
            return Math.Cos(v);
        }

        [APILevel(APIFlags.LSL, "llFabs")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Fabs(ScriptInstance instance, double v)
        {
            return Math.Abs(v);
        }

        [APILevel(APIFlags.LSL, "llLog")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Log(ScriptInstance instance, double v)
        {
            return Math.Log(v);
        }

        [APILevel(APIFlags.LSL, "llLog10")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Log10(ScriptInstance instance, double v)
        {
            return Math.Log10(v);
        }

        [APILevel(APIFlags.LSL, "llPow")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Pow(ScriptInstance instance, double bas, double exponent)
        {
            return Math.Pow(bas, exponent);
        }

        [APILevel(APIFlags.LSL, "llSin")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Sin(ScriptInstance instance, double v)
        {
            return Math.Sin(v);
        }

        [APILevel(APIFlags.LSL, "llSqrt")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Sqrt(ScriptInstance instance, double v)
        {
            return Math.Sqrt(v);
        }

        [APILevel(APIFlags.LSL, "llTan")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Tan(ScriptInstance instance, double v)
        {
            return Math.Tan(v);
        }

        [APILevel(APIFlags.LSL, "llVecDist")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double VecDist(ScriptInstance instance, Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }

        [APILevel(APIFlags.LSL, "llVecMag")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double VecMag(ScriptInstance instance, Vector3 v)
        {
            return v.Length;
        }

        [APILevel(APIFlags.LSL, "llVecNorm")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 VecNorm(ScriptInstance instance, Vector3 v)
        {
            return v / v.Length;
        }

        [APILevel(APIFlags.LSL, "llModPow")]
        [ForcedSleep(1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int ModPow(ScriptInstance instance, int a, int b, int c)
        {
            return ((int)Math.Pow(a, b)) % c;
        }

        [APILevel(APIFlags.LSL, "llRot2Euler")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 Rot2Euler(ScriptInstance instance, Quaternion q)
        {
            double roll, pitch, yaw;

            q.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(roll, pitch, yaw);
        }

        [APILevel(APIFlags.LSL, "llRot2Angle")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Rot2Angle(ScriptInstance instance, Quaternion r)
        {
            /* based on http://wiki.secondlife.com/wiki/LlRot2Angle */
            double s2 = r.Z * r.Z; // square of the s-element
            double v2 = r.X * r.X + r.Y * r.Y + r.Z * r.Z; // sum of the squares of the v-elements

            if (s2 < v2)
            {   // compare the s-component to the v-component
                return 2.0d * Math.Acos(Math.Sqrt(s2 / (s2 + v2))); // use arccos if the v-component is dominant
            }
            if (Math.Abs(v2) >= Double.Epsilon)
            {   // make sure the v-component is non-zero
                return 2.0d * Math.Asin(Math.Sqrt(v2 / (s2 + v2))); // use arcsin if the s-component is dominant
            }

            return 0.0; // argument is scaled too small to be meaningful, or it is a zero rotation, so return zer
        }

        [APILevel(APIFlags.LSL, "llRot2Axis")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 Rot2Axis(ScriptInstance instance, Quaternion q)
        {
            return VecNorm(instance, new Vector3(q.X, q.Y, q.Z)) * Math.Sign(q.W);
        }

        [APILevel(APIFlags.LSL, "llAxisAngle2Rot")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Quaternion AxisAngle2Rot(ScriptInstance instance, Vector3 axis, double angle)
        {
            return Quaternion.CreateFromAxisAngle(axis, angle);
        }

        [APILevel(APIFlags.LSL, "llEuler2Rot")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Quaternion Euler2Rot(ScriptInstance instance, Vector3 v)
        {
            return Quaternion.CreateFromEulers(v);
        }

        [APILevel(APIFlags.LSL, "llAngleBetween")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double AngleBetween(ScriptInstance instance, Quaternion a, Quaternion b)
        {   /* based on http://wiki.secondlife.com/wiki/LlAngleBetween */
            Quaternion r = b / a;
            double s2 = r.W * r.W;
            double v2 = r.X * r.X + r.Y * r.Y + r.Z * r.Z;
            if (s2 < v2)
            {
                return 2.0 * Math.Acos(Math.Sqrt(s2 / (s2 + v2)));
            }
            else if (v2 > Double.Epsilon)
            {
                return 2.0 * Math.Asin(Math.Sqrt(v2 / (s2 + v2)));
            }
            return 0f;
        }

        [APILevel(APIFlags.LSL, "llAxes2Rot")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Quaternion Axes2Rot(ScriptInstance instance, Vector3 fwd, Vector3 left, Vector3 up)
        {
            double s;
            double t = fwd.X + left.Y + up.Z + 1.0;

            if(t >= 1.0)
            {
                s = 0.5 / Math.Sqrt(t);
                return new Quaternion((left.Z - up.Y) * s, (up.X - fwd.Z) * s, (fwd.Y - left.X) * s, 0.25 / s);
            }
            else
            {
                double m = (left.Y > up.Z) ? left.Y : up.Z;

                if(m < fwd.X)
                {
                    s = Math.Sqrt(fwd.X - (left.Y + up.Z) + 1.0);
                    return new Quaternion(
                        s * 0.5,
                        (fwd.Y + left.X) * (0.5 / s),
                        (up.X + fwd.Z) * (0.5 / s),
                        (left.Z - up.Y) * (0.5 / s));
                }
                else if(Math.Abs(m - left.Y) < Double.Epsilon)
                {
                    s = Math.Sqrt(left.Y - (up.Z + fwd.X) + 1.0);
                    return new Quaternion(
                        (fwd.Y + left.X) * (0.5 / s),
                        s * 0.5,
                        (left.Z + up.Y) * (0.5 / s),
                        (up.X - fwd.Z) * (0.5 / s));
                }
                else
                {
                    s = Math.Sqrt(up.Z - (fwd.X + left.Y) + 1.0);
                    return new Quaternion(
                        (up.X + fwd.Z) * (0.5 / s),
                        (left.Z + up.Y) * (0.5 / s),
                        s * 0.5,
                        (fwd.Y - left.X) * (0.5 / s));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRot2Fwd")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 Rot2Fwd(ScriptInstance instance, Quaternion r)
        {
            double x, y, z, sq;
            sq = r.LengthSquared;
            if(Math.Abs(1.0 - sq) > Double.Epsilon)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = r.X * r.X - r.Y * r.Y - r.Z * r.Z + r.W * r.W;
            y = 2 * (r.X * r.Y + r.Z * r.W);
            z = 2 * (r.X * r.Z - r.Y * r.W);
            return new Vector3(x, y, z);
        }

        [APILevel(APIFlags.LSL, "llRot2Left")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 Rot2Left(ScriptInstance instance, Quaternion r)
        {
            double x, y, z, sq;

            sq = r.LengthSquared;
            if (Math.Abs(1.0 - sq) > Double.Epsilon)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = 2 * (r.X * r.Y - r.Z * r.W);
            y = -r.X * r.X + r.Y * r.Y - r.Z * r.Z + r.W * r.W;
            z = 2 * (r.X * r.W + r.Y * r.Z);
            return new Vector3(x, y, z);
        }

        [APILevel(APIFlags.LSL, "llRot2Up")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 Rot2Up(ScriptInstance instance, Quaternion r)
        {
            double x, y, z, sq;

            sq = r.LengthSquared;
            if (Math.Abs(1.0 - sq) > Double.Epsilon)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = 2 * (r.X * r.Z + r.Y * r.W);
            y = 2 * (-r.X * r.W + r.Y * r.Z);
            z = -r.X * r.X - r.Y * r.Y + r.Z * r.Z + r.W * r.W;
            return new Vector3(x, y, z);
        }

        [APILevel(APIFlags.LSL, "llRotBetween")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Quaternion RotBetween(ScriptInstance instance, Vector3 a, Vector3 b)
        {
            return Quaternion.RotBetween(a, b);
        }

        [APILevel(APIFlags.LSL, "llFloor")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int Floor(ScriptInstance instance, double f)
        {
            return (int)Math.Floor(f);
        }

        [APILevel(APIFlags.LSL, "llCeil")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int Ceil(ScriptInstance instance, double f)
        {
            return (int)Math.Ceiling(f);
        }

        [APILevel(APIFlags.LSL, "llRound")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int Round(ScriptInstance instance, double f)
        {
            return (int)Math.Round(f, MidpointRounding.AwayFromZero);
        }

        private readonly Random random = new Random();
        [APILevel(APIFlags.LSL, "llFrand")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double Frand(ScriptInstance instance, double mag)
        {
            return random.NextDouble() * mag;
        }
    }
}
