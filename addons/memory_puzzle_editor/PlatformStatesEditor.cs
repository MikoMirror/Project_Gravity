using Godot;
using System;

[Tool]
public partial class PlatformStatesEditor : EditorProperty
{
    private GridContainer grid;
    private GodotObject targetObject;
    private bool isUpdating = false;
    private bool _needsUpdate = false;
    private bool _isDisposed = false;

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
        if (_isDisposed || isUpdating) return;
        isUpdating = true;

        targetObject = GetEditedObject();
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
        if (_isDisposed || targetObject == null) return;

        int rows, columns;
        Godot.Collections.Array<bool> states;

        if (targetObject is MemoryPuzle memoryPuzle)
        {
            rows = memoryPuzle.RowCount;
            columns = memoryPuzle.ColumnCount;
            states = memoryPuzle.PlatformStates;
        }
        else if (targetObject is Terminal terminal)
        {
            rows = terminal.CustomRows;
            columns = terminal.CustomColumns;
            states = terminal.CustomPlatformStates;
        }
        else
        {
            return;
        }

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
                    ButtonPressed = index < states.Count && states[index],
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
        if (_isDisposed || targetObject == null) return;

        var newStates = GetPlatformStates();
        EmitChanged(GetEditedProperty(), newStates);

        if (targetObject is MemoryPuzle memoryPuzle)
        {
            memoryPuzle.PlatformStates = newStates;
            memoryPuzle.UpdatePlatformState(row, col, toggled);
        }
        else if (targetObject is Terminal terminal)
        {
            terminal.CustomPlatformStates = newStates;
            terminal.UpdateCustomState(row * terminal.CustomColumns + col, toggled);
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

    public override void _Process(double delta)
    {
        if (_isDisposed) return;

        if (_needsUpdate)
        {
            UpdateGrid();
            _needsUpdate = false;
        }
    }

    public void SafeDispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            foreach (var child in grid.GetChildren())
            {
                child.QueueFree();
            }
            grid.QueueFree();
            QueueFree();
        }
    }
}