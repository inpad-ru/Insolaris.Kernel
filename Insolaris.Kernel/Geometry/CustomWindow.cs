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
        public XYZ UpForCount { get; set; }
        public XYZ DownForCount { get; set; }
        public XYZ LeftForCount { get; set; }
        public XYZ RightForCount { get; set; }
        public double Area { get; }
        public CustomWindow(XYZ up, XYZ down, XYZ left, XYZ right) //Можно задать двумя точками
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
        }
        public void GetCalculationWindow(Transform transform)
        {
            UpForCount = transform.OfPoint(Up);
            DownForCount = transform.OfPoint(Down);
            LeftForCount = transform.OfPoint(Left);
            RightForCount = transform.OfPoint(Right);
        }
    }
}
