using Godot;

public partial class Cable : Node3D
{
	private AnimationPlayer _animationPlayer;
	private bool _isActivated = false;

	[Export]
	public string ActivateAnimationName { get; set; } = "cable_enable";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		ResetState();
	}

	public void Activate()
	{
		if (!_isActivated)
		{
			_isActivated = true;
			_animationPlayer.Play(ActivateAnimationName);
		}
	}

	public void Deactivate()
	{
		if (_isActivated)
		{
			_isActivated = false;
			_animationPlayer.PlayBackwards(ActivateAnimationName);
		}
	}

	public void ResetState()
	{
		_isActivated = false;
		_animationPlayer.Stop();
		_animationPlayer.Play(ActivateAnimationName);
		_animationPlayer.Seek(0, true);
		_animationPlayer.Stop();
	}
}
