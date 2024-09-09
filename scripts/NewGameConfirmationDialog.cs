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
		titleLabel = GetNode<Label>("TitleLabel");
		messageLabel = GetNode<Label>("MessageLabel");
		noButton = GetNode<Button>("NoButton");
		yesButton = GetNode<Button>("YesButton");
		noButton.Pressed += OnNoPressed;
		yesButton.Pressed += OnYesPressed;

		// Hide the dialog initially
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
