# Sokoban in Unity
A simple implementation of the Sokoban game with added functionality using Unity 3D and C#.
## Game Rules
Sokoban is a classic puzzle game where the player needs to push boxes onto target tiles. 
The game takes place on a 2D grid, with each tile being either passable or impassable. 
The player can move in four cardinal directions and can push boxes but cannot pull them. 
The objective is to place all the boxes onto the target tiles.
## Game Features
- Level Loading with the next level, and reset level (R)
- Currently, the game has 2 fully functional levels and a 3rd testing level
- Undo Redo functionality (Z, X)
- Win Condition Checking (If all boxes are on targets a "Win Menu" is shown)
- Shortest Route without touching a Crate on click (Click on a valid space to move to it) via BFS pathfinding
- Inability to Win (Checks that provide info if the game cannot be finished):
  - A box in a corner that is not on a target tile automatically means the level cannot be completed.
  - A box stuck against a wall that ends in a corner with no target tiles along its path.
  - Four boxes arranged in a square, where at least one of them is not on a target tile.
  - A square of two boxes and two walls.
- Memory Allocation after the game has started is minimal
## Visualization
The Visualization is done relatively simply. The game uses the 3D Unity Project but with a top-down viewpoint.
There are no sounds, animations or any kind of complex textures.
Here is what each block represents:
- Black - Wall
- Red - Crate
- Blue - Target
- Orange - Player
- White - Empty Space
- Green - Player on Target
- Dark Red - Crate on Target
## Level Creation
Levels in the game are represented by a .txt file in a Levels folder.
There is a file that has all the needed information on how to create a level and all the needed requirements.
Example:
- There should be exactly one player.
- There should be at least one box.
- The number of target tiles should be greater than or equal to the number of boxes.

## Implementation
- The game is implemented using a Matrix (2D Array) which represents the current game state.
Each block is read from a file at the start of the game and the data is filled in its respective row and column if the data is correct(the width is the same as the specified in the file... etc.).
Additionally, after the block is read it is instanced so that it's visually available.
- Move checking is implemented by checking the block that is in the direction of the input and it moves the player if there is not a wall.
If there is a crate the same check is done to the crate for its next block and if the move is valid - the whole action is completed.
- Each move is stored in a Manager so that you can undo and redo moves.
Moves are represented by the Player's two positions(Initial and Target) and potentially a Crate's positions if a crate was moved
- Upon a crate entering a target there is a check for a Win(All Crates are on Targets)
A Win Menu is shown upon the check returning true
- A Breath First Search Path Finding Algorithm was implemented for the game so that whenever the player clicks on an empty or target block it checks for the shortest way to get there without moving a crate.
If there isn't a way there is a Debug notification.
- There are several checks for "Inability" to win. Whenever a crate has been moved - There is a series of checks that look if the game cannot be won and signals via Debug notifications.
- The whole memory allocation is minimalized so that Coroutines are not used.
