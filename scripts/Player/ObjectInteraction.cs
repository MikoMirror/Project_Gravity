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
		if (HeldObject == null && !PolaroidCamera.InCameraMode)
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
	}

	private void StartLiftingObject(RigidBody3D rigidBody)
	{
		HeldObject = rigidBody;
		IsLifting = true;
		HighlightObject(HeldObject, true);

		// Configure physics properties for lifting
		HeldObject.GravityScale = 0;
		HeldObject.LinearDamp = 5;
		HeldObject.AngularDamp = 5;
		HeldObject.CustomIntegrator = true;
		HeldObject.Freeze = false;
	}

	public void StopLifting()
	{
		if (HeldObject != null)
		{
			HighlightObject(HeldObject, false);

			// Reset physics properties
			HeldObject.GravityScale = 1;
			HeldObject.LinearDamp = 0;
			HeldObject.AngularDamp = 0;
			HeldObject.CustomIntegrator = false;

			// Apply a small impulse when releasing to prevent sticking
			HeldObject.ApplyImpulse(-Camera.GlobalTransform.Basis.Z * 0.5f);

			ResetObjectRotation();
			HeldObject = null;
		}
		IsLifting = false;
	}

	private void UpdateLiftedObjectPosition(double delta)
	{
		if (HeldObject == null) return;

		Vector3 cameraPosition = Camera.GlobalPosition;
		Vector3 cameraForward = -Camera.GlobalTransform.Basis.Z;

		LiftTarget = GetCursorWorldPosition();

		Vector3 directionToTarget = (LiftTarget - cameraPosition).Normalized();
		Vector3 targetPos = cameraPosition + directionToTarget * MaxLiftDistance;
		Vector3 currentPos = HeldObject.GlobalPosition;

		Vector3 desiredVelocity = (targetPos - currentPos) / (float)delta;
		LimitLiftSpeed(ref desiredVelocity);

		HeldObject.LinearVelocity = desiredVelocity;
		ApplyLiftedObjectRotation();
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
	// Calculate the direction from the held object to the player
	Vector3 objectToPlayer = (GlobalPosition - HeldObject.GlobalPosition).Normalized();

	// Create a look-at basis that orients the object towards the player
	Basis lookAtBasis = new Basis(
		objectToPlayer.Cross(Vector3.Up).Normalized(),
		Vector3.Up,
		-objectToPlayer
	);

	// Create rotation quaternions for vertical and horizontal rotations
	Quaternion verticalRotation = Quaternion.FromEuler(new Vector3(Mathf.DegToRad(VerticalRotation), 0, 0));
	Quaternion horizontalRotation = Quaternion.FromEuler(new Vector3(0, Mathf.DegToRad(HorizontalRotation), 0));

	// Combine the rotations
	Quaternion objectRotation = horizontalRotation * verticalRotation;

	// Convert the quaternion to a basis
	Basis objectRotationBasis = new Basis(objectRotation);

	// Combine the look-at orientation with the object's own rotation
	Basis finalRotationBasis = lookAtBasis * objectRotationBasis;

	// Ensure the basis is orthonormalized to prevent deformation
	finalRotationBasis = finalRotationBasis.Orthonormalized();

	// Apply the rotation
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
}
