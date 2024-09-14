using Godot;
using System;

public partial class Lamp : Node3D
{
	private AudioStreamPlayer3D audioPlayer;

	public override void _Ready()
	{
		// Get the AudioStreamPlayer3D node
		audioPlayer = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");

		// Load and set the audio stream
		var stream = GD.Load<AudioStream>("res://assets/Sounds/ambient/lamp.mp3");
		audioPlayer.Stream = stream;

		// Set to play looping (if it's an MP3)
		if (stream is AudioStreamMP3 mp3Stream)
		{
			mp3Stream.Loop = true;
		}

		// Start playing the sound
		audioPlayer.Play();

		// Optionally, you can adjust these properties:
		audioPlayer.UnitSize = 10; // Adjust this to change how quickly the sound attenuates with distance
		audioPlayer.MaxDistance = 20; // Maximum distance at which the sound can be heard
	}
}
