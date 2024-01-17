using System;
using Godot;

public partial class TaskDetails : Control {
	private Task _task;
	private Control _panel;

	public override void _Ready() {
		_panel = GetNode<Control>("%TaskDetailsContainer");
	}
	
	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed() &&
			!_panel.GetGlobalRect().HasPoint(click.GlobalPosition)) {
			HideTask();
		}
	}

	public void ShowTask(Task task) {
		if (Visible) {
			HideTask();
		}
		Visible = true;
		GetTree().Paused = true;
		_task = task;
		
		GetNode<RichTextLabel>("%Text").AppendText(task.Text);
		GetNode<RichTextLabel>("%Description").AppendText(task.Description);
	}

	private void HideTask() {
		if (!Visible) {
			return;
		}
		Visible = false;
		GetTree().Paused = false;
		_task = null;
		
		GetNode<RichTextLabel>("%Text").Clear();
		GetNode<RichTextLabel>("%Description").Clear();
	}

	private void DeleteTask() {
		_task.Delete();
		HideTask();
	}
}