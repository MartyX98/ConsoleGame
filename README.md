# ConsoleGame
I have started this project with no clear goal in mind. I was mainly inspired by javidx9's ["Code-It-Yourself! First Person Shooter"](https://www.youtube.com/watch?v=xW8skO7MFYw) tutorial and later by his ["Super Fast Ray Casting in Tiled Worlds using DDA"](https://www.youtube.com/watch?v=NbSee-XM7WA) tutorial and wanted to see if could make sense of it myself, hence this project.

## What's the goal?
No idea! I don't even know what kind of game it is going to be. So far I am only laying the foundation for any game in general, and adding useful features along the way, so think of this as some primitive engine (if you could call it that, i have no idea how game engines work) to build game on top of. If this repo ever starts evolving towards some specific game, i will if course update this. 

**The main directives** I would however like to stick to along the developement are following..
- Terminal as the graphical output.
  - That's what it's build for, and what I want it to remain as. It's not practical (at all), but as Woodkid said somewhere "*constraints could allow for more creative freedom or fruitful outcome, if done right.*" (don't quote him or me on that, i couldn't find the source, I might have just pulled it out of my ass, but I'm pretty sure he conveyed this message in some inteview)
  - Other outputs (such as sounds, etc..) are not out of the table and might be introduced later

## Features / TODO list
- [x] Vector class (not final though)
- [x] Plane class (matrix in essence) for handling textures, maps, display, etc..
- [x] Basic DDA based raycasting logic and rendering output as "3D" environment.
- [x] Entity class for handling both player and any other future entities.
- [ ] Input handler very limited in it's current implementation. From the main game loop, a single key click is registered too many times.
- [ ] Floor texture shifts sideways too much when the player entity is rotating -> Related to the problem above.
- [ ] Icons class' name no longer represents all it does. Perhaps rename it to "Textures"?
- [ ] Some textures/icons are grandients of chars instead of a single characters. Maybe I should add some simple class to handle the logic of mapping floats to the char index in the grandient instead of having to do it always in the main code.
