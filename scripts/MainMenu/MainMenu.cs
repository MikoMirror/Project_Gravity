using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] private NodePath startButtonPath = "VBoxContainer/Start";
	[Export] private NodePath confirmationDialogPath = "NewGameConfirmationDialog";
	[Export] private Button settingsButton;

	private Button startButton;
	private NewGameConfirmationDialog newGameConfirmationDialog;
	private GameState gameState;

	public override void _Ready()
	{
		PlayBackgroundMusic();
		InitializeComponents();
		ConnectSignals();
		SetProcessUnhandledInput(true);
	}

	private void PlayBackgroundMusic()
	{
		GetNode<MusicManager>("/root/MusicManager").PlayMusic("res://assets/Music/Main.ogg");
	}

	private void InitializeComponents()
	{
		startButton = GetNodeOrNull<Button>(startButtonPath);
		newGameConfirmationDialog = GetNode<NewGameConfirmationDialog>(confirmationDialogPath);
		gameState = GetNode<GameState>("/root/GameState");

		ValidateComponents();
	}

	private void ValidateComponents()
	{
		if (gameState == null)
			GD.PrintErr("GameState not found. Make sure it's set up as an AutoLoad.");

		if (newGameConfirmationDialog == null)
			GD.PrintErr("NewGameConfirmationDialog not found. Check the node path in the inspector.");

		if (settingsButton == null)
			GD.PrintErr("Settings button not assigned in the inspector.");
	}

	private void ConnectSignals()
	{
		if (newGameConfirmationDialog != null)
		{
			newGameConfirmationDialog.Confirmed += OnConfirmNewGame;
			newGameConfirmationDialog.Canceled += OnCancelNewGame;
		}

		if (settingsButton != null)
			settingsButton.Pressed += OnSettingsButtonPressed;
	}

	public void OnStartNewGamePressed()
	{
		if (SaveLoadManager.Instance.SaveFileExists())
			newGameConfirmationDialog.ShowDialog();
		else
			StartNewGame();
	}

	public void OnLoadGamePressed()
	{
		if (SaveLoadManager.Instance.SaveFileExists())
			LoadGame();
		else
			GD.Print("No save file found.");
	}

	public void OnSettingsButtonPressed()
	{
		var settingsScene = GD.Load<PackedScene>("res://scenes/SupportScenes/settings.tscn");
		if (settingsScene != null)
		{
			var settingsInstance = settingsScene.Instantiate<Control>();
			AddChild(settingsInstance);
		}
		else
		{
			GD.PrintErr("Failed to load settings scene.");
		}
	}

	public void OnExitPressed()
	{
		GetTree().Quit();
	}

	public void OnConfirmNewGame()
	{
		GD.Print("Confirm new game");
		StartNewGame();
	}

	public void OnCancelNewGame()
	{
		GD.Print("Cancel new game");
	}

	private void StartNewGame()
	{
		if (gameState == null)
		{
			GD.PrintErr("GameState not found. Check AutoLoad setup.");
			return;
		}

		gameState.CurrentLevel = "res://scenes/Levels/Level_1.tscn";
		SaveLoadManager.Instance.DeleteSaveFile();
		gameState.SaveCurrentLevel();
		GetTree().ChangeSceneToFile(gameState.CurrentLevel);
	}

	private void LoadGame()
	{
		if (gameState == null)
		{
			GD.PrintErr("GameState not found. Check AutoLoad setup.");
			return;
		}

		if (SaveLoadManager.Instance.LoadGame(gameState))
			GetTree().ChangeSceneToFile(gameState.CurrentLevel);
		else
			GD.PrintErr("Failed to load game.");
	}

	public override void _UnhandledInput(InputEvent @event)
{
	if (@event.IsActionPressed("ui_cancel"))
	{
		GetViewport().SetInputAsHandled();
		// Optionally, you can add any specific behavior for ESC in the main menu here
		GD.Print("ESC pressed in Main Menu");
	}
}
}
