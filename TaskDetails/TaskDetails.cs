using System;
using System.Linq;
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
		GetNode<Control>("%TaskDetailsEditing").Hide();
		GetNode<Control>("%TaskDetailsShowing").Show();
		if (App.View is not List) {
			GetNode<Button>("%SetAsRoot").Hide();
		}
		
		var text = task.Text;
		if (task.Completed != DateTime.MinValue) {
			text = "[s]" + text + "[/s]";
		}
		var textLabel = GetNode<ImprovedRichTextLabel>("%Text");
		textLabel.Clear();
		textLabel.AppendText(text);
		
		var descriptionLabel = GetNode<ImprovedRichTextLabel>("%Description");
		descriptionLabel.Clear();
		descriptionLabel.AppendText(task.Description);
	}

	private void HideTask() {
		if (!Visible) {
			return;
		}
		Visible = false;
		GetTree().Paused = false;
		_task = null;
	}
	
	private void SetAsRoot() {
		Organizer.RootId = (Organizer.RootId == _task.Id ? 0 : _task.Id);
		App.View.Render();
		HideTask();
		/*if (App.View is List list) {
			list.RootId = (list.RootId == _task.Id ? 0 : _task.Id);
			App.View.Render();
			HideTask();
		}*/
	}
	
	private void CompleteTask() {
		_task.Completed = (_task.Completed == DateTime.MinValue ? DateTime.Now : DateTime.MinValue);
		_task.Save();
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
					lineEdit.Text = value.ToString();
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

	private void DeleteTask() {
		_task.Delete();
		HideTask();
	}

	private void SaveTask() {
		var form = GetNode<Control>("%TaskDetailsEditing");
		
		foreach (var child in form.GetChildren()) {
			if (child is Label) {
				continue;
			}
			var property = _task.GetType().GetField(child.Name)!;
			
			switch (child) {
				case LineEdit lineEdit:
					if (property.FieldType == typeof(int)) {
						int.TryParse(lineEdit.Text, out var number);
						property.SetValue(_task, number);
					} else {
						property.SetValue(_task, lineEdit.Text);
					}
					break;
				case TextEdit textEdit:
					property.SetValue(_task, textEdit.Text);
					break;
				case CheckBox checkBox:
					property.SetValue(_task, checkBox.ButtonPressed);
					break;
			}
		}
		
		_task.Save();
		ShowTask(_task);
	}

	private void SubmittedTask(string _) {
		SaveTask();
	}
}