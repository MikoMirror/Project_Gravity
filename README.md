# Project Gravity
![image](https://github.com/user-attachments/assets/4c569bde-1d91-484d-b7d8-d2ce17d1b10b)
---

"Project Gravity" is a small puzzle game consisting of 4 levels made with [Godot Engine 4.3 (.NET)](https://godotengine.org/download/windows/) 

Development Setup
--
To open the game in Godot Engine 4.3 (.NET):
- Clone or download this repository.
- Open Godot Engine, and import the project.
- In the Godot editor, click the "Build Project" button to compile the project.
- Navigate to Project Settings and enable the "Memory Puzzle Editor" plugin.
  
---

This project includes a custom "Memory Puzzle Editor" plugin, developed specifically for managing the MemoryPuzzle scene. The plugin extends the functionality of the Godot editor by providing the following enhancements:

- Creating a custom grid-based editor for PlatformStates in MemoryPuzzle and CustomPlatformStates in Terminal objects.

- Visualizing platform states as interactive checkboxes.

- Providing custom spinbox editors for row and column counts.

- Dynamically updating the grid when properties change.

- Allowing direct manipulation of platform states in the Godot Inspector.

- Integrating seamlessly with the Godot editor workflow.

It simplifies the process of designing memory puzzles and terminal layouts by providing a visual, interactive interface within the Godot editor.

![image](https://github.com/user-attachments/assets/02786199-841f-4c9e-a890-3b18d0e4dd0a)
---

# $${\color{red}IMPORTANT}$$

This project uses Vulkan as its primary rendering engine. To ensure optimal performance:

1. Confirm that your graphics card supports Vulkan. In some cases, you may need to update your graphics drivers.

If Vulkan is not supported, the game may default to software rendering on the CPU, which can significantly reduce performance and visual quality. You can verify which GPU the game is utilizing by running the ProjectGravity.console.exe tool.

