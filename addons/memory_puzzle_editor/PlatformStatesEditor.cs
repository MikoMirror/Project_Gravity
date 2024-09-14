using Godot;
using System;

[Tool]
public partial class PlatformStatesEditor : EditorProperty
{
	private GridContainer grid;
	private MemoryPuzle targetObject;
	private bool isUpdating = false;
	private bool _needsUpdate = false;
	private bool _isDisposed = false;

	public override void _Ready()
	{
		GD.Print("PlatformStatesEditor _Ready called");
		var vbox = new VBoxContainer();
		grid = new GridContainer();
		vbox.AddChild(grid);
		AddChild(vbox);
		SetBottomEditor(vbox);
	}

	public override void _UpdateProperty()
	{
		if (_isDisposed || isUpdating) return;
		isUpdating = true;

		targetObject = GetEditedObject() as MemoryPuzle;
		if (targetObject == null)
		{
			isUpdating = false;
			return;
		}

		_needsUpdate = true;
		isUpdating = false;
	}

	public void UpdateGrid()
	{
		GD.Print("PlatformStatesEditor UpdateGrid called");
		if (_isDisposed) return;

		int rows = targetObject.RowCount;
		int columns = targetObject.ColumnCount;
		var currentStates = targetObject.PlatformStates;

		// Clear existing checkboxes
		foreach (var child in grid.GetChildren())
		{
			child.QueueFree();
		}

		grid.Columns = columns;

		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < columns; col++)
			{
				int index = row * columns + col;
				var checkBox = new CheckBox
				{
					ButtonPressed = index < currentStates.Count && currentStates[index],
					SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
					CustomMinimumSize = new Vector2(20, 20),
					FocusMode = Control.FocusModeEnum.None
				};

				int capturedRow = row;
				int capturedCol = col;
				checkBox.Toggled += (bool toggled) => OnCheckBoxToggled(capturedRow, capturedCol, toggled);
				grid.AddChild(checkBox);
			}
		}
	}

	private void OnCheckBoxToggled(int row, int col, bool toggled)
	{
		GD.Print($"PlatformStatesEditor OnCheckBoxToggled called: row {row}, col {col}, toggled {toggled}");
		if (_isDisposed) return;

		var newStates = GetPlatformStates();
		EmitChanged(GetEditedProperty(), newStates);
		targetObject.PlatformStates = newStates;
		targetObject.UpdatePlatformState(row, col, toggled);
	}

	private Godot.Collections.Array<bool> GetPlatformStates()
	{
		var states = new Godot.Collections.Array<bool>();
		foreach (var child in grid.GetChildren())
		{
			if (child is CheckBox checkBox)
			{
				states.Add(checkBox.ButtonPressed);
			}
		}
		return states;
	}

	public override void _ExitTree()
	{
		SafeDispose();
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		if (_isDisposed) return;

		if (_needsUpdate)
		{
			UpdateGrid();
			 _needsUpdate = false;
		}
	}

	public bool IsDisposed()
	{
		return _isDisposed;
	}

	public void SafeDispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			// Clean up resources
			foreach (var child in grid.GetChildren())
			{
				child.QueueFree();
			}
			grid.QueueFree();
			QueueFree(); // Queue the editor itself for deletion
		}
	}
}
