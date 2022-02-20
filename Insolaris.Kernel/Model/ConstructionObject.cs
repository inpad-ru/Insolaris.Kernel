using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Insolaris.Model
{
    public abstract class ConstructionObject
    {
        public abstract Element RevitElement { get; }
        public abstract List<Geometry.CalculationSurface> Surfaces { get; }

    }
}
