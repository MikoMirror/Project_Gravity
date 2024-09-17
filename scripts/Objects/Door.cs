using Godot;

public partial class Door : Node3D
{
	private AnimationPlayer _animationPlayer;
	private bool _isOpen = false;

	[Export]
	public string OpenAnimationName { get; set; } = "open_door";

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		ResetState();
	}

	public void Open()
	{
		if (!_isOpen && _animationPlayer.HasAnimation(OpenAnimationName))
		{
			_isOpen = true;
			_animationPlayer.Play(OpenAnimationName);
		}
	}

	public void Close()
	{
		if (_isOpen && _animationPlayer.HasAnimation(OpenAnimationName))
		{
			_isOpen = false;
			_animationPlayer.PlayBackwards(OpenAnimationName);
		}
	}

	public void ResetState()
	{
		_isOpen = false;
		if (_animationPlayer.HasAnimation(OpenAnimationName))
		{
			_animationPlayer.Stop();
			_animationPlayer.Play(OpenAnimationName);
			_animationPlayer.Seek(0, true);
			_animationPlayer.Stop();
		}
	}
}
