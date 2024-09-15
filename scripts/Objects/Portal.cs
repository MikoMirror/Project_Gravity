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

	public override void _Ready()
	{
		_portalArea = GetNodeOrNull<Area3D>("PortalArea");
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
		_gameState.IsComingFromPortal = true;
		_gameState.CurrentLevel = TargetLevelPath;
		_gameState.SaveCurrentLevel();

		// Start the teleport animation on the player
		player.Teleport(GlobalPosition, -GlobalTransform.Basis.Z);

		// Use CallDeferred to change the level after the animation completes
		var animationPlayer = player.GetNode<AnimationPlayer>("AnimationPlayer");
		if (animationPlayer != null)
		{
			animationPlayer.AnimationFinished += (animName) =>
			{
				if (animName == "teleportation")
				{
					ChangeLevelDeferred();
				}
			};
		}
		else
		{
			GD.PrintErr("Portal: AnimationPlayer not found in Player. Changing level immediately.");
			ChangeLevelDeferred();
		}
	}

	private void ChangeLevelDeferred()
	{
		var levelManager = GetNode<LevelManager>("/root/LevelManager");
		if (levelManager != null)
		{
			levelManager.CallDeferred(nameof(LevelManager.ChangeLevel), TargetLevelPath);
		}
		else
		{
			GD.PrintErr("Portal: LevelManager not found. Unable to change level.");
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
}
