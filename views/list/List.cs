using System.Linq;
using Godot;

public partial class List : TaskView {
	private static int _focusedId;
	private bool _isNested;
	private int _rootId;
	private PackedScene _listScene;
	private PackedScene _taskScene;
	private Control _taskList;

	public static void FocusTask(int taskId) {
		_focusedId = taskId;
	}

	public override void _Ready() {
		_listScene = GD.Load<PackedScene>("res://views/list/List.tscn");
		_taskScene = GD.Load<PackedScene>("res://views/list/ListTask.tscn");
		_taskList = GetNode<Control>("%TaskList");
		Render();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("focus_new_task_field")) {
			GetNode<LineEdit>("%NewTaskText").GrabFocus();
		}
	}

	public override async void Render() {
		foreach (var child in _taskList.GetChildren()) {
			child.QueueFree();
		}
		if (!_isNested) {
			GetNode<Control>("%Spacer").Hide();
			_rootId = (Organizer.RootId == 0 ? 0 : App.Tasks[Organizer.RootId].Parent);
		}
		
		Organizer.Organize();
		var tasks = Organizer.Tasks.Where(task => task.Parent == _rootId).ToList();
		foreach (var task in tasks) {
			var taskNode = _taskScene.Instantiate<ListTask>();
			taskNode.SetTask(task);
			_taskList.AddChild(taskNode);
			
			if (!task.Expanded) {
				continue;
			}
			
			var sublist = _listScene.Instantiate<List>();
			sublist._rootId = task.Id;
			sublist._isNested = true;
			_taskList.AddChild(sublist);
		}
		
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (_focusedId == _rootId) {
			var newTaskText = GetNode<Control>("%NewTaskText");
			newTaskText.GrabFocus();
			App.ScrollTo(newTaskText);
		} else if (tasks.Count == 0 && _isNested) {
			Hide();
		} else if (_isNested) {
			GetNode<Control>("%NewTask").Hide();
		}
	}

	private void CreateTask() {
		var newTaskText = GetNode<LineEdit>("%NewTaskText");
		var text = newTaskText.Text;
		newTaskText.Text = "";
		if (string.IsNullOrWhiteSpace(text)) {
			newTaskText.ReleaseFocus();
			return;
		}

		_focusedId = _rootId;
		var task = Task.Create(text, _rootId);
		task.Save();
	}

	private void SubmittedTask(string _) {
		CreateTask();
	}
}