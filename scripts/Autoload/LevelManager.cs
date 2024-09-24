using Godot;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{
	#region Fields
	private List<Node3D> _initialSceneState = new();
	private string _currentLevelScene;
	private PackedScene _pauseMenuScene;
	private PauseMenu _pauseMenuInstance;
	private Player _player;
	private string _pendingTargetPortalName;
	#endregion

	#region Initialization
	public override void _Ready()
	{
		InitializeLevel();
		SetupPlayer();
		HandlePendingPortal();
		InitializeMemoryPuzzles();
	}

	private void InitializeLevel()
	{
		_currentLevelScene = GetTree().CurrentScene.SceneFilePath;
		SaveInitialSceneState();
		CallDeferred(nameof(HideLevelLoading));
		_pauseMenuScene = GD.Load<PackedScene>("res://scenes/pause_menu.tscn");
	}

	private void SetupPlayer()
	{
		_player = GetTree().CurrentScene.FindChild("Player", true, false) as Player;
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null && !string.IsNullOrEmpty(gameState.TargetPortalName))
		{
			CallDeferred(nameof(PositionPlayerAtPortal), gameState.TargetPortalName);
			gameState.IsComingFromPortal = false;
		}
		_player?.StartSpawnAnimation();
	}

	private void HandlePendingPortal()
	{
		if (!string.IsNullOrEmpty(_pendingTargetPortalName))
		{
			CallDeferred(nameof(DeferredPositionPlayerAtPortal), _pendingTargetPortalName);
			_pendingTargetPortalName = null;
		}
	}
	#endregion

	#region Input Handling
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel") && GetTree().CurrentScene.Name != "MainMenu")
		{
			TogglePauseMenu();
			GetViewport().SetInputAsHandled();
		}
	}
	#endregion

	#region Scene State Management
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
		if (node == null) return;

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
	#endregion

	#region Level Management
	public void RestartLevel()
	{
		ClosePauseMenu();
		GetTree().CreateTimer(0.1f).Timeout += DeferredRestartLevel;
	}

	private void DeferredRestartLevel()
	{
		if (!IsInsideTree())
		{
			CallDeferred(nameof(DeferredRestartLevel));
			return;
		}

		try
		{
			var sceneTree = GetTree();
			if (sceneTree != null)
			{
				sceneTree.ReloadCurrentScene();
				sceneTree.CreateTimer(0.1f).Timeout += DeactivateAllPlatforms;
			}
			else
			{
				GD.PrintErr("LevelManager: Scene tree is null in DeferredRestartLevel");
				CallDeferred(nameof(DeferredRestartLevel));
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error in DeferredRestartLevel: {e.Message}");
			CallDeferred(nameof(DeferredRestartLevel));
		}
	}

	private void RestoreInitialState()
	{
		var currentScene = GetTree().CurrentScene;
		foreach (Node3D savedNode in _initialSceneState)
		{
			if (currentScene.FindChild(savedNode.Name, true, false) is Node3D currentNode)
			{
				ResetNodeState(currentNode, savedNode);
			}
		}
		ResetNodeAnimations(currentScene);
		GetTree().Paused = false;
	}

	private void ResetNodeState(Node3D currentNode, Node3D savedNode)
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

	private void HideLevelLoading()
	{
		var levelLoading = GetTree().Root.GetNodeOrNull<Control>("LevelLoading");
		if (levelLoading != null)
		{
			levelLoading.Visible = false;
			levelLoading.QueueFree();
		}
	}
	#endregion

	#region Puzzle Management
	private void InitializeMemoryPuzzles()
	{
		var memoryPuzzles = new List<MemoryPuzzle>();
		FindMemoryPuzzlesRecursive(GetTree().CurrentScene, memoryPuzzles);
		foreach (var memoryPuzle in memoryPuzzles)
		{
			memoryPuzle.ManualSetup();
		}
	}

	private void FindMemoryPuzzlesRecursive(Node node, List<MemoryPuzzle> memoryPuzzles)
	{
		if (node is MemoryPuzzle memoryPuzzle)
		{
			memoryPuzzles.Add(memoryPuzzle);
		}

		foreach (var child in node.GetChildren())
		{
			FindMemoryPuzzlesRecursive(child, memoryPuzzles);
		}
	}
	#endregion

	#region Pause Menu
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
	#endregion

	#region Level Changing
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
		DeactivateAllPlatforms();

		GetTree().CreateTimer(0.5f).Timeout += () => FinalizeLevelSetup(currentScene);
	}

	private void FinalizeLevelSetup(Node currentScene)
	{
		if (!IsInstanceValid(currentScene) || !currentScene.IsInsideTree())
		{
			GD.PrintErr("LevelManager: Scene is no longer valid or not in tree. Aborting setup.");
			return;
		}

		PositionPlayerAtPortal(_pendingTargetPortalName);
		_pendingTargetPortalName = null;

		var player = currentScene.FindChild("Player", true, false) as Player;
		player?.FinishTeleportation();

		if (player == null)
		{
			GD.PrintErr("LevelManager: Player not found in the current scene");
		}

		ForceSceneUpdate(currentScene);
		RenderingServer.ForceSync();
		UpdateWorldEnvironment(currentScene);

		GD.Print("LevelManager: New level setup complete");
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
				meshInstance.GetSurfaceOverrideMaterial(i)?.NotifyPropertyListChanged();
			}
		}

		foreach (var child in rootNode.GetChildren())
		{
			ForceSceneUpdate(child);
		}
	}

	private void UpdateWorldEnvironment(Node currentScene)
{
	var worldEnvironment = currentScene.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
	if (worldEnvironment?.Environment != null)
	{
		worldEnvironment.Environment = worldEnvironment.Environment.Duplicate() as Godot.Environment;
		worldEnvironment.NotifyPropertyListChanged();
	}
}

	#endregion

	#region Object Reset
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
		if (node == null) return;

		ResetAnimationPlayer(node);
		ResetSpecificObjects(node);

		foreach (var child in node.GetChildren())
		{
			if (child != null)
			{
				ResetObjectStates(child);
			}
		}
	}

	private void ResetAnimationPlayer(Node node)
	{
		var animationPlayer = node.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (animationPlayer != null)
		{
			animationPlayer.Stop();
			animationPlayer.Seek(0, true);
		}
	}

	private void ResetSpecificObjects(Node node)
	{
		switch (node)
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
	#endregion

	#region Game State
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
	#endregion

	#region Player Positioning
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
	#endregion

	#region Platform Management
	private void DeactivateAllPlatforms()
	{
		if (!IsInsideTree())
		{
			CallDeferred(nameof(DeactivateAllPlatforms));
			return;
		}

		try
		{
			var platforms = GetTree().GetNodesInGroup("ActivatePlatforms");
			foreach (var platform in platforms)
			{
				if (platform is activatePlatform activePlatform)
				{
					activePlatform.DeactivatePlatformAndDoor();
				}
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error deactivating platforms: {e.Message}");
		}
	}
	#endregion
}
