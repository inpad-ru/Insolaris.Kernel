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
        public override Element RevitElement { get; }
        public override List<CalculationSurface> Surfaces { get; }
    }
}
