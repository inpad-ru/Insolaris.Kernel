using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Kernel.Geometry
{
    public class PointNLC : CalculationPoint
    {
        public UV PointUV { get; }
        public XYZ Point3D { get; set; }
        public XYZ Normal { get; }
        public double Width { get; }
        public double Height { get; set; }
        public Calculation.CalculationResult CalculationResult { get; set; }

        //После рефакторинга
        public XYZ XYZ { get; set; } = XYZ.Zero;
        public double NLC { get; set; } //КЕО
        public bool IsEnough { get; set; } //Достаточно ли света, если да, то те точки, что ближе к стене в двумерном массиве, стоящие на данном столбце НЕ будем рассчитывать
        public PointNLC()
        {

        }
        public PointNLC(UV uv, XYZ p, XYZ normal, double h, double w)
        {
            PointUV = uv;
            Point3D = p;
            Normal = normal;
            Height = h;
            Width = w;

            //Построим окно на оснввнии данной точки
            //Вызовем данный метод из КЕО калькулятора, так для инсоляции этот функционал не нужен
        }
    }
}
