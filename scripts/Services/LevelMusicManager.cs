using Godot;

public partial class LevelMusicManager : Node
{
	[Export] private string musicTrackPath;

	public override void _Ready()
	{
		GetNode<MusicManager>("/root/MusicManager").PlayMusic(musicTrackPath);
	}
}
