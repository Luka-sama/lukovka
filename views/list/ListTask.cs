using System;
using Godot;

public partial class ListTask : Control {
	private Task _task;
	
	public void SetTask(Task task) {
		_task = task;
		
		var text = task.Text;
		if (Organizer.HasFilter("WithPath") || Organizer.HasFilter("WithPathWithoutFirst")) {
			var currentTask = _task;
			while (App.Tasks.ContainsKey(currentTask.Parent)) {
				currentTask = App.Tasks[currentTask.Parent];
				if (Organizer.HasFilter("WithPath") || App.Tasks.ContainsKey(currentTask.Parent)) {
					text = $"{currentTask.Text}. {text}";
				}
			}
		}
		if (task.IsFolder) {
			text = "[b]" + text + "[/b]";
			GetNode<Control>("%Complete").Hide();
			GetNode<Control>("%Spacer").Hide();
		} else {
			GetNode<Control>("%Complete").Show();
			GetNode<Control>("%Spacer").Show();
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
		var textNode = GetNode<ImprovedRichTextLabel>("%Text");
		textNode.SetText(text);

		var expandButton = GetNode<Button>("%Expand");
		expandButton.Show();
		if (Organizer.HasFilter("NoHierarchy")) {
			expandButton.Hide();
		} else if (task.Expanded) {
			expandButton.Text = "↓";
		} else if (task.Children.Count < 1) {
			expandButton.Text = "+";
		} else {
			expandButton.Text = "→";
		}
	}

	private void CompleteTask() {
		_task.Complete();
	}

	private void OpenTask(InputEvent @event) {
		if (_task.Id > 0 && @event is InputEventMouseButton click && click.IsPressed() && click.DoubleClick) {
			App.ShowDetails(_task);
		}
	}

	private void ExpandTask() {
		if (Organizer.HasFilter("NoHierarchy")) {
			App.ShowError("You can't expand a task with no hierarchy.");
			return;
		}
		
		_task.Expanded = !_task.Expanded;
		if (App.View is List) {
			List.FocusTask(_task.Expanded && _task.Id > 0 ? _task.Id : 0);
		}
		App.View.Render();
	}
}