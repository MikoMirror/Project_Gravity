using Godot;

public partial class LevelMusicManager : Node
{
	[Export] private string musicTrackPath;
	[Export] public bool LoopMusic { get; set; } = true;

	private MusicManager _musicManager;

	public override void _Ready()
	{
		_musicManager = GetNode<MusicManager>("/root/MusicManager");
		PlayLevelMusic();
	}

	private void PlayLevelMusic()
	{
			_musicManager.LoopMusic = LoopMusic;
			_musicManager.PlayMusic(musicTrackPath);
	}

	public void OnLoopMusicChanged()
	{
		if (_musicManager != null)
		{
			_musicManager.LoopMusic = LoopMusic;
			_musicManager.OnLoopMusicChanged();
		}
	}
}
