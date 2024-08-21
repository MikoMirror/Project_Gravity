using Godot;

public partial class Door : Node3D
{
	private AnimationPlayer _animationPlayer;

	[Export]
	public string OpenAnimationName { get; set; } = "open_door";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public void Open()
	{
		_animationPlayer.Play(OpenAnimationName);
	}

	public void Close()
	{
		_animationPlayer.PlayBackwards(OpenAnimationName);
	}
}
