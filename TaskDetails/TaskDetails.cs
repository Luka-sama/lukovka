using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;

public partial class TaskDetails : Control {
	private Task _task;
	private Control _panel;

	public override void _Ready() {
		_panel = GetNode<Control>("%TaskDetailsContainer");
		var form = GetNode<Control>("%TaskDetailsEditing");
		foreach (var child in form.GetChildren()) {
			if (child is LineEdit lineEdit) {
				lineEdit.TextSubmitted += SubmittedTask;
			}
		}
	}
	
	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed() &&
			!_panel.GetGlobalRect().HasPoint(click.GlobalPosition)) {
			if (GetNode<Control>("%TaskDetailsEditing").Visible && SaveOrTestTask(true)) {
				var confirmClose = GetNode<ConfirmationDialog>("%ConfirmClose");
				confirmClose.PopupCentered();
			} else {
				HideTask();
			}
		}
	}

	public void ShowTask(Task task) {
		if (Visible) {
			HideTask();
		}
		Visible = true;
		GetTree().Paused = true;
		_task = task;
		if (App.View is not List) {
			GetNode<Button>("%SetAsRoot").Hide();
		}

		GetNode<Label>("%Id").Text = "ID: " + task.Id;

		var text = task.Text;
		if (task.IsFolder) {
			text = "[b]" + text + "[/b]";
		}
		if (task.Completed != DateTime.MinValue) {
			text = "[s][i]" + text + "[/i][/s]";
		}
		GetNode<ImprovedRichTextLabel>("%Text").SetText(text);
		GetNode<ImprovedRichTextLabel>("%Description").SetText(task.Description);
	}

	private void HideTask() {
		if (!Visible) {
			return;
		}
		GetNode<Control>("%TaskDetailsEditing").Hide();
		GetNode<Control>("%TaskDetailsShowing").Show();
		Hide();
		GetTree().Paused = false;
		_task = null;
	}
	
	private void SetAsRoot() {
		Organizer.State.RootId = (Organizer.State.RootId == _task.Id ? 0 : _task.Id);
		if (Organizer.State.RootId == 0 || App.Tasks[Organizer.State.RootId].Parent == 0) {
			Organizer.RemoveFilter("NoRootTaskParent");
		}
		App.View.Render();
		HideTask();
	}
	
	private void CompleteTask() {
		_task.Complete();
		ShowTask(_task);
	}

	private void EditTask() {
		GetNode<Control>("%TaskDetailsShowing").Hide();
		var form = GetNode<Control>("%TaskDetailsEditing");
		form.Show();
		form.GetNode<LineEdit>("Text").GrabFocus();

		var children = form.GetChildren();
		foreach (var child in children.Where((_, index) => index % 2 == 1 && index < children.Count - 2)) {
			var value = _task.GetType().GetField(child.Name)!.GetValue(_task);
			if (value == null) {
				continue;
			}
			
			switch (child) {
				case LineEdit lineEdit:
					if (value is DateTime date) {
						lineEdit.Text = (
							date != DateTime.MinValue ? date.ToLocalTime().ToString(CultureInfo.CurrentCulture) : ""
						);
					} else {
						lineEdit.Text = value.ToString();
					}
					break;
				case TextEdit textEdit:
					textEdit.Text = value.ToString();
					break;
				case CheckBox checkBox:
					checkBox.ButtonPressed = (bool)value;
					break;
			}
		}
	}

	private void ConfirmDelete() {
		var childrenCount = _task.CountChildren();
		var confirmDelete = GetNode<ConfirmationDialog>("%ConfirmDelete");
		confirmDelete.PopupCentered();
		confirmDelete.DialogText = "Delete task" + (childrenCount > 0 ? " with " + childrenCount + " children" : "") + "?";
	}

	private void DeleteTask() {
		_task.Delete();
		HideTask();
	}

	private void SaveTask() {
		SaveOrTestTask();
	}

	private bool SaveOrTestTask(bool onlyTest = false) {
		var form = GetNode<Control>("%TaskDetailsEditing");
		var hasChanged = false;
		var oldParent = _task.Parent;
		foreach (var child in form.GetChildren()) {
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
					} else if (property.FieldType == typeof(DateTime)) {
						var date = DateTime.MinValue;
						if (!string.IsNullOrWhiteSpace(text)) {
							date = DateTime
								.Parse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal)
								.ToUniversalTime();
						}
						hasChanged = SetValue(property, date, onlyTest) || hasChanged;
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

	private bool SetValue(FieldInfo property, object value, bool onlyTest) {
		if (property.GetValue(_task)!.Equals(value)) {
			return false;
		}
		if (!onlyTest) {
			property.SetValue(_task, value);
		}
		return true;
	}

	private void SubmittedTask(string _) {
		SaveTask();
	}
}