using Godot;
using System;

public partial class MainMenu : Control
{
	private Button startButton;
	private Button settingsButton;
	private Button exitButton;
	private AnimationPlayer animationPlayer;
	private ColorRect fadeOverlay;
	private string nextScene = "";

	public override void _Ready()
	{
		GD.Print("MainMenu _Ready called");

		// Get references to the buttons
		startButton = GetNodeOrNull<Button>("VBoxContainer/Start");
		settingsButton = GetNodeOrNull<Button>("VBoxContainer/Settings");
		exitButton = GetNodeOrNull<Button>("VBoxContainer/Exit");

		GD.Print($"Start button found: {startButton != null}");
		GD.Print($"Settings button found: {settingsButton != null}");
		GD.Print($"Exit button found: {exitButton != null}");

		// Get reference to the AnimationPlayer and FadeOverlay
		animationPlayer = GetNodeOrNull<AnimationPlayer>("FadeAnimation");
		fadeOverlay = GetNodeOrNull<ColorRect>("FadeOverlay");

		GD.Print($"AnimationPlayer found: {animationPlayer != null}");
		GD.Print($"FadeOverlay found: {fadeOverlay != null}");

		// Connect button signals to methods
		if (startButton != null) 
		{
			startButton.Pressed += OnStartPressed;
			GD.Print("Start button connected");
		}
		if (settingsButton != null) 
		{
			settingsButton.Pressed += OnSettingsPressed;
			GD.Print("Settings button connected");
		}
		if (exitButton != null) 
		{
			exitButton.Pressed += OnExitPressed;
			GD.Print("Exit button connected");
		}

		// Connect animation finished signal
		if (animationPlayer != null)
		{
			animationPlayer.AnimationFinished += OnAnimationFinished;
			GD.Print("AnimationPlayer connected");
		}

		GD.Print("MainMenu setup complete");
	}

	private void OnStartPressed()
	{
		GD.Print("Start button pressed");

		nextScene = LoadLastLevel();
		if (string.IsNullOrEmpty(nextScene))
		{
			nextScene = "res://scenes/Level_1.tscn";
		}
		
		GD.Print($"Next scene: {nextScene}");

		// Start fade in animation
		ShowFadeOverlayAndAnimate();
	}

	private void OnSettingsPressed()
	{
		// Implement settings functionality
		GD.Print("Settings button pressed");
	}

	private void OnExitPressed()
	{
		GD.Print("Exit button pressed");
		nextScene = "quit";  // This is now a special value, not a scene path
		ShowFadeOverlayAndAnimate();
	}

	private void ShowFadeOverlayAndAnimate()
	{
		if (fadeOverlay != null)
		{
			fadeOverlay.Visible = true;
		}
		if (animationPlayer != null)
		{
			animationPlayer.Play("fade_in");
		}
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "fade_in")
		{
			if (!string.IsNullOrEmpty(nextScene))
			{
				if (nextScene == "quit")
				{
					GD.Print("Quitting the game");
					GetTree().Quit();
				}
				else
				{
					GD.Print($"Changing to scene: {nextScene}");
					GetTree().ChangeSceneToFile(nextScene);
				}
			}
			else
			{
				GD.PrintErr("Next scene is empty");
			}
		}
	}

	private string LoadLastLevel()
	{
		// Implement your logic to load the last played level
		return "";
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			GD.Print($"Mouse click detected at: {mouseEvent.Position}");
		}
	}
}
