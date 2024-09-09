using Godot;
using System;

public partial class GameState : Node
{
	public Vector3 PlayerPosition { get; set; }
	public string TargetPortalName { get; set; }
	public bool IsComingFromPortal { get; set; } = false;

	public string CurrentLevel { get; set; } = "res://scenes/Level_1.tscn";

	public void StorePlayerData(Player player, string targetPortalName)
	{
		PlayerPosition = player.GlobalTransform.Origin;
		TargetPortalName = targetPortalName;
	}

	public void SaveCurrentLevel()
	{
		var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);
		saveGame.StoreLine(CurrentLevel);
		saveGame.Close();
	}

	public void LoadCurrentLevel()
	{
		if (FileAccess.FileExists("user://savegame.save"))
		{
			var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);
			CurrentLevel = saveGame.GetLine();
			saveGame.Close();
		}
	}
}
