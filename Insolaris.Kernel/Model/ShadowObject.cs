using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Kernel.Model
{
    public class ShadowObject //Вполне может быть : ConstructionObject
    {
        public List<XYZ> ShadowPoints { get; set; } //Может быть нужна своя затеняемая точка, но не факт
        public double Width { get; set; } //параметр "a" из СП
        public double Height { get; set; } //высота затеняющего здания
        public double Distance { get; set; } //узнаем только, когда узнаем про рассчитываемое окно H (нет в СП)
        public double HeightCalculation { get; set; } //узнаем, только когда узнаем расчётное окно Hр по СП
        public double MirrorFactor { get; set; } //коэффициент ро отражения фасада из СП //хотя может быть внутри он и не нужен
    }
}
