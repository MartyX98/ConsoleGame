___
## Initial upload (2025-01-12)
- Added the initial version of the project.
- Loads an ASCII game map from a text file, initializes the player entity and renders it's POV through DDA raycasting.
- Contains a simple input handler for moving the player entity around the map, based on reading the console window input. 
- Renders the minimap on screen, with the player entity represented by a '☻', and the walls that are rendered by '#'.

### Known issues
- Input handler issues
    - A bit limited due to the OS level key press delay when holding down a key. This should be fixed in the future.
    - From the main game loop, a single key click is registered too many times.
- The background texture (floor) shifts sideways too much when the player entity is rotating. This is because of the issue above.
- Inconsistent use of the 'Icons.Air' and maybe more. That's just my sloppy coding, I will fix it in the future.

![](other/Preview_v0.gif)
___


