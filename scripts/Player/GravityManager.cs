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
	private GravityJumpsIndicator _jumpsIndicator;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		if (_player == null)
		{
			GD.PrintErr("GravityManager must be a child of a Player node!");
			return;
		}
		_playerHead = _player.GetNode<Node3D>("Head");
		PackedScene indicatorScene = GD.Load<PackedScene>("res://scenes/gravity_jumps_indicator.tscn");
		if (indicatorScene != null)
		{
			_jumpsIndicator = indicatorScene.Instantiate<GravityJumpsIndicator>();
			AddChild(_jumpsIndicator);
		}
		else
		{
			GD.PrintErr("Failed to load GravityJumpsIndicator scene!");
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
			_player.UpdateHeldObjectGravity(IsGravityReversed);
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
			_player.UpdateHeldObjectGravity(IsGravityReversed);
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
		
		Quaternion targetRotation = Quaternion.FromEuler(new Vector3(angle, 0, 0));
		_player.Basis = _player.Basis.Slerp(new Basis(targetRotation), progress);
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
}
