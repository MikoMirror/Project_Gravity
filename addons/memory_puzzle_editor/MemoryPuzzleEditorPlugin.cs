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
		GD.Print("PlatformStatesInspectorPlugin _CanHandle called for object: ", @object);
		return @object is MemoryPuzle;
	}

	public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
	{
		GD.Print("PlatformStatesInspectorPlugin _ParseProperty called for object: ", @object, " name: ", name);
		if (@object == null || name == null)
		{
			return false;
		}

		if (name == "PlatformStates" && type == Variant.Type.Array)
		{
			if (platformStatesEditor != null && IsInstanceValid(platformStatesEditor))
			{
				platformStatesEditor.SafeDispose();
			}
			platformStatesEditor = new PlatformStatesEditor();
			AddPropertyEditor(name, platformStatesEditor);
			return true;
		}
		else if (name == "RowCount" || name == "ColumnCount")
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
		try
		{
			GD.Print("PlatformStatesInspectorPlugin GetEditorForProperty called for property: ", name);
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
					try
					{
						editor.EmitChanged(name, (int)value);
						if (platformStatesEditor != null && IsInstanceValid(platformStatesEditor))
						{
							platformStatesEditor.UpdateGrid();
						}
						var memoryPuzle = @object as MemoryPuzle;
						if (memoryPuzle != null)
						{
							memoryPuzle.UpdatePuzzle();
						}
					}
					catch (Exception e)
					{
						GD.PrintErr($"Error in spinBox.ValueChanged: {e.Message}");
					}
				};

				return editor;
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error in GetEditorForProperty: {e.Message}");
		}
		return null;
	}
}
