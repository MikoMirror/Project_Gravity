using Godot;
using System;

public partial class PauseMenu : Control
{
	private Button _resumeButton;
	private Button _restartLevelButton;
	private Button _settingsButton;
	private Button _quitButton;
	private LevelManager _levelManager;

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
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) // ESC key
		{
			OnResumePressed();
			GetViewport().SetInputAsHandled(); // Prevent the event from propagating
		}
	}

	private void OnResumePressed()
	{
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
		// Implement settings functionality here
		GD.Print("Settings button pressed");
	}

	private void OnQuitPressed()
	{
		// Close the pause menu
		if (_levelManager != null)
		{
			_levelManager.ClosePauseMenu();
		}

		// We'll handle saving in the LevelManager instead
		if (_levelManager != null)
		{
			_levelManager.SaveGameState();
		}
		else
		{
			GD.PrintErr("PauseMenu: LevelManager not set, unable to save game state");
		}

		// Change to the main menu scene
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}
}
