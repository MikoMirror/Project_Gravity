using Godot;
using System;

public partial class Character : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public const float MouseSensitivity = 0.05f;
	public const float WalkShakeAmount = 0.05f;
	public const float WalkShakeSpeed = 15.0f;
	public const float LandingShakeAmount = 0.2f;
	public const float AirControl = 0.3f; 
	public const float GravityMultiplier = 1.5f;

	public float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * GravityMultiplier; 

	private Node3D _head;
	private Camera3D _camera;
	private float _shakeTime = 0f;
	private Vector3 _initialCameraPosition;
	private float _landingShake = 0f;
	private bool _wasInAir = false;
	private ColorRect _cameraOverlay;
	private bool _overlayVisible = false;

	 public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_initialCameraPosition = _camera.Position;
		Input.MouseMode = Input.MouseModeEnum.Captured;

		// Get the reference to the existing ColorRect
		_cameraOverlay = GetNode<ColorRect>("Head/Camera3D/MeshInstance3D/cameraPost");
		_cameraOverlay.Visible = false; // Initially hide the overlay
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
			_head.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
			var headRotation = _head.Rotation;
			headRotation.X = Mathf.Clamp(headRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
			_head.Rotation = headRotation;
		}
		else if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				ToggleCameraOverlay();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y -= gravity * (float)delta;
			_wasInAir = true;
		}
		else if (_wasInAir)
		{
			_landingShake = LandingShakeAmount;
			_wasInAir = false;
		}

		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			_wasInAir = true;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			if (IsOnFloor())
			{
				velocity.X = direction.X * Speed;
				velocity.Z = direction.Z * Speed;
				ApplyWalkShake(delta);
			}
			else
			{
				// Apply air control
				velocity.X += direction.X * Speed * AirControl * (float)delta;
				velocity.Z += direction.Z * Speed * AirControl * (float)delta;
				
				// Clamp air speed to prevent excessive acceleration
				float airSpeedXZ = new Vector2(velocity.X, velocity.Z).Length();
				if (airSpeedXZ > Speed)
				{
					float scale = Speed / airSpeedXZ;
					velocity.X *= scale;
					velocity.Z *= scale;
				}
			}
		}
		else if (IsOnFloor())
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
		ApplyLandingShake(delta);
	}

	private void ApplyWalkShake(double delta)
	{
		_shakeTime += (float)delta * WalkShakeSpeed;
		float shakeOffset = Mathf.Sin(_shakeTime) * WalkShakeAmount;
		_camera.Position = _initialCameraPosition + new Vector3(0, shakeOffset, 0);
	}

	private void ApplyLandingShake(double delta)
	{
		if (_landingShake > 0)
		{
			_landingShake = Mathf.MoveToward(_landingShake, 0, (float)delta * 4);
			float landingOffset = Mathf.Sin(_landingShake * 20) * _landingShake;
			_camera.Position = _initialCameraPosition + new Vector3(0, landingOffset, 0);
		}
		else if (IsOnFloor() && Velocity.X == 0 && Velocity.Z == 0)
		{
			_camera.Position = _initialCameraPosition;
		}
	}

	private void ToggleCameraOverlay()
	{
		_overlayVisible = !_overlayVisible;
		_cameraOverlay.Visible = _overlayVisible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}
}
