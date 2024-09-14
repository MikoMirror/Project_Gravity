using Godot;
using System;

public partial class DamagePlatform : Node3D
{
	private Area3D _area;

	public override void _Ready()
	{
		_area = GetNode<Area3D>("Area3D");

		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
		}
		else
		{
			GD.PrintErr("DamagePlatform: No Area3D found as a child of this node.");
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		GD.Print($"DamagePlatform: Body entered - {body.Name}");
		if (body is Player player)
		{
			GD.Print("DamagePlatform: Player detected, calling Die()");
			player.Die();
		}
	}
}
