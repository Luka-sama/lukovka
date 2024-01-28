using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class List : TaskView {
	private static readonly List<ListTask> UsedTaskNodes = new();
	private static readonly List<ListTask> FreeTaskNodes = new();
	private static int _focusedId = -1;
	private static bool _hasFocusedTaskChildren;
	private static bool _shouldScroll;
	private bool _isNested;
	private int _rootId;
	private PackedScene _listScene;
	private PackedScene _taskScene;
	private Control _taskList;
	private Control _hiddenTasks;

	public static void FocusTask(int taskId) {
		if (_focusedId == taskId) {
			return;
		}
		if (_focusedId > 0 && !_hasFocusedTaskChildren) {
			App.Tasks[_focusedId].Expanded = false;
		}
		_focusedId = taskId;
	}

	public override void _Ready() {
		_listScene = GD.Load<PackedScene>("res://views/list/List.tscn");
		_taskScene = GD.Load<PackedScene>("res://views/list/ListTask.tscn");
		_taskList = GetNode<Control>("%TaskList");
		_hiddenTasks = GetNode<Control>("%HiddenTasks");
		Render();
	}

	public override void _Process(double _) {
		if (_isNested || FreeTaskNodes.Count + UsedTaskNodes.Count >= App.Tasks.Count) {
			return;
		}
		for (var i = 0; i < 3; i++) {
			CreateTaskNode();
		}
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("focus_new_task_field")) {
			GetNode<LineEdit>("%NewTaskText").GrabFocus();
		}
	}

	public override async void Render() {
		if (!_isNested) {
			_hasFocusedTaskChildren = false;
			foreach (var taskNode in UsedTaskNodes) {
				taskNode.Reparent(_hiddenTasks);
				FreeTaskNodes.Add(taskNode);
			}
			UsedTaskNodes.Clear();
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
		} else if (Organizer.Tasks.Find(task => task.Id == _rootId).IsFolder) {
			GetNode<Control>("%Spacer").CustomMinimumSize /= 2;
		}
		
		var tasks = Organizer.Tasks;
		if (!Organizer.HasFilter("NoHierarchy")) {
			tasks = tasks.Where(task => task.Parent == _rootId).ToList();
		} else if (Organizer.State.GroupBy != -1) {
			tasks = tasks.Where(task => task.Group == _rootId).ToList();
		}
		foreach (var task in tasks) {
			if (FreeTaskNodes.Count < 1) {
				CreateTaskNode();
			}
			var taskNode = FreeTaskNodes[0];
			FreeTaskNodes.Remove(taskNode);
			UsedTaskNodes.Add(taskNode);
			taskNode.Reparent(_taskList);
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
		if (_focusedId > 0 && _focusedId == _rootId) {
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
		} else if (_isNested || Organizer.State.RootId != 0 || Organizer.State.GroupBy != -1) {
			GetNode<Control>("%NewTask").Hide();
		}
	}

	private void CreateTaskNode() {
		var taskNode = _taskScene.Instantiate<ListTask>();
		FreeTaskNodes.Add(taskNode);
		_hiddenTasks.AddChild(taskNode);
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