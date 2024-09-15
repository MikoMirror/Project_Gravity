using Godot;
using System;

public partial class NewGameConfirmationDialog : Control
{
	[Signal]
	public delegate void ConfirmedEventHandler();

	[Signal]
	public delegate void CanceledEventHandler();

	private Label titleLabel;
	private Label messageLabel;
	private Button noButton;
	private Button yesButton;

	public override void _Ready()
	{
		// Initialize the fields by getting the nodes from the scene
		titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
		messageLabel = GetNode<Label>("VBoxContainer/MessageLabel");
		noButton = GetNode<Button>("VBoxContainer/HBoxContainer/NoButton");
		yesButton = GetNode<Button>("VBoxContainer/HBoxContainer/YesButton");

		// Check if the buttons were found before connecting signals
		if (noButton != null)
		{
			noButton.Pressed += OnNoPressed;
		}
		else
		{
			GD.PrintErr("NoButton not found in the scene.");
		}

		if (yesButton != null)
		{
			yesButton.Pressed += OnYesPressed;
		}
		else
		{
			GD.PrintErr("YesButton not found in the scene.");
		}

		Hide();
	}

	private void OnNoPressed()
	{
		EmitSignal(SignalName.Canceled);
		Hide();
	}

	private void OnYesPressed()
	{
		EmitSignal(SignalName.Confirmed);
		Hide();
	}

	public void ShowDialog()
	{
		Show();
	}
}
