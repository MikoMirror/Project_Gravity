using Godot;
using System;

[Tool]
public partial class MemoryPuzzleEditorPlugin : EditorPlugin
{
	private PlatformStatesInspectorPlugin inspectorPlugin;

	public override void _EnterTree()
	{
		GD.Print("MemmoryPuzleEditorPlugin _EnterTree called");
		inspectorPlugin = new PlatformStatesInspectorPlugin();
		AddInspectorPlugin(inspectorPlugin);
	}

	public override void _ExitTree()
	{
		GD.Print("MemmoryPuzleEditorPlugin _ExitTree called");
		RemoveInspectorPlugin(inspectorPlugin);
		inspectorPlugin.Cleanup(); // Call cleanup method
		inspectorPlugin = null;
	}
}

public partial class PlatformStatesInspectorPlugin : EditorInspectorPlugin
{
	private PlatformStatesEditor platformStatesEditor;

	// Add this method for cleanup
	public void Cleanup()
	{
		if (platformStatesEditor != null && IsInstanceValid(platformStatesEditor))
		{
			platformStatesEditor.SafeDispose();
			platformStatesEditor = null;
		}
	}

	public override bool _CanHandle(GodotObject @object)
	{
		return @object is MemoryPuzle || @object is Terminal;
	}

	public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
	{
		if (@object == null || name == null)
		{
			return false;
		}

		if ((@object is MemoryPuzle && name == "PlatformStates") || 
			(@object is Terminal && name == "CustomPlatformStates"))
		{
			if (platformStatesEditor != null && IsInstanceValid(platformStatesEditor))
			{
				platformStatesEditor.SafeDispose();
			}
			platformStatesEditor = new PlatformStatesEditor();
			AddPropertyEditor(name, platformStatesEditor);
			return true;
		}
		else if ((@object is MemoryPuzle && (name == "RowCount" || name == "ColumnCount")) ||
				 (@object is Terminal && (name == "CustomRows" || name == "CustomColumns")))
		{
			var editor = GetEditorForProperty(name, type, hintType, hintString, usageFlags, @object);
			if (editor != null)
			{
				AddPropertyEditor(name, editor);
				return true;
			}
		}
		return false;
	}

	private EditorProperty GetEditorForProperty(string name, Variant.Type type, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, GodotObject @object)
	{
		if (type == Variant.Type.Int)
		{
			var container = new VBoxContainer();
			var spinBox = new SpinBox
			{
				MinValue = 1,
				MaxValue = 20,
				Step = 1,
				Value = 1
			};
			container.AddChild(spinBox);

			var editor = new EditorProperty();
			editor.Label = name;
			editor.AddChild(container);
			editor.AddFocusable(spinBox);

			spinBox.ValueChanged += (double value) =>
			{
				editor.EmitChanged(name, (int)value);
				if (platformStatesEditor != null && IsInstanceValid(platformStatesEditor))
				{
					platformStatesEditor.UpdateGrid();
				}
				if (@object is MemoryPuzle memoryPuzle)
				{
					memoryPuzle.UpdatePuzzle();
				}
				else if (@object is Terminal terminal)
				{
					terminal.UpdateGridSize(terminal.CustomRows, terminal.CustomColumns);
				}
			};

			return editor;
		}
		return null;
	}
}
