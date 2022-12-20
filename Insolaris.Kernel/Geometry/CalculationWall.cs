using Autodesk.Revit.DB;
using Insolaris.Geometry;
using Insolaris.Kernel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Kernel.Geometry
{
    public class CalculationWall
    {
        public XYZ Normal { get; set; }
        public XYZ Xdir { get; set; }
        public XYZ Ydir { get; set; }
        public CalculationSurface CalculationSurface { get; set; }
        public List<CustomWindow> Windows { get; set; }
        public List<ShadowObject> ShadowObjects { get; set; }
        public PointNLC[,] PointNLCs { get; set; } = new PointNLC[6, 10];
        public List<List<PointNLC>> PointNLCs1 { get; set; } = new List<List<PointNLC>>();
        public Transform LocalBasis { get; set; }  //Думаю понадобиться самостоятельно составить Transform для местной системы координат
        public CalculationWall(List<SurfacePointWithValues> ins_point, CalculationSurface surface)
        {
            CalculationSurface = surface;
            var localNormal = (CalculationSurface.Face as PlanarFace).FaceNormal;
            Normal = localNormal;
            Xdir = localNormal.CrossProduct(XYZ.BasisZ).Normalize();
            Ydir = XYZ.BasisZ.CrossProduct(localNormal).Normalize();
            var ds1 = 2000 / 304.8; //Размер окна задаст пользователь
            var ds2 = 2000 / 304.8; //Размер окна задаст пользователь
            CreateWindows(ins_point, ds1, ds2);
            var ds3 = 1000 / 304.8; //Размер сетки задаст пользователь
            var ds4 = 1000 / 304.8; //Размер сетки задаст пользователь
            var ds5 = 6000 / 304.8; //Глубина расчёта задаст пользователь
            CreateCalculationMesh(ins_point, ds3, ds4, ds5);
            CreateNormal();

        }
        private void CreateWindows(List<SurfacePointWithValues> ins_point, double height, double width)
        {
            //вычислить отметку пола
            //вычислить отметку рабочей поверхности 
            Windows = new List<CustomWindow>();

            //Обозначим хард-кодом размеры окна 2х2 метра
            //Стороны окна обозначены при взгляде изнутри помещения, лево-право определим через векторное произвередие BasisZ 
            foreach (var point in ins_point)
            { 
                var up = point.Point3D + XYZ.BasisZ * height / 2;
                var down = point.Point3D - XYZ.BasisZ * width / 2;
                var leftDirection = Normal.CrossProduct(XYZ.BasisZ).Normalize();
                var rightDirection = XYZ.BasisZ.CrossProduct(Normal).Normalize();
                var dl = leftDirection * width / 2;
                var dr = rightDirection * width / 2;
                var left = point.Point3D + leftDirection * width / 2;
                var right = point.Point3D + rightDirection * width / 2;
                var customWindow = new CustomWindow(up, down, left, right);
                Windows.Add(customWindow);
            }
        }
        private void CreateCalculationMesh(List<SurfacePointWithValues> ins_points, double normalStep, double ortoNormalStep, double depth)
        {
            
            var meshStart = ins_points.First().Point3D - (CalculationSurface.Face as PlanarFace).FaceNormal * depth;
            var p = new PointNLC();
            p.XYZ = meshStart;
            PointNLCs[0, 0] = p; 
            for (int i = 0; i < 6; i++)
            {
                for (int j = 1; j < 10; j++)
                {
                    var newP = new PointNLC();
                    newP.XYZ = PointNLCs[i, j - 1].XYZ + Xdir * ortoNormalStep;
                    PointNLCs[i, j] = newP;
                } 
                if (i == 5)
                {
                    continue;
                }
                var newColomn = new PointNLC();
                newColomn.XYZ = PointNLCs[i, 0].XYZ + Ydir * normalStep;
                PointNLCs[i + 1, 0] = newColomn;
            }

        }
        private void CreateNormal()
        {


        }
    }
}
