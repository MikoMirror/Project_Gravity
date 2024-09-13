using Godot;
using System;

public class PlayerTeleporter
{
	private AnimationPlayer _animationPlayer;
	private ColorRect _teleportOverlay;
	private bool _isTeleporting = false;
	private Player _player;
	private Camera3D _camera;
	private Node3D _head;
	private Vector3 _playerStartPosition;
	private Transform3D _playerStartTransform;

	public PlayerTeleporter(Player player, AnimationPlayer animationPlayer, ColorRect teleportOverlay, Camera3D camera, Node3D head)
	{
		_player = player;
		_animationPlayer = animationPlayer;
		_teleportOverlay = teleportOverlay;
		_camera = camera;
		_head = head;
	}

	public void StartTeleportAnimation(Vector3 targetPosition, Vector3? forwardDirection = null)
	{
		if (_isTeleporting) return;

		_isTeleporting = true;
		_playerStartPosition = _player.GlobalPosition;
		_playerStartTransform = _player.GlobalTransform;
		_teleportOverlay.Visible = true;
		_animationPlayer.Play("teleportation");

		float teleportDelay = 1.4f;
		float animationLength = _animationPlayer.GetAnimation("teleportation").Length;

		_animationPlayer.GetTree().CreateTimer(teleportDelay).Timeout += () => 
		{
			TeleportTo(targetPosition, forwardDirection ?? -_playerStartTransform.Basis.Z);
		};

		_animationPlayer.GetTree().CreateTimer(animationLength).Timeout += () =>
		{
			if (Godot.GodotObject.IsInstanceValid(_teleportOverlay) && !_teleportOverlay.IsQueuedForDeletion())
				_teleportOverlay.Visible = false;
			_isTeleporting = false;
		};
	}

	private void TeleportTo(Vector3 position, Vector3 forwardDirection)
	{
		if (!_isTeleporting) return;

		if (Godot.GodotObject.IsInstanceValid(_camera) && Godot.GodotObject.IsInstanceValid(_head))
		{
			Transform3D originalCameraTransform = _camera.GlobalTransform;

			if (Godot.GodotObject.IsInstanceValid(_player))
			{
				_player.GlobalPosition = position;
				_head.LookAt(_player.GlobalPosition + forwardDirection);
			}
			else
			{
				GD.Print("PlayerTeleporter: Player not valid, but continuing with camera adjustment.");
			}

			_camera.GlobalTransform = originalCameraTransform;
		}
	}

	public bool IsTeleporting => _isTeleporting;
}
