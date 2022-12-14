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
        public ConceptualBuilding(Element element) //ВАЖЕН порядок вызова методов, так как без Surfaceces не получить CalculationPlans
        {
            RevitElement = element;
            Surfaces = GetCalculationSurfaces(element, true);
            Name = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            CalculationPlans = GetCalculationPlans(element); //Но для Insolaris-a он не нужен!!! Может быть в данном классе создать несколько конструкторов???
            //TransformOfObject = CreateTransform(element);
        }
    }
}
