using Godot;
using System;

[Tool]
public partial class MemoryPuzle : Node3D
{
	[Signal]
	public delegate void PlatformStatesChangedEventHandler();

	[Signal]
	public delegate void SetupCompletedEventHandler();

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
				UpdatePlatformStatesArray();
				CallDeferred(nameof(UpdatePuzzle));
				CallDeferred(nameof(UpdateTerminalPosition));
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
				UpdatePlatformStatesArray();
				CallDeferred(nameof(UpdatePuzzle));
				CallDeferred(nameof(UpdateTerminalPosition));
			}
		}
	}

	[Export]
	public string PlatformScenePath = "res://scenes/memory_platform.tscn";

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
				UpdateVisualRepresentation();
			}
		}
	}

	[Export]
	public Godot.Collections.Array<bool> PlatformStates { get; set; } = new Godot.Collections.Array<bool>();

	private Node3D[,] platformInstances;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			CallDeferred(nameof(UpdatePuzzle));
		}
		else
		{
			CallDeferred(nameof(CreatePlatformGrid));
		}
	}

	private void UpdatePlatformStatesArray()
	{
		int newSize = RowCount * ColumnCount;
		if (PlatformStates.Count != newSize)
		{
			var newStates = new Godot.Collections.Array<bool>();
			for (int i = 0; i < newSize; i++)
			{
				newStates.Add(i < PlatformStates.Count ? PlatformStates[i] : false);
			}
			PlatformStates = newStates;
			EmitSignal(SignalName.PlatformStatesChanged);
		}
	}

	public void UpdatePuzzle()
	{
		if (!IsInsideTree()) return;

		GD.Print($"UpdatePuzzle called. Rows: {RowCount}, Columns: {ColumnCount}");
		
		if (Engine.IsEditorHint())
		{
			UpdateVisualRepresentation();
		}
		else
		{
			CreatePlatformGrid();
		}
		UpdateTerminalGridSize();
		UpdateTerminalPosition();
		EmitSignal(SignalName.SetupCompleted);
	}

	private void UpdateVisualRepresentation()
	{
		if (!Engine.IsEditorHint()) return;

		foreach (Node child in GetChildren())
		{
			if (child is CsgBox3D)
			{
				child.QueueFree();
			}
		}

		// Create visual representation
		for (int row = 0; row < RowCount; row++)
		{
			for (int col = 0; col < ColumnCount; col++)
			{
				var visualPlatform = new CsgBox3D();
				visualPlatform.Name = $"VisualPlatform_{row}_{col}";
				visualPlatform.Size = new Vector3(1, 0.1f, 1);
				visualPlatform.Position = new Vector3(col * Spacing.X, 0, row * Spacing.Z);

				int index = row * ColumnCount + col;
				bool isActive = index < PlatformStates.Count && PlatformStates[index];

				visualPlatform.MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = isActive ? new Color(0, 1, 0, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f),
					Transparency = BaseMaterial3D.TransparencyEnum.Alpha
				};
				AddChild(visualPlatform);
			}
		}
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
		platformInstances = null; // Ensure the array is reset
	}

	private void CreatePlatformGrid()
	{
		if (!IsInsideTree()) return;

		ClearPlatforms();

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
		for (int row = 0; row < RowCount; row++)
		{
			for (int col = 0; col < ColumnCount; col++)
			{
				Node3D instantiatedNode = platformScene.Instantiate<Node3D>();
				if (instantiatedNode == null)
				{
					GD.PushError("Failed to instantiate platform scene as Node3D.");
					continue;
				}

				MemoryPlatform memoryPlatform = instantiatedNode.GetNodeOrNull<MemoryPlatform>(".");
				if (memoryPlatform == null)
				{
					GD.PushError("MemoryPlatform script not found on instantiated node.");
					continue;
				}

				AddChild(instantiatedNode);
				instantiatedNode.Owner = GetTree()?.EditedSceneRoot ?? this;
				instantiatedNode.Name = $"Platform_{row}_{col}";
				instantiatedNode.Position = new Vector3(col * Spacing.X, 0, row * Spacing.Z);

				int index = row * ColumnCount + col;
				memoryPlatform.IsActive = index < PlatformStates.Count && PlatformStates[index];

				GD.Print($"Platform node instantiated at position: {instantiatedNode.Position}, Active: {memoryPlatform.IsActive}");

				platformInstances[row, col] = instantiatedNode;
			}
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
			var screen = terminalNode.GetNodeOrNull<MeshInstance3D>("StaticBody3D/CollisionShape3D2/Screen");
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
			terminalNode.Position = new Vector3((ColumnCount - 1) * Spacing.X / 2, 0, RowCount * Spacing.Z);
			GD.Print($"Terminal node moved to position: {terminalNode.Position}");
		}
	}

	public void UpdatePlatformState(int row, int col, bool isActive)
	{
		GD.Print($"UpdatePlatformState called: row={row}, col={col}, isActive={isActive}");
		
		int index = row * ColumnCount + col;
		if (index < PlatformStates.Count)
		{
			PlatformStates[index] = isActive;
			
			GD.Print($"Platform state updated: index={index}, isActive={isActive}");

			if (Engine.IsEditorHint())
			{
				UpdateVisualRepresentation();
			}
			else if (platformInstances != null && platformInstances[row, col] != null)
			{
				MemoryPlatform memoryPlatform = platformInstances[row, col].GetNodeOrNull<MemoryPlatform>(".");
				if (memoryPlatform != null)
				{
					memoryPlatform.IsActive = isActive;
					GD.Print($"MemoryPlatform IsActive set to: {isActive}");
				}
				else
				{
					GD.PushError($"MemoryPlatform script not found on platform at row={row}, col={col}");
				}
			}
			else
			{
				GD.PushError($"Platform instance not found at row={row}, col={col}");
			}

			// Emit signal to notify about the state change
			EmitSignal(SignalName.PlatformStatesChanged);
		}
		else
		{
			GD.PushError($"Invalid platform index: {index}. RowCount={RowCount}, ColumnCount={ColumnCount}");
		}
	}
}
