using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Model
{
    public abstract class ConstructionObject
    {
        public Element RevitElement { get; protected set; }
        public List<Geometry.CalculationSurface> Surfaces { get; protected set; }
        public int CalculationPointsNumber { get; private set; }
        public double TotalCalculationSurfaceArea { get; private set; }
        public bool IsShading { get; set; } = true;
        public bool IsSelected { get; set; } = true;
        public string Name { get; set; }

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

                surfaces.Add(new Geometry.CalculationSurface(face, elemTransform));
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
                surf.CreateOrUpdatePartition(ds);
                pointCount += surf.CalculationPoints.Count;
                area += surf.FaceArea;
            }//);
            CalculationPointsNumber = pointCount;
            TotalCalculationSurfaceArea = area;
        }
    }
}
