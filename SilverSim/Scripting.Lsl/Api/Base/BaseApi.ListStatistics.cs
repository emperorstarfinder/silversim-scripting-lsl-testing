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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL)]
        [Description("Calculates the range.")]
        public const int LIST_STAT_RANGE = 0;
        [APILevel(APIFlags.LSL)]
        [Description("Calculates the smallest number.")]
        public const int LIST_STAT_MIN = 1;
        [APILevel(APIFlags.LSL)]
        [Description("Calculates the largest number.")]
        public const int LIST_STAT_MAX = 2;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_MEAN = 3;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_MEDIAN = 4;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_STD_DEV = 5;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_SUM = 6;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_SUM_SQUARES = 7;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_NUM_COUNT = 8;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_GEOMETRIC_MEAN = 9;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_HARMONIC_MEAN = 100;

        [APILevel(APIFlags.LSL, "llListStatistics")]
        [IsPure]
        public double ListStatistics(int operation, AnArray src)
        {
            switch (operation)
            {
                case LIST_STAT_RANGE: return ListRange(src);
                case LIST_STAT_MIN: return ListMin(src);
                case LIST_STAT_MAX: return ListMax(src);
                case LIST_STAT_MEAN: return ListMean(src);
                case LIST_STAT_MEDIAN: return ListMedian(src);
                case LIST_STAT_NUM_COUNT: return ListNumericLength(src);
                case LIST_STAT_STD_DEV: return ListStdDev(src);
                case LIST_STAT_SUM: return ListSum(src);
                case LIST_STAT_SUM_SQUARES: return ListSumSquares(src);
                case LIST_STAT_GEOMETRIC_MEAN: return ListGeometricMean(src);
                case LIST_STAT_HARMONIC_MEAN: return ListHarmonicMean(src);
                default: return 0;
            }
        }

        private bool IsValue(IValue iv, out double v)
        {
            switch (iv.LSL_Type)
            {
                case LSLValueType.Integer:
                case LSLValueType.Float:
                    v = iv.AsReal;
                    return true;

                case LSLValueType.String:
                    return double.TryParse(iv.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

                default:
                    v = 0;
                    return false;
            }
        }

        private double ListMin(AnArray src)
        {
            double minimum = double.PositiveInfinity;
            double entry = double.MaxValue;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry) &&
                    entry < minimum)
                {
                    minimum = entry;
                }
            }

            return minimum;
        }

        private double ListMax(AnArray src)
        {
            double maximum = double.NegativeInfinity;
            double entry = double.MinValue;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry) &&
                    entry > maximum)
                {
                    maximum = entry;
                }
            }

            return maximum;
        }

        private double ListRange(AnArray src)
        {
            double maximum = double.MinValue;
            double minimum = double.MaxValue;
            double entry;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    if (entry > maximum)
                    {
                        maximum = entry;
                    }
                    if (entry < minimum)
                    {
                        minimum = entry;
                    }
                }
            }

            return maximum - minimum;
        }

        private int ListNumericLength(AnArray src)
        {
            int count = 0;
            double entry;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    count++;
                }
            }

            return count;
        }

        private double ListSum(AnArray src)
        {
            double sum = 0;
            double entry;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    sum += entry;
                }
            }

            return sum;
        }

        private double ListSumSquares(AnArray src)
        {
            double sum = 0;
            double entry;
            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    sum += entry * entry;
                }
            }
            return sum;
        }

        private double ListMean(AnArray src)
        {
            double sum = 0;
            double entry;
            int count = 0;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    sum += entry;
                    ++count;
                }
            }

            return sum / count;
        }

        private double ListMedian(AnArray src) => ListQi(src, 0.5);

        private double ListGeometricMean(AnArray src)
        {
            double ret = 1.0;
            int count = 0;
            double entry;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry) && entry >= 0)
                {
                    ret *= entry;
                    ++count;
                }
                else
                {
                    return 0;
                }
            }
            return count != 0 ? Math.Pow(ret, 1.0 / count) : 0.0;
        }

        private double ListHarmonicMean(AnArray src)
        {
            double ret = 0.0;
            double entry;
            int count = 0;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    ret += 1.0 / entry;
                    ++count;
                }
            }

            return count / ret;
        }

        private double ListVariance(AnArray src)
        {
            double s = 0;
            int count = 0;
            double entry;
            double sum = 0;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    s += Math.Pow(entry, 2);
                    sum += entry;
                    ++count;
                }
            }
            return (s - (count * Math.Pow(sum / count, 2))) / (count - 1);
        }

        private double ListStdDev(AnArray src)
        {
            return Math.Sqrt(ListVariance(src));
        }

        private double[] NumericSort(AnArray src)
        {
            var resList = new List<double>();
            double entry;

            foreach(IValue value in src)
            {
                if (IsValue(value, out entry))
                {
                    resList.Add(entry);
                }
            }

            double[] resArray = resList.ToArray();
            Array.Sort(resArray);
            return resArray;
        }

        private double ListQi(AnArray src, double i)
        {
            double[] j = NumericSort(src);

            return Math.Abs(Math.Ceiling(j.Length * i) - (j.Length * i)) < double.Epsilon ?
                (j[(int)((j.Length * i) - 1)] + j[(int)(j.Length * i)]) / 2 :
                j[(int)(Math.Ceiling(j.Length * i)) - 1];
        }
    }
}
