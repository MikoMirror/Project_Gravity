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

		var gameState = GetNode<GameState>("/root/GameState");
		gameState.StorePlayerData(player, TargetPortalName);
		GD.Print($"Portal: Stored player data. Target portal: {TargetPortalName}");

		ShowLevelLoading();

		gameState.IsComingFromPortal = true;
		
		// Autosave the current level before changing
		gameState.CurrentLevel = TargetLevelPath;
		gameState.SaveCurrentLevel();

		// Use CallDeferred to change the scene after this frame
		GetTree().CreateTimer(0.1f).Timeout += () =>
		{
			GetTree().ChangeSceneToFile(TargetLevelPath);
		};
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
			ShowLevelLoading();

			player.TeleportTo(targetPortal.GlobalPosition, targetPortal.GlobalTransform.Basis.Z);

			GetTree().CreateTimer(1f).Timeout += HideLevelLoading;
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
