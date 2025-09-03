Credit to https://github.com/CapsCollective/super-mario-bros for base code


Mario Bros AI Pathfinding
An AI system that makes Mario automatically navigate platformer levels using A* and Theta* pathfinding algorithms.

What it does
Smart Navigation: Mario finds the best path to his destination, avoiding obstacles and death pits
Jump Planning: Automatically jumps over gaps and onto platforms
Real-time Mapping: Scans the screen to understand the level layout
Visual Debugging: Shows the planned path in the Unity console

How it works
The AI converts the game world into a grid and uses pathfinding algorithms to navigate:
■ Walls/Obstacles
▣ Mario's position
! Target destination
* Planned path
□ Safe walkable areas

Setup
Add Coordinates.cs to an empty GameObject
Drag Mario and obstacles into the script's fields
Make sure Mario has the "Player" tag
Press play and watch Mario navigate automatically

Files
Coordinates.cs - Main AI controller
AStar.cs - Pathfinding algorithm
Node.cs - Grid system
PriorityQueue.cs - Pathfinding helper

Tech Stack
C#, Unity, A*/Theta* algorithms
