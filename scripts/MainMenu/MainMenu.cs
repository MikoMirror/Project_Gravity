using Godot;
using System;

public partial class MainMenu : Control
{
	#region Exports
	[Export] private NodePath startButtonPath = "VBoxContainer/Start";
	[Export] private NodePath confirmationDialogPath = "NewGameConfirmationDialog";
	[Export] private Button settingsButton;
	#endregion

	#region Private Fields
	private Button startButton;
	private NewGameConfirmationDialog newGameConfirmationDialog;
	private GameState gameState;
	#endregion

	#region Lifecycle Methods
	public override void _Ready()
	{
		PlayBackgroundMusic();
		InitializeComponents();
		ConnectSignals();
		SetProcessUnhandledInput(true);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			GD.Print("ESC pressed in Main Menu");
		}
	}
	#endregion

	#region Initialization Methods
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
	#endregion

	#region Button Event Handlers
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
	#endregion

	#region Dialog Event Handlers
	public void OnConfirmNewGame()
	{
		GD.Print("Confirm new game");
		StartNewGame();
	}

	public void OnCancelNewGame()
	{
		GD.Print("Cancel new game");
	}
	#endregion

	#region Game Management Methods
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
	#endregion
}
