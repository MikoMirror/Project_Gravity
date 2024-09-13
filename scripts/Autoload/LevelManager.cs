using Godot;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{
	private List<Node3D> _initialSceneState;
	private string _currentLevelScene;
	private Control _levelLoadingInstance;
	private AnimationPlayer _fadeAnimation;
	private ColorRect _fadeOverlay;
	private PackedScene _pauseMenuScene;
	private PauseMenu _pauseMenuInstance;
	private Player _player;
	private string _pendingTargetPortalName;

	public override void _Ready()
	{
		GD.Print("LevelManager: _Ready called");
			_currentLevelScene = GetTree().CurrentScene.SceneFilePath;
			GD.Print($"LevelManager: Current scene path: {_currentLevelScene}");
			SaveInitialSceneState();
			
			// Hide the loading screen
			CallDeferred(nameof(HideLevelLoading));
			
			_pauseMenuScene = GD.Load<PackedScene>("res://scenes/pause_menu.tscn");
			_player = GetNode<Player>("Player");  // Adjust the path if necessary
			
			var gameState = GetNode<GameState>("/root/GameState");
			if (gameState != null)
			{
				if (!string.IsNullOrEmpty(gameState.TargetPortalName))
				{
					CallDeferred(nameof(PositionPlayerAtPortal), gameState.TargetPortalName);
				}

				if (gameState.IsComingFromPortal)
				{
					
					gameState.IsComingFromPortal = false;
				}
			}
			else
			{
				GD.PrintErr("LevelManager: GameState not found.");
			}
			
			// Find the Player node and trigger the fade out animation
			var player = GetTree().CurrentScene.FindChild("Player", true, false) as Player;
			if (player != null)
			{
				player.StartFadeOutAnimation();
			}
			else
			{
				GD.PrintErr("LevelManager: Player not found in the current scene");
			}

			// Check if we have a pending portal positioning
			if (!string.IsNullOrEmpty(_pendingTargetPortalName))
			{
				CallDeferred(nameof(DeferredPositionPlayerAtPortal), _pendingTargetPortalName);
				_pendingTargetPortalName = null;
			}
	}

	public void PositionPlayerAtPortal(string targetPortalName)
	{
		GD.Print($"LevelManager: PositionPlayerAtPortal called with targetPortalName: {targetPortalName}");
		_pendingTargetPortalName = targetPortalName;
		if (IsInsideTree())
		{
			CallDeferred(nameof(DeferredPositionPlayerAtPortal), targetPortalName);
		}
	}

	private void DeferredPositionPlayerAtPortal(string targetPortalName)
	{
		GD.Print($"LevelManager: DeferredPositionPlayerAtPortal called with targetPortalName: {targetPortalName}");

		if (!IsInsideTree())
		{
			GD.PrintErr("LevelManager: Not inside tree. Unable to position player.");
			return;
		}

		var currentScene = GetTree()?.CurrentScene;
		if (currentScene == null)
		{
			GD.PrintErr("LevelManager: Current scene is null. Unable to position player.");
			return;
		}

		var targetPortal = currentScene.FindChild(targetPortalName, true, false) as Portal;
		var player = currentScene.FindChild("Player", true, false) as Player;

		if (targetPortal != null && player != null)
		{
			player.GlobalPosition = targetPortal.GlobalPosition;
			GD.Print($"LevelManager: Positioned player at: {player.GlobalPosition}");
		}
		else
		{
			GD.PrintErr($"LevelManager: Failed to position player. Target portal: {targetPortal}, Player: {player}");
		}

		// Reset GameState
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null)
		{
			gameState.TargetPortalName = "";
			gameState.IsComingFromPortal = false;
		}
		else
		{
			GD.PrintErr("LevelManager: GameState not found.");
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) // ESC key
		{
			TogglePauseMenu();
		}
	}

	private void SaveInitialSceneState()
	{
		_initialSceneState = new List<Node3D>();
		SaveNodeState(GetTree().CurrentScene);
	}

	private void SaveNodeState(Node node)
	{
		if (node is Node3D node3D && (node3D is RigidBody3D || node3D is Player))
		{
			try
			{
				var duplicatedNode = node3D.Duplicate(0);
				if (duplicatedNode != null)
				{
					_initialSceneState.Add(duplicatedNode as Node3D);
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"LevelManager: Error duplicating node {node.Name}: {e.Message}");
			}
		}

		for (int i = 0; i < node.GetChildCount(); i++)
		{
			var child = node.GetChild(i);
			if (child != null && !child.IsQueuedForDeletion())
			{
				SaveNodeState(child);
			}
		}
	}

	private void ResetNodeAnimations(Node node)
	{
		if (node is AnimationPlayer animPlayer)
		{
			animPlayer.Stop();
			animPlayer.Seek(0, true);
		}

		for (int i = 0; i < node.GetChildCount(); i++)
		{
			ResetNodeAnimations(node.GetChild(i));
		}
	}

	public void RestartLevel()
	{
		ClosePauseMenu();
		// Implement level restart logic here
		// For example:
		GetTree().ReloadCurrentScene();
	}

	private void DeferredRestoreInitialState()
	{
		try
		{
			GD.Print("LevelManager: Starting DeferredRestoreInitialState");
			if (IsInsideTree())
			{
				var tree = GetTree();
				if (tree != null)
				{
					GD.Print("LevelManager: SceneTree is not null");
					if (tree.CurrentScene != null)
					{
						GD.Print("LevelManager: CurrentScene is not null, calling RestoreInitialState");
						RestoreInitialState();
					}
					else
					{
						GD.Print("LevelManager: CurrentScene is null, deferring RestoreInitialState");
						CallDeferred(nameof(DeferredRestoreInitialState));
					}
				}
				else
				{
					GD.PrintErr("LevelManager: SceneTree is null, deferring RestoreInitialState");
					CallDeferred(nameof(DeferredRestoreInitialState));
				}
			}
			else
			{
				GD.PrintErr("LevelManager: Not inside tree, deferring RestoreInitialState");
				CallDeferred(nameof(DeferredRestoreInitialState));
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"LevelManager: Error during DeferredRestoreInitialState - {e.Message}\n{e.StackTrace}");
		}
	}

	private void RestoreInitialState()
	{
		var currentScene = GetTree().CurrentScene;
		foreach (Node3D savedNode in _initialSceneState)
		{
			Node3D currentNode = currentScene.FindChild(savedNode.Name, true, false) as Node3D;
			if (currentNode != null)
			{
				currentNode.GlobalTransform = savedNode.GlobalTransform;
				if (currentNode is RigidBody3D rigidBody)
				{
					rigidBody.LinearVelocity = Vector3.Zero;
					rigidBody.AngularVelocity = Vector3.Zero;
				}
				
				// Reset specific object types
				if (currentNode is activatePlatform platform)
				{
					platform.ResetState();
				}
				else if (currentNode is Door door)
				{
					door.Close();
				}
				else if (currentNode is Cable cable)
				{
					cable.Deactivate();
				}
				else if (currentNode is GravityOrb gravityOrb)
				{
					gravityOrb.Reset();  
				}
			}
		}
		ResetNodeAnimations(currentScene);

		GetTree().Paused = false;
	}

	private void HideLevelLoadingAfterDelay()
	{
		FindAndHideLevelLoading();
	}

	private void FindAndHideLevelLoading()
	{
		// Try to find the loading screen with different possible names
		_levelLoadingInstance = GetTree().Root.GetNodeOrNull<Control>("Level_loading") 
			?? GetTree().Root.GetNodeOrNull<Control>("LevelLoading")
			?? GetTree().Root.GetNodeOrNull<Control>("lelev_loading");

		if (_levelLoadingInstance != null)
		{
			GD.Print("LevelManager: LevelLoading node found");
			HideLevelLoading();
		}
		else
		{
			GD.PrintErr("LevelManager: LevelLoading node not found!");
			// Print the entire scene tree for debugging
			PrintSceneTree(GetTree().Root, 0);
		}
	}

	private void HideLevelLoading()
	{
		var levelLoading = GetTree().Root.GetNodeOrNull<Control>("LevelLoading");
		if (levelLoading != null)
		{
			levelLoading.Visible = false;
			levelLoading.QueueFree();
			GD.Print("LevelManager: LevelLoading hidden and removed");
		}
		else
		{
			GD.Print("LevelManager: LevelLoading not found (might have been already removed)");
		}
	}

	// Helper method to print the entire scene tree for debugging
	private void PrintSceneTree(Node node, int depth)
	{
		string indent = new string(' ', depth * 2);
		GD.Print($"{indent}{node.Name} ({node.GetType()})");
		foreach (var child in node.GetChildren())
		{
			PrintSceneTree(child, depth + 1);
		}
	}

	private void InitializeMemoryPuzzles()
	{
		var currentScene = GetTree().CurrentScene;
		var memoryPuzzles = new List<MemoryPuzle>();
		FindMemoryPuzzlesRecursive(currentScene, memoryPuzzles);

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
		_pauseMenuInstance.ProcessMode = Node.ProcessModeEnum.Always;
		_pauseMenuInstance.Initialize(this);
		
		// Drop the lifted object if the player is holding one
		if (_player != null)
		{
			_player.DropLiftedObjectIfHolding();
		}
		
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
		GD.Print($"LevelManager: Changing level to {newLevelPath}");
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null)
		{
			gameState.CurrentLevel = newLevelPath;
			gameState.SaveCurrentLevel();
			_pendingTargetPortalName = gameState.TargetPortalName;
			GD.Print($"LevelManager: Target portal name: {_pendingTargetPortalName}");
		}
		else
		{
			GD.PrintErr("LevelManager: GameState not found. Unable to change level.");
		}

		GetTree().ChangeSceneToFile(newLevelPath);
	}

	public void SaveGameState()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		if (gameState != null)
		{
			// Update gameState properties here
			gameState.SaveCurrentLevel();
		}
		else
		{
			GD.PrintErr("GameState not found. Make sure it's set up as an AutoLoad.");
		}
	}

	public void QuitToMainMenu()
	{
		// Close the pause menu
		ClosePauseMenu();

		// Ensure the game is unpaused
		GetTree().Paused = false;

		// Reset the mouse mode
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Change to the main menu scene
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/main_menu.tscn");
	}
}
