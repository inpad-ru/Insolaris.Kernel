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
        public Dictionary<CalculationSurface, List<SurfacePointWithValues>> CalculationPoints { get; } = new Dictionary<CalculationSurface, List<SurfacePointWithValues>>(); //Словарь в котором есть все точки плиток с поверхности по всем поверхностям
        public Dictionary<CalculationSurface, List<CustomWindow>> Windows { get; } = new Dictionary<CalculationSurface, List<CustomWindow>>(); //Словарь в котором есть все окна с поверхности по всем поверхностям
        public Dictionary<CalculationSurface, List<XYZ>> TruthCalculationPoints { get; } = new Dictionary<CalculationSurface, List<XYZ>>(); //Тот же словарь, что и CalculationPoints, только вместо одной точки их стало 6 и они последовательно отдаляются от стены

        // Свойства после рефакторинга
        public double Elevation { get; set; }
        public List<CalculationWall> CalculationWalls {get; set;}



        public CalculationPlan(Face f, Transform elementTransform, CalculationSurface surface, List<SurfacePointWithValues> points)
        {

            CalculationPoints.Add(surface, points);        
            CreateCalculationWalls(surface, points);

        }
        public CalculationPlan()
        {
            //Объект формируется вне после рфакторинга
        }

        //ЭТОТ МЕТОД НУЖЕН ТОЛЬКО ДЛЯ СТАРОГО КОДА, ЕГО НАДО ЗАПИЛИТЬ В CalculationWall
        private void CreateCalculationWalls(CalculationSurface surface, List<SurfacePointWithValues> points) //Метод, который формирует расчётную плоскость с помощью пользовательских данных
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
                var projectPointOnFloor = point + (-XYZ.BasisZ) * ((3000 / 304.8) / 2); //Опустил точку на пол
                for (double i = 0; i < 6000 / 304.8; i += 1000 / 304.8 )
                {
                    var truthCalcPoint = projectPointOnFloor + (-normal) * (i);         //Подвинул точку вглубь здания на 6 метров (но мне нужно не только на 6)
                    listPoint.Add(truthCalcPoint);                                      //В будущем по необходимости нужно сменить тип XYZ на SurfacePointWithValues
                }
                
            }
        }

    }
}
