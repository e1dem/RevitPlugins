using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitPlugins.SelectionFilters
{
    /// <summary>
    /// Filter used to constrain a user's selection to walls
    /// </summary>
    public class WallSelectionFilter : ISelectionFilter
    {
        private Document doc;

        public WallSelectionFilter(Document doc)
        {
            this.doc = doc;
        }

        public bool AllowElement(Element e)
        {
            if (e is Wall)
            {
                return true;
            }

            return false;
        }

        public bool AllowReference(Reference r, XYZ p)
        {
            GeometryObject geometryObject = doc.GetElement(r).GetGeometryObjectFromReference(r);
            if (geometryObject != null
                && (geometryObject is Edge || geometryObject is Face))
            {
                return true;
            }

            return false;
        }
    }
}
