using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Geometry
{
    public sealed class CalculationSurface
    {
        private Face face;
        public Reference FaceReference { get; }
        public List<SurfacePointWithValues> CalculationPoints { get; }
        public bool IsPartitioned { get; private set; }
        public Face Face => face;
        public double FaceArea { get; private set; }
        private Transform elementTransform { get; set; }

        public CalculationSurface(Face f, Transform elementTransform)
        {
            if (!(f is PlanarFace))
                throw new InvalidOperationException($"Face is not planar, it is {f.GetType().Name}. This type of face cannot be partitioned yet.");

            face = f;
            FaceArea = f.Area;
            FaceReference = f.Reference;
            CalculationPoints = new List<SurfacePointWithValues>();
            this.elementTransform = elementTransform;

        }

        public void UpdateFace(Face f)
        {
            face = f;

        }

        public bool CreateOrUpdatePartition(double ds)
        {
            if (face == null)
                throw new ArgumentNullException("Calculation Face");

            CalculationPoints.Clear();

            try
            {
                var fBB = face.GetBoundingBox();
                double uStart = fBB.Min.U;
                double vStart = fBB.Min.V;
                //Parallel.For(uStart, fBB.Max.U, u =>
                for (double u = uStart; u < fBB.Max.U; u += ds)
                {
                    for (double v = vStart; v < fBB.Max.V; v += ds)
                    {
                        UV uv = new UV(u, v);
                        if (!face.IsInside(uv))
                            continue;

                        XYZ normal = elementTransform.OfVector(face.ComputeNormal(uv));
                        XYZ point = elementTransform.OfPoint(face.Evaluate(uv));
                        SurfacePointWithValues p = new SurfacePointWithValues(uv, point, normal);
                        CalculationPoints.Add(p);
                    }
                    UV u_vLast = new UV(u, fBB.Max.V);
                    XYZ normalLastV = elementTransform.OfVector(face.ComputeNormal(u_vLast));
                    XYZ pointLastV = elementTransform.OfPoint(face.Evaluate(u_vLast));
                    SurfacePointWithValues p_lastV = new SurfacePointWithValues(u_vLast, pointLastV, normalLastV);
                    CalculationPoints.Add(p_lastV);
                }//);
                UV uLast_vLast = new UV(fBB.Max.U, fBB.Max.V);
                XYZ normalLast = elementTransform.OfVector(face.ComputeNormal(uLast_vLast));
                XYZ pointLast = elementTransform.OfPoint(face.Evaluate(uLast_vLast));
                SurfacePointWithValues p_last = new SurfacePointWithValues(uLast_vLast, pointLast, normalLast);
                CalculationPoints.Add(p_last);
                //foreach (EdgeArray edAr in face.EdgeLoops)
                //{
                //    foreach (Edge ed in edAr)
                //    {
                //        double steps = ed.ApproximateLength / ds;
                //        double stepParameter = 1 / steps;

                //        for (double de = 0; de < 1; de += stepParameter)
                //        {
                //            UV uv = ed.EvaluateOnFace(de, face);
                //            XYZ normal = elementTransform.OfVector(face.ComputeNormal(uv));
                //            XYZ point = elementTransform.OfPoint(face.Evaluate(uv));

                //            SurfacePointWithValues p = new SurfacePointWithValues(uv, point, normal);
                //            CalculationPoints.Add(p);
                //        }

                //        UV endUV = ed.EvaluateOnFace(1, face);
                //        XYZ endPoint = elementTransform.OfPoint(ed.Evaluate(1));
                //        XYZ endNormal = elementTransform.OfVector(face.ComputeNormal(endUV));

                //        SurfacePointWithValues endEdgePoint = new SurfacePointWithValues(endUV, endPoint, endNormal);
                //        CalculationPoints.Add(endEdgePoint);
                //    }
                //}

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
