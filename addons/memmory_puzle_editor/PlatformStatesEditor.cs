using Godot;
using System;

[Tool]
public partial class PlatformStatesEditor : EditorProperty
{
	private GridContainer grid;
	private MemmoryPuzle targetObject;

	public override void _Ready()
	{
		var vbox = new VBoxContainer();
		grid = new GridContainer();
		vbox.AddChild(grid);
		AddChild(vbox);
		SetBottomEditor(vbox);
	}

	public override void _UpdateProperty()
	{
		targetObject = GetEditedObject() as MemmoryPuzle;
		if (targetObject == null) return;

		UpdateGrid();
	}

	public void UpdateGrid()
	{
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
					ButtonPressed = index < currentStates.Count, // Fixed comparison issue
					SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
					SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
					CustomMinimumSize = new Vector2(20, 20),
					FocusMode = Control.FocusModeEnum.None
				};

				checkBox.Toggled += (toggled) => 
				{
					EmitChanged(GetEditedProperty(), GetPlatformStates());
					targetObject.UpdatePuzzle();
				};
				grid.AddChild(checkBox);
			}
		}
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
