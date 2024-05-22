using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;

public partial class TaskDetails : Control {
	private static Task _task;
	private static TaskDetails _root;
	private static Control _showing;
	private static Control _editing;
	private static Control _panel;
	private static GridContainer _form;
	private static Button _completeButton;
	private static Label _idLabel;
	private static RichTextLabel _infoLabel;
	private static ProgressBar _progressBar;
	private static ImprovedRichTextLabel _textLabel;
	private static ImprovedRichTextLabel _descriptionLabel;
	private static ScrollContainer _scrollContainer;

	public override void _Ready() {
		_root = this;
		_showing = GetNode<Control>("%TaskDetailsShowing");
		_editing = GetNode<Control>("%TaskDetailsEditing");
		_panel = GetNode<Control>("%TaskDetailsContainer");
		_form = GetNode<GridContainer>("%Form");
		_completeButton = GetNode<Button>("%Complete");
		_idLabel = GetNode<Label>("%Id");
		_infoLabel = GetNode<RichTextLabel>("%Info");
		_progressBar = GetNode<ProgressBar>("%ProgressBar");
		_textLabel = GetNode<ImprovedRichTextLabel>("%Text");
		_descriptionLabel = GetNode<ImprovedRichTextLabel>("%Description");
		_scrollContainer = GetNode<ScrollContainer>("%DescriptionScrollContainer");
		
		if (App.IsMobile()) {
			_form.Columns = 1;
		}
		foreach (var child in _form.GetChildren()) {
			if (child is LineEdit lineEdit) {
				lineEdit.TextSubmitted += _ => SaveTask();
			}
		}
	}
	
	public override void _UnhandledInput(InputEvent @event) {
		var isEditing = GetNode<Control>("%TaskDetailsEditing").Visible;
		if ((!isEditing || !App.IsMobile()) &&
			@event is InputEventMouseButton click && click.IsPressed() &&
			!_panel.GetGlobalRect().HasPoint(click.GlobalPosition)
		) {
			if (isEditing && SaveOrTestTask(true)) {
				ConfirmDialog.Show("Close without saving?", HideTask);
			} else {
				HideTask();
			}
		}
	}

	public static async void ShowTask(Task task) {
		if (_root.Visible) {
			HideTask();
		}
		_root.Visible = true;
		_root.GetTree().Paused = true;
		_task = task;
		if (task.Folder) {
			_completeButton.Hide();
		}
		_idLabel.Text = $"ID: {task.Id}";
		
		var done = task.CountPointsDone();
		var total = task.CountPoints();
		_progressBar.Value = Mathf.Floor(100f * done / total);

		var info = $"[b]Created:[/b] {task.Created.ToLocalTime()}.";
		if (task.Updated != DateTime.MinValue && (task.Updated - task.Created).TotalSeconds >= 1) {
			info += $" [b]Updated:[/b] {task.Updated.ToLocalTime()}.";
		}
		if (task.Completed != DateTime.MinValue) {
			info += $" [b]Completed:[/b] {task.Completed.ToLocalTime()}.";
		}
		if (done > 0 || total > 0) {
			info += $" [b]Done:[/b] {done}/{total}.";
		}
		_infoLabel.Clear();
		_infoLabel.AppendText(info);

		var text = task.Text;
		if (task.Folder) {
			text = "[b]" + text + "[/b]";
		}
		if (task.Completed != DateTime.MinValue) {
			text = "[s][i]" + text + "[/i][/s]";
		}
		_textLabel.SetText(text);

		_descriptionLabel.SetText(task.Description);
		await _root.ToSignal(_root.GetTree(), SceneTree.SignalName.ProcessFrame);
		_scrollContainer.CustomMinimumSize = new Vector2(
			_scrollContainer.CustomMinimumSize.X,
			Mathf.Min(500, _descriptionLabel.GetContentHeight())
		);
	}

	private static void HideTask() {
		if (!_root.Visible) {
			return;
		}
		_editing.Hide();
		_showing.Show();
		_root.Hide();
		_root.GetTree().Paused = false;
		_task = null;
	}
	
