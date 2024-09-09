using Godot;
using System;

public partial class Terminal : Node3D
{
	[Export]
	public NodePath ScreenPath = "StaticBody3D/CollisionShape3D2/Screen";

	private MeshInstance3D screen;
	private ShaderMaterial screenMaterial;
	private ImageTexture stateTexture;

	public override void _Ready()
	{
		var memoryPuzle = GetParent() as MemoryPuzle;
		if (memoryPuzle != null)
		{
			memoryPuzle.Connect(MemoryPuzle.SignalName.SetupCompleted, new Callable(this, nameof(OnMemoryPuzleSetupCompleted)));
			memoryPuzle.Connect(MemoryPuzle.SignalName.PlatformStatesChanged, new Callable(this, nameof(UpdatePlatformStates)));
			
			OnMemoryPuzleSetupCompleted();
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
			GD.PushError($"Screen node not found at path: {ScreenPath}");
		else
		{
			// Create a unique instance of the shader material for this terminal
			ShaderMaterial originalMaterial = screen.GetActiveMaterial(0) as ShaderMaterial;
			if (originalMaterial != null)
			{
				screenMaterial = originalMaterial.Duplicate() as ShaderMaterial;
				screen.SetSurfaceOverrideMaterial(0, screenMaterial);
			}
			else
			{
				GD.PushError("Screen material is not a ShaderMaterial");
			}

			UpdatePlatformStates();
		}
	}

	public void UpdateGridSize(int rows, int columns)
	{
		if (screenMaterial != null)
		{
			screenMaterial.SetShaderParameter("grid_size", new Vector2(columns, rows));
		}
	}

	private void UpdatePlatformStates()
	{
		var memoryPuzle = GetParent() as MemoryPuzle;
		if (memoryPuzle == null) return;

		int rows = memoryPuzle.RowCount;
		int columns = memoryPuzle.ColumnCount;

		UpdateGridSize(rows, columns);

		// Create a new image to represent the platform states
		var image = Image.CreateEmpty(columns, rows, false, Image.Format.R8);

		for (int i = 0; i < memoryPuzle.PlatformStates.Count; i++)
		{
			int x = i % columns;
			int y = i / columns;
			image.SetPixel(x, y, memoryPuzle.PlatformStates[i] ? Colors.White : Colors.Black);
		}

		// Create or update the state texture
		if (stateTexture == null)
		{
			stateTexture = ImageTexture.CreateFromImage(image);
		}
		else
		{
			stateTexture.Update(image);
		}

		// Set the state texture in the shader
		screenMaterial.SetShaderParameter("state_texture", stateTexture);
	}
}
