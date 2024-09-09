using Godot;
using System;

public partial class MainMenu : Control
{
	private Button startButton;
	private Button loadGameButton;
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
		loadGameButton = GetNodeOrNull<Button>("VBoxContainer/Load Game");
		settingsButton = GetNodeOrNull<Button>("VBoxContainer/Settings");
		exitButton = GetNodeOrNull<Button>("VBoxContainer/Exit");

		GD.Print($"Start button found: {startButton != null}");
		GD.Print($"Load Game button found: {loadGameButton != null}");
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
			startButton.Pressed += OnStartNewGamePressed;
			GD.Print("Start New Game button connected");
		}
		if (loadGameButton != null) 
		{
			loadGameButton.Pressed += OnLoadGamePressed;
			GD.Print("Load Game button connected");
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

	private void OnStartNewGamePressed()
	{
		GD.Print("Start New Game button pressed");
		var gameState = GetNode<GameState>("/root/GameState");
		gameState.CurrentLevel = "res://scenes/Level_1.tscn";
		gameState.SaveCurrentLevel();
		nextScene = gameState.CurrentLevel;
		ShowFadeOverlayAndAnimate();
	}

	private void OnLoadGamePressed()
	{
		GD.Print("Load Game button pressed");
		nextScene = LoadLastLevel();
		if (string.IsNullOrEmpty(nextScene))
		{
			GD.Print("No saved game found. Starting from Level 1.");
			nextScene = "res://scenes/Level_1.tscn";
		}
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
		var gameState = GetNode<GameState>("/root/GameState");
		gameState.LoadCurrentLevel();
		return gameState.CurrentLevel;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			GD.Print($"Mouse click detected at: {mouseEvent.Position}");
		}
	}
}
