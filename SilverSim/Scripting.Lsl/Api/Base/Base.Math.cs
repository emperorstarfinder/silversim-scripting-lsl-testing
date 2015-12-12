// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

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
        public double Max(ScriptInstance instance, double a, double b)
        {
            return (a > b) ? a : b;
        }

        [APILevel(APIFlags.OSSL, "osMin")]
        public double Min(ScriptInstance instance, double a, double b)
        {
            return (a < b) ? a : b;
        }

        [APILevel(APIFlags.LSL, "llAbs")]
        public int Abs(ScriptInstance instance, int v)
        {
            return (v < 0) ? -v : v;
        }

        [APILevel(APIFlags.LSL, "llAcos")]
        public double Acos(ScriptInstance instance, double v)
        {
            return Math.Acos(v);
        }

        [APILevel(APIFlags.LSL, "llAsin")]
        public double Asin(ScriptInstance instance, double v)
        {
            return Math.Asin(v);
        }

        [APILevel(APIFlags.LSL, "llAtan2")]
        public double Atan2(ScriptInstance instance, double y, double x)
        {
            return Math.Atan2(y, x);
        }

        [APILevel(APIFlags.LSL, "llCos")]
        public double Cos(ScriptInstance instance, double v)
        {
            return Math.Cos(v);
        }

        [APILevel(APIFlags.LSL, "llFabs")]
        public double Fabs(ScriptInstance instance, double v)
        {
            return Math.Abs(v);
        }

        [APILevel(APIFlags.LSL, "llLog")]
        public double Log(ScriptInstance instance, double v)
        {
            return Math.Log(v);
        }

        [APILevel(APIFlags.LSL, "llLog10")]
        public double Log10(ScriptInstance instance, double v)
        {
            return Math.Log10(v);
        }

        [APILevel(APIFlags.LSL, "llPow")]
        public double Pow(ScriptInstance instance, double bas, double exponent)
        {
            return Math.Pow(bas, exponent);
        }

        [APILevel(APIFlags.LSL, "llSin")]
        public double Sin(ScriptInstance instance, double v)
        {
            return Math.Sin(v);
        }

        [APILevel(APIFlags.LSL, "llSqrt")]
        public double Sqrt(ScriptInstance instance, double v)
        {
            return Math.Sqrt(v);
        }

        [APILevel(APIFlags.LSL, "llTan")]
        public double Tan(ScriptInstance instance, double v)
        {
            return Math.Tan(v);
        }

        [APILevel(APIFlags.LSL, "llVecDist")]
        public double VecDist(ScriptInstance instance, Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }

        [APILevel(APIFlags.LSL, "llVecMag")]
        public double VecMag(ScriptInstance instance, Vector3 v)
        {
            return v.Length;
        }

        [APILevel(APIFlags.LSL, "llVecNorm")]
        public Vector3 VecNorm(ScriptInstance instance, Vector3 v)
        {
            return v / v.Length;
        }

        [APILevel(APIFlags.LSL, "llModPow")]
        [ForcedSleep(1)]
        public int ModPow(ScriptInstance instance, int a, int b, int c)
        {
            return ((int)Math.Pow(a, b)) % c;
        }

        [APILevel(APIFlags.LSL, "llRot2Euler")]
        public Vector3 Rot2Euler(ScriptInstance instance, Quaternion q)
        {
            double roll, pitch, yaw;

            q.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(roll, pitch, yaw);
        }

        [APILevel(APIFlags.LSL, "llRot2Angle")]
        public double Rot2Angle(ScriptInstance instance, Quaternion r)
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
        public Vector3 Rot2Axis(ScriptInstance instance, Quaternion q)
        {
            return VecNorm(instance, new Vector3(q.X, q.Y, q.Z)) * Math.Sign(q.W);
        }

        [APILevel(APIFlags.LSL, "llAxisAngle2Rot")]
        public Quaternion AxisAngle2Rot(ScriptInstance instance, Vector3 axis, double angle)
        {
            return Quaternion.CreateFromAxisAngle(axis, angle);
        }

        [APILevel(APIFlags.LSL, "llEuler2Rot")]
        public Quaternion Euler2Rot(ScriptInstance instance, Vector3 v)
        {
            return Quaternion.CreateFromEulers(v);
        }

        [APILevel(APIFlags.LSL, "llAngleBetween")]
        public double AngleBetween(ScriptInstance instance, Quaternion a, Quaternion b)
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
        public Quaternion Axes2Rot(ScriptInstance instance, Vector3 fwd, Vector3 left, Vector3 up)
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
        public Vector3 Rot2Fwd(ScriptInstance instance, Quaternion r)
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
        public Vector3 Rot2Left(ScriptInstance instance, Quaternion r)
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
        public Vector3 Rot2Up(ScriptInstance instance, Quaternion r)
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
        public Quaternion RotBetween(ScriptInstance instance, Vector3 a, Vector3 b)
        {
            return Quaternion.RotBetween(a, b);
        }

        [APILevel(APIFlags.LSL, "llFloor")]
        public int Floor(ScriptInstance instance, double f)
        {
            return (int)Math.Floor(f);
        }

        [APILevel(APIFlags.LSL, "llCeil")]
        public int Ceil(ScriptInstance instance, double f)
        {
            return (int)Math.Ceiling(f);
        }

        [APILevel(APIFlags.LSL, "llRound")]
        public int Round(ScriptInstance instance, double f)
        {
            return (int)Math.Round(f, MidpointRounding.AwayFromZero);
        }

        private readonly Random random = new Random();
        [APILevel(APIFlags.LSL, "llFrand")]
        public double Frand(ScriptInstance instance, double mag)
        {
            return random.NextDouble() * mag;
        }
    }
}
