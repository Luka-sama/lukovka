using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public enum Endpoint {Task, States, Recording}

public partial class App : Control {
	public static readonly Dictionary<int, Task> Tasks = new();
	public static TaskView View { get; private set; }
	public static Rect2 Rect => _scrollContainer.GetGlobalRect();
	private const string ServerUrl = "https://enveltia.net/lukovka.php?key=YGDOMvwzrPy87u2dfN0r";
	private const string RecordingUrl = "https://enveltia.net/pomodoro/pomodoro.php";
	private const string BackupDir = "user://backups";
	private static Control _root;
	private static ScrollContainer _scrollContainer;

	public override void _Ready() {
		View = GetNode<TaskView>("%View");
		_root = this;
		_scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		if (IsMobile()) {
			GetTree().Root.ContentScaleFactor = 2.25f;
		}

		Request(HttpClient.Method.Get, "");
		Request(HttpClient.Method.Get, "", Endpoint.States);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed()) {
			GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
		} else if (@event.IsActionPressed("quit")) {
			Organizer.Clear();
			GetTree().Quit();
		}
	}

	public static bool IsMobile() {
		return OS.GetName() == "Android";
	}

	public static void ScrollTo(Control control) {
		_scrollContainer.EnsureControlVisible(control);
	}

	public static void Request(HttpClient.Method method, string requestData, Endpoint endpoint = Endpoint.Task) {
		var httpRequest = new HttpRequest();
		_root.AddChild(httpRequest);
		httpRequest.RequestCompleted += (result, responseCode, headers, body) =>
			HttpRequestCompleted(result, responseCode, headers, body, httpRequest, endpoint);
		var url = ServerUrl + (endpoint == Endpoint.States ? "&states" : "");
		if (endpoint == Endpoint.Recording) {
			url = RecordingUrl;
		}
		if (httpRequest.Request(url, null, method, requestData) != Error.Ok) {
			ErrorDialog.Show("An error occurred in the HTTP request.", true);
		}
	}

	private static void HttpRequestCompleted(
		long result, long responseCode, string[] headers, byte[] body, HttpRequest httpRequest, Endpoint endpoint
	) {
		httpRequest.QueueFree();
		var data = body.GetStringFromUtf8();
		if (result != (long)HttpRequest.Result.Success || responseCode != 200 ||
		    (endpoint == Endpoint.Recording && data != "ok")) {
			ErrorDialog.Show(
				"An error occurred in the HTTP request. " +
				$"Result code is {result}, response code is {responseCode}, data is {data}.",
				true
			);
			return;
		}

		if (string.IsNullOrEmpty(data) || endpoint == Endpoint.Recording) {
			return;
		}
		Backup(data, endpoint == Endpoint.States);
		try {
			if (endpoint == Endpoint.States) {
				var allStates = JsonConvert.DeserializeObject<State[]>(data);
				foreach (var state in allStates) {
					Organizer.AddOrRestoreState(state);
				}
			} else {
				var allTasks = JsonConvert.DeserializeObject<Task[]>(data);
				foreach (var task in allTasks) {
					Tasks[task.Id] = task;
					Task.NextId = Mathf.Max(task.Id + 1, Task.NextId);
				}
			}
		} catch(Exception e) {
			Console.WriteLine(e);
			ErrorDialog.Show($"JSON parsing failed: {data}", true);
		}
		if (endpoint == Endpoint.Task) {
			foreach (var task in Tasks.Values.Where(task => Tasks.ContainsKey(task.Parent))) {
				Tasks[task.Parent].Children.Add(task);
			}
			View.Render();
		}
	}

	private static void Backup(string data, bool states) {
		var date = DateOnly.FromDateTime(DateTime.Now).ToString();
		var name = (states ? "states" : "tasks");
		var path = $"{BackupDir}/{date}_{name}.txt";
		if (IsMobile() || FileAccess.FileExists(path)) {
			return;
		}
		if (!DirAccess.DirExistsAbsolute(BackupDir)) {
			DirAccess.MakeDirAbsolute(BackupDir);
		}
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		file.StoreString(data);
	}
}
