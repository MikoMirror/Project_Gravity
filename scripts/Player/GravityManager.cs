using Godot;
using System;

public partial class GravityManager : Node
{
	[Export] public float GravityStrength = 9.8f;
	[Export] public float GravityFlipDuration = 0.5f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public int MaxGravityJumps = 3;
	public bool IsGravityReversed { get; private set; } = false;
	private float _flipTimer = 0f;
	private bool _isFlipping = false;
	private Player _player;
	private Node3D _playerHead;
	private ColorRect _gravityIndicator;
	private int _currentGravityJumps;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		if (_player == null)
		{
			GD.PrintErr("GravityManager must be a child of a Player node!");
			return;
		}
		_playerHead = _player.GetNode<Node3D>("Head");

		// Find the GravityIndicator (ColorRect) in the PlayerUI
		_gravityIndicator = _player.GetNode<ColorRect>("PlayerUI/ColorRect");
		if (_gravityIndicator == null)
		{
			GD.PrintErr("ColorRect (GravityIndicator) not found in PlayerUI!");
		}

		_currentGravityJumps = MaxGravityJumps;
		UpdateGravityIndicator();
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
		if (TryToggleGravity())
		{
			// Additional logic if needed when gravity is successfully toggled
		}
	}

	public bool TryToggleGravity()
	{
		if (_currentGravityJumps > 0 && !_isFlipping)
		{
			IsGravityReversed = !IsGravityReversed;
			_isFlipping = true;
			_flipTimer = 0f;
			UpdateHeldObjectGravity(IsGravityReversed);
			_currentGravityJumps--;
			UpdateGravityIndicator();
			return true;
		}
		return false;
	}

	public void ReplenishGravityJump()
	{
		if (_currentGravityJumps < MaxGravityJumps)
		{
			_currentGravityJumps++;
			UpdateGravityIndicator();
		}
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

	private void UpdateGravityIndicator()
	{
		if (_gravityIndicator != null)
		{
			float value = 0f;
			switch (_currentGravityJumps)
			{
				case 1:
					value = 0.3f;
					break;
				case 2:
					value = 0.6f;
					break;
				case 3:
					value = 1f;
					break;
				default:
					value = 0f;
					break;
			}
			_gravityIndicator.Material.Set("shader_parameter/value", value);
		}
	}

	public void ResetGravityJumps()
	{
		_currentGravityJumps = MaxGravityJumps;
		IsGravityReversed = false;
		_isFlipping = false;
		_flipTimer = 0f;
		UpdateGravityIndicator();
	}

	public bool CanUseGravityJump()
	{
		return _currentGravityJumps > 0;
	}

	public void UseGravityJump()
	{
		if (_currentGravityJumps > 0)
		{
			_currentGravityJumps--;
			UpdateGravityIndicator();
		}
	}
}
