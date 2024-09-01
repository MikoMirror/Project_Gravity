using Godot;
using System;

[Tool]
public partial class PlatformStatesEditor : EditorProperty
{
	private GridContainer grid;
	private MemoryPuzle targetObject;

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
		GD.Print("PlatformStatesEditor _UpdateProperty called");
		targetObject = GetEditedObject() as MemoryPuzle;
		if (targetObject == null) return;

		UpdateGrid();
	}

	public void UpdateGrid()
	{
		GD.Print("PlatformStatesEditor UpdateGrid called");
		if (targetObject == null) return;

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
					ButtonPressed = index < currentStates.Count && currentStates[index], // Set initial state
					SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
					CustomMinimumSize = new Vector2(20, 20),
					FocusMode = Control.FocusModeEnum.None
				};

				checkBox.Toggled += OnCheckBoxToggled;
				grid.AddChild(checkBox);
			}
		}
	}

	private void OnCheckBoxToggled(bool toggled)
	{
		GD.Print("PlatformStatesEditor OnCheckBoxToggled called");
		EmitChanged(GetEditedProperty(), GetPlatformStates());
		targetObject.PlatformStates = GetPlatformStates();
		targetObject.UpdatePlatformStates(); // Call this instead of UpdatePuzzle
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
}
