using Godot;
using System;

public partial class Player
{
	private void HandleMovement(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			ApplyGravity(ref velocity, delta);
		}
		else
		{
			if (WasInAir)
			{
				LandingShake = LandingShakeAmount;
				WasInAir = false;
			}

			if (Input.IsActionJustPressed("jump"))
			{
				Jump(ref velocity);
			}
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			MovePlayer(ref velocity, direction, delta);
		}
		else if (IsOnFloor())
		{
			Decelerate(ref velocity);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void ApplyGravity(ref Vector3 velocity, double delta)
	{
		velocity.Y -= Gravity * (float)delta;
		WasInAir = true;
	}

	private void Jump(ref Vector3 velocity)
	{
		velocity.Y = JumpVelocity;
		WasInAir = true;
	}

	private void MovePlayer(ref Vector3 velocity, Vector3 direction, double delta)
	{
		if (IsOnFloor())
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X += direction.X * Speed * AirControl * (float)delta;
			velocity.Z += direction.Z * Speed * AirControl * (float)delta;

			LimitAirSpeed(ref velocity);
		}
	}

	private void LimitAirSpeed(ref Vector3 velocity)
	{
		float airSpeedXZ = new Vector2(velocity.X, velocity.Z).Length();
		if (airSpeedXZ > Speed)
		{
			float scale = Speed / airSpeedXZ;
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
