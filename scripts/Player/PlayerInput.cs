using Godot;
using System;

public partial class Player
{
	public override void _Input(InputEvent @event)
	{
		HandleMouseInput(@event);
	}

	private void HandleMouseInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotatePlayer(mouseMotion);
		}
		else if (@event is InputEventMouseButton mouseButton)
		{
			HandleMouseButtons(mouseButton);
		}
	}

	private void RotatePlayer(InputEventMouseMotion mouseMotion)
	{
		RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
		Head.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));

		Vector3 headRotation = Head.Rotation;
		headRotation.X = Mathf.Clamp(headRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
		Head.Rotation = headRotation;
	}

   private void HandleMouseButtons(InputEventMouseButton mouseButton)
	{
		if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
		{
			PolaroidCamera.ToggleCameraMode(this);
		}
		else if (mouseButton.ButtonIndex == MouseButton.Left && !PolaroidCamera.InCameraMode)
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
		else if (IsLifting && HeldObject != null && !PolaroidCamera.InCameraMode)
		{
			RotateLiftedObject(mouseButton);
		}
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

	// Wrap both rotations to keep them between 0 and 360 degrees
	VerticalRotation = Mathf.Wrap(VerticalRotation, 0, 360);
	HorizontalRotation = Mathf.Wrap(HorizontalRotation, 0, 360);
}
}
