using Godot;
using System;

[Tool]
public partial class MemoryPuzzle : Node3D
{
	#region Signals
	[Signal]
	public delegate void PlatformStatesChangedEventHandler();

	[Signal]
	public delegate void SetupCompletedEventHandler();
	#endregion

	#region Exports
	[Export]
	public string TerminalScenePath = "res://assets/OWN/Prefabs/terminal.tscn";
	
	[Export]
	public string PlatformScenePath = "res://assets/OWN/Prefabs/memory_platform.tscn";

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
				UpdateVisualRepresentation();
				CallDeferred(nameof(UpdatePuzzle));
				CallDeferred(nameof(UpdatePuzzleArea));
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
				UpdateVisualRepresentation();
				CallDeferred(nameof(UpdatePuzzle));
				CallDeferred(nameof(UpdatePuzzleArea));
			}
		}
	}

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
				CallDeferred(nameof(UpdatePuzzleArea));
			}
		}
	}

	[Export]
	public Godot.Collections.Array<bool> PlatformStates { get; set; } = new Godot.Collections.Array<bool>();

	[Export]
	public Area3D PuzzleArea
	{
		get
		{
			if (!ShowPlatforms) return null;

			if (_puzzleArea == null)
			{
				_puzzleArea = GetNodeOrNull<Area3D>("PuzzleArea");
				if (_puzzleArea == null)
				{
					_puzzleArea = new Area3D();
					_puzzleArea.Name = "PuzzleArea";
					AddChild(_puzzleArea);
					_puzzleArea.Owner = GetTree().EditedSceneRoot ?? this;
				}
			}
			return _puzzleArea;
		}
		set => _puzzleArea = value;
	}

	[Export]
	public Node3D[] GlassGates { get; set; } = new Node3D[0];

	private bool _showTerminal = true;
	private bool _showPlatforms = true;

	[Export]
	public bool ShowTerminal
	{
		get => _showTerminal;
		set
		{
			if (_showTerminal != value)
			{
				_showTerminal = value;
				UpdateTerminalPresence();
			}
		}
	}

	[Export]
