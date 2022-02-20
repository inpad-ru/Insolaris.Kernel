using Autodesk.Revit.DB;
using Insolaris.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Model
{
    public class ConceptualArea : ConstructionObject
    {
        public override List<CalculationSurface> Surfaces { get; }
        public override Element RevitElement { get; }
    }
}
