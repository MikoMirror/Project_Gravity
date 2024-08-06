using Godot;
using System;

public partial class Player
{
	private void HandleMovement(double delta)
{
	Vector3 velocity = Velocity;
	ApplyGravity(ref velocity, delta);

	if (IsOnFloor() || IsOnCeiling())
	{
		HandleGroundedState(ref velocity);
	}
	else
	{
		WasInAir = true;
	}
	Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
	Vector3 direction = Vector3.Zero;
	Vector3 cameraForward = -Camera.GlobalTransform.Basis.Z; 
	Vector3 cameraRight = Camera.GlobalTransform.Basis.X;

	cameraForward.Y = 0;
	cameraRight.Y = 0;
	cameraForward = cameraForward.Normalized();
	cameraRight = cameraRight.Normalized();
	direction = (cameraForward * -inputDir.Y + cameraRight * inputDir.X).Normalized();

	if (direction != Vector3.Zero)
	{
		MovePlayer(ref velocity, direction, delta);
	}
	else if (IsOnFloor() || IsOnCeiling())
	{
		Decelerate(ref velocity);
	}

	Velocity = velocity;
	MoveAndSlide();
}

	private void HandleGroundedState(ref Vector3 velocity)
	{
		if (WasInAir)
		{
			LandingShake = LandingShakeAmount;
			WasInAir = false;
		}
		if (Input.IsActionJustPressed("jump"))
		{
			velocity.Y = _gravityManager.IsGravityReversed ? -JumpVelocity : JumpVelocity;
			WasInAir = true;
		}
	}

	private void ApplyGravity(ref Vector3 velocity, double delta)
	{
		velocity.Y += _gravityManager.GetCurrentGravity() * (float)delta;
	}

	private void MovePlayer(ref Vector3 velocity, Vector3 direction, double delta)
{
	float currentSpeed = IsSprinting ? SprintSpeed : Speed;
	if (IsOnFloor())
	{
		velocity.X = direction.X * currentSpeed;
		velocity.Z = direction.Z * currentSpeed;
	}
	else
	{
		velocity.X += direction.X * currentSpeed * AirControl * (float)delta;
		velocity.Z += direction.Z * currentSpeed * AirControl * (float)delta;
		LimitAirSpeed(ref velocity, currentSpeed);
	}
}

	private void LimitAirSpeed(ref Vector3 velocity, float currentSpeed)
	{
		float airSpeedXZ = new Vector2(velocity.X, velocity.Z).Length();
		if (airSpeedXZ > currentSpeed)
		{
			float scale = currentSpeed / airSpeedXZ;
			velocity.X *= scale;
			velocity.Z *= scale;
		}
	}

	private void Decelerate(ref Vector3 velocity)
	{
		velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
	}
}
