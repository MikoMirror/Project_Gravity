using Godot;
using System;

public partial class GameState : Node
{
	public Vector3 PlayerPosition { get; set; }
	public string TargetPortalName { get; set; }
	public bool IsComingFromPortal { get; set; } = false;
	public string CurrentLevel { get; set; } = "res://scenes/Level_1.tscn";

	[Signal]
	public delegate void GameSavedEventHandler();

	[Signal]
	public delegate void GameLoadedEventHandler();

	public override void _Ready()
	{
		// Ensure this node is in the scene tree
		if (!IsInsideTree())
		{
			GetTree().Root.AddChild(this);
		}
	}

	public void StorePlayerData(Player player, string targetPortalName)
	{
		PlayerPosition = player.GlobalTransform.Origin;
		TargetPortalName = targetPortalName;
	}

	public void SaveCurrentLevel()
	{
		if (SaveLoadManager.Instance != null)
		{
			SaveLoadManager.Instance.SaveGame(this);
			EmitSignal(SignalName.GameSaved);
		}
		else
		{
			GD.PrintErr("Cannot save game: SaveLoadManager is not initialized.");
		}
	}

	public bool LoadCurrentLevel()
	{
		if (SaveLoadManager.Instance == null)
		{
			GD.PrintErr("SaveLoadManager.Instance is null in GameState.LoadCurrentLevel");
			return false;
		}

		bool loadSuccess = SaveLoadManager.Instance.LoadGame(this);
		if (loadSuccess)
		{
			GD.Print($"Game loaded in GameState. Current level: {CurrentLevel}");
			EmitSignal(SignalName.GameLoaded);
			return true;
		}
		else
		{
			GD.PrintErr("Failed to load game in GameState.LoadCurrentLevel");
			return false;
		}
	}
}
