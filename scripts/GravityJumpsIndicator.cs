using Godot;
using System;

public partial class GravityJumpsIndicator : Control
{
	private const int MAX_JUMPS = 3;
	public int _remainingJumps = MAX_JUMPS;
	private TextureRect[] _jumpIndicators;
	private Label _restartLabel;
	
	[Export]
	private Color InactiveColor = Colors.Gray;

	public override void _Ready()
	{
		_jumpIndicators = new TextureRect[MAX_JUMPS];
		for (int i = 0; i < MAX_JUMPS; i++)
		{
			_jumpIndicators[i] = GetNode<TextureRect>($"Jump{i + 1}");
		}
		
		_restartLabel = new Label();
		_restartLabel.Text = "Press 'G' to restart the level";
		_restartLabel.Visible = false;
		_restartLabel.AnchorBottom = 1;
		_restartLabel.AnchorTop = 1;
		_restartLabel.AnchorLeft = 0.5f;
		_restartLabel.AnchorRight = 0.5f;
		_restartLabel.GrowHorizontal = GrowDirection.Both;
		_restartLabel.VerticalAlignment = VerticalAlignment.Bottom;
		_restartLabel.HorizontalAlignment = HorizontalAlignment.Center;
		AddChild(_restartLabel);

		UpdateIndicator();
	}

	public bool CanJump() => _remainingJumps > 0;

	public void UseJump()
	{
		if (CanJump())
		{
			_remainingJumps--;
			UpdateIndicator();
		}
	}

	private void UpdateIndicator()
	{
		for (int i = 0; i < MAX_JUMPS; i++)
		{
			_jumpIndicators[i].Modulate = i < _remainingJumps ? _jumpIndicators[i].Modulate : InactiveColor;
		}
		
		_restartLabel.Visible = _remainingJumps == 0;
	}

	public void ResetJumps()
	{
		_remainingJumps = MAX_JUMPS;
		UpdateIndicator();
	}
}
