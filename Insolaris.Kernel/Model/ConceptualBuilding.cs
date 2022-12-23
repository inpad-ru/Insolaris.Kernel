using Autodesk.Revit.DB;
using Insolaris.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Model
{
    public class ConceptualBuilding : ConstructionObject
    {
        public double RoomWidth { get; set; } = 1800;
        public double LevelHeight { get; set; } = 3000;
        public double WallThickness { get; set; } = 250;
        public double WindowWidth { get; set; } = 1800;
        public double WindowHeight { get; set; } = 3000;
        public double CalculationDepth { get; set; } = 6000;
        public double MeshNormalStep { get; set; } = 1000;
        public double MeshOrtoNormalStep { get; set; } = 1000;
        public ConceptualBuilding(Element element) //ВАЖЕН порядок вызова методов, так как без Surfaceces не получить CalculationPlans
        {
            RevitElement = element;
            Surfaces = GetCalculationSurfaces(element, true);
            Name = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            CalculationPlans = GetCalculationPlans(element); //Но для Insolaris-a он не нужен!!! Может быть в данном классе создать несколько конструкторов???
            //CalculationPlans1 = GetCalculationPlans1(element); //ПОСЛЕ РЕФАКТОРИНГА //Этот метод нельзя вызывать из конструктора, надо отдельно
            //TransformOfObject = CreateTransform(element);
        }
    }
}
