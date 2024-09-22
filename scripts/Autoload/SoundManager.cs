using Godot;
using System.Collections.Generic;

public partial class SoundManager : Node
{
	private Dictionary<string, AudioStreamPlayer> _soundPlayers = new Dictionary<string, AudioStreamPlayer>();
	public float SoundVolume { get; private set; } = 0.5f; // Default to 50%
	public static SoundManager Instance { get; private set; }

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			GD.PushWarning("More than one SoundManager instance detected!");
		}
		LoadSoundVolume();
	}

	public void PlaySound(string soundPath)
	{
		if (!_soundPlayers.ContainsKey(soundPath))
		{
			var audioStream = ResourceLoader.Load<AudioStream>(soundPath);
			if (audioStream != null)
			{
				var player = new AudioStreamPlayer();
				player.Stream = audioStream;
				player.VolumeDb = Mathf.LinearToDb(SoundVolume);
				AddChild(player);
				_soundPlayers[soundPath] = player;
			}
			else
			{
				GD.PrintErr($"Failed to load audio: {soundPath}");
				return;
			}
		}

		_soundPlayers[soundPath].Play();
	}

	public void SetSoundVolume(float volume)
	{
		SoundVolume = Mathf.Clamp(volume, 0f, 1f);
		foreach (var player in _soundPlayers.Values)
		{
			player.VolumeDb = Mathf.LinearToDb(SoundVolume);
		}
	}

	public void SaveSoundVolume()
	{
		ProjectSettings.SetSetting("audio/sound_volume", SoundVolume);
		ProjectSettings.Save();
	}

	private void LoadSoundVolume()
	{
		SoundVolume = (float)ProjectSettings.GetSetting("audio/sound_volume", 0.5f);
		SetSoundVolume(SoundVolume);
	}
}
