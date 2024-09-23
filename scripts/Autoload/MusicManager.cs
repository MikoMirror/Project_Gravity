using Godot;

public partial class MusicManager : Node
{
	private AudioStreamPlayer _musicPlayer;
	public float MusicVolume { get; private set; } = 0.5f; // Default to 50%
	public bool LoopMusic { get; set; } = true; // Add this line

	public override void _Ready()
	{
		_musicPlayer = new AudioStreamPlayer();
		AddChild(_musicPlayer);
		LoadMusicVolume();
	}

	public void PlayMusic(string trackPath)
	{
		if (_musicPlayer.Stream?.ResourcePath == trackPath) 
		{
			SetLooping(_musicPlayer.Stream);
			return;
		}
		
		var audioStream = ResourceLoader.Load<AudioStream>(trackPath);
		if (audioStream != null)
		{
			_musicPlayer.Stream = audioStream;
			SetLooping(audioStream);
			_musicPlayer.Play();
		}
		else
		{
			GD.PrintErr($"Failed to load audio: {trackPath}");
		}
	}

	private void SetLooping(AudioStream audioStream)
	{
		if (audioStream is AudioStreamOggVorbis oggStream)
		{
			oggStream.Loop = LoopMusic;
		}
		else if (audioStream is AudioStreamMP3 mp3Stream)
		{
			mp3Stream.Loop = LoopMusic;
		}
		// Add more conditions for other audio stream types if needed
	}

	public void OnLoopMusicChanged()
	{
		if (_musicPlayer.Stream != null)
		{
			SetLooping(_musicPlayer.Stream);
		}
	}

	public void SetMusicVolume(float volume)
	{
		MusicVolume = Mathf.Clamp(volume, 0f, 1f);
		_musicPlayer.VolumeDb = Mathf.LinearToDb(MusicVolume);
	}

	public void SaveMusicVolume()
	{
		ProjectSettings.SetSetting("audio/music_volume", MusicVolume);
		ProjectSettings.Save();
	}

	private void LoadMusicVolume()
	{
		MusicVolume = (float)ProjectSettings.GetSetting("audio/music_volume", 0.5f);
		SetMusicVolume(MusicVolume);
	}
}
