using Godot;
using System;

[Tool]
public partial class MemmoryPuzle : Node3D
{
	[Export]
	public string TerminalScenePath = "res://scenes/terminal.tscn";

	private int _rowCount = 1;
	private int _columnCount = 1;
	private Vector3 _spacing = new Vector3(2, 0, 2);

	[Export(PropertyHint.Range, "1,20,1")]
	public int RowCount 
	{ 
		get => _rowCount; 
		set 
		{
			if (_rowCount != value)
			{
				_rowCount = value;
				UpdatePuzzle();
			}
		}
	}

	[Export(PropertyHint.Range, "1,20,1")]
	public int ColumnCount 
	{ 
		get => _columnCount; 
		set 
		{
			if (_columnCount != value)
			{
				_columnCount = value;
				UpdatePuzzle();
			}
		}
	}

	[Export]
	public string PlatformScenePath = "res://scenes/memmory_platform.tscn"; // Set the default path

	[Export]
	public Vector3 Spacing 
	{ 
		get => _spacing; 
		set 
		{
			if (_spacing != value)
			{
				_spacing = value;
				UpdatePlatformPositions();
				UpdateTerminalPosition();
			}
		}
	}

	[Export]
	public Godot.Collections.Array<bool> PlatformStates { get; set; } = new Godot.Collections.Array<bool>();

	private Node3D[,] platformInstances;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			UpdatePuzzle();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationReady)
		{
			UpdatePuzzle();
		}
	}

	public void UpdatePuzzle()
	{
		if (string.IsNullOrEmpty(PlatformScenePath))
		{
			GD.PushError("PlatformScenePath is not set.");
			return;
		}

		GD.Print($"UpdatePuzzle called. PlatformScenePath: {PlatformScenePath}");
		ClearPlatforms();
		CreatePlatformGrid();
		UpdateTerminalGridSize(); // Ensure this is called
	}

	private void ClearPlatforms()
	{
		if (platformInstances != null)
		{
			foreach (var platform in platformInstances)
			{
				if (platform != null)
				{
					platform.QueueFree();
				}
			}
		}
	}

	private void CreatePlatformGrid()
	{
		if (string.IsNullOrEmpty(PlatformScenePath))
		{
			GD.PushError("PlatformScenePath is not set.");
			return;
		}

		GD.Print($"Loading platform scene from: {PlatformScenePath}");
		PackedScene platformScene = GD.Load<PackedScene>(PlatformScenePath);
		if (platformScene == null)
		{
			GD.PushError($"Failed to load platform scene from '{PlatformScenePath}'");
			return;
		}

		platformInstances = new Node3D[RowCount, ColumnCount];
		int platformCount = 0;
		for (int row = 0; row < RowCount; row++)
		{
			for (int col = 0; col < ColumnCount; col++)
			{
				Node3D platformNode = platformScene.Instantiate<Node3D>();
				if (platformNode == null)
				{
					GD.PushError("Failed to instantiate platform node.");
					continue;
				}

				AddChild(platformNode);
				platformNode.Owner = GetTree().EditedSceneRoot;
				platformNode.Name = platformCount.ToString();
				platformNode.Position = new Vector3(col * Spacing.X, 0, row * Spacing.Z);

				GD.Print($"Platform node instantiated at position: {platformNode.Position}");

				platformInstances[row, col] = platformNode;
				platformCount++;
			}
		}

		// Add or move the Terminal scene at the very bottom of the right-hand platform
		Node3D terminalNode = FindTerminalNode();
		if (terminalNode == null)
		{
			if (!string.IsNullOrEmpty(TerminalScenePath))
			{
				GD.Print($"Loading terminal scene from: {TerminalScenePath}");
				PackedScene terminalScene = GD.Load<PackedScene>(TerminalScenePath);
				if (terminalScene == null)
				{
					GD.PushError($"Failed to load terminal scene from '{TerminalScenePath}'");
					return;
				}

				terminalNode = terminalScene.Instantiate<Node3D>();
				if (terminalNode == null)
				{
					GD.PushError("Failed to instantiate terminal node.");
					return;
				}

				AddChild(terminalNode);
				terminalNode.Owner = GetTree().EditedSceneRoot;
				terminalNode.Name = "Terminal";
			}
		}

		if (terminalNode != null)
		{
			terminalNode.Position = new Vector3((ColumnCount - 1) * Spacing.X, 0, RowCount * Spacing.Z);
			GD.Print($"Terminal node moved to position: {terminalNode.Position}");
			UpdateTerminalGridSize(); // Update the grid size after positioning the terminal
		}
	}

	private Node3D FindTerminalNode()
	{
		foreach (Node child in GetChildren())
		{
			if (child is Node3D node && node.Name == "Terminal")
				return node;
		}
		return null;
	}

	private void UpdateTerminalGridSize()
	{
		Node3D terminalNode = FindTerminalNode();
		if (terminalNode != null)
		{
			var screen = terminalNode.GetNode<MeshInstance3D>("StaticBody3D/CollisionShape3D2/Screen");
			if (screen != null)
			{
				var material = screen.GetActiveMaterial(0) as ShaderMaterial;
				if (material != null)
				{
					GD.Print($"Updating grid size to: {ColumnCount}x{RowCount}");
					material.SetShaderParameter("grid_size", new Vector2(ColumnCount, RowCount));
				}
			}
		}
	}

	private void UpdatePlatformPositions()
	{
		if (platformInstances != null)
		{
			for (int row = 0; row < RowCount; row++)
			{
				for (int col = 0; col < ColumnCount; col++)
				{
					if (platformInstances[row, col] != null)
					{
						platformInstances[row, col].Position = new Vector3(col * Spacing.X, 0, row * Spacing.Z);
					}
				}
			}
		}
	}

	private void UpdateTerminalPosition()
	{
		Node3D terminalNode = FindTerminalNode();
		if (terminalNode != null)
		{
			terminalNode.Position = new Vector3((ColumnCount - 1) * Spacing.X, 0, RowCount * Spacing.Z);
			GD.Print($"Terminal node moved to position: {terminalNode.Position}");
		}
	}
}
