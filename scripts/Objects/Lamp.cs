using Godot;

public partial class Lamp : Node3D
{
	private AudioStreamPlayer3D _audioPlayer;
	private SoundManager _soundManager;

	public override void _Ready()
	{
		_audioPlayer = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		_soundManager = GetNode<SoundManager>("/root/SoundManager");

		var stream = GD.Load<AudioStream>("res://assets/Sounds/ambient/lamp.mp3");
		_audioPlayer.Stream = stream;

		if (stream is AudioStreamMP3 mp3Stream)
		{
			mp3Stream.Loop = true;
		}

		_audioPlayer.Play();
		_audioPlayer.UnitSize = 10;
		_audioPlayer.MaxDistance = 20;
		UpdateVolume();
	}

	public override void _Process(double delta)
	{
		UpdateVolume();
	}

	private void UpdateVolume()
	{
		_audioPlayer.VolumeDb = Mathf.LinearToDb(_soundManager.SoundVolume);
	}
}
