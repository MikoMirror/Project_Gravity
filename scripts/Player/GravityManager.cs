// GravityManager.cs
using Godot;
using System;

public partial class GravityManager : Node
{
	[Export] public float GravityStrength = 9.8f;
	[Export] public float GravityFlipDuration = 1.5f;
	[Export] public float RotationSpeed = 5.0f;
	public bool IsGravityReversed { get; private set; } = false;
	private float _flipTimer = 0f;
	private bool _isFlipping = false;
	private float _currentRotation = 0f;
	private Player _player;
	private Node3D _playerHead;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		if (_player == null)
		{
			GD.PrintErr("GravityManager must be a child of a Player node!");
		}
		else
		{
			_playerHead = _player.GetNode<Node3D>("Head");
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
	if (!_isFlipping)
	{
		IsGravityReversed = !IsGravityReversed;
		_isFlipping = true;
		_flipTimer = 0f;
		_player.UpdateHeldObjectGravity(IsGravityReversed);
	}
}

	
	private void HandleGravityFlip(double delta)
{
	_flipTimer += (float)delta;
	float targetRotation = IsGravityReversed ? Mathf.Pi : 0;
	_currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation, RotationSpeed * (float)delta);
	
	Quaternion rotation = Quaternion.FromEuler(new Vector3(0, 0, _currentRotation));
	_player.Basis = new Basis(rotation);
	
	_player.UpDirection = IsGravityReversed ? Vector3.Down : Vector3.Up;

	if (_flipTimer >= GravityFlipDuration)
	{
		_isFlipping = false;
		_currentRotation = targetRotation;
		_player.Basis = new Basis(Quaternion.FromEuler(new Vector3(0, 0, targetRotation)));
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
