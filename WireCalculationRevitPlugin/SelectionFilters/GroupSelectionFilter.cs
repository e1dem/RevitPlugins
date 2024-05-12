using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitPlugins.SelectionFilters
{
    /// <summary>
    /// Filter to constrain a user's picking to groups of elements
    /// </summary>
    public class GroupSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return e.Category != null && e.Category.Id.Value.Equals((int)BuiltInCategory.OST_IOSModelGroups);
        }
        public bool AllowReference(Reference r, XYZ p)
        {
            return false;
        }
    }
}
