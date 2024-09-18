using Godot;
using System;

public partial class GlassGate : Node3D
{
	#region Private Fields
	private AnimationPlayer _animationPlayer;
	private SoundManager _soundManager;
	private AudioStreamPlayer3D _audioPlayer;
	#endregion

	#region Exports
	[Export]
	public string OpenAnimationName { get; set; } = "open_glass";

	[Export]
	public string CloseAnimationName { get; set; } = "close_glass";

	[Export]
	public string GlassGateOpenSoundPath { get; set; } = "res://assets/Sounds/openDoor.mp3";

	[Export]
	public float UnitSize { get; set; } = 20f;

	[Export]
	public float MaxDistance { get; set; } = 30f;

	[Export(PropertyHint.Range, "-80,24")]
	public float BaseVolumeDb { get; set; } = 0f;
	#endregion

	#region Lifecycle Methods
	public override void _Ready()
	{
		InitializeComponents();
		SetupAudio();
	}

	public override void _Process(double delta)
	{
		UpdateVolume();
	}
	#endregion

	#region Public Methods
	public void Open()
	{
		if (_animationPlayer != null)
		{
			GD.Print("GlassGate: Playing open animation");
			_animationPlayer.Play(OpenAnimationName);
			PlayGlassGateOpenSound();
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
	#endregion

	#region Private Methods
	private void InitializeComponents()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		if (_animationPlayer == null)
		{
			GD.PrintErr("GlassGate: AnimationPlayer not found!");
		}

		_soundManager = GetNode<SoundManager>("/root/SoundManager");
		_audioPlayer = new AudioStreamPlayer3D();
		AddChild(_audioPlayer);
	}

	private void SetupAudio()
	{
		var stream = GD.Load<AudioStream>(GlassGateOpenSoundPath);
		_audioPlayer.Stream = stream;
		_audioPlayer.UnitSize = UnitSize;
		_audioPlayer.MaxDistance = MaxDistance;
		_audioPlayer.VolumeDb = BaseVolumeDb;
		_audioPlayer.Autoplay = false;
	}

	private void PlayGlassGateOpenSound()
	{
		if (_audioPlayer != null)
		{
			_audioPlayer.Play();
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
