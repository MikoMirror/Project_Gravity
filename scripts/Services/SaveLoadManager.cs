using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class SaveLoadManager : Node
{
	private const string SAVE_FILE_PATH = "user://savegame.json";

	public static SaveLoadManager Instance { get; private set; }

	public override void _EnterTree()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			QueueFree();
		}
	}

	public void SaveGame(GameState gameState)
	{
		var saveData = new Dictionary<string, object>
		{
			["CurrentLevel"] = gameState.CurrentLevel,
			["PlayerPosition"] = new float[] { gameState.PlayerPosition.X, gameState.PlayerPosition.Y, gameState.PlayerPosition.Z },
			["TargetPortalName"] = gameState.TargetPortalName,
			["IsComingFromPortal"] = gameState.IsComingFromPortal
			// Add any other game state data you want to save
		};

		string jsonString = JsonSerializer.Serialize(saveData);
		
		using var file = FileAccess.Open(SAVE_FILE_PATH, FileAccess.ModeFlags.Write);
		file.StoreString(jsonString);
	}

	public bool LoadGame(GameState gameState)
	{
		if (!FileAccess.FileExists(SAVE_FILE_PATH))
		{
			GD.Print("No save file found.");
			return false;
		}

		using var file = FileAccess.Open(SAVE_FILE_PATH, FileAccess.ModeFlags.Read);
		string jsonString = file.GetAsText();

		try
		{
			var saveData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

			gameState.CurrentLevel = saveData["CurrentLevel"].GetString();
			var playerPosArray = saveData["PlayerPosition"].EnumerateArray();
			float x = 0, y = 0, z = 0;
			int index = 0;
			foreach (var element in playerPosArray)
			{
				switch (index)
				{
					case 0: x = element.GetSingle(); break;
					case 1: y = element.GetSingle(); break;
					case 2: z = element.GetSingle(); break;
				}
				index++;
				if (index > 2) break;
			}
			gameState.PlayerPosition = new Vector3(x, y, z);
			gameState.TargetPortalName = saveData["TargetPortalName"].GetString();
			gameState.IsComingFromPortal = saveData["IsComingFromPortal"].GetBoolean();
			// Load any other game state data you've saved

			return true;
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error loading save file: {e.Message}");
			return false;
		}
	}

	public bool SaveFileExists()
	{
		return FileAccess.FileExists(SAVE_FILE_PATH);
	}

	public void DeleteSaveFile()
	{
		if (FileAccess.FileExists(SAVE_FILE_PATH))
		{
			DirAccess.RemoveAbsolute(SAVE_FILE_PATH);
		}
	}
}
