using Godot;
using System.Collections.Generic;

public partial class activatePlatform : Node3D
{
	#region Fields
	private AnimationPlayer _platformAnimationPlayer;
	private Area3D _area;
	private bool _isActivated = false;
	private int _objectsOnPlatform = 0;
	private Door _associatedDoor;
	private SoundManager _soundManager;
	private List<AnimationPlayer> _cableAnimationPlayers = new List<AnimationPlayer>();

	[Export] public string ActivateAnimationName { get; set; } = "activate_platform";
	[Export] public NodePath AssociatedDoorPath { get; set; }
	[Export] public NodePath CableListPath { get; set; }
	[Export] private string _activationSoundPath = "res://assets/Sounds/Platform_Activate.mp3";
	#endregion

	#region Initialization
	public override void _Ready()
	{
		SetupComponents();
		SetupAssociatedDoor();
		SetupCables();
		AddToGroup("ActivatePlatforms");
		ResetState();
	}

	private void SetupComponents()
	{
		_platformAnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_area = GetNode<Area3D>("Area3D");
		_soundManager = GetNode<SoundManager>("/root/SoundManager");

		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
			_area.BodyExited += OnBodyExited;
		}
		else
		{
			GD.PrintErr("ActivatePlatform: No Area3D found as a child of this node.");
		}
	}

	private void SetupAssociatedDoor()
	{
		if (!string.IsNullOrEmpty(AssociatedDoorPath))
		{
			_associatedDoor = GetNode<Door>(AssociatedDoorPath);
			if (_associatedDoor == null)
			{
				GD.PrintErr("ActivatePlatform: Associated door not found at the specified path.");
			}
		}
	}

	private void SetupCables()
	{
		if (!string.IsNullOrEmpty(CableListPath))
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
	#endregion

	#region Event Handlers
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
	#endregion

	#region Platform State Management
	private void ActivatePlatformAndDoor()
	{
		_isActivated = true;
		PlayAnimation(ActivateAnimationName);
		_associatedDoor?.Open();
		AnimateCables("cable_enable");
		_soundManager.PlaySound(_activationSoundPath);
		UpdateVisualState(true);
	}

	public void DeactivatePlatformAndDoor()
	{
		if (!IsInstanceValid(this) || IsQueuedForDeletion())
		{
			return;
		}

		_isActivated = false;
		PlayAnimation(ActivateAnimationName, true);
		_associatedDoor?.Close();
		AnimateCables("cable_enable", true);
		UpdateVisualState(false);
	}

	public void ResetState()
	{
		_isActivated = false;
		_objectsOnPlatform = 0;
		ResetAnimation(ActivateAnimationName);
		_associatedDoor?.ResetState();
		ResetCableAnimations();
		UpdateVisualState(false);
	}
	#endregion

	#region Animation Helpers
	private void PlayAnimation(string animationName, bool backwards = false)
	{
		if (_platformAnimationPlayer != null && _platformAnimationPlayer.HasAnimation(animationName))
		{
			if (backwards)
			{
				_platformAnimationPlayer.PlayBackwards(animationName);
			}
			else
			{
				_platformAnimationPlayer.Play(animationName);
			}
		}
	}

	private void ResetAnimation(string animationName)
	{
		if (_platformAnimationPlayer != null && _platformAnimationPlayer.HasAnimation(animationName))
		{
			_platformAnimationPlayer.Stop();
			_platformAnimationPlayer.Play(animationName);
			_platformAnimationPlayer.Seek(0, true);
			_platformAnimationPlayer.Stop();
		}
	}

	private void AnimateCables(string animationName, bool backwards = false)
	{
		foreach (var cableAnimPlayer in _cableAnimationPlayers)
		{
			if (cableAnimPlayer != null && cableAnimPlayer.HasAnimation(animationName))
			{
				if (backwards)
				{
					cableAnimPlayer.PlayBackwards(animationName);
				}
				else
				{
					cableAnimPlayer.Play(animationName);
				}
			}
		}
	}

	private void ResetCableAnimations()
	{
		foreach (var cableAnimPlayer in _cableAnimationPlayers)
		{
			if (cableAnimPlayer.HasAnimation("cable_enable"))
			{
				cableAnimPlayer.Stop();
				cableAnimPlayer.Play("cable_enable");
				cableAnimPlayer.Seek(0, true);
				cableAnimPlayer.Stop();
			}
		}
	}
	#endregion

	#region Visual State Management
	private void UpdateVisualState(bool activated)
	{
		var meshInstance = GetNodeOrNull<MeshInstance3D>("PlatformMesh");
		if (meshInstance != null && meshInstance.MaterialOverride is ShaderMaterial material)
		{
			material.SetShaderParameter("activated", activated);
		}
	}
	#endregion
}
