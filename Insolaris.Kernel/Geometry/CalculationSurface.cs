using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Geometry
{
    public class CalculationSurface
    {
        public Reference FaceReference { get; }
        public List<SurfacePointWithValues> CalculationPoints { get; }
        public bool IsPartitioned { get; private set; }
        private Face Face { get; set; }

        public CalculationSurface(Face f)
        {
            if (!(f is PlanarFace) && !(f is CylindricalFace))
                throw new InvalidOperationException($"Face is not planar or cylindrical, it is {f.GetType().Name}. This type of face cannot be partioned yet.");

            Face = f;
            FaceReference = f.Reference;
            CalculationPoints = new List<SurfacePointWithValues>();
        }

        public void UpdateFace(Face f)
        {
            Face = f;
        }

        public bool CreateOrUpdatePartition(double ds)
        {
            if (Face == null)
                throw new ArgumentNullException("Calculation Face");

            CalculationPoints.Clear();

            try
            {
                var fBB = Face.GetBoundingBox();
                double uStart = fBB.Min.U + ds;
                double vStart = fBB.Min.V + ds;

                for (double u = uStart; u < fBB.Max.U; u += ds)
                {
                    for (double v = vStart; v < fBB.Max.V; v += ds)
                    {
                        UV uv = new UV(u, v);
                        if (!Face.IsInside(uv))
                            continue;

                        XYZ normal = Face.ComputeNormal(uv);
                        XYZ point = Face.Evaluate(uv);

                        SurfacePointWithValues p = new SurfacePointWithValues(uv, point, normal);
                        CalculationPoints.Add(p);
                    }
                }

                foreach (EdgeArray edAr in Face.EdgeLoops)
                {
                    foreach (Edge ed in edAr)
                    {
                        double steps = ed.ApproximateLength / ds;
                        int stepParameter = (int)(1 / steps);
                        int stepCount = (int)steps;

                        for (int i = 0; i < stepCount; i++)
                        {
                            UV uv = ed.EvaluateOnFace(i * stepParameter, Face);
                            XYZ normal = Face.ComputeNormal(uv);
                            XYZ point = Face.Evaluate(uv);

                            SurfacePointWithValues p = new SurfacePointWithValues(uv, point, normal);
                            CalculationPoints.Add(p);
                        }

                        UV endUV = ed.EvaluateOnFace(1, Face);
                        XYZ endPoint = ed.Evaluate(1);
                        XYZ endNormal = Face.ComputeNormal(endUV);

                        SurfacePointWithValues endEdgePoint = new SurfacePointWithValues(endUV, endPoint, endNormal);
                        CalculationPoints.Add(endEdgePoint);
                    }
                }

                IsPartitioned = true;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
