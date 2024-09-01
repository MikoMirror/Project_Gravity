using Godot;
using System;

public partial class Terminal : Node3D
{
	[Export]
	public NodePath ScreenPath = "StaticBody3D/CollisionShape3D2/Screen";

	private MeshInstance3D screen;
	private ShaderMaterial screenMaterial;

	public override void _Ready()
	{
		var memoryPuzle = GetParent() as MemoryPuzle;
		if (memoryPuzle != null)
		{
			// Connect to the SetupCompleted signal
			memoryPuzle.Connect(MemoryPuzle.SignalName.SetupCompleted, new Callable(this, nameof(OnMemoryPuzleSetupCompleted)));
		}
		else
		{
			GD.PushError("Terminal's parent is not a MemoryPuzle");
		}
	}

	private void OnMemoryPuzleSetupCompleted()
	{
		screen = GetNodeOrNull<MeshInstance3D>(ScreenPath);
		
		if (screen == null)
		{
			GD.PushError($"Screen node not found at path: {ScreenPath}");
			return;
		}

		// Initialize screenMaterial
		screenMaterial = screen.GetActiveMaterial(0) as ShaderMaterial;
		if (screenMaterial == null)
		{
			GD.PushError("Screen material is not a ShaderMaterial");
			return;
		}

		// Rest of your initialization code
		// ...
	}

	public void UpdateGridSize(int rows, int columns)
	{
		if (screenMaterial != null)
		{
			screenMaterial.SetShaderParameter("grid_size", new Vector2(columns, rows));
		}
	}
}
