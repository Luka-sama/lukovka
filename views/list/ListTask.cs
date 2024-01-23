using System;
using System.Linq;
using Godot;

public partial class ListTask : Control {
	private Task _task;
	
	public void SetTask(Task task) {
		_task = task;
		
		var text = task.Text;
		if (task.Completed != DateTime.MinValue) {
			text = "[s]" + text + "[/s]";
		}
		if (!string.IsNullOrEmpty(task.Description)) {
			text += " [color=#EEEEEE]≡[/color]";
		}
		GetNode<ImprovedRichTextLabel>("%Text").AppendText(text);

		if (task.Expanded) {
			GetNode<Button>("%Expand").Text = "🠗";
		} else if (App.Tasks.Values.All(task2 => task2.Parent != task.Id)) {
			GetNode<Button>("%Expand").Text = "+";
		}
	}

	private void CompleteTask() {
		_task.Completed = (_task.Completed == DateTime.MinValue ? DateTime.Now : DateTime.MinValue);
		_task.Save();
	}

	private void OpenTask(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed() && click.DoubleClick) {
			App.ShowDetails(_task);
		}
	}

	private void ExpandTask() {
		_task.Expanded = !_task.Expanded;
		if (App.View is List) {
			List.FocusTask(_task.Id);
		}
		App.View.Render();
	}
}
