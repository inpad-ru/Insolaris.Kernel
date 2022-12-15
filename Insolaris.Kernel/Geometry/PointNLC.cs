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
        public XYZ Point3D { get; }
        public XYZ Normal { get; }
        public double Width { get; }
        public double Height { get; set; }
        public Calculation.CalculationResult CalculationResult { get; set; }

        //После рефакторинга
        public XYZ XYZ { get; set; }
        public double NLC { get; set; } //КЕО
        public bool IsEnough { get; set; } //Достаточно ли света, если да, то те точки, что ближе к стене в двумерном массиве, стоящие на данном столбце НЕ будем рассчитывать
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
        public CustomWindow CreateWindow()
        {
            //Обозначим хард-кодом размеры окна 2х2 метра
            //Стороны окна обозначены при взгляде изнутри помещения, лево-право определим через векторное произвередие BasisZ 
            var winHeight = 2000 / 304.8;
            var winWidth = 2000 / 304.8;

            var up = Point3D + XYZ.BasisZ * winHeight / 2;
            var down = Point3D - XYZ.BasisZ * winHeight / 2;

            var leftDirection = Normal.CrossProduct(XYZ.BasisZ).Normalize();
            var rightDirection = XYZ.BasisZ.CrossProduct(Normal).Normalize();
            var dl = leftDirection * winWidth / 2;
            var dr = rightDirection * winWidth / 2;
            var left = Point3D + leftDirection * winWidth / 2;
            var right = Point3D + rightDirection * winWidth / 2;

            var customWindow = new CustomWindow(up, down, left, right);

            return customWindow;
        }
    }
}
