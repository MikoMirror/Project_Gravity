using Godot;
using System;

public partial class BorderGlass : Node3D
{
	private AnimationPlayer _animationPlayer;

	[Export]
	public string OpenAnimationName { get; set; } = "open_glass";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		if (_animationPlayer == null)
		{
			GD.PrintErr("BorderGlass: AnimationPlayer not found!");
		}
	}

	public void Open()
	{
		if (_animationPlayer != null)
		{
			GD.Print("BorderGlass: Playing open_glass animation");
			_animationPlayer.Play("open_glass");
		}
		else
		{
			GD.PrintErr("BorderGlass: Cannot play animation, AnimationPlayer is null");
		}
	}

	public void Close()
	{
		_animationPlayer?.PlayBackwards(OpenAnimationName);
	}
}
