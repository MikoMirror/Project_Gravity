using Godot;
using System;

public partial class PauseMenu : Control
{
	private Button _resumeButton;
	private Button _restartLevelButton;
	private Button _settingsButton;
	private Button _quitButton;
	private LevelManager _levelManager;
	private GameState gameState;
	private Settings _settingsInstance;

	public void Initialize(LevelManager levelManager)
	{
		_levelManager = levelManager;
	}

	public override void _Ready()
	{
		// Try to find buttons directly under the PauseMenu node
		_resumeButton = GetNodeOrNull<Button>("VBoxContainer/ResumeButton");
		_restartLevelButton = GetNodeOrNull<Button>("VBoxContainer/Restart Level");
		_settingsButton = GetNodeOrNull<Button>("VBoxContainer/Settings");
		_quitButton = GetNodeOrNull<Button>("VBoxContainer/QuitButton");

		if (_resumeButton != null)
		{
			_resumeButton.Pressed += OnResumePressed;
			_resumeButton.GrabFocus(); // Automatically focus the resume button
		}
		else
		{
			GD.PrintErr("PauseMenu: ResumeButton not found");
		}

		if (_restartLevelButton != null)
		{
			_restartLevelButton.Pressed += OnRestartLevelPressed;
		}
		else
		{
			GD.PrintErr("PauseMenu: Restart Level button not found");
		}

		if (_settingsButton != null)
		{
			_settingsButton.Pressed += OnSettingsPressed;
		}
		else
		{
			GD.PrintErr("PauseMenu: Settings button not found");
		}

		if (_quitButton != null)
		{
			_quitButton.Pressed += OnQuitPressed;
		}
		else
		{
			GD.PrintErr("PauseMenu: QuitButton not found");
		}

		// Get the GameState singleton
		gameState = GetNode<GameState>("/root/GameState");
		if (gameState == null)
		{
			GD.PrintErr("GameState not found. Make sure it's set up as an AutoLoad.");
		}

		// Set the pause menu to process even when the game is paused
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) // ESC key
		{
			HandleEscapePress();
			GetViewport().SetInputAsHandled(); // Prevent the event from propagating
		}
	}

	private void HandleEscapePress()
	{
		if (_settingsInstance != null && IsInstanceValid(_settingsInstance))
		{
			if (_settingsInstance.HandleEscapePress())
			{
				OnSettingsBackButtonPressed();
			}
		}
		else
		{
			OnResumePressed();
		}
	}

	private void OnResumePressed()
	{
		GD.Print("Resume button pressed"); // Add this line for debugging
		if (_levelManager != null)
		{
			_levelManager.ClosePauseMenu();
		}
		else
		{
			GD.PrintErr("PauseMenu: LevelManager not set");
		}
	}

	private void OnRestartLevelPressed()
	{
		if (_levelManager != null)
		{
			_levelManager.RestartLevel();
		}
		else
		{
			GD.PrintErr("PauseMenu: LevelManager not set");
		}
	}

	private void OnSettingsPressed()
	{
		// Check if settings are already open
		if (_settingsInstance != null && IsInstanceValid(_settingsInstance))
		{
			GD.Print("Settings are already open");
			return;
		}

		var settingsScene = ResourceLoader.Load<PackedScene>("res://scenes/SupportScenes/settings.tscn");
		if (settingsScene != null)
		{
			_settingsInstance = settingsScene.Instantiate<Settings>();
			
			// Set the settings menu to process even when the game is paused
			_settingsInstance.ProcessMode = ProcessModeEnum.Always;
			
			// Add the settings scene as a child of the current scene
			GetTree().Root.AddChild(_settingsInstance);
			
			// Optionally, you can position the settings menu to appear centered
			_settingsInstance.AnchorRight = 1;
			_settingsInstance.AnchorBottom = 1;
			
			// Hide the pause menu
			this.Hide();
			
			// Connect to the back button pressed signal of the settings menu
			_settingsInstance.BackButtonPressed += OnSettingsBackButtonPressed;
		}
		else
		{
			GD.PrintErr("Failed to load settings scene.");
		}
	}

	private void OnSettingsBackButtonPressed()
	{
		// Show the pause menu again
		this.Show();
		
		// Clear the reference to the settings instance
		_settingsInstance = null;
		
		// Set focus back to the resume button
		_resumeButton?.GrabFocus();
	}

	private void OnQuitPressed()
	{
		if (gameState != null)
		{
			gameState.SaveCurrentLevel();
		}
		else
		{
			GD.PrintErr("Cannot save game: GameState is not initialized.");
		}

		if (_levelManager != null)
		{
			_levelManager.QuitToMainMenu();
		}
		else
		{
			GD.PrintErr("PauseMenu: LevelManager not set");
			// Fallback to direct scene change if LevelManager is not available
			GetTree().Paused = false;
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/main_menu.tscn");
		}
	}
}
