using Autodesk.Revit.DB;
using Insolaris.Geometry;
using Insolaris.Kernel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Kernel.Geometry
{
    public class CalculationWall
    {
        public XYZ Normal { get; set; }
        public CalculationSurface CalculationSurface { get; set; }
        public List<CustomWindow> Windows { get; set; }
        public List<ShadowObject> ShadowObjects { get; set; }
        public PointNLC[,] PointNLCs { get; set;}
        public Transform LocalBasis { get; set; }// Думаю понадобиться самостоятельно составить Transform для местной системы координат

    }
}
