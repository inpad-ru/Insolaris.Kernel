using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Kernel.Geometry
{

    public class CustomWindow
    {
        public XYZ Up { get; }
        public XYZ Down { get; }
        public XYZ Left { get; }
        public XYZ Right { get; }
        public double Area { get; }
        public CustomWindow(XYZ up, XYZ down, XYZ left, XYZ right)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
        }
    }
}
