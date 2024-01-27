using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class List : TaskView {
	private static int _focusedId = -1;
	private static bool _hasFocusedTaskChildren;
	private static bool _shouldScroll;
	private static readonly Dictionary<int, ListTask> _taskNodeById = new();
	private bool _isNested;
	private int _rootId;
	private PackedScene _listScene;
	private PackedScene _taskScene;
	private Control _taskList;

	public static void FocusTask(int taskId) {
		if (_focusedId == taskId) {
			return;
		}
		if (_focusedId > 0 && !_hasFocusedTaskChildren) {
			App.Tasks[_focusedId].Expanded = false;
		}
		_focusedId = taskId;
	}
	
	public static void Clear() {
		foreach (var taskNode in _taskNodeById.Values) {
			taskNode.QueueFree();
		}
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
		if (!_isNested) {
			_hasFocusedTaskChildren = false;
			var hiddenTasks = GetNode<Control>("%HiddenTasks");
			foreach (var taskNode in _taskNodeById.Values) {
				taskNode.Reparent(hiddenTasks);
			}
			foreach (var child in _taskList.GetChildren()) {
				child.QueueFree();
			}
			GetNode<Control>("%Spacer").Hide();
			
			Organizer.Organize();
			if (Organizer.HasFilter("NoRootTaskParent")) {
				_rootId = (Organizer.State.RootId == 0 ? 0 : App.Tasks[Organizer.State.RootId].Parent);
			} else {
				_rootId = 0;
			}
		} else if (App.Tasks[_rootId].IsFolder) {
			GetNode<Control>("%Spacer").CustomMinimumSize /= 2;
		}
		
		var tasks = Organizer.Tasks;
		if (!Organizer.HasFilter("NoHierarchy")) {
			tasks = tasks.Where(task => task.Parent == _rootId).ToList();
		}
		foreach (var task in tasks) {
			if (!_taskNodeById.TryGetValue(task.Id, out var taskNode)) {
				taskNode = _taskScene.Instantiate<ListTask>();
				_taskNodeById.Add(task.Id, taskNode);
				_taskList.AddChild(taskNode);
			} else {
				taskNode.Reparent(_taskList);
			}
			
			taskNode.SetTask(task);
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
			_hasFocusedTaskChildren = (tasks.Count > 0);
			var newTaskText = GetNode<Control>("%NewTaskText");
			var isNewTaskTextVisible = App.Rect.Intersects(newTaskText.GetGlobalRect());
			if (!App.IsMobile() && isNewTaskTextVisible) {
				newTaskText.GrabFocus();
			}
			if (_shouldScroll) {
				_shouldScroll = false;
				App.ScrollTo(newTaskText);
			}
		} else if (_isNested && tasks.Count == 0) {
			Hide();
		} else if (_isNested || Organizer.State.RootId != 0) {
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

		FocusTask(_rootId);
		_shouldScroll = true;
		var task = Task.Create(text, _rootId);
		task.Save();
	}

	private void SubmittedTask(string _) {
		CreateTask();
	}
}