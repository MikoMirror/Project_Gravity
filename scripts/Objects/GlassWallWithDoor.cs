using Godot;
using System;

public partial class GlassWallWithDoor : Node3D
{
	private const string DoorOpenAnimation = "door_open";
	private AnimationPlayer animationPlayer;
	private bool isDoorOpen = false;

	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		if (animationPlayer == null)
		{
			GD.PrintErr("AnimationPlayer not found in GlassWallWithDoor");
		}
		else
		{
			GD.Print("AnimationPlayer found successfully");
		}
	}

	public void ToggleDoor()
	{
		GD.Print("ToggleDoor called");
		if (animationPlayer != null)
		{
			GD.Print($"AnimationPlayer found, isDoorOpen: {isDoorOpen}"); 
			if (isDoorOpen)
			{
				animationPlayer.PlayBackwards(DoorOpenAnimation);
				isDoorOpen = false;
			}
			else
			{
				animationPlayer.Play(DoorOpenAnimation);
				isDoorOpen = true;
			}
			GD.Print($"Animation should be playing, isDoorOpen: {isDoorOpen}"); 
		}
		else
		{
			GD.Print("AnimationPlayer is null"); 
		}
	}
}
