using System;
using Godot;

public partial class ListTask : Control {
	private const int NestingOffset = 35;
	private Task _task;
	
	public void SetTask(Task task) {
		_task = task;
		
		var text = task.Text;
		if (task.Completed != DateTime.MinValue) {
			text = "[s]" + text + "[/s]";
		}
		GetNode<RichTextLabel>("%Text").AppendText(text);
		
		GetNode<Control>("%Nested").CustomMinimumSize = new Vector2(task.NestingLevel * NestingOffset, 0);
	}

	private void CompleteTask() {
		_task.Completed = (_task.Completed == DateTime.MinValue ? DateTime.Now : DateTime.MinValue);
		_task.Save();
	}

	private void OpenTask(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed() && click.DoubleClick) {
			App.TaskDetails.ShowTask(_task);
		}
	}
}