using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Insolaris.Utils;

namespace Insolaris.Geometry
{
    internal class InsolationCone
    {
        private const double RadianSecondsRatio = 0.00007272205;
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
        private Solid GenericConeSolid { get; set; }

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
            CastingRange = 39999 / Math.Sin(HalfAngle); //Maximum allowed length of an arc's radius in Revit is 40000, thus the casting length is also limited.
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

            XYZ cutPlaneNormal = boundingConeHalfAngle == 0 
                ? normal 
                : InsolationCalculationUtils.GetPlaneNormalOfTwoIntersectingCones(normal, ConeFrameZ, boundingConeHalfAngle, HalfAngle);

            Plane cutPlane = Plane.CreateByNormalAndOrigin(cutPlaneNormal, XYZ.Zero);
            CurveLoop cl = new CurveLoop();
            double dist = CastingRange;
            cl.Append(Line.CreateBound(cutPlane.YVec * dist - cutPlane.XVec * dist, -cutPlane.XVec * dist - cutPlane.YVec * dist));
            cl.Append(Line.CreateBound(-cutPlane.XVec * dist - cutPlane.YVec * dist, cutPlane.XVec * dist - cutPlane.YVec * dist));
            cl.Append(Line.CreateBound(cutPlane.XVec * dist - cutPlane.YVec * dist, cutPlane.XVec * dist + cutPlane.YVec * dist));
            cl.Append(Line.CreateBound(cutPlane.XVec * dist + cutPlane.YVec * dist, -cutPlane.XVec * dist + cutPlane.YVec * dist));

            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { cl }, cutPlane.Normal, CastingRange);
            BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(cone, planeSolid, BooleanOperationsType.Intersect);

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
        private void CreateOrUpdateGeneralConeSolid()
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

            Plane conePlane = Plane.CreateByThreePoints(startLine.GetEndPoint(0), startLine.GetEndPoint(1) - XYZ.BasisZ, startLine.GetEndPoint(1));
            Plane cutPlane = Plane.CreateByThreePoints(startLine.GetEndPoint(1), castedEnd, startLine.GetEndPoint(0));
            Line startOffsetLine = Line.CreateBound(startLine.GetEndPoint(0) + cutPlane.Normal, startLine.GetEndPoint(1) + cutPlane.Normal);
            // Single-face solid cannot be created via BrepBuilder even if it's an OpenShell, so you have add an additional face
            // Consider eliminating unnecessary geometry by cuting it with BooleanOperationsUtils.CutWithHalfSpace()
            var brepPlane = BRepBuilderSurfaceGeometry.Create(conePlane, null);
            var planeId = breper.AddFace(brepPlane, false);
            var planeLoopId = breper.AddLoop(planeId);

            var edge4 = BRepBuilderEdgeGeometry.Create(startLine.GetEndPoint(0), startOffsetLine.GetEndPoint(0));
            var edge5 = BRepBuilderEdgeGeometry.Create(startOffsetLine);
            var edge6 = BRepBuilderEdgeGeometry.Create(startOffsetLine.GetEndPoint(1), startLine.GetEndPoint(1));
            var edgeId4 = breper.AddEdge(edge4);
            var edgeId5 = breper.AddEdge(edge5);
            var edgeId6 = breper.AddEdge(edge6);

            breper.AddCoEdge(planeLoopId, edgeId1, true);
            breper.AddCoEdge(planeLoopId, edgeId4, false);
            breper.AddCoEdge(planeLoopId, edgeId5, false);
            breper.AddCoEdge(planeLoopId, edgeId6, false);

            breper.FinishLoop(planeLoopId);
            breper.FinishFace(planeId);

            bool isAvailable = breper.IsResultAvailable();
            var outcomeResult = breper.Finish();
            GenericConeSolid = breper.GetResult();

            CurveLoop cutTempLoop = new CurveLoop();
            cutTempLoop.Append(Line.CreateBound(startLine.GetEndPoint(1), startLine.GetEndPoint(0)));
            cutTempLoop.Append(Line.CreateBound(startLine.GetEndPoint(0), insolationArc.GetEndPoint(1)));
            cutTempLoop.Append(Line.CreateBound(insolationArc.GetEndPoint(1), startLine.GetEndPoint(1)));
            Solid cutTempSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { cutTempLoop }, cutPlane.Normal, 10);
            BooleanOperationsUtils.ExecuteBooleanOperationModifyingOriginalSolid(GenericConeSolid, cutTempSolid, BooleanOperationsType.Difference);
        }
    }
}
