using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Insolaris.Kernel.Geometry;

namespace Insolaris.Geometry
{
    public sealed class CalculationSurface  //Создание точек от этого класса начинается для любого плагина, получение первоначального разбиения точек по граням очень важно
    {
        private Face face;
        public Reference FaceReference { get; }
        public List<SurfacePointWithValues> CalculationPoints { get; }
        public List<SurfacePointWithValues> TruthCalcPoints { get; }
        public bool IsPartitioned { get; private set; }
        public Face Face => face;
        public double FaceArea { get; private set; }
        public Transform ElementTransform { get; }
        public Dictionary<double, List<SurfacePointWithValues>> PointsInPlan { get; set; } = new Dictionary<double, List<SurfacePointWithValues>>();

        //После рефакторинга
        public Dictionary<double, List<SurfacePointWithValues>> PointsInPlan1 { get; set; } = new Dictionary<double, List<SurfacePointWithValues>>();

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
            var ds = 3000 / 304.8;                                                //Это заглушка, сделать, чтобы ds тянулось из VM
            CreateOrUpdatePartition(ds);
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
                   
                    var deltaU = fBB.Max.U - ds;
                    if (u < deltaU || u_remainder < 1E-3) //Remanders can be so small as BRepBuilder won't can build DirectShape => we need to ignore boxes in height < 1E-3
                    {                                     
                        u_center = u + ds / 2;
                        partWidth = ds;
                    }
                    else
                    {
                        var remainder_half = u_remainder / 2;
                        u_center = fBB.Max.U - remainder_half;
                        partWidth = u_remainder;
                    }
                    if (partWidth < 1E-20)
                        continue;

                    for (double v = vStart; v < fBB.Max.V; v += ds)
                    {
                        var deltaV = fBB.Max.V - ds;
                        if (v < deltaV || v_remainder < 1E-3) //Remanders can be so small as BRepBuilder won't can build DirectShape => we need to ignore boxes in height < 1E-3
                        {
                            v_center = v + ds / 2;
                            partHeight = ds;
                        }
                        else
                        {
                            var remainder_half = v_remainder / 2;
                            v_center = fBB.Max.V - remainder_half;
                            partHeight = v_remainder;
                        }
                        if (partHeight < 1E-20)
                            continue;

                        UV uv_center = new UV(u_center, v_center);
                        if (!face.IsInside(uv_center))
                            continue;

                        XYZ normal = ElementTransform.OfVector(face.ComputeNormal(uv_center));
                        XYZ normal_new = face.ComputeNormal(uv_center);
                        XYZ point_center = ElementTransform.OfPoint(face.Evaluate(uv_center));
                        XYZ point_center_new = face.Evaluate(uv_center);
                        SurfacePointWithValues p_center = new SurfacePointWithValues(uv_center, point_center, normal, partHeight, partWidth);
                        SurfacePointWithValues ins_point = new SurfacePointWithValues(uv_center, point_center, normal, partHeight, partWidth);
                        TruthCalcPoints.Add(p_center);
                       

                        //===//Логика формирования словаря, чтобы упорядочить точки в горизонтальной плоскости для КЕО
                        if (PointsInPlan.ContainsKey(p_center.Point3D.Z))
                        {
                            PointsInPlan[p_center.Point3D.Z].Add(p_center);
                        }
                        else
                        {
                            var listPoint = new List<SurfacePointWithValues>();
                            PointsInPlan.Add(p_center.Point3D.Z, listPoint);
                            PointsInPlan[p_center.Point3D.Z].Add(p_center);
                        }
                        //===========================================================================================

                        //===//Логика формирования словаря, чтобы упорядочить точки в горизонтальной плоскости для КЕО ПОСЛЕ РЕФАКТОРИНГА
                        if (PointsInPlan.ContainsKey(ins_point.Point3D.Z))
                        {
                            PointsInPlan[ins_point.Point3D.Z].Add(p_center);
                        }
                        else
                        {
                            var listPoint = new List<SurfacePointWithValues>();
                            PointsInPlan1.Add(ins_point.Point3D.Z, listPoint);
                            PointsInPlan1[ins_point.Point3D.Z].Add(ins_point);
                        }
                        //===========================================================================================

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
                MessageBox.Show("поймал");
                return false;
            }
        }
    }
}
