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
        public List<SurfacePointWithValues> TruthCalcPoints { get; }
        public bool IsPartitioned { get; private set; }
        public Face Face => face;
        public double FaceArea { get; private set; }
        public Transform ElementTransform { get; }

        public CalculationSurface(Face f, Transform elementTransform)
        {
            if (!(f is PlanarFace))
                throw new InvalidOperationException($"Face is not planar, it is {f.GetType().Name}. This type of face cannot be partitioned yet.");

            face = f;
            FaceArea = f.Area;
            FaceReference = f.Reference;
            CalculationPoints = new List<SurfacePointWithValues>();
            TruthCalcPoints = new List<SurfacePointWithValues>();
            this.ElementTransform = elementTransform;

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
                double u_center = uStart, v_center = vStart;
                //Parallel.For(uStart, fBB.Max.U, u =>
                double u_remainder = fBB.Max.U % ds;
                double v_remainder = fBB.Max.V % ds;
                double partHeight = ds;
                double partWidth = ds;
                for (double u = uStart; u < fBB.Max.U; u += ds)
                {
                    if (u < fBB.Max.U - ds)
                    {
                        u_center = u + ds / 2;
                        partWidth = ds;
                    }
                    else
                    {
                        u_center = fBB.Max.U - u_remainder / 2;
                        partWidth = u_remainder;
                    }
                    if (partWidth < 0.005)
                        continue;

                    for (double v = vStart; v < fBB.Max.V; v += ds)
                    {
                        if (v < fBB.Max.V - ds)
                        {
                            v_center = v + ds / 2;
                            partHeight = ds;
                        }
                        else
                        {
                            v_center = fBB.Max.V - v_remainder / 2;
                            partHeight = v_remainder;
                        }
                        if (partHeight < 0.005)
                            continue;

                        UV uv_center = new UV(u_center, v_center);
                        if (!face.IsInside(uv_center))
                            continue;


                        XYZ normal = ElementTransform.OfVector(face.ComputeNormal(uv_center));
                        XYZ point_center = ElementTransform.OfPoint(face.Evaluate(uv_center));
                        SurfacePointWithValues p_center = new SurfacePointWithValues(uv_center, point_center, normal, partHeight, partWidth);
                        TruthCalcPoints.Add(p_center);
                    }
                }//);

                //foreach (EdgeArray edAr in face.EdgeLoops)
                //{
                //    foreach (Edge ed in edAr)
                //    {
                //        double steps = ed.ApproximateLength / ds;
                //        double stepParameter = 1 / steps;

                //        for (double de = 0; de < 1; de += stepParameter)
                //        {
                //            UV uv = ed.EvaluateOnFace(de, face);
                //            XYZ normal = ElementTransform.OfVector(face.ComputeNormal(uv));
                //            XYZ point = ElementTransform.OfPoint(face.Evaluate(uv));

                //            SurfacePointWithValues p = new SurfacePointWithValues(uv, point, normal);
                //            CalculationPoints.Add(p);
                //        }

                //        UV endUV = ed.EvaluateOnFace(1, face);
                //        XYZ endPoint = ElementTransform.OfPoint(ed.Evaluate(1));
                //        XYZ endNormal = ElementTransform.OfVector(face.ComputeNormal(endUV));

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
