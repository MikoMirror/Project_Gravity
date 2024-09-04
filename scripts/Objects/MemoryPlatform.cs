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
	private bool _isInteractive = true;
	private bool isInRedState = false;

	public void SetInteractive(bool interactive)
	{
		_isInteractive = interactive;
	}

	public void ResetActivation()
	{
		hasBeenActivated = false;
		UpdateNeonColor();
	}

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

	public void SetRedState()
	{
		isInRedState = true;
		UpdateNeonColor();
	}

	public void ResetColor()
	{
		isInRedState = false;
		UpdateNeonColor();
	}

	private void OnBodyEntered(Node body)
	{
		if (!_isInteractive) return;

		if (body is CollisionObject3D)
		{
			if (IsActive && !hasBeenActivated && !isInRedState)
			{
				PermanentlyActivate();
			}
			else if (!IsActive)
			{
				// Reset the puzzle if stepping on an inactive platform
				var memoryPuzzle = GetParent() as MemoryPuzle;
				memoryPuzzle?.ResetPuzzle();
			}
			UpdateNeonColor(true);
		}
	}

	private void PermanentlyActivate()
	{
		hasBeenActivated = true;
		neonMaterial.SetShaderParameter("emission_color", new Vector3(0.0f, 0.5f, 1.0f)); // Light Blue
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
			if (isInRedState)
			{
				neonMaterial.SetShaderParameter("emission_color", new Vector3(1.0f, 0.0f, 0.0f)); // Red
				return;
			}

			if (hasBeenActivated)
			{
				neonMaterial.SetShaderParameter("emission_color", new Vector3(0.0f, 0.5f, 1.0f)); // Light Blue
				return;
			}

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
