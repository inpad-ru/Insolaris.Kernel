using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Insolaris.Utils;

namespace Insolaris.Geometry
{
    public sealed class InsolationCone
    {
        public DateTime StartDateTime { get; }
        public DateTime EndDateTime { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public double HalfAngle { get; private set; }
        private double CastingRange { get; set; }
        public XYZ StartVector { get; private set; }
        public XYZ EndVector { get; private set; }
        public XYZ ConeFrameX { get; private set; }
        public XYZ ConeFrameY { get; private set; }
        public XYZ ConeFrameZ { get; private set; }
        public Solid GenericConeSolid { get; private set; }

        public double PeriodInRadians { get; private set; }

        public InsolationCone(SunAndShadowSettings settings)
        {
            if (settings.SunAndShadowType != SunAndShadowType.OneDayStudy)
                throw new InvalidOperationException("One day study sun settings required.");
            
            StartDateTime = settings.StartDateAndTime;
            EndDateTime = settings.EndDateAndTime;
            Latitude = settings.Latitude;
            Longitude = settings.Longitude;

            CalculateGeometrySettings(settings);
        }

        private void CalculateGeometrySettings(SunAndShadowSettings settings)
        {
            DateTime oldActiveTime = settings.ActiveFrameTime;

            DateTime start = settings.StartDateAndTime;
            DateTime end = settings.EndDateAndTime;
            DateTime mid = start + new TimeSpan((end - start).Ticks / 2);

            settings.StartDateAndTime = start;
            double startAzimuth = settings.GetFrameAzimuth(settings.ActiveFrame);
            XYZ startVector = InsolationCalculationUtils.GetSunDirection(settings.GetFrameAltitude(settings.ActiveFrame), settings.GetFrameAzimuth(settings.ActiveFrame));
            settings.StartDateAndTime = mid;
            XYZ midVector = InsolationCalculationUtils.GetSunDirection(settings.GetFrameAltitude(settings.ActiveFrame), settings.GetFrameAzimuth(settings.ActiveFrame));
            settings.StartDateAndTime = end;
            double endAzimuth = settings.GetFrameAzimuth(settings.ActiveFrame);
            XYZ endVector = InsolationCalculationUtils.GetSunDirection(settings.GetFrameAltitude(settings.ActiveFrame), settings.GetFrameAzimuth(settings.ActiveFrame));

            settings.StartDateAndTime = oldActiveTime;

            Arc normalInsolationArc = Arc.Create(startVector, endVector, midVector);
            HalfAngle = Math.Asin(normalInsolationArc.Radius);
            CastingRange = 29000 / Math.Sin(HalfAngle); //Maximum allowed length of an arc's radius in Revit is 30000, thus the casting length is also limited.
            PeriodInRadians = normalInsolationArc.Length / normalInsolationArc.Radius;
            StartVector = startVector;
            EndVector = endVector;
            ConeFrameX = (normalInsolationArc.GetEndPoint(0) - normalInsolationArc.Center).Normalize();
            ConeFrameZ = normalInsolationArc.Center.Normalize();
            ConeFrameY = PeriodInRadians < Math.PI ? ConeFrameZ.CrossProduct(ConeFrameX) : -ConeFrameZ.CrossProduct(ConeFrameX);
        }

        /// <summary>
        /// Creates a specific conical solid in a given apex point and boundaries.
        /// </summary>
        /// <param name="center">Cone's apex translation.</param>
        /// <param name="normal">Normal vector of calculation point on its surface.</param>
        /// <param name="boundingConeHalfAngle">If calculation context defies a bounding cone along point's normal, then angle (0,pi/2), otherwise 0</param>
        /// <returns></returns>
        public Solid GetConeSolid(XYZ center, XYZ normal, double boundingConeHalfAngle)
        {
            CreateOrUpdateGeneralConeSolid();

            Solid cone = SolidUtils.Clone(GenericConeSolid);

            bool isBoundingConeOrPlane = true;
            if (MathUtils.AreDoublesAlmostEqual(0, boundingConeHalfAngle) || MathUtils.AreDoublesAlmostEqual(Math.PI / 2, boundingConeHalfAngle))
                isBoundingConeOrPlane = false;

            XYZ cutPlaneNormal = isBoundingConeOrPlane
                ? InsolationCalculationUtils.GetPlaneNormalOfTwoIntersectingCones(normal, ConeFrameZ, boundingConeHalfAngle, HalfAngle)
                : normal;


            Plane cutPlane = Plane.CreateByNormalAndOrigin(cutPlaneNormal, XYZ.Zero);
            CurveLoop cl = new CurveLoop();
            double dist = CastingRange * 2;
            cl.Append(Line.CreateBound(cutPlane.YVec * dist - cutPlane.XVec * dist, -cutPlane.XVec * dist - cutPlane.YVec * dist));
            cl.Append(Line.CreateBound(-cutPlane.XVec * dist - cutPlane.YVec * dist, cutPlane.XVec * dist - cutPlane.YVec * dist));
            cl.Append(Line.CreateBound(cutPlane.XVec * dist - cutPlane.YVec * dist, cutPlane.XVec * dist + cutPlane.YVec * dist));
            cl.Append(Line.CreateBound(cutPlane.XVec * dist + cutPlane.YVec * dist, -cutPlane.XVec * dist + cutPlane.YVec * dist));

            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { cl }, cutPlane.Normal, dist);

            try
            {
                BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(cone, planeSolid, BooleanOperationsType.Intersect);
            }
            catch
            {
                return null;
            }

            if (cone == null)
                return null;

            Transform trans = Transform.Identity;
            trans.Origin = center;
            cone = SolidUtils.CreateTransformed(cone, trans);

            return cone;
        }

