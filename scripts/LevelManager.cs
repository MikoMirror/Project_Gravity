using Godot;
using System;
using System.Collections.Generic;

public partial class LevelManager : Node
{
	private List<Node3D> _initialSceneState;
	private string _currentLevelScene;
	private PackedScene _levelLoadingScene;
	private Control _levelLoadingInstance;

	public override void _Ready()
	{
		GD.Print("LevelManager: _Ready called");
		_currentLevelScene = GetTree().CurrentScene.SceneFilePath;
		GD.Print($"LevelManager: Current scene path: {_currentLevelScene}");
		SaveInitialSceneState();
		
		_levelLoadingScene = GD.Load<PackedScene>("res://scenes/lelev_loading.tscn");
		
		var gameState = GetNode<GameState>("/root/GameState");
		if (!string.IsNullOrEmpty(gameState.TargetPortalName))
		{
			CallDeferred(nameof(PositionPlayerAtTargetPortal), gameState.TargetPortalName);
		}

		if (gameState.IsComingFromPortal)
		{
			GetTree().CreateTimer(0.5f).Timeout += HideLevelLoadingAfterDelay;
			gameState.IsComingFromPortal = false;
		}
	}

	private void PositionPlayerAtTargetPortal(string targetPortalName)
	{
		GD.Print($"LevelManager: Positioning player at target portal: {targetPortalName}");
		var targetPortal = GetTree().CurrentScene.FindChild(targetPortalName, true, false) as Portal;
		if (targetPortal != null)
		{
			GD.Print($"LevelManager: Found target portal at position: {targetPortal.GlobalPosition}");
			var player = GetTree().CurrentScene.FindChild("Player", true, false) as Player;
			if (player != null)
			{
				player.GlobalPosition = targetPortal.GlobalPosition;
				player.GetNode<Node3D>("Head").LookAt(targetPortal.GlobalTransform.Origin + targetPortal.GlobalTransform.Basis.Z);
				GD.Print($"LevelManager: Positioned player at: {player.GlobalPosition}");
			}
			else
			{
				GD.PrintErr("LevelManager: Player not found in the current scene");
			}
		}
		else
		{
			GD.PrintErr($"LevelManager: Target portal '{targetPortalName}' not found in the current scene");
		}
		
		var gameState = GetNode<GameState>("/root/GameState");
		gameState.TargetPortalName = "";
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("restart_level"))
		{
			RestartLevel();
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
		try
		{
			GD.Print("LevelManager: Starting RestartLevel");
			var tree = GetTree();
			if (tree != null)
			{
				GD.Print("LevelManager: Pausing the game");
				tree.Paused = true;

				GD.Print("LevelManager: Saving current scene state");
				SaveInitialSceneState();

				GD.Print("LevelManager: Reloading the current scene");
				var currentScene = tree.CurrentScene;
				var currentScenePath = currentScene.SceneFilePath;
				GD.Print($"LevelManager: Current scene path: {currentScenePath}");

				tree.ChangeSceneToFile(currentScenePath);
				tree.Paused = false;

				GD.Print("LevelManager: Scene reloaded");
			}
			else
			{
				GD.PrintErr("LevelManager: Unable to restart level. SceneTree is null.");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"LevelManager: Error during RestartLevel - {e.Message}\n{e.StackTrace}");
		}
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
		if (_levelLoadingInstance != null)
		{
			_levelLoadingInstance.Visible = false;
			_levelLoadingInstance.QueueFree();
			_levelLoadingInstance = null;
			GD.Print("LevelManager: LevelLoading hidden and removed");
		}
		else
		{
			GD.PrintErr("LevelManager: LevelLoading instance is null when trying to hide");
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
}
