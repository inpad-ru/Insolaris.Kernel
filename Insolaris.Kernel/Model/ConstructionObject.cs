using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Insolaris.Geometry;
using Insolaris.Kernel.Geometry;

namespace Insolaris.Model
{
    public abstract class ConstructionObject //Надо будет взять в констукторе solids, а верх и боковые грани в разых методах
    {
        public Element RevitElement { get; protected set; }
        public List<Geometry.CalculationSurface> Surfaces { get; protected set; }
        public Dictionary<double, CalculationPlan> CalculationPlans { get; set; } = new Dictionary<double, CalculationPlan>();
        public int CalculationPointsNumber { get; private set; }
        public double TotalCalculationSurfaceArea { get; private set; }
        public bool IsShading { get; set; } = true;
        public bool IsSelected { get; set; } = true;
        public string Name { get; set; }
        public Transform TransformOfObject { get; set; }
        public double ToleranceNLC { get; set; }
        
        //После рефакторинга это уже будет не словарь, а лист 
        public List<CalculationPlan> CalculationPlans1 { get; set; }



        protected static List<Geometry.CalculationSurface> GetCalculationSurfaces(Element elem, bool isBuildingOrArea)   
        {
            List<Geometry.CalculationSurface> surfaces = new List<Geometry.CalculationSurface>();
         
            Options geomOptions = new Options { ComputeReferences = true };
            var geometry = elem.get_Geometry(geomOptions);
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            Transform elemTransform = (elem as FamilyInstance).GetTotalTransform();
            ConcurrentBag<Face> faces = new ConcurrentBag<Face>();
            //Parallel.ForEach(geometry, go =>
            foreach (GeometryObject go in geometry)
            {
                if (go is Solid sol)
                {
                    foreach (Face face in sol.Faces)
                        faces.Add(face);
                }
                else if (go is GeometryInstance gi)
                {
                    var instGeometry = gi.GetSymbolGeometry();
                    foreach (var instObj in instGeometry)
                    {
                        if (instObj is Solid solid)
                        {
                            foreach (Face f in solid.Faces)
                                faces.Add(f);
                        }
                    }
                }
            }//);

            //if (faces.Count == 0)
            //Parallel.ForEach(faces, face =>
            foreach (Face face in faces)
            {
                if (!(face is PlanarFace))
                    continue;//return;//

                double midZ = 0.70710678118;

                var fBB = face.GetBoundingBox();
                UV bbCenter = (fBB.Min + fBB.Max) / 2;
                XYZ point3D = face.Evaluate(bbCenter);
                XYZ normal = face.ComputeNormal(bbCenter);

                if (isBuildingOrArea && (normal.Z > midZ || normal.Z < -midZ))
                    continue;//return;//
                if (!isBuildingOrArea && normal.Z < midZ)
                    continue;//return;//
                var surf = new CalculationSurface(face, elemTransform);
                surfaces.Add(surf);
            }//);
            return surfaces;
        }

        public void CreateSurfacePartition(double ds)
        {
            if (ds == 0)
                throw new InvalidCastException("ds is zero");

            int pointCount = 0;
            double area = 0;
            //Parallel.ForEach(Surfaces, surf =>
            foreach (var surf in Surfaces)
            {
                if(!surf.CreateOrUpdatePartition(ds))
                {
                    MessageBox.Show("Не построилась точка - центр плитки");
                };
                pointCount += surf.TruthCalcPoints.Count;
                area += surf.FaceArea;
            }//);
            CalculationPointsNumber = pointCount;
            TotalCalculationSurfaceArea = area;
        }

        public Dictionary<double, CalculationPlan> GetCalculationPlans(Element elem)
        {
            var calculationPlans = new Dictionary<double, CalculationPlan>();
            foreach (var surf in Surfaces)
            {
            
                var transform = surf.ElementTransform;
                var face = surf.Face;
                var pointInPlan = surf.PointsInPlan;
                foreach (var pair in pointInPlan)
                {
                    var elevation = pair.Key;                
                    var points = pair.Value;
                    if (CalculationPlans.ContainsKey(elevation))
                    {
                        CalculationPlans[elevation].CalculationPoints.Add(surf,points);
                    }
                    else
                    {
                        var calcPlan = new CalculationPlan(face, transform, surf, points);
                        CalculationPlans.Add(elevation, calcPlan);
                        calculationPlans.Add(elevation, calcPlan);
                    }
                }
            }
            return calculationPlans;
        }
        public List<CalculationPlan> GetCalculationPlans1(Element elem, 
                                                          double roomWidth, 
                                                          double levelHeight,
                                                          double wallThickness,
                                                          double windowWidth,
                                                          double windowHeight,
                                                          double calculationDepth,
                                                          double meshOrtoNormalStep,
                                                          double meshNormalStep
                                                          )
        {
            var calculationPlans = new List<CalculationPlan>();
            var surfForPlanCreate = Surfaces.First();
            var dictForPlanCreate = surfForPlanCreate.PointsInPlan1;
            foreach (var pair in dictForPlanCreate)
            {
                var plan = new CalculationPlan();
                plan.Elevation = pair.Key;
                calculationPlans.Add(plan);
            }
            //Теперь есть лист планов

            foreach (var surf in Surfaces)
            {
                foreach (var pair in surf.PointsInPlan1)
                {
                    var calculationWall = new CalculationWall(pair.Value, 
                                                              surf,
                                                              roomWidth / 304.8,
                                                              levelHeight / 304.8,
                                                              wallThickness / 304.8,
                                                              windowWidth / 304.8,
                                                              windowHeight / 304.8,
                                                              calculationDepth / 304.8,
                                                              meshOrtoNormalStep / 304.8,
                                                              meshNormalStep / 304.8
                                                              ); //создали стенку 
                    var plan = calculationPlans.Where(x => x.Elevation == pair.Key).FirstOrDefault();//Можно оптимизировать с помощью CalculationPlan ---> Dictionary <double, List<CalculationWalls>>
                    plan.CalculationWalls.Add(calculationWall);
                }
            }

            return calculationPlans;
        }

        /*public Transform CreateTransform(Element element)
        {
            Options geomOptions = new Options { ComputeReferences = true };
            var geometry = element.get_Geometry(geomOptions);
            PlanarFace planar = null;
            foreach (GeometryObject go in geometry)
            {
                if (go is Solid)
                {
                    var sol = go as Solid;
                    foreach (Face face in sol.Faces)
                    {
                        if ((face as PlanarFace).FaceNormal == XYZ.BasisZ)
                        {
                            planar = face as PlanarFace;
                        }
                    }
                }
            }
            if (planar == null)
            {
                return null;
            }
        }*/

    }
}
