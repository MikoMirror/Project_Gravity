using Godot;
using System;

public partial class Player
{
	private float _horizontalRotation = 0f;
	public Vector3 InitialCameraPosition; 

	private void ApplyCameraShakes(double delta)
	{
		ApplyWalkShake(delta);
		ApplyLandingShake(delta);
	}

	private void ApplyWalkShake(double delta)
	{
		if (IsOnFloor())
		{
			ShakeTime += (float)delta * WalkShakeSpeed;
			float shakeOffset = Mathf.Sin(ShakeTime) * WalkShakeAmount;
			_camera.Position = InitialCameraPosition + new Vector3(0, shakeOffset, 0);
		}
	}

	private void ApplyLandingShake(double delta)
	{
		if (LandingShake > 0)
		{
			LandingShake = Mathf.MoveToward(LandingShake, 0, (float)delta * 4);
			float landingOffset = Mathf.Sin(LandingShake * 20) * LandingShake;
			 _camera.Position = InitialCameraPosition + new Vector3(0, landingOffset, 0);
		}
		else if (IsOnFloor() && Velocity.X == 0 && Velocity.Z == 0)
		{
			_camera.Position = InitialCameraPosition;
		}
	}
	 public void ResetCameraRotation()
	{
		UpdateCameraRotation();
	}
		 
	private void UpdateCameraRotation()
	{
		Quaternion cameraRotation = Quaternion.FromEuler(new Vector3(Mathf.DegToRad(VerticalRotation), 0, 0));
		_camera.Transform = new Transform3D(new Basis(cameraRotation), _camera.Transform.Origin);
	}
	}
