using Godot;
using System;

public partial class GravityManager : Node
{
	[Export] public float GravityStrength = 9.8f;
	[Export] public float GravityFlipDuration = 0.5f;
	[Export] public float RotationSpeed = 10.0f;
	public bool IsGravityReversed { get; private set; } = false;
	private float _flipTimer = 0f;
	private bool _isFlipping = false;
	private Player _player;
	private Node3D _playerHead;
	private PlayerUI _jumpsIndicator;

	 public override void _Ready()
	{
		_player = GetParent<Player>();
		if (_player == null)
		{
			GD.PrintErr("GravityManager must be a child of a Player node!");
			return;
		}
		_playerHead = _player.GetNode<Node3D>("Head");

		_jumpsIndicator = _player.GetNode<PlayerUI>("PlayerUI"); // Assuming "PlayerUI" is the node name in your player scene
		if (_jumpsIndicator == null)
		{
			GD.PrintErr("PlayerUI not found in Player!");
		}
		}
	public override void _PhysicsProcess(double delta)
	{
		if (_isFlipping)
		{
			HandleGravityFlip(delta);
		}
	}

	public void ToggleGravity()
	{
		if (_jumpsIndicator.CanJump())
		{
			IsGravityReversed = !IsGravityReversed;
			_isFlipping = true;
			_flipTimer = 0f;
			UpdateHeldObjectGravity(IsGravityReversed);
			_jumpsIndicator.UseJump();
		}
	}

	public bool TryToggleGravity()
	{
		if (_jumpsIndicator.CanJump() && !_isFlipping)
		{
			IsGravityReversed = !IsGravityReversed;
			_isFlipping = true;
			_flipTimer = 0f;
			UpdateHeldObjectGravity(IsGravityReversed);
			_jumpsIndicator.UseJump();
			return true;
		}
		return false;
	}

	private void HandleGravityFlip(double delta)
	{
		_flipTimer += (float)delta;
		float progress = Mathf.Clamp(_flipTimer / GravityFlipDuration, 0, 1);
		float angle = IsGravityReversed ? Mathf.Pi : 0;
		
		Quaternion startRotation = _player.Basis.GetRotationQuaternion().Normalized();
		Quaternion targetRotation = Quaternion.FromEuler(new Vector3(angle, 0, 0)).Normalized();
		Quaternion newRotation = startRotation.Slerp(targetRotation, progress).Normalized();
		
		_player.Basis = new Basis(newRotation);
		_player.UpDirection = IsGravityReversed ? Vector3.Down : Vector3.Up;
		
		if (_flipTimer >= GravityFlipDuration)
		{
			_isFlipping = false;
			_player.Basis = new Basis(targetRotation);
			_player.ResetCameraRotation();
		}
	}

	public float GetCurrentGravity()
	{
		return IsGravityReversed ? GravityStrength : -GravityStrength;
	}

	public void FlipPlayerBody(Player player)
	{
		var tween = CreateTween();
		Quaternion startRotation = player.GlobalTransform.Basis.GetRotationQuaternion();
		Quaternion endRotation = Quaternion.FromEuler(new Vector3(Mathf.Pi, 0, 0)) * startRotation;
		tween.TweenMethod(Callable.From<Quaternion>((rotation) => UpdatePlayerRotation(player, rotation)), 
						  startRotation, endRotation, GravityFlipDuration);
	}

	private void UpdatePlayerRotation(Player player, Quaternion rotation)
	{
		player.GlobalTransform = new Transform3D(new Basis(rotation), player.GlobalPosition);
	}

	public void UpdateHeldObjectGravity(bool isGravityReversed)
	{
		if (_player.IsLifting && _player.HeldObject != null)
		{
			_player.HeldObject.GravityScale = isGravityReversed ? -1 : 1;
		}
	}
}
