using Godot;
using System;
using System.Collections.Generic;

public partial class LoadLevels : Control
{
	[Export]
	private Vector2 ButtonSize = new Vector2(300, 200);

	[Export]
	private int ButtonSpacing = 20;

	private ScrollContainer scrollContainer;
	private HBoxContainer hboxContainer;

	private List<LevelData> levels = new List<LevelData>
	{
		new LevelData { Number = 1, Name = "The Flat", Memories = "1/1", ThumbnailPath = "res://path/to/thumbnail_1.png" },
		new LevelData { Number = 2, Name = "The Slums", Memories = "6/7", ThumbnailPath = "res://path/to/thumbnail_2.png" },
		new LevelData { Number = 3, Name = "Rooftops", Memories = "6/5", ThumbnailPath = "res://path/to/thumbnail_3.png" },
		// Add more levels as needed
	};

	public override void _Ready()
	{
		scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
		hboxContainer = scrollContainer.GetNode<HBoxContainer>("HBoxContainer");

		SetupScrollContainer();
		CreateLevelButtons();
	}

	private void SetupScrollContainer()
	{
		scrollContainer.AnchorsPreset = (int)LayoutPreset.FullRect;
		scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;

		hboxContainer.AddThemeConstantOverride("separation", ButtonSpacing);
	}

	private void CreateLevelButtons()
	{
		foreach (var level in levels)
		{
			var vbox = new VBoxContainer();
			var button = new TextureButton();
			var nameLabel = new Label();
			var memoriesLabel = new Label();

			// Setup button
			button.TextureNormal = GD.Load<Texture2D>(level.ThumbnailPath);
			button.CustomMinimumSize = ButtonSize;
			button.IgnoreTextureSize = true;
			button.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
			button.Pressed += () => OnLevelSelected(level.Number);

			// Setup labels
			nameLabel.Text = $"{level.Number}. {level.Name}";
			nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			memoriesLabel.Text = $"{level.Memories} Memories";
			memoriesLabel.HorizontalAlignment = HorizontalAlignment.Center;

			// Add to containers
			vbox.AddChild(button);
			vbox.AddChild(nameLabel);
			vbox.AddChild(memoriesLabel);
			hboxContainer.AddChild(vbox);
		}
	}

	private void OnLevelSelected(int levelNumber)
	{
		GD.Print($"Level {levelNumber} selected");
		// Add your logic to load the selected level
	}
}

public class LevelData
{
	public int Number { get; set; }
	public string Name { get; set; }
	public string Memories { get; set; }
	public string ThumbnailPath { get; set; }
}
