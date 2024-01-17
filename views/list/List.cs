using Godot;

public partial class List : TaskView {
	private PackedScene _taskScene;
	private Control _taskList;
	
	public override void _Ready() {
		_taskScene = GD.Load<PackedScene>("res://views/list/ListTask.tscn");
		_taskList = GetNode<Control>("%TaskList");
		Render();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("focus_new_task_field")) {
			GetNode<LineEdit>("%NewTaskText").GrabFocus();
		}
	}
	
	public override void Render() {
		foreach (var child in _taskList.GetChildren()) {
			child.QueueFree();
		}
		foreach (var task in App.Tasks.Values) {
			var taskNode = _taskScene.Instantiate<ListTask>();
			taskNode.SetTask(task);
			_taskList.AddChild(taskNode);
		}
	}

	private async void CreateTask() {
		var newTaskText = GetNode<LineEdit>("%NewTaskText");
		var text = newTaskText.Text;
		newTaskText.Text = "";
		if (string.IsNullOrWhiteSpace(text)) {
			newTaskText.ReleaseFocus();
			return;
		}
		
		var task = new Task {
			Text = text
		};
		task.Save();
		
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		scrollContainer.ScrollVertical = Mathf.RoundToInt(scrollContainer.GetVScrollBar().MaxValue);
	}

	private void SubmittedTask(string _) {
		CreateTask();
	}
}