using Godot;

public partial class Door : Node3D
{
	#region Private Fields
	private AnimationPlayer _animationPlayer;
	private bool _isOpen = false;
	private SoundManager _soundManager;
	private AudioStreamPlayer3D _audioPlayer;
	#endregion

	#region Exports
	[Export]
	public string OpenAnimationName { get; set; } = "open_door";

	[Export]
	public string DoorOpenSoundPath { get; set; } = "res://assets/Sounds/openDoor.mp3";

	[Export]
	public float UnitSize { get; set; } = 20f;

	[Export]
	public float MaxDistance { get; set; } = 30f;

	[Export(PropertyHint.Range, "-80,24")]
	public float BaseVolumeDb { get; set; } = 0f;  // New export for base volume
	#endregion

	#region Lifecycle Methods
	public override void _Ready()
	{
		InitializeComponents();
		SetupAudio();
		ResetState();
	}

	public override void _Process(double delta)
	{
		UpdateVolume();
	}
	#endregion

	#region Public Methods
	public void Open()
{
	if (!_isOpen && _animationPlayer != null && _animationPlayer.HasAnimation(OpenAnimationName))
	{
		_isOpen = true;
		_animationPlayer.Play(OpenAnimationName);
		PlayDoorOpenSound();
	}
	else if (_animationPlayer == null)
	{
		GD.PushWarning("Unable to open Door: AnimationPlayer not found.");
	}
}

public void Close()
{
	if (_isOpen && _animationPlayer != null && _animationPlayer.HasAnimation(OpenAnimationName))
	{
		_isOpen = false;
		_animationPlayer.PlayBackwards(OpenAnimationName);
	}
	else if (_animationPlayer == null)
	{
		GD.PushWarning("Unable to close Door: AnimationPlayer not found.");
	}
}

	public void ResetState()
{
	_isOpen = false;
	if (_animationPlayer != null && _animationPlayer.HasAnimation(OpenAnimationName))
	{
		_animationPlayer.Stop();
		_animationPlayer.Play(OpenAnimationName);
		_animationPlayer.Seek(0, true);
		_animationPlayer.Stop();
	}
	else
	{
		GD.PushWarning("Unable to reset Door state: AnimationPlayer or animation not found.");
	}
}
	#endregion

	#region Private Methods
	private void InitializeComponents()
{
	_animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
	if (_animationPlayer == null)
	{
		GD.PushError("AnimationPlayer not found in Door node.");
	}
	
	_soundManager = GetNode<SoundManager>("/root/SoundManager");
	_audioPlayer = new AudioStreamPlayer3D();
	AddChild(_audioPlayer);
}

	private void SetupAudio()
	{
		var stream = GD.Load<AudioStream>(DoorOpenSoundPath);
		_audioPlayer.Stream = stream;
		_audioPlayer.UnitSize = UnitSize;
		_audioPlayer.MaxDistance = MaxDistance;
		_audioPlayer.VolumeDb = BaseVolumeDb;  
		_audioPlayer.Autoplay = false;
	}

	private void PlayDoorOpenSound()
	{
		if (_audioPlayer != null)
		{
			GetTree().CreateTimer(0.5).Timeout += () => _audioPlayer.Play();
		}
		else
		{
			GD.PrintErr("AudioStreamPlayer3D not initialized.");
		}
	}

	private void UpdateVolume()
	{
		if (_audioPlayer != null && _soundManager != null)
		{
			float globalVolume = Mathf.LinearToDb(_soundManager.SoundVolume);
			_audioPlayer.VolumeDb = BaseVolumeDb + globalVolume;
		}
	}
	#endregion
}
