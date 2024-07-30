using Godot;
using System;

public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public const float MouseSensitivity = 0.05f;
	public const float WalkShakeAmount = 0.05f;
	public const float WalkShakeSpeed = 15.0f;
	public const float LandingShakeAmount = 0.2f;
	public const float AirControl = 0.3f;
	public const float GravityMultiplier = 1.5f;
	
	private const float MinLiftDistance = 1.5f;
	private const float MaxLiftDistance = 2.5f;
	private const float LiftSpeed = 5.0f;
	private const float MaxLiftWeight = 50.0f;
	private const float ThrowForce = 5.0f;
	private const float PushForce = 2.0f;
	private const float RotationSpeed = 2.0f;
	private float _verticalRotation = 0f;
	private float _horizontalRotation = 0f;

	public float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity") * GravityMultiplier;

	private Node3D _head;
	private Camera3D _camera;
	private RayCast3D _interactionRay;
	private Marker3D _handPosition;
	private float _shakeTime = 0f;
	private Vector3 _initialCameraPosition;
	private float _landingShake = 0f;
	private bool _wasInAir = false;
	private ColorRect _cameraOverlay;
	private bool _inCameraMode = false;
	private RigidBody3D _heldObject = null;
	private bool _isLifting = false;
	private Vector3 _liftTarget;

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_interactionRay = _camera.GetNode<RayCast3D>("InteractionRay");
		_handPosition = _camera.GetNode<Marker3D>("HandPosition");
		_initialCameraPosition = _camera.Position;
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_cameraOverlay = GetNode<ColorRect>("Head/Camera3D/MeshInstance3D/cameraPost");
		_cameraOverlay.Visible = false;

		_interactionRay.Enabled = true;
		_interactionRay.TargetPosition = -_camera.GlobalTransform.Basis.Z * MaxLiftDistance;
		_interactionRay.CollideWithAreas = false;
		_interactionRay.CollideWithBodies = true;
		_interactionRay.CollisionMask = 1;
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
			 if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
		{
			ToggleCameraMode();
		}
		else if (mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed)
			{
				TryStartLifting();
			}
			else
			{
				StopLifting();
			}
		}
		else if (_isLifting && _heldObject != null)
		{
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				_verticalRotation += RotationSpeed;
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				_horizontalRotation += RotationSpeed;
			}
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
				velocity.X += direction.X * Speed * AirControl * (float)delta;
				velocity.Z += direction.Z * Speed * AirControl * (float)delta;

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

		if (_isLifting && _heldObject != null)
		{
			UpdateLiftedObjectPosition(delta);
		}
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

	private void ToggleCameraMode()
	{
		_inCameraMode = !_inCameraMode;
		_cameraOverlay.Visible = _inCameraMode;
	}

	private void TryStartLifting()
{
	if (_heldObject == null)
	{
		_interactionRay.ForceRaycastUpdate();
		if (_interactionRay.IsColliding())
		{
			var collider = _interactionRay.GetCollider();
			if (collider is RigidBody3D rigidBody && rigidBody.Mass <= MaxLiftWeight)
			{
				_heldObject = rigidBody;
				_isLifting = true;
				HighlightObject(_heldObject, true);
				
				// Configure physics properties for lifting
				_heldObject.GravityScale = 0;  // Disable gravity
				_heldObject.LinearDamp = 5;    // Add some damping
				_heldObject.AngularDamp = 5;   // Add angular damping
				_heldObject.CustomIntegrator = true;  // We'll handle the physics
				_heldObject.Freeze = false;    // Ensure the object is not frozen
			}
		}
	}
}

