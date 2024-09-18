using Godot;
using System;

public partial class PauseMenu : Control
{
	// Fields
	private Button _resumeButton;
	private Button _restartLevelButton;
	private Button _settingsButton;
	private Button _quitButton;
	private LevelManager _levelManager;
	private GameState _gameState;
	private Settings _settingsInstance;

	// Initialization
	public void Initialize(LevelManager levelManager)
	{
		_levelManager = levelManager;
	}

	public override void _Ready()
	{
		SetupButtons();
		SetupGameState();
		ProcessMode = ProcessModeEnum.Always;
	}

	// Button Setup
	private void SetupButtons()
	{
		SetupButton(ref _resumeButton, "VBoxContainer/ResumeButton", OnResumePressed, true);
		SetupButton(ref _restartLevelButton, "VBoxContainer/Restart Level", OnRestartLevelPressed);
		SetupButton(ref _settingsButton, "VBoxContainer/Settings", OnSettingsPressed);
		SetupButton(ref _quitButton, "VBoxContainer/QuitButton", OnQuitPressed);
	}

	private void SetupButton(ref Button button, string path, Action pressedAction, bool grabFocus = false)
	{
		button = GetNodeOrNull<Button>(path);
		if (button != null)
		{
			button.Pressed += pressedAction;
			if (grabFocus) button.GrabFocus();
		}
		else
		{
			GD.PrintErr($"PauseMenu: {path} button not found");
		}
	}

	private void SetupGameState()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		if (_gameState == null)
		{
			GD.PrintErr("GameState not found. Make sure it's set up as an AutoLoad.");
		}
	}

	// Input Handling
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			HandleEscapePress();
			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleEscapePress()
	{
		if (_settingsInstance != null && IsInstanceValid(_settingsInstance) && _settingsInstance.HandleEscapePress())
		{
			OnSettingsBackButtonPressed();
		}
		else
		{
			OnResumePressed();
		}
	}

	// Button Actions
	private void OnResumePressed()
	{
		GD.Print("Resume button pressed");
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
		if (_settingsInstance != null && IsInstanceValid(_settingsInstance))
		{
			GD.Print("Settings are already open");
			return;
		}

		OpenSettingsMenu();
	}

	private void OpenSettingsMenu()
	{
		var settingsScene = ResourceLoader.Load<PackedScene>("res://scenes/SupportScenes/settings.tscn");
		if (settingsScene != null)
		{
			_settingsInstance = settingsScene.Instantiate<Settings>();
			_settingsInstance.ProcessMode = ProcessModeEnum.Always;
			GetTree().Root.AddChild(_settingsInstance);
			_settingsInstance.AnchorRight = 1;
			_settingsInstance.AnchorBottom = 1;
			Hide();
			_settingsInstance.BackButtonPressed += OnSettingsBackButtonPressed;
		}
		else
		{
			GD.PrintErr("Failed to load settings scene.");
		}
	}

	private void OnSettingsBackButtonPressed()
	{
		Show();
		_settingsInstance = null;
		_resumeButton?.GrabFocus();
	}

	private void OnQuitPressed()
	{
		SaveGameAndQuit();
	}

	private void SaveGameAndQuit()
	{
		if (_gameState != null)
		{
			_gameState.SaveCurrentLevel();
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
			FallbackQuitToMainMenu();
		}
	}

	private void FallbackQuitToMainMenu()
	{
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/main_menu.tscn");
	}
}
