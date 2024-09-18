using Godot;
using System;

[Tool]
public partial class Terminal : Node3D
{
	[Export]
	public NodePath ScreenPath = "StaticBody3D/CollisionShape3D2/Screen";

	private int _customRows = 3;
	private int _customColumns = 3;
	private bool _needsUpdate = false;

	[Export(PropertyHint.Range, "1,20,1")]
	public int CustomRows
	{
		get => _customRows;
		set
		{
			if (_customRows != value)
			{
				_customRows = value;
				UpdateShaderGridSize();
			}
		}
	}

	[Export(PropertyHint.Range, "1,20,1")]
	public int CustomColumns
	{
		get => _customColumns;
		set
		{
			if (_customColumns != value)
			{
				_customColumns = value;
				UpdateShaderGridSize();
			}
		}
	}

	[Export]
	public Godot.Collections.Array<bool> CustomPlatformStates { get; set; } = new Godot.Collections.Array<bool>();

	private MeshInstance3D screen;
	private ShaderMaterial screenMaterial;
	private ImageTexture stateTexture;
	private bool isStandalone = false;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
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
				isStandalone = true;
				SetupStandaloneTerminal();
			}
		}
		else
		{
			InitializeScreenMaterial();
		}
	}

	private void SetupStandaloneTerminal()
	{
		InitializeScreenMaterial();
		UpdateCustomPlatformStates();
	}

	private void OnMemoryPuzleSetupCompleted()
	{
		InitializeScreenMaterial();
		UpdatePlatformStates();
	}

	private void InitializeScreenMaterial()
	{
		screen = GetNodeOrNull<MeshInstance3D>(ScreenPath);
		
		if (screen == null)
		{
			GD.PushError($"Screen node not found at path: {ScreenPath}");
			return;
		}

		ShaderMaterial originalMaterial = screen.GetActiveMaterial(0) as ShaderMaterial;
		if (originalMaterial != null)
		{
			screenMaterial = originalMaterial.Duplicate() as ShaderMaterial;
			screen.SetSurfaceOverrideMaterial(0, screenMaterial);
			UpdateShaderGridSize();
		}
		else
		{
			GD.PushError("Screen material is not a ShaderMaterial");
		}
	}

	private void UpdateShaderGridSize()
	{
		if (screenMaterial != null)
		{
			screenMaterial.SetShaderParameter("grid_size", new Vector2(CustomColumns, CustomRows));
		}
		else
		{
			GD.Print("Screen material is null. Trying to reinitialize...");
			InitializeScreenMaterial();
		}
	}

	public void UpdateGridSize(int rows, int columns)
	{
		CustomRows = rows;
		CustomColumns = columns;
		ResizeCustomPlatformStates();
		UpdateCustomPlatformStates();
	}

	private void ResizeCustomPlatformStates()
	{
		int totalSize = CustomRows * CustomColumns;
		while (CustomPlatformStates.Count < totalSize)
		{
			CustomPlatformStates.Add(false);
		}
		if (CustomPlatformStates.Count > totalSize)
		{
			CustomPlatformStates.Resize(totalSize);
		}
	}

	private void UpdatePlatformStates()
	{
		if (isStandalone)
		{
			UpdateCustomPlatformStates();
			return;
		}

		var memoryPuzle = GetParent() as MemoryPuzle;
		if (memoryPuzle == null) return;

		int rows = memoryPuzle.RowCount;
		int columns = memoryPuzle.ColumnCount;

		UpdateGridSize(rows, columns);

		var image = Image.CreateEmpty(columns, rows, false, Image.Format.R8);

		for (int i = 0; i < memoryPuzle.PlatformStates.Count; i++)
		{
			int x = i % columns;
			int y = i / columns;
			image.SetPixel(x, y, memoryPuzle.PlatformStates[i] ? Colors.White : Colors.Black);
		}

		UpdateStateTexture(image);
	}

	private void UpdateCustomPlatformStates()
	{
		ResizeCustomPlatformStates();
		var image = Image.CreateEmpty(CustomColumns, CustomRows, false, Image.Format.R8);

		for (int i = 0; i < CustomPlatformStates.Count; i++)
		{
			int x = i % CustomColumns;
			int y = i / CustomColumns;
			image.SetPixel(x, y, CustomPlatformStates[i] ? Colors.White : Colors.Black);
		}

		UpdateStateTexture(image);
	}

	private void UpdateStateTexture(Image image)
	{
		if (stateTexture == null)
		{
			stateTexture = ImageTexture.CreateFromImage(image);
		}
		else
		{
			stateTexture.Update(image);
		}

		screenMaterial.SetShaderParameter("state_texture", stateTexture);
	}

	public void UpdateCustomState(int index, bool state)
	{
		if (index >= 0 && index < CustomPlatformStates.Count)
		{
			CustomPlatformStates[index] = state;
			UpdateCustomPlatformStates();
		}
	}
}
