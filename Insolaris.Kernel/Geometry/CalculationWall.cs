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
        public CalculationSurface CalculationSurface { get; set; }
        public List<CustomWindow> Windows { get; set; }
        public List<ShadowObject> ShadowObjects { get; set; }
        public PointNLC[,] PointNLCs { get; set; }
        public Transform LocalBasis { get; set; }  //Думаю понадобиться самостоятельно составить Transform для местной системы координат
        public CalculationWall(List<SurfacePointWithValues> ins_point)
        {
            var ds1 = 2000 / 304.8; //Размер окна задаст пользователь
            var ds2 = 2000 / 304.8; //Размер окна задаст пользователь
            CreateWindows(ins_point, ds1, ds2);
            var ds3 = 1000 / 304.8; //Размер сетки задаст пользователь
            var ds4 = 1000 / 304.8; //Размер сетки задаст пользователь
            var ds5 = 6000 / 304.8; //Глубина расчёта задаст пользователь
            CreateCalculationMesh(ins_point, ds3, ds4, ds5);

        }
        private void CreateWindows(List<SurfacePointWithValues> ins_point, double height, double width)
        {
            //вычислить отметку пола
            //вычислить отметку рабочей поверхности 
            Windows = new List<CustomWindow>();
        }
        private void CreateCalculationMesh(List<SurfacePointWithValues> ins_points, double normalStep, double ortoNormalStep, double depth)
        {



            //PointNLCs 
        }
    }
}
