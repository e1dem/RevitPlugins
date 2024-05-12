using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitPlugins.SelectionFilters
{
    /// <summary>
    /// Filter to constrain a user's picking to rooms
    /// </summary>
    public class RoomSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return e.Category != null && e.Category.Id.Value.Equals((int)BuiltInCategory.OST_Rooms);
        }
        public bool AllowReference(Reference r, XYZ p)
        {
            return false;
        }
    }
}
