using Godot;
using System;

public partial class PlayerUI : Control
{
	private const int MAX_JUMPS = 3;
	private int _remainingJumps = MAX_JUMPS;
	private ColorRect _gravityIndicator;
	private Label _restartLabel;
	private Label _teleportLabel;
	private bool _canTeleport = false;

	[Signal]
	public delegate void TeleportRequestedEventHandler();

	public override void _Ready()
	{
		_gravityIndicator = GetNode<ColorRect>("ColorRect");
		_restartLabel = GetNode<Label>("RestartLabel");
		_teleportLabel = GetNode<Label>("teleportLabel");
		_teleportLabel.Visible = false;

		UpdateIndicator();
	}

	public override void _Input(InputEvent @event)
	{
		if (_canTeleport && @event.IsActionPressed("ui_interaction"))
		{
			EmitSignal(SignalName.TeleportRequested);
		}
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

	public void ReplenishOneJump()
	{
		if (_remainingJumps < MAX_JUMPS)
		{
			_remainingJumps++;
			UpdateIndicator(true);
		}
	}

	private void UpdateIndicator(bool isReplenishing = false)
	{
		if (_gravityIndicator != null)
		{
			float value = _remainingJumps switch
			{
				1 => 0.3f,
				2 => 0.7f,
				3 => 1f,
				_ => 0f
			};
			_gravityIndicator.Material.Set("shader_parameter/value", value);

			if (isReplenishing)
			{
				// You can add a visual effect for replenishing here if needed
				// For example, a quick flash or color change
			}
		}
		
		_restartLabel.Visible = _remainingJumps == 0;
	}

	public void ResetJumps()
	{
		_remainingJumps = MAX_JUMPS;
		UpdateIndicator();
	}

	public void UpdateUI()
	{
		UpdateIndicator();
	}

	public void ShowTeleportLabel(bool show)
	{
		_canTeleport = show;
		_teleportLabel.Visible = show;
	}
}
