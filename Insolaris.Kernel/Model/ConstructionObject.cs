using System;
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

        protected static List<Geometry.CalculationSurface> GetCalculationSurfaces(Element elem, bool isBuildingOrArea)   
        {
            List<Geometry.CalculationSurface> surfaces = new List<Geometry.CalculationSurface>();
         
            Options geomOptions = new Options { ComputeReferences = true };
            var geometry = elem.get_Geometry(geomOptions);
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            List<Face> faces = new List<Face>();
            foreach (GeometryObject go in geometry)
            {
                if (go is Solid sol)
                {
                    foreach (Face face in sol.Faces)
                        faces.Add(face);
                }
                else if (go is GeometryInstance gi)
                {
                    var instGeometry = gi.GetInstanceGeometry();
                    foreach (var instObj in instGeometry)
                    {
                        if (instObj is Solid solid)
                        {
                            foreach (Face f in solid.Faces)
                                faces.Add(f);
                        }
                    }
                }
            }

            //if (faces.Count == 0)

            foreach (Face face in faces)
            {
                if (!(face is PlanarFace)) continue;

                double midZ = 0.70710678118;

                var fBB = face.GetBoundingBox();
                UV bbCenter = (fBB.Min + fBB.Max) / 2;
                XYZ point3D = face.Evaluate(bbCenter);
                XYZ normal = face.ComputeNormal(bbCenter);

                if (isBuildingOrArea && normal.Z > midZ)
                    continue;
                if (!isBuildingOrArea && normal.Z < midZ)
                    continue;

                surfaces.Add(new Geometry.CalculationSurface(face));
            }
            return surfaces;
        }

        protected void CreateSurfacePartition(double ds)
        {
            int pointCount = 0;
            double area = 0;
            foreach (var surf in Surfaces)
            {
                surf.CreateOrUpdatePartition(ds);
                pointCount += surf.CalculationPoints.Count;
                area += surf.FaceArea;
            }
            CalculationPointsNumber = pointCount;
            TotalCalculationSurfaceArea = area;
        }
    }
}
