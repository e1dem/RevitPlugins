using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Diagnostics;
using System.Resources;
using RevitPlugins.WireCalculationRevitPlugin.Windows;
using WireCalculationRevitPlugin.Properties;
using RevitPlugins.SelectionFilters;

namespace RevitPlugins.WireCalculationRevitPlugin
{
    /// <summary>
    ///  The class is the entry point for the Revit plug-in.
    ///  The plugin creates wires along a room walls automatically based on:
    ///  1) wires parameters (wire type and wiring type) selected by a user;
    ///  2) start and end points picked by a user.
    ///  At the moment, creating wires in a single room is only supported. 
    ///  That means that both start and end endpoints picked should be located in the same room.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class WireCalculationRevitPlugin : IExternalCommand
    {
        private static ResourceManager resourceManager = RevitPlugin.ResourceManager;
        /// <summary>
        /// Method <c>Execute</c> is run when a user clicks on a command in the Revit user interface
        /// listed under the External Tools drop-down button on the Add-Ins tab
        /// </summary>
        /// <param name="commandData">
        /// An bject providing API access to the Revit application. The application object in turn 
        /// provides access to the document that is currently active in the user interface 
        /// and its corresponding database and model.
        /// </param>
        /// <param name="message">
        /// A parameter with the additional ref keyword that can be modified within the method 
        /// implementation. This parameter can be set in the external command when the command 
        /// fails or is cancelled. When this message gets set – and the <c>Execute()</c> method 
        /// returns a failure or cancellation result – an error dialog is displayed by Revit 
        /// with this message text included.
        /// </param>
        /// <param name="elements">
        /// A parameter which allows to choose elements that will be highlighted on the plan in case
        /// the external command fails or is cancelled.
        /// </param>
        /// <returns>
        /// A result of the Autodesk.Revit.UI.Result type telling Revit whether the command execution
        /// has succeeded, failed or been cancelled
        /// </returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {            
            // Get the Revit application and document objects
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;           

            // Get the object representing a user selection from the Revit user interface
            Selection selection = uiapp.ActiveUIDocument.Selection;
            // Ask a user to pick a start point on a wall in any room
            WallSelectionFilter wallPointPickFilter = new WallSelectionFilter(doc);
            Reference startPointRef = selection.PickObject(ObjectType.PointOnElement, wallPointPickFilter, resourceManager.GetString("StartPointPickPrompt"));
            XYZ startPoint = new XYZ(startPointRef.GlobalPoint.X, startPointRef.GlobalPoint.Y, startPointRef.GlobalPoint.Z);
            Room room = doc.GetRoomAtPoint(startPoint);

            Debug.WriteLine("The start point: x={0}; y={1}", startPointRef.GlobalPoint.X, startPointRef.GlobalPoint.Y);
            
            // Ask a user to pick an end point on a wall in the same room
            Reference endPointRref;
            XYZ endPoint;

            while (true)
            {
                endPointRref = selection.PickObject(ObjectType.PointOnElement, wallPointPickFilter, resourceManager.GetString("EndPointPickPrompt"));
                endPoint = new XYZ(endPointRref.GlobalPoint.X, endPointRref.GlobalPoint.Y, endPointRref.GlobalPoint.Z);
                if (room.Name.Equals(doc.GetRoomAtPoint(endPoint).Name))
                {
                    break;
                } else
                {
                    Debug.WriteLine("The start point room: {0}; the end point room: {1}", doc.GetRoomAtPoint(startPoint).Name, doc.GetRoomAtPoint(endPoint).Name);
                    WindowsUtils.ShowWarningMessage(resourceManager.GetString("EndPointSelectionErrorMessage"));
                }                
            }

            
            ElectricalSetting electricalSetting = doc.Settings.ElectricalSetting;
            IEnumerable<WireType> wireTypes = electricalSetting.WireTypes.Cast<WireType>();
            IEnumerable<WiringType> wiringTypes = Enum.GetValues(typeof(WiringType)).Cast<WiringType>();
            
            // Ask a user to select wires parameters
            WireParamsWindow wireParamsWindow = new WireParamsWindow(wireTypes, wiringTypes);            
            bool? result = wireParamsWindow.ShowDialog();

            if (result != true)
            {
                WindowsUtils.ShowWarningMessage(resourceManager.GetString("EndPointSelectionErrorDialogCancelMessage"));
                return Result.Cancelled;
            }
            else
            {
                WireType wireType = wireParamsWindow.GetSelectedWireType();                
                WiringType wiringType = wireParamsWindow.GetSelectedWiringType();
                IList<XYZ> wireVertexPoints = CalculateWireVertexPoints(room, startPoint, endPoint);                

                using (Transaction trans = new Transaction(doc)) { 
                    trans.Start("CreateWireTrans");
                    Wire.Create(doc, wireType.Id, doc.ActiveView.Id, wiringType, wireVertexPoints, null, null);
                    trans.Commit();

                    return Result.Succeeded;
                }
            }
        }

