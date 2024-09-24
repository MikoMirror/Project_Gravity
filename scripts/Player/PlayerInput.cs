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
		else if (@event.IsActionPressed(InteractionAction))
		{
			TryInteractWithDoor();
		}
	}

	private void TogglePauseMenu()
	{
		var currentScene = GetTree().CurrentScene;
		if (currentScene.Name.ToString().StartsWith("Level"))
		{
			var levelManager = currentScene.GetNode<LevelManager>(".");
			if (levelManager != null)
			{
				levelManager.TogglePauseMenu();
			}
			else
			{
				GD.PrintErr("Player: LevelManager not found on the current scene");
			}
		}
		else
		{
			GD.Print("Player: Pause menu not available in this scene");
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

		Vector3 cameraRotation = _camera.Rotation;
		cameraRotation.X -= Mathf.DegToRad(yRotation);
		cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
		_camera.Rotation = cameraRotation;
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
		else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
		{
			TryChangeObjectGravity();
		}
	}

	private const string InteractionAction = "ui_interaction";
	private const float InteractionDistance = 2.0f; 

	private void TryInteractWithDoor()
	{
		var camera = GetNode<Camera3D>("Head/Camera3D");
		var spaceState = GetWorld3D().DirectSpaceState;
		var from = camera.GlobalPosition;
		var to = from + -camera.GlobalTransform.Basis.Z * InteractionDistance;
		
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		
		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			var collider = result["collider"].As<Node>();
			GD.Print($"Ray hit: {collider.Name}"); 

			// Check if the collider is a StaticBody3D
			if (collider is StaticBody3D staticBody)
			{
				// Find the GlassWallWithDoor script
				var glassWallWithDoor = staticBody.GetParent()?.GetParent() as GlassWallWithDoor;
				if (glassWallWithDoor != null)
				{
					GD.Print("Interacting with door"); 
					glassWallWithDoor.ToggleDoor();
				}
				else
				{
					GD.Print("GlassWallWithDoor script not found"); // Debug print
				}
			}
			else
			{
				GD.Print("Hit object is not a StaticBody3D"); // Debug print
			}
		}
		else
		{
			GD.Print("Ray did not hit anything"); // Debug print
		}
	}

	private void ToggleDoorAnimation(AnimationPlayer animationPlayer)
	{
		if (animationPlayer.IsPlaying())
		{
			animationPlayer.PlayBackwards();
		}
		else
		{
			animationPlayer.Play("door_open");
		}
	}

	  private void TryChangeObjectGravity()
	{
		if (_gravityManager.CanUseGravityJump())
		{
			InteractionRay.ForceRaycastUpdate();
			if (InteractionRay.IsColliding())
			{
				var collider = InteractionRay.GetCollider();
				if (collider is RigidBody3D rigidBody)
				{
					ChangeObjectGravity(rigidBody);
					_gravityManager.UseGravityJump();
				}
			}
		}
	}

	

}
