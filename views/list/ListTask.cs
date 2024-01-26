using System;
using System.Linq;
using Godot;

public partial class ListTask : Control {
	private Task _task;
	
	public void SetTask(Task task) {
		_task = task;
		
		var text = task.Text;
		if (task.IsFolder) {
			text = "[b]" + text + "[/b]";
			GetNode<Control>("%Complete").Hide();
			GetNode<Control>("%Spacer").Hide();
		}
		if (task.Completed != DateTime.MinValue) {
			text = "[s][i]" + text + "[/i][/s]";
		}
		if (task.Date != DateTime.MinValue) {
			text += " [b]" + DateOnly.FromDateTime(task.Date.ToLocalTime()) + "[/b]";
		}
		if (!string.IsNullOrEmpty(task.Description)) {
			text += " [color=#EEEEEE]≡[/color]";
		}
		GetNode<ImprovedRichTextLabel>("%Text").SetText(text);

		var expandButton = GetNode<Button>("%Expand");
		if (task.Expanded) {
			expandButton.Text = "↓";
		} else if (App.Tasks.Values.All(task2 => task2.Parent != task.Id)) {
			expandButton.Text = "+";
		} else {
			expandButton.Text = "→";
		}
	}

	private void CompleteTask() {
		_task.Completed = (_task.Completed == DateTime.MinValue ? DateTime.Now.ToUniversalTime() : DateTime.MinValue);
		_task.Save();
	}

	private void OpenTask(InputEvent @event) {
		if (_task.Id > 0 && @event is InputEventMouseButton click && click.IsPressed() && click.DoubleClick) {
			App.ShowDetails(_task);
		}
	}

	private void ExpandTask() {
		_task.Expanded = !_task.Expanded;
		if (App.View is List) {
			List.FocusTask(_task.Expanded && _task.Id > 0 ? _task.Id : 0);
		}
		App.View.Render();
	}
}