private void OnHeldObjectCollision(Node body)
{
	if (body != this && body is Node3D body3D)  // Ignore collisions with the player and ensure it's a 3D node
	{
		// Adjust the object's velocity to prevent penetration
		Vector3 separationVector = _heldObject.GlobalPosition - body3D.GlobalPosition;
		separationVector = separationVector.Normalized() * 0.1f;  // Small separation
		_heldObject.GlobalPosition += separationVector;
		_heldObject.LinearVelocity = Vector3.Zero;
	}
}

	private void StopLifting()
{
	if (_heldObject != null)
	{
		HighlightObject(_heldObject, false);
		
		// Reset physics properties
		_heldObject.GravityScale = 1;  // Re-enable gravity
		_heldObject.LinearDamp = 0;
		_heldObject.AngularDamp = 0;
		_heldObject.CustomIntegrator = false;  // Let Godot handle physics again
		
		// Apply a small impulse when releasing to prevent sticking
		_heldObject.ApplyImpulse(-_camera.GlobalTransform.Basis.Z * 0.5f);
		
		ResetObjectRotation();
		_heldObject = null;
	}
	_isLifting = false;
}
	private void UpdateLiftedObjectPosition(double delta)
{
	if (_heldObject == null) return;

	Vector3 cameraPosition = _camera.GlobalPosition;
	Vector3 cameraForward = -_camera.GlobalTransform.Basis.Z;

	// Get the cursor world position
	_liftTarget = GetCursorWorldPosition();

	// Project the lift target onto a sphere around the camera
	Vector3 directionToTarget = (_liftTarget - cameraPosition).Normalized();
	Vector3 targetPos = cameraPosition + directionToTarget * MaxLiftDistance;

	Vector3 currentPos = _heldObject.GlobalPosition;
	
	// Calculate the desired velocity
	Vector3 desiredVelocity = (targetPos - currentPos) / (float)delta;
	
	// Limit the maximum speed
	float maxSpeed = 10f;
	if (desiredVelocity.Length() > maxSpeed)
	{
		desiredVelocity = desiredVelocity.Normalized() * maxSpeed;
	}

	// Apply the velocity
	_heldObject.LinearVelocity = desiredVelocity;

	// Apply rotation
	Quaternion verticalRotation = new Quaternion(Vector3.Right, Mathf.DegToRad(_verticalRotation));
	Quaternion horizontalRotation = new Quaternion(Vector3.Up, Mathf.DegToRad(_horizontalRotation));
	Quaternion combinedRotation = verticalRotation * horizontalRotation;

	// Combine the rotation with the look-at rotation
	Vector3 lookAtDir = (GlobalPosition - _heldObject.GlobalPosition).Normalized();
	Quaternion lookAtRotation = new Quaternion(Vector3.Up, Mathf.Atan2(lookAtDir.X, lookAtDir.Z));
	
	// Convert the final Quaternion to a Basis
	Basis finalRotationBasis = new Basis(combinedRotation * lookAtRotation);

	// Apply the rotation
	_heldObject.AngularVelocity = Vector3.Zero;
	_heldObject.GlobalTransform = new Transform3D(finalRotationBasis, _heldObject.GlobalPosition);
}

private void ResetObjectRotation()
{
	_verticalRotation = 0f;
	_horizontalRotation = 0f;
}

	private Vector3 GetCursorWorldPosition()
	{
		Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2;
		Vector3 rayOrigin = _camera.ProjectRayOrigin(screenCenter);
		Vector3 rayEnd = rayOrigin + _camera.ProjectRayNormal(screenCenter) * MaxLiftDistance;

		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithBodies = true;
		query.CollisionMask = 1;

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			Vector3 collisionPoint = (Vector3)result["position"];
			float distanceToCollision = rayOrigin.DistanceTo(collisionPoint);
			
			if (distanceToCollision < MinLiftDistance)
			{
				return rayOrigin + _camera.GlobalTransform.Basis.Z * -MinLiftDistance;
			}
			else if (distanceToCollision > MaxLiftDistance)
			{
				return rayOrigin + _camera.GlobalTransform.Basis.Z * -MaxLiftDistance;
			}
			else
			{
				return collisionPoint;
			}
		}
		else
		{
			return rayOrigin + _camera.GlobalTransform.Basis.Z * -MaxLiftDistance;
		}
	}

	private void HighlightObject(RigidBody3D obj, bool highlight)
	{
		if (obj.GetChildCount() > 0 && obj.GetChild(0) is MeshInstance3D meshInstance)
		{
			StandardMaterial3D material = meshInstance.GetActiveMaterial(0) as StandardMaterial3D;
			if (material == null)
			{
				material = new StandardMaterial3D();
				meshInstance.SetSurfaceOverrideMaterial(0, material);
			}

			material.EmissionEnabled = highlight;
			if (highlight)
			{
				material.Emission = new Color(1, 1, 0); // Yellow
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}
}
