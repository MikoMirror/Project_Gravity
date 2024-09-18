using Godot;
using System;

public partial class Player
{
	 private void HandleObjectLifting(double delta)
	{
		if (IsLifting && HeldObject != null)
		{
			Vector3 targetPosition = GetHeldObjectTargetPosition();
			Vector3 toTarget = targetPosition - HeldObject.GlobalPosition;
			float forceMagnitude = 1000f;
			Vector3 force = toTarget * forceMagnitude;
			HeldObject.ApplyCentralForce(force);
			if (HeldObject.LinearVelocity.LengthSquared() > 100)
			{
				HeldObject.LinearVelocity = HeldObject.LinearVelocity.Normalized() * 10;
			}
			HeldObject.LookAt(Camera.GlobalPosition, Vector3.Up);
		}
	}

	  private Vector3 GetHeldObjectTargetPosition()
	{
		Vector3 cameraForward = -Camera.GlobalTransform.Basis.Z.Normalized();
		return Camera.GlobalPosition + cameraForward * MaxLiftDistance;
	}

	 public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (!IsLifting)
				{
					TryStartLifting();
				}
				else
				{
					StopLifting();
				}
			}
		}
	}

	 public void TryStartLifting()
	{
		if (IsLifting)
		{
			StopLifting();
			return;
		}
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
		HeldObject.GravityScale = 0;
		HeldObject.LinearDamp = 5;
		HeldObject.AngularDamp = 10;
		HeldObject.Freeze = false;
		UpdateHeldObjectPosition();
	}

	 private void UpdateHeldObjectPosition()
	{
		if (HeldObject == null || !IsLifting) return;
		Vector3 cameraForward = -Camera.GlobalTransform.Basis.Z.Normalized();
		Vector3 desiredPosition = Camera.GlobalPosition + cameraForward * MaxLiftDistance;
		var spaceState = GetWorld3D().DirectSpaceState;
		var query = new PhysicsRayQueryParameters3D();
		query.From = Camera.GlobalPosition;
		query.To = desiredPosition;
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		query.CollisionMask = 1;
		var result = spaceState.IntersectRay(query);
		Vector3 newPosition;
		if (result.Count > 0)
		{
			newPosition = (Vector3)result["position"];
			newPosition += (Camera.GlobalPosition - newPosition).Normalized() * 0.1f;
		}
		else
		{
			newPosition = desiredPosition;
		}
		HeldObject.GlobalPosition = newPosition;
		HeldObject.LookAt(Camera.GlobalPosition, Vector3.Up);
	}

	 public void StopLifting()
	{
		if (HeldObject != null)
		{
			HeldObject.GravityScale = _gravityManager.IsGravityReversed ? -1 : 1;
			HeldObject.LinearDamp = 0;
			HeldObject.AngularDamp = 0.1f;
			HeldObject.Freeze = false;
			HeldObject.LinearVelocity = Vector3.Zero;
			HeldObject.AngularVelocity = Vector3.Zero;
			HeldObject.ApplyCentralImpulse(-Camera.GlobalTransform.Basis.Z * 2f);
			HeldObject = null;
		}
		IsLifting = false;
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
	if (HeldObject == null) return;
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

	public void DropLiftedObjectIfHolding()
	{
		if (IsLifting && HeldObject != null)
		{
			StopLifting();
		}
	}

	 private void ChangeObjectGravity(RigidBody3D rigidBody)
	{
		rigidBody.GravityScale = -rigidBody.GravityScale;
		// Optional: Add visual or sound effect to indicate gravity change
	}
}
