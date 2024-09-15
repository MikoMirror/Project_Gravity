using Godot;
using System;

public partial class Portal : Node3D
{
	public enum PortalType { BetweenLevels, WithinLevel }

	[Export] private PortalType _type = PortalType.WithinLevel;
	[Export] private string _targetLevelPath = "";
	[Export] private string _targetPortalName = "";
	[Export] private bool _isTeleportActive = true; 

	public PortalType Type
	{
		get => _type;
		set => _type = value;
	}

	public string TargetLevelPath
	{
		get => _targetLevelPath;
		set => _targetLevelPath = value;
	}

	public string TargetPortalName
	{
		get => _targetPortalName;
		set => _targetPortalName = value;
	}

	public bool IsTeleportActive
	{
		get => _isTeleportActive;
		set => _isTeleportActive = value;
	}

	private Area3D _portalArea;
	private GameState _gameState;
	private Player _playerInRange;
	private PackedScene _levelLoadingScene;
	private Control _levelLoadingInstance;

	public override void _Ready()
	{
		_portalArea = GetNode<Area3D>("PortalArea");
		_gameState = GetNode<GameState>("/root/GameState");

		if (_portalArea != null)
			{
				_portalArea.BodyEntered += OnBodyEntered;
				_portalArea.BodyExited += OnBodyExited;
			}
		else
			{
				GD.PrintErr("Portal: PortalArea child node not found!");
			}

		_levelLoadingScene = GD.Load<PackedScene>("res://scenes/SupportScenes/lelev_loading.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		if (_isTeleportActive && _playerInRange != null && @event.IsActionPressed("ui_interaction"))
		{
			if (Type == PortalType.BetweenLevels)
			{
				TeleportBetweenLevels(_playerInRange);
			}
			else
			{
				TeleportWithinLevel(_playerInRange);
			}
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_isTeleportActive && body is Player player)
		{
			_playerInRange = player;
			player.GetNode<PlayerUI>("PlayerUI").ShowTeleportLabel(true);
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (_isTeleportActive && body is Player player && player == _playerInRange)
		{
			player.GetNode<PlayerUI>("PlayerUI").ShowTeleportLabel(false);
				_playerInRange = null;
		}
	}

	private void TeleportBetweenLevels(Player player)
	{
		if (string.IsNullOrEmpty(TargetLevelPath) || string.IsNullOrEmpty(TargetPortalName))
		{
			GD.PrintErr("Portal: Target level path or portal name is not set!");
			return;
		}

		_gameState.StorePlayerData(player, TargetPortalName);
		ShowLevelLoading();

		_gameState.IsComingFromPortal = true;
		_gameState.CurrentLevel = TargetLevelPath;
		_gameState.SaveCurrentLevel();

		// Start the teleport animation
		player.Teleport(GlobalPosition, -GlobalTransform.Basis.Z);

		// Find LevelManager as the root of the current scene
		var levelManager = GetTree().CurrentScene as LevelManager;
		if (levelManager != null)
		{
			levelManager.ChangeLevel(TargetLevelPath);
		}
		else
		{
			GD.PrintErr("Portal: LevelManager not found. Unable to change level.");
			HideLevelLoading();
		}
	}

	private void PositionPlayerAtTargetPortal(string targetPortalName)
	{
		GD.Print($"Portal: Positioning player at target portal: {targetPortalName}");
		var currentScene = GetTree().CurrentScene;
		var levelManager = currentScene.GetNode<LevelManager>(".");  // Assuming LevelManager is attached to the root of the scene

		if (levelManager != null)
		{
			levelManager.PositionPlayerAtPortal(targetPortalName);
		}
		else
		{
			GD.PrintErr("Portal: LevelManager not found in the new scene. Unable to position player.");
			PositionPlayerManually(targetPortalName);
		}

		// Hide loading screen
		HideLevelLoading();
	}

	private void PositionPlayerManually(string targetPortalName)
	{
		var currentScene = GetTree().CurrentScene;
		var targetPortal = currentScene.FindChild(targetPortalName, true, false) as Portal;
		var player = currentScene.FindChild("Player", true, false) as Player;

		if (targetPortal != null && player != null)
		{
			player.GlobalPosition = targetPortal.GlobalPosition;
			player.GetNode<Node3D>("Head").LookAt(player.GlobalPosition + targetPortal.GlobalTransform.Basis.Z);
			GD.Print($"Portal: Positioned player at: {player.GlobalPosition}");
		}
		else
		{
			GD.PrintErr($"Portal: Failed to position player. Target portal: {targetPortal}, Player: {player}");
		}
	}

	private void TeleportWithinLevel(Player player)
	{
		if (string.IsNullOrEmpty(TargetPortalName))
		{
			GD.PrintErr("Portal: Target portal name is not set!");
			return;
		}

		var targetPortal = GetTree().CurrentScene.FindChild(TargetPortalName, true, false) as Portal;
		if (targetPortal != null)
		{
			player.Teleport(targetPortal.GlobalPosition, targetPortal.GlobalTransform.Basis.Z);
		}
		else
		{
			GD.PrintErr($"Portal: Target portal '{TargetPortalName}' not found in the current scene!");
		}
	}

	private void ShowLevelLoading()
	{
		if (_levelLoadingInstance == null)
		{
			_levelLoadingInstance = _levelLoadingScene.Instantiate<Control>();
			GetTree().Root.AddChild(_levelLoadingInstance);
			_levelLoadingInstance.Name = "LevelLoading"; 
		}
		_levelLoadingInstance.Visible = true;
		GD.Print("Portal: LevelLoading shown and added to the scene");
	}

	private void HideLevelLoading()
	{
		if (_levelLoadingInstance != null)
		{
			_levelLoadingInstance.Visible = false;
			_levelLoadingInstance.QueueFree();
			_levelLoadingInstance = null;
		}
	}
}
