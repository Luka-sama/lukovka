using System;
using Godot;

public partial class ListTask : Control {
	private Task _task;
	
	public void SetTask(Task task) {
		_task = task;
		
		if (task.IsFolder || Organizer.HasFilter("NoCompleteButton")) {
			GetNode<Control>("%Complete").Hide();
			GetNode<Control>("%Spacer").Hide();
		} else {
			GetNode<Control>("%Complete").Show();
			GetNode<Control>("%Spacer").Show();
		}

		var textNode = GetNode<ImprovedRichTextLabel>("%Text");
		textNode.SetText(GetText());

		var progressBar = GetNode<ProgressBar>("%ProgressBar");
		progressBar.Visible = Organizer.HasFilter("WithProgressBar") && task.Id > 0;
		if (progressBar.Visible) {
			progressBar.Value = Mathf.Floor(100f * task.CountPointsDone() / task.CountPoints());
		}

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

	private string GetText() {
		var text = _task.Text;
		
		if (Organizer.HasFilter("WithPath") || Organizer.HasFilter("WithPathWithoutFirst")) {
			var currentTask = _task;
			while (App.Tasks.ContainsKey(currentTask.Parent)) {
				currentTask = App.Tasks[currentTask.Parent];
				if (Organizer.HasFilter("WithPath") || App.Tasks.ContainsKey(currentTask.Parent)) {
					text = $"{currentTask.Text}. {text}";
				}
			}
		}
		
		if (_task.IsFolder) {
			text = $"[b]{text}[/b]";
		}
		
		if (_task.Completed != DateTime.MinValue) {
			text = $"[s][i]{text}[/i][/s]";
		}
		
		if (_task.Date != DateTime.MinValue || _task.StartDate != DateTime.MinValue) {
			var hasDate = (_task.Date != DateTime.MinValue);
			var hasStartDate = (_task.StartDate != DateTime.MinValue);
			var startDate = _task.StartDate.ToLocalTime();
			var date = _task.Date.ToLocalTime();
			if (!hasDate) {
				hasStartDate = false;
				date = startDate;
				startDate = DateTime.MinValue;
			}
			var format = GetDatetimeFormat(date);
			if (hasStartDate) {
				format = Mathf.Max(format, GetDatetimeFormat(startDate));
			}
			text += " [b]" + (!hasDate ? "[i][color=#6b578c]" : "");
			var sameDate = (
				startDate.Day == date.Day && startDate.Month == date.Month && startDate.Year == date.Year
			);
			text += format switch {
				0 => (hasStartDate ? $"{startDate:dd.MM} – " : "") + $"{date:dd.MM}",
				1 => (hasStartDate ? $"{startDate:dd.MM.y} – " : "") + $"{date:dd.MM.y}",
				2 when sameDate => $"{startDate:dd.MM HH:mm}–{date:HH:mm}",
				2 => (hasStartDate ? $"{startDate:dd.MM HH:mm} – " : "") + $"{date:dd.MM HH:mm}",
				3 when sameDate => $"{startDate:dd.MM.y HH:mm}–{date:HH:mm}",
				3 => (hasStartDate ? $"{startDate:dd.MM.y HH:mm} – " : "") + $"{date:dd.MM.y HH:mm}",
				_ when sameDate => $"{startDate:dd.MM.y HH:mm:ss}–{date:HH:mm:ss}",
				_ => (hasStartDate ? $"{startDate:dd.MM.y HH:mm:ss} – " : "") + $"{date:dd.MM.y HH:mm:ss}",
			};
			text += (!hasDate ? "[/color][/i]" : "") + "[/b]";
		}

		if (_task.Points != 0 && _task.PointsDone == 0) {
			text += $" [b][i][{_task.Points}][/i][/b]";
		} else if (_task.Points != 0 || _task.PointsDone != 0) {
			text += $" [b][i][{_task.PointsDone}/{_task.Points}][/i][/b]";
		}
		
		if (_task.Tags != null) {
			foreach (var tag in _task.Tags) {
				text += $" [color=#6b578c][b][i]{tag}[/i][/b][/color]";
			}
		}
		
		if (!string.IsNullOrEmpty(_task.Description)) {
			text += " [color=#EEEEEE]≡[/color]";
		}
		
		return text;
	}

	private int GetDatetimeFormat(DateTime date) {
		if (date.Year == DateTime.Now.Year && date.Hour == 0 && date.Minute == 0 && date.Second == 0) {
			return 0;
		} else if (date.Hour == 0 && date.Minute == 0 && date.Second == 0) {
			return 1;
		} else if (date.Year == DateTime.Now.Year && date.Second == 0) {
			return 2;
		} else if (date.Second == 0) {
			return 3;
		} else {
			return 4;
		}
	}

	private void CompleteTask() {
		_task.Complete();
	}

	private void OpenTask(InputEvent @event) {
		if (_task.Id > 0 && @event is InputEventMouseButton click && click.IsPressed() && click.DoubleClick) {
			TaskDetails.ShowTask(_task);
		}
	}

	private void ExpandTask() {
		if (Organizer.HasFilter("NoHierarchy")) {
			ErrorDialog.Show("You can't expand a task with no hierarchy.");
			return;
		}
		
		_task.Expanded = !_task.Expanded;
		if (App.View is List) {
			List.FocusTask(_task.Expanded && _task.Id > 0 ? _task.Id : 0);
		}
		App.View.Render();
	}
}