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
        public ConceptualBuilding(Element element)
        {
            RevitElement = element;
            Surfaces = GetCalculationSurfaces(element, true);
        }
    }
}