        public bool IsPointInsideCone(XYZ point, XYZ coneApex)
        {
            XYZ normal = (point - coneApex).Normalize();
            double angle = normal.AngleTo(ConeFrameZ);
            if (angle > HalfAngle)
                return false;
            return true;
        }

        /// <summary>
        /// This method creates a single-face solid of an insolation cone in a given point.
        /// </summary>
        public void CreateOrUpdateGeneralConeSolid()
        {
            if (GenericConeSolid != null && GenericConeSolid.Volume > 0)
                return;

            XYZ center = new XYZ();
            Frame coneFrame = new Frame(center, ConeFrameX, ConeFrameY, ConeFrameZ);
            ConicalSurface surf = ConicalSurface.Create(coneFrame, HalfAngle);

            XYZ castedStart = center + StartVector * CastingRange;
            XYZ castedEnd = center + EndVector * CastingRange;
            Line startLine = Line.CreateBound(castedStart, center);
            Arc insolationArc = Arc.Create(center + ConeFrameZ * CastingRange * Math.Cos(HalfAngle),
                CastingRange * Math.Sin(HalfAngle), 
                0, 
                PeriodInRadians, 
                ConeFrameX, 
                ConeFrameY);

            BRepBuilder breper = new BRepBuilder(BRepType.OpenShell);
            var brepSurf = BRepBuilderSurfaceGeometry.Create(surf, null);
            var faceId = breper.AddFace(brepSurf, false);

            BRepBuilderEdgeGeometry edge1 = BRepBuilderEdgeGeometry.Create(startLine);
            BRepBuilderEdgeGeometry edge2 = BRepBuilderEdgeGeometry.Create(center, castedEnd);
            BRepBuilderEdgeGeometry edge3 = BRepBuilderEdgeGeometry.Create(insolationArc.CreateReversed());

            //if (PeriodInRadians == Math.PI)
            //{
            //    Almost impossible but if errors occure due to this reason, it is because
            //    the insolation surface is no longer conical but rather planar.
            //}

            var edgeId1 = breper.AddEdge(edge1);
            var edgeId2 = breper.AddEdge(edge2);
            var edgeId3 = breper.AddEdge(edge3);

            var surfLoopId = breper.AddLoop(faceId);
            breper.AddCoEdge(surfLoopId, edgeId1, false);
            breper.AddCoEdge(surfLoopId, edgeId2, false);
            breper.AddCoEdge(surfLoopId, edgeId3, false);
            breper.FinishLoop(surfLoopId);
            breper.FinishFace(faceId);

            XYZ tempTriangleVertex1 = -ConeFrameZ;
            XYZ tempTriangleVertex2 = (center - castedStart).Normalize();

            Plane tempTrianglePlane = Plane.CreateByThreePoints(center, tempTriangleVertex1, tempTriangleVertex2);
            // Single-face solid cannot be created via BrepBuilder even if it's an OpenShell, so you have add an additional face
            var brepPlane = BRepBuilderSurfaceGeometry.Create(tempTrianglePlane, null);
            var planeId = breper.AddFace(brepPlane, false);
            var planeLoopId = breper.AddLoop(planeId);

            Line tempTriangleLine1 = Line.CreateBound(center, tempTriangleVertex1);
            Line tempTriangleLine2 = Line.CreateBound(tempTriangleVertex1, tempTriangleVertex2);
            Line tempTriangleLine3 = Line.CreateBound(tempTriangleVertex2, center);
            var edge4 = BRepBuilderEdgeGeometry.Create(tempTriangleLine1);
            var edge5 = BRepBuilderEdgeGeometry.Create(tempTriangleLine2);
            var edge6 = BRepBuilderEdgeGeometry.Create(tempTriangleLine3);
            var edgeId4 = breper.AddEdge(edge4);
            var edgeId5 = breper.AddEdge(edge5);
            var edgeId6 = breper.AddEdge(edge6);

            breper.AddCoEdge(planeLoopId, edgeId4, false);
            breper.AddCoEdge(planeLoopId, edgeId5, false);
            breper.AddCoEdge(planeLoopId, edgeId6, false);

            breper.FinishLoop(planeLoopId);
            breper.FinishFace(planeId);

            bool isAvailable = breper.IsResultAvailable();
            var outcomeResult = breper.Finish();
            GenericConeSolid = breper.GetResult();

            Plane cutPlane = Plane.CreateByNormalAndOrigin(-ConeFrameZ, center);
            double cutPrismHalfWidth = 100;
            XYZ cutPrismV1 = cutPlane.Origin + cutPlane.XVec * cutPrismHalfWidth + cutPlane.YVec * cutPrismHalfWidth;
            XYZ cutPrismV2 = cutPrismV1 - cutPlane.YVec * 2 * cutPrismHalfWidth;
            XYZ cutPrismV3 = cutPrismV2 - cutPlane.XVec * 2 * cutPrismHalfWidth;
            XYZ cutPrismV4 = cutPrismV3 + cutPlane.YVec * 2 * cutPrismHalfWidth;

            CurveLoop cutTempLoop = new CurveLoop();
            cutTempLoop.Append(Line.CreateBound(cutPrismV1, cutPrismV2));
            cutTempLoop.Append(Line.CreateBound(cutPrismV2, cutPrismV3));
            cutTempLoop.Append(Line.CreateBound(cutPrismV3, cutPrismV4));
            cutTempLoop.Append(Line.CreateBound(cutPrismV4, cutPrismV1));

            Solid cutTempSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { cutTempLoop }, cutPlane.Normal, cutPrismHalfWidth);
            BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(GenericConeSolid, cutTempSolid, BooleanOperationsType.Difference);
        }
    }
}
