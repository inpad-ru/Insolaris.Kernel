using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Geometry
{
    public class SurfacePointWithValues
    {
        public UV PointUV { get; }
        public XYZ Point3D { get; }
        public XYZ Normal { get; }
        double InsolationSum { get; set; }
        double MaxSpanInsolation { get; set; }
        public SurfacePointWithValues(UV uv, XYZ p, XYZ normal)
        {
            PointUV = uv;
            Point3D = p;
            Normal = normal;
        }

        public void SetSum(double sum)
        {
            InsolationSum = sum;
        }
        public void SetMaxSpan(double span)
        {
            MaxSpanInsolation = span;
        }
        public TimeSpan GetInsolationSum()
        {
            return TimeSpan.FromSeconds(InsolationSum);
        }
        public TimeSpan GetMaxSpanInsolation()
        {
            return TimeSpan.FromSeconds(MaxSpanInsolation);
        }
    }
}
