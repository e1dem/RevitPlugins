using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Diagnostics;

namespace RevitPlugins
{
    public class RoomUtils
    {

        /// <summary>
        /// (For testing purposes) Calculates a room perimeter based on its boundary segments' length
        /// </summary>
        /// <param name="room">The room perimeter is calculated for</param>
        /// <param name="opts">The SpatialElementBoundaryOptions object</param>
        /// <returns>Calculated perimeter</returns>
        public static double CalculateRoomPerimeter(Room room, SpatialElementBoundaryOptions opts)
        {
            IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(opts);
            double roomPerimeter = 0;

            if (segments != null) // The room may not be bound
            {
                foreach (IList<BoundarySegment> segmentList in segments)
                {
                    foreach (BoundarySegment segment in segmentList)
                    {
                        roomPerimeter += segment.GetCurve().Length;

                        // Get the curve start point
                        // segment.GetCurve().GetEndPoint(0);

                        // Get the curve end point
                        // segment.GetCurve().GetEndPoint(1);
                    }
                }
            }

            Debug.WriteLine("Calculated: " + roomPerimeter + " from class: " + room.Perimeter);

            return roomPerimeter;
        }

        /// <summary>
        /// Returns the copy of a boundary segments list sorted so that each next segment intersects with the previous one.
        /// Note: Directions of segments are not checked at the moment. The end point of a segment may correspond 
        /// to the end and not the beginning of the next one.
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public static IList<IList<BoundarySegment>> GetBoundarySegmentsSorted(IList<IList<BoundarySegment>> segments)
        {            
            IList<IList<BoundarySegment>> orderedSegments = new List<IList<BoundarySegment>>();

            foreach (IList<BoundarySegment> segmentList in segments)
            {
                BoundarySegment[] orderedSegment = segmentList.ToArray();
                for (int i = 0; i < orderedSegment.Length - 1; i++)
                {
                    for (int j = i + 1; j < orderedSegment.Length; j++)
                    {
                        if (orderedSegment[i].GetCurve().Intersect(orderedSegment[j].GetCurve()) == SetComparisonResult.Overlap)
                        {
                            BoundarySegment tmp = orderedSegment[i + 1];
                            orderedSegment[i + 1] = orderedSegment[j];
                            orderedSegment[j] = tmp;
                            break;
                        }
                    }
                }

                orderedSegments.Add(orderedSegment);
            }

            return orderedSegments;
        }

        /// <summary>
        /// Calculates an element's center point based on its bounding box.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        private static XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Max + bounding.Min) * 0.5;
            return center;
        }

        /// <summary>
        /// Returns a room's center point. The third coordinate is equal
        /// to the bottom of the room
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static XYZ GetRoomCenter(Room room)
        {
            // Get the room center point.
            XYZ boundCenter = GetElementCenter(room);
            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            return roomCenter;
        }

        /// <summary>
        /// Returns a collection of doors in a particular room.
        /// </summary>
        /// <param name="room">The room to find doors in.</param>
        /// <returns>The collection of doors in the room.</returns>
        public static IEnumerable<Element> GetRoomDoors(Room room)
        {
            IEnumerable<Element> doors = new FilteredElementCollector(room.Document)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_Doors).Where(e => {
                    FamilyInstance door = (FamilyInstance)e;

                    return e.LevelId.Equals(room.LevelId)
                            && door != null
                            && ((door.FromRoom != null && door.FromRoom.Id.Equals(room.Id)) || (door.ToRoom != null && door.ToRoom.Id.Equals(room.Id)));
                });

            return doors;
        }

        public static IEnumerable<Element> FilterRoomDoors(IEnumerable<Element> doors, Room room)
        {
            SpatialElementBoundaryOptions opts = new SpatialElementBoundaryOptions();
            IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(opts);
            IEnumerable <Element> result = new List<Element>();


            if (segments != null) // the room may not be bound
            {
                foreach (IList<BoundarySegment> segmentList in segments)
                {
                    foreach (BoundarySegment segment in segmentList)
                    {
                        foreach (Element e in doors)
                        {
                            FamilyInstance? door = e as FamilyInstance;
                            if (door != null && door.Host.Id.Equals(segment.ElementId))
                            {
                                result.Append(door);
                                Debug.WriteLine("Door for the room found", "The door for the selected room found: " + door.Id.ToString());
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// Return the room in which the given point is located
        public static Room? GetRoomOfGroup(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            // Add a category filter to the collector so that the collector only provided access to rooms
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room? room = null;
            foreach (Element elem in collector)
            {
                room = elem as Room;
                // Though the collector is only expected to return rooms, it's still good practice
                // to double-check that the room variable contains a valid room, just in case.
                if (room != null)
                {
                    // Decide if this point is in the picked room                  
                    if (room.IsPointInRoom(point))
                    {
                        break;
                    }
                }
            }
            return room;
        }

        /// <summary>
        /// Copy the group to each of the provided rooms. The position
        /// at which the group should be placed is based on the target
        /// room's center point: it should have the same offset from
        /// this point as the original had from the center of its room
        /// </summary>        
        public static void CopyGroupToRooms(Document doc, IList<Reference> rooms, XYZ sourceCenter, GroupType gt, XYZ groupOrigin)
        {
            XYZ offset = groupOrigin - sourceCenter;
            XYZ offsetXY = new XYZ(offset.X, offset.Y, 0);
            foreach (Reference r in rooms)
            {
                Room roomTarget = doc.GetElement(r) as Room;
                if (roomTarget != null)
                {
                    XYZ roomCenter = GetRoomCenter(roomTarget);
                    Group group = doc.Create.PlaceGroup(roomCenter + offsetXY, gt);
                }
            }
        }
    }
}
