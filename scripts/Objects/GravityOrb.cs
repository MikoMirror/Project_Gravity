using Godot;
using System;

public partial class GravityOrb : Node3D
{
	private Node3D _sphere;
	private Area3D _area;

	public override void _Ready()
	{
		_sphere = GetNode<Node3D>("Sphere");
		if (_sphere == null)
		{
			GD.PushWarning("GravityOrb: Sphere node not found. Visual feedback may not work.");
		}

		_area = GetNode<Area3D>("Area3D");
		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
		}
		else
		{
			GD.PushWarning("GravityOrb: Area3D node not found.");
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			TryActivate(player);
		}
	}

	public bool TryActivate(Player player)
{
	if (_sphere == null || _sphere.Visible)
	{
		var jumpsIndicator = player.GetNode<PlayerUI>("PlayerUI"); // Access from Player
		if (jumpsIndicator != null)
		{
			jumpsIndicator.ReplenishOneJump();
			if (_sphere != null) _sphere.Visible = false;
			player.TriggerFlashEffect();
			return true;
		}
	}
	return false;
}
}
