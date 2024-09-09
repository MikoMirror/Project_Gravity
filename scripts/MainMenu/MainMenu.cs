using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] private NodePath startButtonPath = "VBoxContainer/Start";
	[Export] private NodePath confirmationDialogPath = "NewGameConfirmationDialog";
	private Button startButton;
	private NewGameConfirmationDialog newGameConfirmationDialog;  // Changed to NewGameConfirmationDialog
	private GameState gameState;
	[Export] private Button settingsButton;

	public override void _Ready()
	{
		InitializeComponents();
		ConnectDialogSignals();
		if (settingsButton != null)
		{
			settingsButton.Pressed += OnSettingsButtonPressed;
		}
		else
		{
			GD.PrintErr("Settings button not assigned in the inspector.");
		}
	}

	private void InitializeComponents()
	{
		startButton = GetNodeOrNull<Button>(startButtonPath);
		newGameConfirmationDialog = GetNode<NewGameConfirmationDialog>(confirmationDialogPath);  // Changed to NewGameConfirmationDialog
		gameState = GetNode<GameState>("/root/GameState");

		if (gameState == null)
		{
			GD.PrintErr("GameState not found. Make sure it's set up as an AutoLoad.");
		}

		if (newGameConfirmationDialog == null)
		{
			GD.PrintErr("NewGameConfirmationDialog not found. Check the node path in the inspector.");
		}
	}

	private void ConnectDialogSignals()
	{
		if (newGameConfirmationDialog != null)
		{
			newGameConfirmationDialog.Confirmed += OnConfirmNewGame;
			newGameConfirmationDialog.Canceled += OnCancelNewGame;
		}
	}

	// This method will be called when the Start button is pressed
	public void OnStartNewGamePressed()
	{
		if (SaveLoadManager.Instance.SaveFileExists())
		{
			newGameConfirmationDialog.ShowDialog();
		}
		else
		{
			StartNewGame();
		}
	}

	// This method will be called when the Load Game button is pressed
	public void OnLoadGamePressed()
	{
		if (SaveLoadManager.Instance.SaveFileExists())
		{
			LoadGame();
		}
		else
		{
			// Show a message that no save file exists
			GD.Print("No save file found.");
			// You might want to show this message to the player in the UI
			// You might want to show this message to the player in the UI
		}
	}

	// This method will be called when the Settings button is pressed
	public void OnSettingsButtonPressed()
	{
		// Load and instance the settings scene
		var settingsScene = GD.Load<PackedScene>("res://scenes/SupportScenes/settings.tscn");
		if (settingsScene != null)
		{
			var settingsInstance = settingsScene.Instantiate<Control>();
			
			// Add the settings instance to the scene tree
			GetTree().Root.AddChild(settingsInstance);
			
			// Optionally, you might want to pause the game or hide the main menu
			// this.Hide();  // Hide the main menu
			// GetTree().Paused = true;  // Pause the game
		}
		else
		{
			GD.PrintErr("Failed to load settings scene.");
		}
	}

	// This method will be called when the Exit button is pressed
	public void OnExitPressed()
	{
		// Quit the game
		GetTree().Quit();
	}

	// This method will be called when the Confirm button in the dialog is pressed
	public void OnConfirmNewGame()
	{
		GD.Print("Confirm new game");
		StartNewGame();
	}

	// This method will be called when the Cancel button in the dialog is pressed
	public void OnCancelNewGame()
	{
		GD.Print("Cancel new game");
		// No action needed, dialog will close automatically
	}

	private void StartNewGame()
	{
		if (gameState == null)
		{
			GD.PrintErr("GameState not found. Check AutoLoad setup.");
			return;
		}

		gameState.CurrentLevel = "res://scenes/Level_1.tscn";
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
		{
			GetTree().ChangeSceneToFile(gameState.CurrentLevel);
			// You might want to show this message to the player in the UI
		}
		else
		{
			GD.PrintErr("Failed to load game.");
			// You might want to show this error to the player in the UI
		}
	}
}
