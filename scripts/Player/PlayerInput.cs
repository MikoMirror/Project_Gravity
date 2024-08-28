using Godot;
using System;

public partial class Player
{
	 public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			HandleMouseLook(mouseMotion);
		}
		
		else if (@event is InputEventMouseButton mouseButton)
		{
			HandleMouseButtons(mouseButton);
		}
		else if (@event.IsActionPressed("change_gravity"))
		{
			if (_gravityManager.TryToggleGravity())
			{
				_gravityManager.FlipPlayerBody(this);
			}
		}
		else if (@event.IsActionPressed("sprint"))
		{
			IsSprinting = true;
		}
		else if (@event.IsActionReleased("sprint"))
		{
			IsSprinting = false;
		}
		else if (@event.IsActionPressed("restart_level"))
{
	GD.Print("Player: Restart level action pressed");
	try
	{
		var currentScene = GetTree().CurrentScene;
		GD.Print($"Player: Current scene is {currentScene.Name}");
		var levelManager = currentScene.GetNode<LevelManager>(".");
		if (levelManager != null)
		{
			GD.Print("Player: LevelManager found, calling RestartLevel");
			levelManager.RestartLevel();
		}
		else
		{
			GD.PrintErr("Player: LevelManager not found on the current scene");
		}
	}
	catch (Exception e)
	{
		GD.PrintErr($"Player: Error during restart level - {e.Message}\n{e.StackTrace}");
	}
}
}
	
	
	 private void HandleMouseLook(InputEventMouseMotion mouseMotion)
	{
		float xRotation = mouseMotion.Relative.X * MouseSensitivity;
		float yRotation = mouseMotion.Relative.Y * MouseSensitivity;

		if (_gravityManager.IsGravityReversed)
		{
			xRotation = -xRotation;
		}
		if (IsCameraInverted)
		{
			xRotation = -xRotation;
			yRotation = -yRotation;
		}

		RotateY(Mathf.DegToRad(-xRotation));
		
		VerticalRotation -= yRotation;
		VerticalRotation = Mathf.Clamp(VerticalRotation, -89, 89);
		
		UpdateCameraRotation();
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

	private void HandleKeyboardInput(InputEvent @event)
	{
		if (@event.IsActionPressed("change_gravity"))
		{
			_gravityManager.ToggleGravity();
			_gravityManager.FlipPlayerBody(this);
		}
			
		if (@event.IsActionPressed("sprint"))
		{
			IsSprinting = true;
		}
		else if (@event.IsActionReleased("sprint"))
		{
			IsSprinting = false;
		}
	}


	private void RotatePlayer(InputEventMouseMotion mouseMotion)
{
	float xRotation = mouseMotion.Relative.X * MouseSensitivity;
	if (_gravityManager.IsGravityReversed)
	{
		xRotation = -xRotation;
	}
	if (IsCameraInverted)
	{
		xRotation = -xRotation;
	}
	RotateY(Mathf.DegToRad(-xRotation));
	float yRotation = mouseMotion.Relative.Y * MouseSensitivity;
	if (IsCameraInverted)
	{
		yRotation = -yRotation;
	}

	Vector3 cameraRotation = Camera.Rotation;
	cameraRotation.X -= Mathf.DegToRad(yRotation);
	cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
	Camera.Rotation = cameraRotation;
}

	  private void HandleMouseButtons(InputEventMouseButton mouseButton)
	{
		if (mouseButton.ButtonIndex == MouseButton.Left)
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
	}

}