	private static void SetAsRoot() {
		Organizer.State.RootId = (Organizer.State.RootId == _task.Id ? 0 : _task.Id);
		if (Organizer.State.RootId == 0 || App.Tasks[Organizer.State.RootId].Parent == 0) {
			Organizer.RemoveFilter("NoRootTaskParent");
		}
		App.View.Render();
		HideTask();
	}
	
	private static void CompleteTask() {
		_task.Complete();
		ShowTask(_task);
	}

	private static void EditTask() {
		_showing.Hide();
		_editing.Show();
		if (!App.IsMobile()) {
			_form.GetNode<LineEdit>("Text").GrabFocus();
		}

		var children = _form.GetChildren();
		foreach (var child in children.Where((_, index) => index % 2 == 1 && index < children.Count - 2)) {
			var value = _task.GetType().GetField(child.Name)!.GetValue(_task);
			
			switch (child) {
				case LineEdit lineEdit:
					lineEdit.Text = value switch {
						DateTime date => (
							date != DateTime.MinValue ? date.ToLocalTime().ToString(CultureInfo.CurrentCulture) : ""
						),
						List<string> list => string.Join(" ", list),
						double number => number.ToString(CultureInfo.InvariantCulture),
						_ => value?.ToString(),
					};
					break;
				case TextEdit textEdit:
					textEdit.Text = value?.ToString();
					break;
				case CheckBox checkBox:
					checkBox.ButtonPressed = (value != null && (bool)value);
					break;
			}
		}
	}

	private static void ConfirmDelete() {
		var childrenCount = _task.CountChildren();
		ConfirmDialog.Show(
			"Delete task" + (childrenCount > 0 ? " with " + childrenCount + " children" : "") + "?",
			DeleteTask
		);
	}

	private static void DeleteTask() {
		_task.Delete();
		HideTask();
	}

	private static void SaveTask() {
		SaveOrTestTask();
	}

	private static bool SaveOrTestTask(bool onlyTest = false) {
		var hasChanged = false;
		var oldParent = _task.Parent;
		foreach (var child in _form.GetChildren()) {
			if (child is Label) {
				continue;
			}
			var property = _task.GetType().GetField(child.Name)!;
			
			switch (child) {
				case LineEdit lineEdit:
					var text = lineEdit.Text;
					if (property.FieldType == typeof(int)) {
						int.TryParse(text, out var number);
						hasChanged = SetValue(property, number, onlyTest) || hasChanged;
					} else if (property.FieldType == typeof(double)) {
						double.TryParse(
							text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number
						);
						hasChanged = SetValue(property, number, onlyTest) || hasChanged;
					} else if (property.FieldType == typeof(DateTime)) {
						var date = DateTime.MinValue;
						if (!string.IsNullOrWhiteSpace(text)) {
							date = DateTime
								.Parse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal)
								.ToUniversalTime();
						}

						hasChanged = SetValue(property, date, onlyTest) || hasChanged;
					} else if (property.FieldType == typeof(List<string>)) {
						var list = text.Split(" ").ToList();
						if (list.Count == 1 && list[0] == "") {
							hasChanged = SetValue(property, null, onlyTest) || hasChanged;
						} else {
							hasChanged = SetValue(property, list, onlyTest) || hasChanged;
						}
					} else {
						hasChanged = SetValue(property, text, onlyTest) || hasChanged;
					}
					break;
				case TextEdit textEdit:
					hasChanged = SetValue(property, textEdit.Text, onlyTest) || hasChanged;
					break;
				case CheckBox checkBox:
					hasChanged = SetValue(property, checkBox.ButtonPressed, onlyTest) || hasChanged;
					break;
			}
		}

		if (!onlyTest) {
			if (_task.Parent != oldParent) {
				App.Tasks[oldParent].Children.Remove(_task);
				App.Tasks[_task.Parent].Children.Add(_task);
			}
			_task.Save();
			ShowTask(_task);
		}
		return hasChanged;
	}

	private static bool SetValue(FieldInfo property, object value, bool onlyTest) {
		var oldValue = property.GetValue(_task);
		if (value is List<string> newList && oldValue is List<string> oldList && oldList.SequenceEqual(newList) ||
		    value == null && oldValue == null ||
		    value != null && value.Equals(oldValue)) {
			return false;
		}
		if (!onlyTest) {
			property.SetValue(_task, value);
		}
		return true;
	}
}