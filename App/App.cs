using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public partial class App : Control {
	public static readonly Dictionary<int, Task> Tasks = new();
	public static TaskView View { get; private set; }
	public static Rect2 Rect => _scrollContainer.GetGlobalRect();
	private const string ServerUrl = "https://enveltia.net/lukovka.php?key=t8B_JR_c_u3y0t6HSabj";
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
		Request(HttpClient.Method.Get, "", true);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed()) {
			GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
		} else if (@event.IsActionPressed("quit")) {
			Organizer.Filter.Free();
			GetTree().Quit();
		}
	}
	
	public static bool IsMobile() {
		return OS.GetName() == "Android";
	}

	public static void ScrollTo(Control control) {
		_scrollContainer.EnsureControlVisible(control);
	}

	public static void Request(HttpClient.Method method, string requestData, bool states = false) {
		var httpRequest = new HttpRequest();
		_root.AddChild(httpRequest);
		httpRequest.RequestCompleted += (result, responseCode, headers, body) => 
			HttpRequestCompleted(result, responseCode, headers, body, httpRequest, states);
		var url = ServerUrl + (states ? "&states" : "");
		if (httpRequest.Request(url, null, method, requestData) != Error.Ok) {
			ErrorDialog.Show("An error occurred in the HTTP request.", true);
		}
	}

	private static void HttpRequestCompleted(
		long result, long responseCode, string[] headers, byte[] body, HttpRequest httpRequest, bool states
	) {
		httpRequest.QueueFree();
		var data = body.GetStringFromUtf8();
		if (result != (long)HttpRequest.Result.Success || responseCode != 200) {
			ErrorDialog.Show(
				"An error occurred in the HTTP request. " +
				$"Result code is {result}, response code is {responseCode}, data is {data}.",
				true
			);
			return;
		}

		if (string.IsNullOrEmpty(data)) {
			return;
		}
		Backup(data, states);
		foreach (var json in data.Split("\n")) {
			try {
				if (states) {
					var state = JsonConvert.DeserializeObject<State>(json);
					Organizer.AddOrRestoreState(state);
				} else {
					var task = JsonConvert.DeserializeObject<Task>(json);
					Tasks[task.Id] = task;
					Task.NextId = Mathf.Max(task.Id + 1, Task.NextId);
				}
			} catch {
				ErrorDialog.Show($"JSON parsing failed: {json}", true);
			}
		}
		if (!states) {
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