        /// <summary>
        /// Calculates intermediate wire vertex points to create wires along a room walls.
        /// </summary>
        /// <param name="room">The room where wires are to be created</param>
        /// <param name="startPoint">The start point of wires to be created</param>
        /// <param name="endPoint">The end point of wires to be created</param>
        /// <returns>The full list of wire vertex points including the start and end ones.</returns>
        private static IList<XYZ> CalculateWireVertexPoints(Room room, XYZ startPoint, XYZ endPoint)
        {
            List<XYZ> result = new List<XYZ>([startPoint, endPoint]);

            SpatialElementBoundaryOptions opts = new SpatialElementBoundaryOptions();
            IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(opts);
            
            if (segments != null) // The room may not be bound
            {                
                IList<IList<BoundarySegment>> orderedSegments = RoomUtils.GetBoundarySegmentsSorted(segments);

                XYZ startPointProjection = new XYZ(startPoint.X, startPoint.Y, ((LocationPoint)room.Location).Point.Z);
                XYZ endPointProjection = new XYZ(endPoint.X, endPoint.Y, ((LocationPoint)room.Location).Point.Z);
                int startPointSegmentIndex = FindBoundarySegmentAtPoint(orderedSegments.ElementAt(0), startPointProjection);
                int endPointSegmentIndex = FindBoundarySegmentAtPoint(orderedSegments.ElementAt(0), endPointProjection);

                // The list of points to be inserted between the start and end ones
                IList<XYZ> intermediatePoints = new List<XYZ>();
                // For the sake of simplicity, for now, use the start point's third coordinate for all vertices except for the final one
                double zCoord = startPoint.Z;

                int startSegmentIndex = Math.Min(startPointSegmentIndex, endPointSegmentIndex);
                int endSegmentIndex = Math.Max(endPointSegmentIndex, startPointSegmentIndex);
                for (int i = startSegmentIndex + 1; i <= endSegmentIndex; i++)
                {
                    if (i == startSegmentIndex + 1)
                    {
                        XYZ firstVertex = new XYZ(orderedSegments.ElementAt(0).ElementAt(i).GetCurve().GetEndPoint(0).X,
                            orderedSegments.ElementAt(0).ElementAt(i).GetCurve().GetEndPoint(0).Y, zCoord);

                        intermediatePoints.Add(firstVertex);
                    }

                    if (i != endSegmentIndex)
                    {
                        XYZ nextVertex = new XYZ(orderedSegments.ElementAt(0).ElementAt(i).GetCurve().GetEndPoint(1).X,
                            orderedSegments.ElementAt(0).ElementAt(i).GetCurve().GetEndPoint(1).Y, zCoord);
                        intermediatePoints.Add(nextVertex);
                    }
                }

                result.InsertRange(1, startPointSegmentIndex <= endPointSegmentIndex ? intermediatePoints : intermediatePoints.Reverse());
            }

                return result;
        }

        /// <summary>
        /// Finds and returns the first boundary segment index in the list the point belongs to.
        /// If the point does not belong any of the segments, -1 is returned.
        /// </summary>
        /// <param name="boundarySegments">The list of boundary segments to search in</param>
        /// <param name="point">The point which segment is searched</param>
        /// <returns>The first boundary segment index in the list the point belongs to. -1 if no segment found</returns>
        private static int FindBoundarySegmentAtPoint(IList<BoundarySegment> boundarySegments, XYZ point)
        {
            int segmentIndex = -1;

            for (int i = 0; i < boundarySegments.Count; i++)
            {
                IntersectionResult res = boundarySegments.ElementAt(i).GetCurve().Project(point);
                if (point.IsAlmostEqualTo(res.XYZPoint))
                {
                    Debug.WriteLine("BoundarySegment is found: {0}", i);
                    segmentIndex = i;
                    break;
                }
            }

            return segmentIndex;
        }
    }
}