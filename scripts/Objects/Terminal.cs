using Godot;
using System;

public partial class Terminal : Node3D
{
	[Export]
	public NodePath ScreenPath;

	private MeshInstance3D screenMesh;
	private ShaderMaterial screenMaterial;

	public override void _Ready()
	{
		screenMesh = GetNode<MeshInstance3D>(ScreenPath);
		if (screenMesh != null)
		{
			screenMaterial = screenMesh.GetActiveMaterial(0) as ShaderMaterial;
		}
	}

	public void UpdateGridSize(int rows, int columns)
	{
		if (screenMaterial != null)
		{
			screenMaterial.SetShaderParameter("grid_size", new Vector2(columns, rows));
		}
	}
}
