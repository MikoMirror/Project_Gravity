using Godot;

public partial class MusicManager : Node
{
	private AudioStreamPlayer _musicPlayer;

	public override void _Ready()
	{
		_musicPlayer = new AudioStreamPlayer();
		AddChild(_musicPlayer);
	}

	public void PlayMusic(string trackPath)
	{
		if (_musicPlayer.Stream?.ResourcePath == trackPath) return;
		
		_musicPlayer.Stream = GD.Load<AudioStream>(trackPath);
		_musicPlayer.Play();
	}
}
