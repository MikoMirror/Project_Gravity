using Godot;
using System.Collections.Generic;

public partial class activatePlatform : Node3D
{
	private AnimationPlayer _platformAnimationPlayer;
	private Area3D _area;
	private bool _isActivated = false;
	private int _objectsOnPlatform = 0;
	private Door _associatedDoor;

	[Export]
	public string ActivateAnimationName { get; set; } = "activate_platform";

	[Export]
	public NodePath AssociatedDoorPath { get; set; }

	[Export]
	public NodePath CableListPath { get; set; } // Path to the parent node containing all cables

	private List<AnimationPlayer> _cableAnimationPlayers = new List<AnimationPlayer>();

	public override void _Ready()
	{
		_platformAnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_area = GetNode<Area3D>("Area3D");

		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
			_area.BodyExited += OnBodyExited;
		}
		else
		{
			GD.PrintErr("ActivatePlatform: No Area3D found as a child of this node.");
		}

		if (AssociatedDoorPath != null && AssociatedDoorPath != "")
		{
			_associatedDoor = GetNode<Door>(AssociatedDoorPath);
			if (_associatedDoor == null)
			{
				GD.PrintErr("ActivatePlatform: Associated door not found at the specified path.");
			}
		}

		// Automatically find all cables
		if (CableListPath != null && CableListPath != "")
		{
			Node3D cableList = GetNode<Node3D>(CableListPath);
			if (cableList != null)
			{
				foreach (Node3D cableNode in cableList.GetChildren())
				{
					var cableAnimPlayer = cableNode.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
					if (cableAnimPlayer != null)
					{
						_cableAnimationPlayers.Add(cableAnimPlayer);
					}
					else
					{
						GD.PrintErr($"ActivatePlatform: No AnimationPlayer found in cable node {cableNode.Name}.");
					}
				}
			}
			else
			{
				GD.PrintErr("ActivatePlatform: Cable list node not found at the specified path.");
			}
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		_objectsOnPlatform++;
		if (!_isActivated)
		{
			ActivatePlatformAndDoor();
		}
	}

	private void OnBodyExited(Node3D body)
	{
		_objectsOnPlatform--;
		if (_objectsOnPlatform == 0 && _isActivated)
		{
			DeactivatePlatformAndDoor();
		}
	}

	private void ActivatePlatformAndDoor()
	{
		_isActivated = true;
		_platformAnimationPlayer.Play(ActivateAnimationName);
		_associatedDoor?.Open();

		// Activate all cables
		foreach (var cableAnimPlayer in _cableAnimationPlayers)
		{
			cableAnimPlayer.Play("cable_enable");
		}
	}

	private void DeactivatePlatformAndDoor()
	{
		_isActivated = false;
		_platformAnimationPlayer.PlayBackwards(ActivateAnimationName);
		_associatedDoor?.Close();

		// Deactivate all cables
		foreach (var cableAnimPlayer in _cableAnimationPlayers)
		{
			cableAnimPlayer.PlayBackwards("cable_enable");
		}
	}
}
