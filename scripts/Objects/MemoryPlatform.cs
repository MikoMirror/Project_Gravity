using Godot;
using System;

public partial class MemoryPlatform : Node3D
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
		GD.Print("MemoryPlatform _Ready called");

		// Initialize paths if they're not set
		if (AreaPath == null || AreaPath.IsEmpty)
		{
			AreaPath = "./Area3D";
		}
		if (NeonPath == null || NeonPath.IsEmpty)
		{
			NeonPath = "./NeonMesh";
		}

		area = GetNode<Area3D>(AreaPath);
		neonMesh = GetNode<MeshInstance3D>(NeonPath);

		if (neonMesh != null)
		{
			// Create a unique instance of the shader material for this platform
			ShaderMaterial originalMaterial = neonMesh.GetActiveMaterial(0) as ShaderMaterial;
			neonMaterial = originalMaterial.Duplicate() as ShaderMaterial;
			neonMesh.SetSurfaceOverrideMaterial(0, neonMaterial);
		}

		if (area != null)
		{
			area.BodyEntered += OnBodyEntered;
			area.BodyExited += OnBodyExited;  // Add this line
		}

		UpdateNeonColor();

		// Debug prints
		GD.Print($"AreaPath: {AreaPath}, Area: {area}");
		GD.Print($"NeonPath: {NeonPath}, NeonMesh: {neonMesh}, NeonMaterial: {neonMaterial}");
	}

	private void OnBodyEntered(Node body)
	{
		if (body is CollisionObject3D)
		{
			UpdateNeonColor(true);
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body is CollisionObject3D)
		{
			UpdateNeonColor(false);
		}
	}

	private void UpdateNeonColor(bool objectInside = false)
	{
		if (neonMaterial != null)
		{
			if (objectInside)
			{
				if (IsActive)
				{
					neonMaterial.SetShaderParameter("emission_color", new Vector3(0.0f, 1.0f, 0.0f)); // Green
				}
				else
				{
					neonMaterial.SetShaderParameter("emission_color", new Vector3(1.0f, 0.0f, 0.0f)); // Red
				}
			}
			else
			{
				neonMaterial.SetShaderParameter("emission_color", new Vector3(1.0f, 1.0f, 1.0f)); // White (default)
			}
		}
	}
}
