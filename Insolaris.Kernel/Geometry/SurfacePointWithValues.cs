using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Geometry
{
    public sealed class SurfacePointWithValues
    {
        public UV PointUV { get; }
        public XYZ Point3D { get; }
        public XYZ Normal { get; }
        public double Width { get; }
        public double Height { get; set; }
        public Calculation.CalculationResult CalculationResult { get; set; }
        public SurfacePointWithValues(UV uv, XYZ p, XYZ normal, double h, double w)
        {
            PointUV = uv;
            Point3D = p;
            Normal = normal;
            Height = h;
            Width = w;
        }

    }
}
