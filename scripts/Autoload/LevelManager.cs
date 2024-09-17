using Godot;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{
	private List<Node3D> _initialSceneState = new();
	private string _currentLevelScene;
	private PackedScene _pauseMenuScene;
	private PauseMenu _pauseMenuInstance;
	private Player _player;
	private string _pendingTargetPortalName;

	public override void _Ready()
	{
		_currentLevelScene = GetTree().CurrentScene.SceneFilePath;
		SaveInitialSceneState();
		CallDeferred(nameof(HideLevelLoading));
		_pauseMenuScene = GD.Load<PackedScene>("res://scenes/pause_menu.tscn");
		_player = GetTree().CurrentScene.FindChild("Player", true, false) as Player;
		
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null)
		{
			if (!string.IsNullOrEmpty(gameState.TargetPortalName))
			{
				CallDeferred(nameof(PositionPlayerAtPortal), gameState.TargetPortalName);
			}
			gameState.IsComingFromPortal = false;
		}

		_player?.StartFadeOutAnimation();

		if (!string.IsNullOrEmpty(_pendingTargetPortalName))
		{
			CallDeferred(nameof(DeferredPositionPlayerAtPortal), _pendingTargetPortalName);
			_pendingTargetPortalName = null;
		}

		InitializeMemoryPuzzles();
	}

	public override void _Input(InputEvent @event)
{
	if (@event.IsActionPressed("ui_cancel") && GetTree().CurrentScene.Name != "MainMenu")
	{
		TogglePauseMenu();
		GetViewport().SetInputAsHandled();
	}
}

	private void SaveInitialSceneState() => SaveNodeState(GetTree().CurrentScene);

	private void SaveNodeState(Node node)
	{
		if (node is Node3D node3D && (node3D is RigidBody3D || node3D is Player))
		{
			try
			{
				var duplicatedNode = node3D.Duplicate(0) as Node3D;
				if (duplicatedNode != null)
				{
					_initialSceneState.Add(duplicatedNode);
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error duplicating node {node.Name}: {e.Message}");
			}
		}

		foreach (var child in node.GetChildren())
		{
			if (child != null && !child.IsQueuedForDeletion())
			{
				SaveNodeState(child);
			}
		}
	}

	private void ResetNodeAnimations(Node node)
	{
		if (node == null)
		{
			return;
		}

		if (node is AnimationPlayer animPlayer)
		{
			animPlayer.Stop();
			animPlayer.Seek(0, true);
		}

		foreach (var child in node.GetChildren())
		{
			if (child != null)
			{
				ResetNodeAnimations(child);
			}
		}
	}

	public void RestartLevel()
	{
		ClosePauseMenu();
		GetTree().ReloadCurrentScene();
	}

	private void RestoreInitialState()
	{
		var currentScene = GetTree().CurrentScene;
		foreach (Node3D savedNode in _initialSceneState)
		{
			if (currentScene.FindChild(savedNode.Name, true, false) is Node3D currentNode)
			{
				currentNode.GlobalTransform = savedNode.GlobalTransform;
				if (currentNode is RigidBody3D rigidBody)
				{
					rigidBody.LinearVelocity = Vector3.Zero;
					rigidBody.AngularVelocity = Vector3.Zero;
				}
				
				switch (currentNode)
				{
					case activatePlatform platform:
						platform.ResetState();
						break;
					case Door door:
						door.Close();
						break;
					case Cable cable:
						cable.Deactivate();
						break;
					case GravityOrb gravityOrb:
						gravityOrb.Reset();
						break;
				}
			}
		}
		ResetNodeAnimations(currentScene);
		GetTree().Paused = false;
	}

	private void HideLevelLoading()
	{
		var levelLoading = GetTree().Root.GetNodeOrNull<Control>("LevelLoading");
		if (levelLoading != null)
		{
			levelLoading.Visible = false;
			levelLoading.QueueFree();
		}
	}

	private void InitializeMemoryPuzzles()
	{
		var memoryPuzzles = new List<MemoryPuzle>();
		FindMemoryPuzzlesRecursive(GetTree().CurrentScene, memoryPuzzles);
		foreach (var memoryPuzle in memoryPuzzles)
		{
			memoryPuzle.ManualSetup();
		}
	}

	private void FindMemoryPuzzlesRecursive(Node node, List<MemoryPuzle> memoryPuzzles)
	{
		if (node is MemoryPuzle memoryPuzle)
		{
			memoryPuzzles.Add(memoryPuzle);
		}

		foreach (var child in node.GetChildren())
		{
			FindMemoryPuzzlesRecursive(child, memoryPuzzles);
		}
	}

	public void TogglePauseMenu()
	{
		if (_pauseMenuInstance == null)
		{
			OpenPauseMenu();
		}
		else
		{
			ClosePauseMenu();
		}
	}

	private void OpenPauseMenu()
	{
		_pauseMenuInstance = _pauseMenuScene.Instantiate<PauseMenu>();
		AddChild(_pauseMenuInstance);
		_pauseMenuInstance.ProcessMode = ProcessModeEnum.Always;
		_pauseMenuInstance.Initialize(this);
		_player?.DropLiftedObjectIfHolding();
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void ClosePauseMenu()
	{
		if (_pauseMenuInstance != null)
		{
			_pauseMenuInstance.QueueFree();
			_pauseMenuInstance = null;
			GetTree().Paused = false;
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public void ChangeLevel(string newLevelPath)
	{
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null)
		{
			gameState.CurrentLevel = newLevelPath;
			gameState.SaveCurrentLevel();
			_pendingTargetPortalName = gameState.TargetPortalName;
		}

		GetTree().ChangeSceneToFile(newLevelPath);
		CallDeferred(nameof(SetupNewLevel));
	}

	private void SetupNewLevel()
	{
		if (!IsInsideTree())
		{
			GD.PrintErr("LevelManager: Not inside tree when setting up new level");
			return;
		}

		GD.Print("LevelManager: Setting up new level");
		var currentScene = GetTree().CurrentScene;
		GD.Print($"LevelManager: Current scene: {currentScene?.Name ?? "null"}");

		ResetLevelObjects();
		GetTree().CreateTimer(0.5f).Timeout += () =>
		{
			if (!IsInstanceValid(currentScene) || !currentScene.IsInsideTree())
			{
				GD.PrintErr("LevelManager: Scene is no longer valid or not in tree. Aborting setup.");
				return;
			}

			PositionPlayerAtPortal(_pendingTargetPortalName);
			_pendingTargetPortalName = null;

			var player = currentScene.FindChild("Player", true, false) as Player;
			if (player != null)
			{
				player.FinishTeleportation();
			}
			else
			{
				GD.PrintErr("LevelManager: Player not found in the current scene");
			}

			// Force update of lights and materials
			ForceSceneUpdate(currentScene);

			// Force RenderingServer update
			RenderingServer.ForceSync();

			// Update WorldEnvironment if present
			var worldEnvironment = currentScene.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
			if (worldEnvironment != null && worldEnvironment.Environment != null)
			{
				worldEnvironment.Environment = worldEnvironment.Environment.Duplicate() as Godot.Environment;
				worldEnvironment.NotifyPropertyListChanged();
			}

			GD.Print("LevelManager: New level setup complete");
		};
	}

	private void ForceSceneUpdate(Node rootNode)
	{
		if (rootNode is Light3D light)
		{
			light.NotifyPropertyListChanged();
		}
		else if (rootNode is MeshInstance3D meshInstance)
		{
			for (int i = 0; i < meshInstance.GetSurfaceOverrideMaterialCount(); i++)
			{
				var material = meshInstance.GetSurfaceOverrideMaterial(i);
				if (material != null)
				{
					material.NotifyPropertyListChanged();
				}
			}
		}

		foreach (var child in rootNode.GetChildren())
		{
			ForceSceneUpdate(child);
		}
	}

	private void ResetLevelObjects()
	{
		var currentScene = GetTree().CurrentScene;
		if (currentScene != null)
		{
			ResetNodeAnimations(currentScene);
			ResetObjectStates(currentScene);
		}
		else
		{
			GD.PrintErr("LevelManager: Current scene is null when trying to reset level objects");
		}
	}

	private void ResetObjectStates(Node node)
	{
		if (node == null)
		{
			return;
		}

		// Reset animation player if present
		var animationPlayer = node.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (animationPlayer != null)
		{
			animationPlayer.Stop();
			animationPlayer.Seek(0, true);
		}

		switch (node)
		{
			case activatePlatform platform when platform != null:
				platform.ResetState();
				break;
			case Door door when door != null:
				door.Close();
				break;
			case Cable cable when cable != null:
				cable.Deactivate();
				break;
			case GravityOrb gravityOrb when gravityOrb != null:
				gravityOrb.Reset();
				break;
		}

		foreach (var child in node.GetChildren())
		{
			if (child != null)
			{
				ResetObjectStates(child);
			}
		}
	}

	public void SaveGameState()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		gameState?.SaveCurrentLevel();
	}

	public void QuitToMainMenu()
	{
		ClosePauseMenu();
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/main_menu.tscn");
	}

	public void PositionPlayerAtPortal(string targetPortalName)
	{
		_pendingTargetPortalName = targetPortalName;
		if (IsInsideTree())
		{
			CallDeferred(nameof(DeferredPositionPlayerAtPortal), targetPortalName);
		}
	}

	private void DeferredPositionPlayerAtPortal(string targetPortalName)
	{
		if (!IsInsideTree())
		{
			GD.PrintErr("LevelManager: Not inside tree. Unable to position player.");
			return;
		}

		var currentScene = GetTree().CurrentScene;
		if (currentScene == null)
		{
			GD.PrintErr("LevelManager: Current scene is null. Unable to position player.");
			return;
		}

		var targetPortal = currentScene.FindChild(targetPortalName, true, false) as Portal;
		var player = currentScene.FindChild("Player", true, false) as Player;

		if (targetPortal != null && player != null)
		{
			GD.Print($"LevelManager: Positioning player at portal {targetPortalName}");
			player.GlobalPosition = targetPortal.GlobalPosition;
			player.LookAt(player.GlobalPosition + targetPortal.GlobalTransform.Basis.Z);
		}
		else
		{
			GD.PrintErr($"LevelManager: Failed to position player. Target portal: {targetPortal}, Player: {player}");
		}

		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null)
		{
			gameState.TargetPortalName = "";
			gameState.IsComingFromPortal = false;
		}
	}
}
