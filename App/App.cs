using System.Collections.Generic;
using Godot;

public partial class App : Control {
	public static readonly Dictionary<int, Task> Tasks = new();
	public static TaskView View { get; private set; }
	public static TaskDetails TaskDetails { get; private set; }

	public override void _Ready() {
		View = GetNode<TaskView>("View");
		TaskDetails = GetNode<TaskDetails>("TaskDetails");
		if (OS.GetName() == "Android") {
			GetTree().Root.ContentScaleFactor = 2f;
		}
		
		// TODO: get tasks from server, use JsonConvert.DeserializeObject<Task>(json);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseButton) {
			GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
		} else if (@event.IsActionPressed("quit")) {
			GetTree().Quit();
		}
	}
}