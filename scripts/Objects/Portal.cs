using Godot;

public partial class Portal : Node3D
{
	[Export]
	public bool IsSpawnPortal = false;

	[Export]
	public NodePath TargetPortalPath; 

	private Portal _targetPortal;
	private bool _isPlayerNearby = false;
	private Player _player; 
	private Label _teleportLabel;
	private Area3D _portalArea; 

	public override void _Ready()
	{
		if (!IsSpawnPortal && !string.IsNullOrEmpty(TargetPortalPath)) 
		{
			_targetPortal = GetNodeOrNull<Portal>(TargetPortalPath);
		}

		_portalArea = GetNode<Area3D>("Area3D");
		if (_portalArea != null)
		{
			_portalArea.BodyEntered += OnBodyEntered;
			_portalArea.BodyExited += OnBodyExited;
		}
		else 
		{
			GD.PrintErr("Area3D node not found in Portal.");
		}
	}

	private void TeleportPlayer()
	{
		if (_targetPortal != null)
		{
			_player.GlobalTransform = _targetPortal.GlobalTransform;
			HideTeleportLabel();
		}
		else
		{
			GD.PrintErr("Portal: Target portal not set or not found.");
		}
	}

	public override void _Process(double delta)
	{
		if (!IsSpawnPortal && _isPlayerNearby && Input.IsActionJustPressed("ui_interaction"))
		{
			TeleportPlayer();
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			_player = player;
			_isPlayerNearby = true;

			if (!IsSpawnPortal) // Only show label if not a spawn portal
			{
				ShowTeleportLabel(player);
			}
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_isPlayerNearby = false;
			HideTeleportLabel();
		}
	}
	
	private void ShowTeleportLabel(Player player)
	{
		_teleportLabel = player.GetNodeOrNull<Label>("PlayerUI/teleportLabel");
		if (_teleportLabel != null)
		{
			_teleportLabel.Visible = true;
		}
		else
		{
			GD.PrintErr("TeleportLabel not found in PlayerUI.");
		}
	}

	private void HideTeleportLabel()
	{
		if (_teleportLabel != null)
		{
			_teleportLabel.Visible = false;
		}
	}
}
