using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Utils
{
    internal static class MathUtils
    {
        internal static bool AreDoublesAlmostEqual(double d1, double d2)
        {
            const double epsilon = 1E-10;
            double diff = Math.Abs(d1 - d2);
            if (diff < epsilon)
                return true;

            return false;
        }
    }
}
