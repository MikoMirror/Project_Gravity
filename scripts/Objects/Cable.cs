using Godot;

public partial class Cable : Node3D
{
	private AnimationPlayer _animationPlayer;

	[Export]
	public string ActivateAnimationName { get; set; } = "cable_enable";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public void Activate()
	{
		_animationPlayer.Play(ActivateAnimationName);
	}

	public void Deactivate()
	{
		_animationPlayer.PlayBackwards(ActivateAnimationName);
	}
}
