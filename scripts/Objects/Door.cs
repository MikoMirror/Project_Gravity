using Godot;

public partial class Door : Node3D
{
	private AnimationPlayer _animationPlayer;
	private bool _isActive = false;

	[Export]
	public string OpenAnimationName { get; set; } = "open_door";

	[Export]
	public string ColorTransitionAnimationName { get; set; } = "color_transition";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		// Ensure the door starts in the inactive (red) state
		ResetState();
	}

	public void Open()
	{
		_animationPlayer.Play(OpenAnimationName);
	}

	public void Close()
	{
		_animationPlayer.PlayBackwards(OpenAnimationName);
	}

	public void Activate()
	{
		if (!_isActive)
		{
			_isActive = true;
			_animationPlayer.Play(ColorTransitionAnimationName);
		}
	}

	public void Deactivate()
	{
		if (_isActive)
		{
			_isActive = false;
			_animationPlayer.PlayBackwards(ColorTransitionAnimationName);
		}
	}

	public void ResetState()
	{
		_isActive = false;
		// Reset the color transition animation to the start (red state)
		_animationPlayer.Stop();
		_animationPlayer.Seek(0, true);
		// Ensure the door is closed
		Close();
	}
}