public bool ShowPlatforms
{
	get => _showPlatforms;
	set
	{
		if (_showPlatforms != value)
		{
			_showPlatforms = value;
			UpdatePlatformsVisibility();
		}
	}
}
	#endregion

	#region Private Fields
	private bool _isPuzzleActive = true;
	private bool _isInRedState = false;
	private bool _needsUpdate = false;
	private MemoryPlatform[,] platformInstances;
	private Area3D _puzzleArea;
	private int _rowCount = 1;
	private int _columnCount = 1;
	private Vector3 _spacing = new Vector3(2, 0, 2);
	private SoundManager SoundManager => GetNode<SoundManager>("/root/SoundManager");
	private const string WrongPlatformSound = "res://assets/Sounds/wrongPlatform.mp3";
	private bool _wrongSoundPlayed = false;
	private bool _isPuzzleComplete = false;
	private Node3D _terminalNode;
	#endregion

	#region Public Properties
	public bool AllActiveSteppedOn { get; private set; } = false;
	#endregion

	#region Lifecycle Methods
	public override void _Ready()
	{
		GD.Print("MemoryPuzle _Ready() method started");

		if (Engine.IsEditorHint())
		{
			GD.Print("Running in editor mode");
			CallDeferred(nameof(UpdatePuzzle));
			CallDeferred(nameof(UpdatePuzzleArea));
			CallDeferred(nameof(UpdatePlatformsVisibility));
		}
		else
		{
			GD.Print("Running in game mode");
			CallDeferred(nameof(CreatePlatformGrid));
			CallDeferred(nameof(UpdatePuzzleArea));
			CallDeferred(nameof(UpdateTerminalPresence));
			CallDeferred(nameof(UpdatePlatformsVisibility));
		}

		CallDeferred(nameof(SetupPuzzleAreaEventHandlers));

		if (GlassGates == null)
		{
			GD.Print("GlassGates is null");
		}
		else if (GlassGates.Length > 0)
		{
			for (int i = 0; i < GlassGates.Length; i++)
			{
				if (GlassGates[i] != null)
				{
					GD.Print($"MemoryPuzzle: GlassGate {i + 1} successfully connected.");
				}
				else
				{
					GD.PrintErr($"MemoryPuzzle: GlassGate {i + 1} is null in the Inspector.");
				}
			}
		}
		else
		{
			GD.Print("MemoryPuzzle: No GlassGates set in the Inspector.");
		}

		GD.Print("MemoryPuzle _Ready() method completed");
	}

	private void SetupPuzzleAreaEventHandlers()
	{
		GD.Print("SetupPuzzleAreaEventHandlers called");
		var puzzleArea = PuzzleArea;
		if (puzzleArea != null && ShowPlatforms)
		{
			GD.Print("Setting up PuzzleArea event handlers");
			puzzleArea.BodyEntered += OnBodyEnteredPuzzleArea;
			puzzleArea.BodyExited += OnBodyExitedPuzzleArea;
		}
		else
		{
			GD.Print($"PuzzleArea is null or ShowPlatforms is false. PuzzleArea: {puzzleArea}, ShowPlatforms: {ShowPlatforms}");
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() && _needsUpdate)
		{
			UpdateVisualRepresentation();
			_needsUpdate = false;
		}
	}
	#endregion

	#region Event Handlers
	private void OnBodyEnteredPuzzleArea(Node3D body)
	{
		if (body is Player && ShowPlatforms)
		{
			_isPuzzleActive = true;
			_wrongSoundPlayed = false;
			if (!_isInRedState)
			{
				SetPlatformsActive(true);
			}
		}
	}

	private void OnBodyExitedPuzzleArea(Node3D body)
	{
		if (body is Player && ShowPlatforms)
		{
			_isPuzzleActive = true;
			_isInRedState = false;
			SetPlatformsActive(true);
			_wrongSoundPlayed = false;
			if (!_isPuzzleComplete)
			{
				ResetPlatformsToOriginalState();
			}
		}
	}
	#endregion

	#region Puzzle Logic
	public void ResetPuzzle()
	{
		if (!_isPuzzleActive || !ShowPlatforms) return;

		_isPuzzleActive = false;
		_isInRedState = true;
		AllActiveSteppedOn = false;
		SetAllPlatformsRed();
		UpdateAllPlatforms();

		if (!_wrongSoundPlayed)
		{
			PlayWrongPlatformSound();
			_wrongSoundPlayed = true;
		}
	}

	private void SetAllPlatformsRed()
	{
		if (platformInstances != null)
		{
			foreach (var platform in platformInstances)
			{
				if (platform is MemoryPlatform memoryPlatform)
				{
					memoryPlatform.SetRedState();
					memoryPlatform.SetNeutral(false);
				}
			}
		}
	}

	private void SetPlatformsActive(bool active)
	{
		if (platformInstances != null)
		{
			foreach (var platform in platformInstances)
			{
				if (platform is MemoryPlatform memoryPlatform)
				{
					memoryPlatform.SetInteractive(active);
					if (active && !_isInRedState)
					{
						memoryPlatform.ResetColor();
					}
				}
			}
		}
	}

	private void UpdateAllPlatforms()
	{
		if (platformInstances != null)
		{
			for (int row = 0; row < RowCount; row++)
			{
				for (int col = 0; col < ColumnCount; col++)
				{
					int index = row * ColumnCount + col;
					if (platformInstances[row, col] is MemoryPlatform memoryPlatform)
					{
						memoryPlatform.ResetActivation();
					}
				}
			}
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
		UpdateTerminalPresence();
		UpdatePuzzleArea();
		EmitSignal(SignalName.SetupCompleted);
	}

	public void UpdatePlatformState(int row, int col, bool isActive)
	{
		if (!ShowPlatforms) return;

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
				MemoryPlatform memoryPlatform = platformInstances[row, col];
				if (memoryPlatform != null)
				{
					memoryPlatform.IsActive = isActive;
					memoryPlatform.UpdateVisuals();
					GD.Print($"MemoryPlatform IsActive set to: {isActive}");
				}
				else
				{
					GD.PushError($"MemoryPlatform script not found on platform at row={row}, col={col}");
				}
			}
			else
			{
				GD.Print($"Platform instances not created yet. State will be applied when platforms are instantiated.");
			}

			EmitSignal(SignalName.PlatformStatesChanged);
			
			if (!Engine.IsEditorHint())
			{
				CheckPuzzleCompletion();

				if (!isActive && !_wrongSoundPlayed)
				{
					PlayWrongPlatformSound();
					ResetPuzzle();
				}
				else
				{
					CheckAllActiveSteppedOn();
				}
			}
		}
		else
		{
			GD.PushError($"Invalid platform index: {index}. RowCount={RowCount}, ColumnCount={ColumnCount}");
		}
	}

	private void CheckPuzzleCompletion()
	{
		if (_isPuzzleComplete) return;

		bool isPuzzleSolved = true;
		foreach (bool state in PlatformStates)
		{
			if (!state)
			{
				isPuzzleSolved = false;
				break;
			}
		}

		GD.Print($"Puzzle solved: {isPuzzleSolved}");

		if (isPuzzleSolved)
		{
			_isPuzzleComplete = true;
			OpenGlassGates();
		}
	}

	private void OpenGlassGates()
	{
		foreach (var glassGate in GlassGates)
		{
			OpenGlassGateInstance(glassGate);
		}
		_isPuzzleComplete = true;
	}

	private void OpenGlassGateInstance(Node3D glassGate)
	{
		if (glassGate != null)
		{
			GD.Print($"Attempting to open GlassGate: {glassGate.Name}");
			if (glassGate.HasMethod("Open"))
			{
				glassGate.Call("Open");
			}
			else
			{
				GD.PrintErr($"GlassGate {glassGate.Name} does not have an Open method. Check the implementation.");
			}
		}
		else
		{
			GD.PrintErr("A GlassGate instance is null. Check the Inspector setup.");
		}
	}

	public void CheckAllActiveSteppedOn()
	{
		if (AllActiveSteppedOn || Engine.IsEditorHint()) return; 

		if (platformInstances == null)
		{
			GD.Print("platformInstances is null in CheckAllActiveSteppedOn. Skipping check.");
			return;
		}

		AllActiveSteppedOn = true;
		for (int row = 0; row < RowCount; row++)
		{
			for (int col = 0; col < ColumnCount; col++)
			{
				int index = row * ColumnCount + col;
				if (index >= PlatformStates.Count)
				{
					GD.PrintErr($"Index {index} is out of range for PlatformStates in CheckAllActiveSteppedOn");
					AllActiveSteppedOn = false;
					return;
				}

				if (PlatformStates[index])
				{
					if (platformInstances[row, col] == null)
					{
						GD.PrintErr($"Platform instance at [{row}, {col}] is null in CheckAllActiveSteppedOn");
						AllActiveSteppedOn = false;
						return;
					}

					if (!platformInstances[row, col].HasBeenActivated)
					{
						AllActiveSteppedOn = false;
						return;
					}
				}
			}
		}

		if (AllActiveSteppedOn)
		{
			SetInactivePlatformsNeutral();
			OpenGlassGates();
			GD.Print("All active platforms have been stepped on!");
		}
	}

	private void SetInactivePlatformsNeutral()
	{
		for (int row = 0; row < RowCount; row++)
		{
			for (int col = 0; col < ColumnCount; col++)
			{
				int index = row * ColumnCount + col;
				if (!PlatformStates[index])
				{
					platformInstances[row, col].SetNeutral(true);
				}
			}
		}
	}
	#endregion

	#region Visual Updates
	private void UpdateVisualRepresentation()
{
	if (Engine.IsEditorHint())
	{
		foreach (Node child in GetChildren())
		{
			if (child is CsgBox3D)
			{
				child.QueueFree();
			}
		}

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
				visualPlatform.Visible = ShowPlatforms; // Ensure visibility respects ShowPlatforms
			}
		}
	}
	else
	{
		for (int i = 0; i < PlatformStates.Count; i++)
		{
			int row = i / ColumnCount;
			int col = i % ColumnCount;
			if (platformInstances != null && platformInstances[row, col] != null)
			{
				MemoryPlatform platform = platformInstances[row, col];
				platform.IsActive = PlatformStates[i];
				platform.UpdateVisuals();
			}
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
		platformInstances = null;
	}
}

	private void CreatePlatformGrid()
{
	if (!IsInsideTree() || platformInstances != null) return;

	ClearPlatforms();

	GD.Print($"PlatformScenePath: {PlatformScenePath}");
	if (string.IsNullOrEmpty(PlatformScenePath))
	{
		GD.PushError("PlatformScenePath is not set.");
		return;
	}

	GD.Print($"Attempting to load platform scene from: {PlatformScenePath}");
	PackedScene platformScene = ResourceLoader.Load<PackedScene>(PlatformScenePath);
	if (platformScene == null)
	{
		GD.PushError($"Failed to load platform scene from '{PlatformScenePath}'");
		return;
	}

	platformInstances = new MemoryPlatform[RowCount, ColumnCount];
	for (int row = 0; row < RowCount; row++)
	{
		for (int col = 0; col < ColumnCount; col++)
		{
			MemoryPlatform memoryPlatform = platformScene.Instantiate<MemoryPlatform>();
			if (memoryPlatform == null)
			{
				GD.PushError("Failed to instantiate platform scene as MemoryPlatform.");
				continue;
			}

			AddChild(memoryPlatform);
			memoryPlatform.Owner = GetTree()?.EditedSceneRoot ?? this;
			memoryPlatform.Name = $"Platform_{row}_{col}";
			memoryPlatform.Position = new Vector3(col * Spacing.X, 0, row * Spacing.Z);

			int index = row * ColumnCount + col;
			memoryPlatform.IsActive = index < PlatformStates.Count && PlatformStates[index];
			memoryPlatform.UpdateVisuals();

			GD.Print($"Platform node instantiated at position: {memoryPlatform.Position}, Active: {memoryPlatform.IsActive}");

			platformInstances[row, col] = memoryPlatform;
		}
	}
	UpdatePuzzleArea();
	UpdatePlatformsVisibility();
}


	private Node3D FindTerminalNode()
	{
		if (_terminalNode == null)
		{
			_terminalNode = GetNodeOrNull<Node3D>("Terminal");
			if (_terminalNode == null && ShowTerminal)
			{
				CreateTerminal();
			}
		}
		return _terminalNode;
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
										GD.Print($"Updating grid size to {RowCount} rows and {ColumnCount} columns");
					material.SetShaderParameter("grid_size", new Vector2(ColumnCount, RowCount));
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

	private void UpdatePuzzleArea()
	{
		var puzzleArea = PuzzleArea;
		if (puzzleArea == null) return;

		var collisionShape = puzzleArea.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (collisionShape == null)
		{
			collisionShape = new CollisionShape3D();
			collisionShape.Name = "CollisionShape3D";
			puzzleArea.AddChild(collisionShape);
			collisionShape.Owner = GetTree().EditedSceneRoot ?? this;
		}

		var boxShape = collisionShape.Shape as BoxShape3D;
		if (boxShape == null)
		{
			boxShape = new BoxShape3D();
			collisionShape.Shape = boxShape;
		}

		float width = (ColumnCount - 1) * Spacing.X + 2;
		float depth = (RowCount - 1) * Spacing.Z + 2;
		float height = 2;

		boxShape.Size = new Vector3(width, height, depth);

		puzzleArea.Position = new Vector3(
			(ColumnCount - 1) * Spacing.X / 2,
			height / 2,
			(RowCount - 1) * Spacing.Z / 2
		);

		puzzleArea.Visible = ShowPlatforms;

		GD.Print($"Updated PuzzleArea size to: {boxShape.Size}, position to: {puzzleArea.Position}, visibility: {puzzleArea.Visible}");
	}
	#endregion

	#region Utility Methods
	public void ManualSetup()
	{
		UpdatePuzzle();
		EmitSignal(SignalName.SetupCompleted);
		EmitSignal(SignalName.PlatformStatesChanged);
	}
	#endregion

	#region Terminal Visibility
	private void UpdateTerminalPresence()
	{
		if (ShowTerminal)
		{
			CreateTerminal();
		}
		else
		{
			RemoveTerminal();
		}
	}

	private void CreateTerminal()
	{
		if (_terminalNode == null && !string.IsNullOrEmpty(TerminalScenePath))
		{
			PackedScene terminalScene = ResourceLoader.Load<PackedScene>(TerminalScenePath);
			if (terminalScene != null)
			{
				_terminalNode = terminalScene.Instantiate<Node3D>();
				_terminalNode.Name = "Terminal";
				AddChild(_terminalNode);
				_terminalNode.Owner = GetTree().EditedSceneRoot ?? this;
				UpdateTerminalPosition();
			}
			else
			{
				GD.PushError($"Failed to load terminal scene from '{TerminalScenePath}'");
			}
		}
	}

	private void RemoveTerminal()
	{
		if (_terminalNode != null)
		{
			_terminalNode.QueueFree();
			_terminalNode = null;
		}
	}

	private void RemovePuzzleArea()
	{
		if (_puzzleArea != null)
		{
			_puzzleArea.QueueFree();
			_puzzleArea = null;
		}
	}

	public void ToggleTerminalVisibility(bool show)
	{
		ShowTerminal = show;
		UpdateTerminalPresence();
	}
	#endregion

	#region Platform Visibility
	private void UpdatePlatformsVisibility()
{
	if (platformInstances != null)
	{
		foreach (var platform in platformInstances)
		{
			if (platform != null)
			{
				if (ShowPlatforms)
				{
					platform.Visible = true;
				}
				else
				{
					platform.QueueFree();
				}
			}
		}

		if (!ShowPlatforms)
		{
			platformInstances = null;
		}
	}

	if (_puzzleArea != null)
	{
		if (ShowPlatforms)
		{
			_puzzleArea.Visible = true;
		}
		else
		{
			_puzzleArea.QueueFree();
			_puzzleArea = null;
		}
	}

	if (Engine.IsEditorHint())
	{
		foreach (Node child in GetChildren())
		{
			if (child is CsgBox3D visualPlatform)
			{
				visualPlatform.Visible = ShowPlatforms;
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

	private void ResetPlatformsToOriginalState()
	{
		if (platformInstances != null)
		{
			for (int row = 0; row < RowCount; row++)
			{
				for (int col = 0; col < ColumnCount; col++)
				{
					int index = row * ColumnCount + col;
					if (index < PlatformStates.Count && platformInstances[row, col] != null)
					{
						platformInstances[row, col].IsActive = PlatformStates[index];
						platformInstances[row, col].ResetActivation();
						platformInstances[row, col].UpdateVisuals();
					}
				}
			}
		}
	}

	private void PlayWrongPlatformSound()
	{
		if (SoundManager != null)
		{
			SoundManager.PlaySound(WrongPlatformSound);
		}
		else
		{
			GD.Print("SoundManager is null, cannot play wrong platform sound.");
		}
	}
	#endregion
}
