using Godot;
using System;

public partial class MemmoryPlatform : Node3D
{
	[Export]
	public NodePath AreaPath;

	[Export]
	public NodePath NeonPath;

	[Export]
	public bool IsActive
	{
		get => isActive;
		set
		{
			isActive = value;
			UpdateNeonColor();
		}
	}

	private Area3D area;
	private MeshInstance3D neonMesh;
	private ShaderMaterial neonMaterial;

	private bool isActive = false;
	private bool hasBeenActivated = false;

	public override void _Ready()
	{
		area = GetNode<Area3D>(AreaPath);
		neonMesh = GetNode<MeshInstance3D>(NeonPath);

		if (neonMesh != null)
		{
			neonMaterial = neonMesh.GetActiveMaterial(0) as ShaderMaterial;
		}

		if (area != null)
		{
			area.BodyEntered += OnBodyEntered;
			area.BodyExited += OnBodyExited;
		}

		UpdateNeonColor();
	}

	private void OnBodyEntered(Node body)
	{
		if (body is CollisionObject3D)
		{
			if (!hasBeenActivated)
			{
				hasBeenActivated = true;
				UpdateNeonColor();
			}
		}
	}

	private void OnBodyExited(Node body)
	{
		// You can add logic here if needed when a body exits the area
	}

	private void UpdateNeonColor()
	{
		if (neonMaterial != null)
		{
			if (!hasBeenActivated)
			{
				neonMaterial.SetShaderParameter("emission_color", new Vector3(0.5f, 0.5f, 0.5f)); // Grey
			}
			else if (IsActive)
			{
				neonMaterial.SetShaderParameter("emission_color", new Vector3(0.0f, 1.0f, 0.0f)); // Green
			}
			else
			{
				neonMaterial.SetShaderParameter("emission_color", new Vector3(1.0f, 0.0f, 0.0f)); // Red
			}
		}
	}
}
