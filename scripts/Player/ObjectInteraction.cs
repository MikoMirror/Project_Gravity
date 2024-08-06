using Godot;
using System;

public partial class Player
{
	private void HandleObjectLifting(double delta)
	{
		if (IsLifting && HeldObject != null)
		{
			UpdateLiftedObjectPosition(delta);
		}
	}

	 public void TryStartLifting()
	{
		InteractionRay.ForceRaycastUpdate();
		if (InteractionRay.IsColliding())
		{
			var collider = InteractionRay.GetCollider();
			if (collider is RigidBody3D rigidBody && rigidBody.Mass <= MaxLiftWeight)
			{
				StartLiftingObject(rigidBody);
			}
		}
	}

	 private void StartLiftingObject(RigidBody3D rigidBody)
	{
		HeldObject = rigidBody;
		IsLifting = true;

		// Configure physics properties for lifting
		HeldObject.GravityScale = 0;
		HeldObject.LinearDamp = 5;
		HeldObject.AngularDamp = 5;
		HeldObject.Freeze = false;
	}

	 public void StopLifting()
	{
		if (HeldObject != null)
		{

			// Reset physics properties
			HeldObject.GravityScale = _gravityManager.IsGravityReversed ? -1 : 1;
			HeldObject.LinearDamp = 0;
			HeldObject.AngularDamp = 0;
			HeldObject.Freeze = false;

			// Apply a small impulse when releasing to prevent sticking
			HeldObject.ApplyImpulse(-Camera.GlobalTransform.Basis.Z * 0.5f);

			HeldObject = null;
		}
		IsLifting = false;
	}

	 private void UpdateLiftedObjectPosition(double delta)
	{
		if (HeldObject == null) return;

		Vector3 targetPosition = Camera.GlobalPosition + (-Camera.GlobalTransform.Basis.Z * MaxLiftDistance);
		Vector3 currentPosition = HeldObject.GlobalPosition;

		// Smoothly move the object towards the target position
		Vector3 newPosition = currentPosition.Lerp(targetPosition, LiftSpeed * (float)delta);
		HeldObject.GlobalPosition = newPosition;

		// Make the object face the camera
		HeldObject.LookAt(Camera.GlobalPosition, Vector3.Up);
	}

private void RotateLiftedObject(InputEventMouseButton mouseButton)
{
	if (mouseButton.ButtonIndex == MouseButton.WheelUp)
	{
		VerticalRotation += RotationSpeed;
	}
	else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
	{
		HorizontalRotation += RotationSpeed;
	}
	VerticalRotation = Mathf.Wrap(VerticalRotation, 0, 360);
	HorizontalRotation = Mathf.Wrap(HorizontalRotation, 0, 360);
}

	private void LimitLiftSpeed(ref Vector3 velocity)
	{
		float maxSpeed = 10f;
		if (velocity.Length() > maxSpeed)
		{
			velocity = velocity.Normalized() * maxSpeed;
		}
	}

	private void ApplyLiftedObjectRotation()
	{
		Vector3 objectToPlayer = (GlobalPosition - HeldObject.GlobalPosition).Normalized();

		Basis lookAtBasis = new Basis(
			objectToPlayer.Cross(Vector3.Up).Normalized(),
			Vector3.Up,
			-objectToPlayer
		);

		Quaternion verticalRotation = Quaternion.FromEuler(new Vector3(Mathf.DegToRad(VerticalRotation), 0, 0));
		Quaternion horizontalRotation = Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(HorizontalRotation), 0));

		Quaternion objectRotation = horizontalRotation * verticalRotation;
		Basis objectRotationBasis = new Basis(objectRotation);

		Basis finalRotationBasis = lookAtBasis * objectRotationBasis;
		finalRotationBasis = finalRotationBasis.Orthonormalized();

		HeldObject.AngularVelocity = Vector3.Zero;
		HeldObject.GlobalTransform = new Transform3D(finalRotationBasis, HeldObject.GlobalPosition);
	}

	private void ResetObjectRotation()
	{
		VerticalRotation = 0f;
		HorizontalRotation = 0f;
	}

	private Vector3 GetCursorWorldPosition()
	{
		Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2;
		Vector3 rayOrigin = Camera.ProjectRayOrigin(screenCenter);
		Vector3 rayEnd = rayOrigin + Camera.ProjectRayNormal(screenCenter) * MaxLiftDistance;

		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithBodies = true;
		query.CollisionMask = 1;

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			Vector3 collisionPoint = (Vector3)result["position"];
			float distanceToCollision = rayOrigin.DistanceTo(collisionPoint);

			return GetValidLiftPosition(rayOrigin, collisionPoint, distanceToCollision);
		}
		else
		{
			return rayOrigin + Camera.GlobalTransform.Basis.Z * -MaxLiftDistance;
		}
	}

	private Vector3 GetValidLiftPosition(Vector3 rayOrigin, Vector3 collisionPoint, float distanceToCollision)
	{
		if (distanceToCollision < MinLiftDistance)
		{
			return rayOrigin + Camera.GlobalTransform.Basis.Z * -MinLiftDistance;
		}
		else if (distanceToCollision > MaxLiftDistance)
		{
			return rayOrigin + Camera.GlobalTransform.Basis.Z * -MaxLiftDistance;
		}
		else
		{
			return collisionPoint;
		}
	}

 	public void UpdateHeldObjectGravity(bool isGravityReversed)
	{
		if (IsLifting && HeldObject != null)
		{
			HeldObject.GravityScale = isGravityReversed ? -1 : 1;
		}
	}
}
