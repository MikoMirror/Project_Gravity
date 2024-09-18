using Godot;
using System;

public partial class GravityOrb : Node3D
{
	#region Private Fields
	private Node3D _sphere;
	private Area3D _area;
	#endregion

	#region Lifecycle Methods
	public override void _Ready()
	{
		InitializeComponents();
	}
	#endregion

	#region Initialization Methods
	private void InitializeComponents()
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
	#endregion

	#region Event Handlers
	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			TryActivate(player);
		}
	}
	#endregion

	#region Public Methods
	public bool TryActivate(Player player)
	{
		GD.Print("TryActivate called"); 
		if (_sphere == null || _sphere.Visible)
		{
			var gravityManager = player.GetNode<GravityManager>("GravityManager");
			if (gravityManager != null)
			{
				GD.Print("Replenishing gravity jump"); 
				gravityManager.ReplenishGravityJump();
				if (_sphere != null) _sphere.Visible = false;
				player.TriggerFlashEffect();
				return true;
			}
			else
			{
				GD.PrintErr("GravityManager not found on Player");
			}
		}
		return false;
	}

	public void Reset()
	{
		if (_sphere != null)
		{
			_sphere.Visible = true;
		}
	}
	#endregion
}
