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
			if (isActive != value)
			{
				isActive = value;
				_needsUpdate = true;
			}
		}
	}

	private Area3D area;
	private MeshInstance3D neonMesh;
	private ShaderMaterial neonMaterial;

	private bool isActive = false;
	private bool hasBeenActivated = false;
	private bool _isInteractive = true;
	private bool isInRedState = false;
	private bool _needsUpdate = false;
	private bool isNeutral = false;

	public bool HasBeenActivated => hasBeenActivated;

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

	public void SetNeutral(bool neutral)
	{
		isNeutral = neutral;
		UpdateNeonColor();
	}

	private void OnBodyEntered(Node body)
	{
		if (!_isInteractive || !(body is CollisionObject3D)) return;

		var memoryPuzzle = GetParent() as MemoryPuzle;
		if (memoryPuzzle == null) return;

		if (IsActive && !hasBeenActivated && !isInRedState)
		{
			PermanentlyActivate();
			memoryPuzzle.UpdatePlatformState((int)(Position.Z / memoryPuzzle.Spacing.Z), 
											  (int)(Position.X / memoryPuzzle.Spacing.X), 
											  true);
		}
		else if (!IsActive && !isNeutral && !memoryPuzzle.AllActiveSteppedOn)
		{
			memoryPuzzle.ResetPuzzle();
		}

		UpdateNeonColor(true);
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
		if (neonMaterial == null) return;

		Vector3 color;
		if (!_isInteractive || isNeutral)
			color = new Vector3(0.5f, 0.5f, 0.5f); // Gray (deactivated or neutral)
		else if (isInRedState)
			color = new Vector3(1.0f, 0.0f, 0.0f); // Red
		else if (hasBeenActivated)
			color = new Vector3(0.0f, 0.5f, 1.0f); // Light Blue
		else if (objectInside)
			color = IsActive ? new Vector3(0.0f, 1.0f, 0.0f) : new Vector3(1.0f, 0.0f, 0.0f); // Green or Red
		else
			color = new Vector3(1.0f, 1.0f, 1.0f); // White (default)

		neonMaterial.SetShaderParameter("emission_color", color);
	}

	public void UpdateVisuals()
	{
		UpdateNeonColor();
	}

	public override void _Process(double delta)
	{
		if (_needsUpdate)
		{
			UpdateNeonColor();
			_needsUpdate = false;
		}
	}
}
