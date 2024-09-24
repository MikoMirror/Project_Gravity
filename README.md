# Project Gravity
![image](https://github.com/user-attachments/assets/4c569bde-1d91-484d-b7d8-d2ce17d1b10b)
---

"Project Gravity" is a small puzzle game consisting of 4 levels made with Godot Engine 4.3 Mono. 

Installation Dev:
To open the game in Godot Engine 4.3 mono download this repository, then import the project. Then in Godot engine click on the “build Project” button and then enable the “Memory Puzzle Editor” plugin in the project settings

This project uses a custom “Memory puzzle editor” plugin that I wrote specifically for MemoryPuzzle scene management.
This plugin enhances the Godot editor by:

- Creating a custom grid-based editor for PlatformStates in MemoryPuzzle and CustomPlatformStates in Terminal objects.

- Visualizing platform states as interactive checkboxes.

- Providing custom spinbox editors for row and column counts.

- Dynamically updating the grid when properties change.

- Allowing direct manipulation of platform states in the Godot Inspector.

- Integrating seamlessly with the Godot editor workflow.

It simplifies the process of designing memory puzzles and terminal layouts by providing a visual, interactive interface within the Godot editor.

![image](https://github.com/user-attachments/assets/94861df8-f408-487e-abc9-5ff2f7379bd2)
---

# $${\color{red}IMPORTANT}$$

This project uses Vulkan as its rendering engine. For optimal performance:

1. Make sure your graphics card supports Vulkan. (In some cases, you may need to update your graphics drivers).

Without proper Vulkan support, the game may fall back to software rendering on the CPU, which could significantly impact performance and visual quality.
You can check which GPU the game uses by running “ProjectGravity.console.exe”

