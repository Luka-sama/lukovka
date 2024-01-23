using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public partial class App : Control {
	public static readonly Dictionary<int, Task> Tasks = new();
	public static TaskView View { get; private set; }
	private const string ServerUrl = "https://enveltia.net/lukovka.php?key=t8B_JR_c_u3y0t6HSabj";
	private static Control _root;
	private static TaskDetails _taskDetails;
	private static ScrollContainer _scrollContainer;

	public override void _Ready() {
		View = GetNode<TaskView>("%View");
		_root = this;
		_taskDetails = GetNode<TaskDetails>("%TaskDetails");
		_scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		if (OS.GetName() == "Android") {
			GetTree().Root.ContentScaleFactor = 2f;
		}
		
		Request(HttpClient.Method.Get);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseButton click && click.IsPressed()) {
			GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
		} else if (@event.IsActionPressed("quit")) {
			GetTree().Quit();
		}
	}

	public static void ShowDetails(Task task) {
		_taskDetails.ShowTask(task);
	}

	public static void ScrollTo(Control control) {
		_scrollContainer.EnsureControlVisible(control);
	}

	public static void Request(HttpClient.Method method, string requestData = "") {
		var httpRequest = new HttpRequest();
		_root.AddChild(httpRequest);
		httpRequest.RequestCompleted += (result, responseCode, headers, body) => 
			HttpRequestCompleted(result, responseCode, headers, body, httpRequest);
		if (httpRequest.Request(ServerUrl, null, method, requestData) != Error.Ok) {
			GD.PushError("An error occurred in the HTTP request.");
		}
	}

	private static void HttpRequestCompleted(
		long result, long responseCode, string[] headers, byte[] body, HttpRequest httpRequest
	) {
		httpRequest.QueueFree();
		if (result != (long)HttpRequest.Result.Success || responseCode != 200) {
			GD.PushError("An error occurred in the HTTP request.");
			return;
		}

		var data = body.GetStringFromUtf8();
		if (string.IsNullOrEmpty(data)) {
			return;
		}
		foreach (var json in data.Split("\n")) {
			try {
				var task = JsonConvert.DeserializeObject<Task>(json);
				Tasks[task.Id] = task;
				Task.NextId = Mathf.Max(task.Id + 1, Task.NextId);
			} catch {
				GD.PushError("JSON parsing failed", json);
			}
		}
		View.Render();
	}
}