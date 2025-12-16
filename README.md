# ConsoleGame
I have started this project with no clear goal in mind. I was mainly inspired by javidx9's ["Code-It-Yourself! First Person Shooter"](https://www.youtube.com/watch?v=xW8skO7MFYw) tutorial and later by his ["Super Fast Ray Casting in Tiled Worlds using DDA"](https://www.youtube.com/watch?v=NbSee-XM7WA) tutorial and wanted to see if I could make sense of it myself, hence this project.

## What's the goal?
No idea! I don't even know what kind of game it is going to be. So far I am only laying the foundation for any game in general, and adding useful features along the way, so think of this as some primitive engine (if you could call it that, i have no idea how game engines work) to build game on top of. If this repo ever starts evolving towards some specific game, i will if course update this. 

**The main directives** I would however like to stick to along the developement are following..
- Terminal as the graphical output.
  - That's what it's build for, and what I want it to remain as. It's not practical (at all), but it's a fun constraint and I want to see how far I could take this with.
- Other kinds of outputs besides visual are not out of the table and might be introduced later.
- Third party libraries may be utilized if necessary.

### To do list:
- [ ] Vector class implemetation isn't perfect at the moment, needs rewrite.
- [ ] Storing textures in .txt files works, but how about world maps where we might need to store more object data in the future?
- [ ] Utilize colors?
	- [x] using [ANSI Escape codes](https://en.wikipedia.org/wiki/ANSI_escape_code) - Provides full RGB range support, but at the price of ass performance due having to use native console API and not direct console buffer updates.
	- [ ] using curses library? It worked for me in prior python version of this project. Gotta give it a go.
- [ ] Refactor of raycaster logic and perhaps moving it into a separate class? I think it should not be within the Main method.
- [ ] Try to add "billboard" texture rendering.

### Known issues:
- Input handler is very limited in it's current implementation. From the main game loop, a single key click is registered too many times.
- [Keyboard Repetition Delay](https://superuser.com/questions/1164303/windows-how-do-i-disable-the-keyboard-delay) also sucks, but that is apparently managed on OS level and couldn't be disabled programmatically without messing with windows registry.
- The background texture (floor and sky) shifts sideways too much when the player entity is rotating. Perhaps the shift should be dependent on changes of player's viewing angle instead of key presses?
- Inconsistent use of the 'Icons.Air' and maybe more. That's just my sloppy coding, I will fix it in the future.
- No collision checking implemented yet.
- Walking or looking out of bounds may cause a crash.
- Textures on on Walls are reversed on 2 out of the 4 directions.

# Developement progress
## Basic Ascii Art texture support (2025-01-20)
- Massive rewrite and expansion of Grid class (formerly Plane class).
- Added basic logic for rendering ascii art texture from a .txt file
- Fixed couple of perspective / visual glitches along the way.
	- Wall texture scales vertically correctly now when viewed from up close.
- Due to texture rendering implementation, darkening walls has been removed and will be reimplemented differently later.
- Added sky into the background :)

![v0](previews/Preview_20250120.gif)

___
## Initial upload (2025-01-12)
- Added the initial version of the project.
- Loads an ASCII game map from a text file, initializes the player entity and renders it's POV through DDA raycasting.
- Contains a simple input handler for moving the player entity around the map, based on reading the console window input. 
- Renders the minimap on screen, with the player entity represented by a '☻', and the walls that are rendered by '#'.

![v0](previews/Preview_20250112.gif)

___

