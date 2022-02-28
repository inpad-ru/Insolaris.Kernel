using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Utils
{
    internal static class InsolationCalculationUtils
    {
        public static double RadianSecondsRatio { get; } = 0.00007272205;

        internal static XYZ GetPlaneNormalOfTwoIntersectingCones(XYZ boundingConeAxis, XYZ boundableConeAxis, double boundingHalfAngle, double boundableHalfAngle)
        {
            double b1 = Math.Cos(boundableHalfAngle);
            double b2 = Math.Cos(boundingHalfAngle);
            XYZ cutPlaneNormal = new XYZ(
                boundingConeAxis.X * b1 - boundableConeAxis.X * b2,
               boundingConeAxis.Y * b1 - boundableConeAxis.Y * b2,
               boundingConeAxis.Z * b1 - boundableConeAxis.Z * b2).Normalize();

            return cutPlaneNormal;
        }

        internal static Solid CreateBoundingCone(XYZ apex, XYZ axis, double halfAngle, double height)
        {
            double genLength = height / Math.Cos(halfAngle);
            double radius = height / Math.Sin(halfAngle);
            XYZ foundationCenter = apex + height * axis;
            Plane foundationPlane = Plane.CreateByNormalAndOrigin(axis, foundationCenter);
            Arc arc1 = Arc.Create(foundationPlane, radius, 0, Math.PI);
            Arc arc2 = Arc.Create(foundationPlane, radius, Math.PI, Math.PI * 2);
            XYZ generatorNormal = (foundationCenter + foundationPlane.XVec * radius - apex).Normalize();
            Frame coneFrame = new Frame(apex, foundationPlane.XVec, foundationPlane.YVec, axis);
            ConicalSurface conicalSurf = ConicalSurface.Create(coneFrame, halfAngle);

            BRepBuilder builder = new BRepBuilder(BRepType.Solid);
            var conicalBuildSurface = BRepBuilderSurfaceGeometry.Create(conicalSurf, null);
            var foundationBuildPlane = BRepBuilderSurfaceGeometry.Create(foundationPlane, null);
            BRepBuilderGeometryId conicalBuildFaceId = builder.AddFace(conicalBuildSurface, false);
            BRepBuilderGeometryId foundationBuildFaceId = builder.AddFace(foundationBuildPlane, false);
            BRepBuilderGeometryId conicalLoopId = builder.AddLoop(conicalBuildFaceId);
            BRepBuilderGeometryId foundationLoopId = builder.AddLoop(foundationBuildFaceId);

            var edge1 = BRepBuilderEdgeGeometry.Create(arc1);
            var edge2 = BRepBuilderEdgeGeometry.Create(arc2);
            BRepBuilderGeometryId edgeId1 = builder.AddEdge(edge1);
            BRepBuilderGeometryId edgeId2 = builder.AddEdge(edge2);

            builder.AddCoEdge(foundationLoopId, edgeId1, false);
            builder.AddCoEdge(foundationLoopId, edgeId2, false);
            builder.FinishLoop(foundationLoopId);
            builder.FinishFace(foundationBuildFaceId);

            builder.AddCoEdge(conicalLoopId, edgeId2, true);
            builder.AddCoEdge(conicalLoopId, edgeId1, true);
            builder.FinishLoop(conicalLoopId);
            builder.FinishFace(conicalBuildFaceId);

            bool isAvailable = builder.IsResultAvailable();
            var outcome = builder.Finish();
            return builder.GetResult();
        }

        public static XYZ GetSunDirection(double altitude, double azimuth) // Revit azimuth is an angle to (0, 1, 0), which is North, ranged (-Pi; Pi]
        {
            XYZ initialDirection = XYZ.BasisY;
            Transform altitudeRotation = Transform.CreateRotation(XYZ.BasisX, altitude);
            XYZ altitudeDirection = altitudeRotation.OfVector(initialDirection);
            double actualAzimuth = 2 * Math.PI - azimuth;

            Transform azimuthRotation = Transform.CreateRotation(XYZ.BasisZ, actualAzimuth);

            return azimuthRotation.OfVector(altitudeDirection);
        }
    }
}
