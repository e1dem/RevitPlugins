# Test Revit 2025 Plugins

## Wire Calculation Revit Plugin

The plugin creates wires along a room walls automatically based on:
1) wires parameters (wire type and wiring type) selected by a user;
2) start and end points picked by the user.

### Restrictions 
1. At the moment, only creating wires with start and end points in a single room is supported. 
After selecting the first (start) point on a wall, the user is allowed to select the second (end) point on a wall only in the same room.
In case of trying to pick the second point in a different room, the user sees a warning message. After closing it, 
he or she can continue choosing the end point.
2. There is no check for space to be a room exactly, so the user should be careful to select points in a ROOM.
3. While creating wires route, it is supposed in code that the choosen room's boundary segments form a closed loop.
4. All the intermediate points created by plugin in the room's corners are located on the level of the start point.
5. For now, there is no checks for door, wihdows or other openings in the walls while creating wires.

### TODO list
1. Find the shortest path between start and end points.
Also: see Restrictions