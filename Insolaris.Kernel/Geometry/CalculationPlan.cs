using Autodesk.Revit.DB;
using Insolaris.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Kernel.Geometry
{
    public class CalculationPlan
    {
        private Face face;
        public Reference FaceReference { get; }
        public Dictionary<CalculationSurface, List<SurfacePointWithValues>> CalculationPoints { get; } = new Dictionary<CalculationSurface, List<SurfacePointWithValues>>(); //Словарь в котором есть все точки плиток с поверхности по всем поверхностям
        public Dictionary<CalculationSurface, List<CustomWindow>> Windows { get; } = new Dictionary<CalculationSurface, List<CustomWindow>>(); //Словарь в котором есть все окна с поверхности по всем поверхностям
        public Dictionary<CalculationSurface, List<XYZ>> TruthCalculationPoints { get; } = new Dictionary<CalculationSurface, List<XYZ>>(); //Тот же словарь, что и CalculationPoints, только вместо одной точки их стало 6 и они последовательно отдаляются от стены
        public List<SurfacePointWithValues> NLCCalcPoints { get; }
        public bool IsPartitioned { get; private set; }
        public Face Face => face;
        public double FaceArea { get; private set; }
        public Transform ElementTransform { get; }
        public double Elevation { get; private set; }
        public double ds { get; } //Под стиль старого кода это будет SurfaceIncrement
        public double MeshStepParallelNormal { get; } //Шаг сетки параллельной нормали поверхности здания //Задаёт пользователь (пока не задаём)
        public double MeshStepOrtoNormal { get; set; } = 0; //Шаг сетки перпендикулярной нормали поверхности здания //Задаёт пользователь (будет 6 точек по 10000
        public CalculationPlan(Face f, Transform elementTransform, CalculationSurface surface, List<SurfacePointWithValues> points)
        {
            if (!(f is PlanarFace))
                throw new InvalidOperationException($"Face is not planar, it is {f.GetType().Name}. This type of face cannot be partitioned yet.");

            face = f;
            FaceArea = f.Area;
            FaceReference = f.Reference;
            //TruthCalcPoints = new List<SurfacePointWithValues>();   // Осталось из старого кода
            this.ElementTransform = elementTransform;
            CalculationPoints.Add(surface, points);
            var ds = 3000 / 304.8;                                 //ВНИМАНИЕ!!! Это заглушка, притянуть ds из viewModel, притом эта ds отвечает за высоту этажа,
                                                                   //но от пользователя ещё потребуется и сама высота окна и расстояние от окна до пола
            this.ds = ds;
            CreateNaturalLightCalcPoint(surface, points);

        }
        public bool CreateNaturalLightCalcPoint(CalculationSurface surface, List<SurfacePointWithValues> points) //Метод, который формирует расчётную плоскость с помощью пользовательских данных
        {
            if (face == null)
                throw new ArgumentNullException("Calculation Face"); //Не знаю зачем эта проверка
            CalculationPoints.Clear();
            try
            {
                var listPoint = new List<XYZ>();
                var listWindows = new List<CustomWindow>();
                TruthCalculationPoints.Add(surface, listPoint);
                Windows.Add(surface, listWindows);
                var normal = (surface.Face as PlanarFace).FaceNormal;
                foreach (var p in points)
                {
                    var window = p.CreateWindow();                                          //Создание окна рядом с расчётной точкой
                    listWindows.Add(window);                                                //Добавление окна в словарь
                    var point = p.Point3D;
                    var projectPointOnFloor = point + (-XYZ.BasisZ) * (ds / 2);             //Опустил точку на пол
                    for (double i = 0; i < 6000 / 304.8; i += 1000 / 304.8 )
                    {
                        var truthCalcPoint = projectPointOnFloor + (-normal) * (i);  //Подвинул точку вглубь здания на 6 метров (но мне нужно не только на 6)
                        listPoint.Add(truthCalcPoint);                                                //В будущем по необходимости нужно сменить тип XYZ на SurfacePointWithValues
                    }
                    
                }
                IsPartitioned = true; //Это из старого кода, теперь полное формирование объекта происходит до калькулятора как в инсоляции, так и в КЕО
                return true;
            }
            catch
            {
                return false;
            } 
        }

    }
}
