using Godot;
using System;

public partial class fps : Label
{
	// This method is called every frame. Delta is the time since the last frame.
	public override void _Process(double delta)
	{
		// Get the current frames per second as an integer
		int fps = (int)Engine.GetFramesPerSecond();
		
		// Update the label text with the current FPS
		Text = $"FPS: {fps}";
	}
}
