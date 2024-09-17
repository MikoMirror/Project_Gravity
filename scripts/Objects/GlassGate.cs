using Godot;
using System;

public partial class GlassGate : Node3D
{
	private AnimationPlayer _animationPlayer;

	[Export]
	public string OpenAnimationName { get; set; } = "open_glass";

	[Export]
	public string CloseAnimationName { get; set; } = "close_glass";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		if (_animationPlayer == null)
		{
			GD.PrintErr("GlassGate: AnimationPlayer not found!");
		}
	}

	public void Open()
	{
		if (_animationPlayer != null)
		{
			GD.Print("GlassGate: Playing open animation");
			_animationPlayer.Play(OpenAnimationName);
		}
		else
		{
			GD.PrintErr("GlassGate: Cannot play animation, AnimationPlayer is null");
		}
	}

	public void Close()
	{
		if (_animationPlayer != null)
		{
			GD.Print("GlassGate: Playing close animation");
			_animationPlayer.Play(CloseAnimationName);
		}
		else
		{
			GD.PrintErr("GlassGate: Cannot play animation, AnimationPlayer is null");
		}
	}
}
