using Godot;

public partial class Settings : Control
{
	[Signal]
	public delegate void BackButtonPressedEventHandler();

	private MusicManager _musicManager;
	private SoundManager _soundManager;
	private HSlider _musicVolumeSlider;
	private HSlider _soundVolumeSlider;
	private Button _backButton;

	public override void _Ready()
	{
		_musicManager = GetNode<MusicManager>("/root/MusicManager");
		_soundManager = GetNode<SoundManager>("/root/SoundManager");
		_musicVolumeSlider = GetNode<HSlider>("VBoxContainer/MusicVolumeSlider");
		_soundVolumeSlider = GetNode<HSlider>("VBoxContainer/SoundVolumeSlider");
		_backButton = GetNode<Button>("VBoxContainer/HBoxContainer2/Back");

		ConfigureSlider(_musicVolumeSlider);
		ConfigureSlider(_soundVolumeSlider);

		_musicVolumeSlider.Value = _musicManager.MusicVolume * 100;
		_soundVolumeSlider.Value = _soundManager.SoundVolume * 100;

		_musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
		_soundVolumeSlider.ValueChanged += OnSoundVolumeChanged;
		_backButton.Pressed += OnBackPressed;

		ProcessMode = ProcessModeEnum.Always;
		SetProcessUnhandledInput(true);
	}

	private void ConfigureSlider(HSlider slider)
	{
		slider.MinValue = 0;
		slider.MaxValue = 100;
		slider.Step = 1; // Adjust this value for desired sensitivity (smaller = more sensitive)
		slider.TickCount = 10;
		slider.TicksOnBorders = true;
	}

	private void OnMusicVolumeChanged(double value)
	{
		float volume = (float)value / 100f;
		_musicManager.SetMusicVolume(volume);
	}

	private void OnSoundVolumeChanged(double value)
	{
		float volume = (float)value / 100f;
		_soundManager.SetSoundVolume(volume);
	}

	private void OnBackPressed()
	{
		_musicManager.SaveMusicVolume();
		_soundManager.SaveSoundVolume();
		EmitSignal(SignalName.BackButtonPressed);
		QueueFree(); // Remove the settings scene
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			OnBackPressed();
		}
	}

	public bool HandleEscapePress()
	{
		OnBackPressed();
		return true;
	}
}
