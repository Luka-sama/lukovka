using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public partial class App : Control {
	public static readonly Dictionary<int, Task> Tasks = new();
	public static TaskView View { get; private set; }
	public static Rect2 Rect => _scrollContainer.GetGlobalRect();
	private const string ServerUrl = "https://enveltia.net/lukovka.php?key=t8B_JR_c_u3y0t6HSabj";
	private static Control _root;
	private static TaskDetails _taskDetails;
	private static ScrollContainer _scrollContainer;
	private static AcceptDialog _error;

	public override void _Ready() {
		View = GetNode<TaskView>("%View");
		_root = this;
		_taskDetails = GetNode<TaskDetails>("%TaskDetails");
		_scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		_error = GetNode<AcceptDialog>("%Error");
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
			GetTree().Quit();
		}
	}
	
	public static bool IsMobile() {
		return OS.GetName() == "Android";
	}

	public static void ShowError(string text, bool writeToConsole = false) {
		if (writeToConsole) {
			GD.PushError(text);
		}
		_error.DialogText = text;
		_error.Size = new Vector2I(400, 100);
		_error.PopupCentered();
	}

	public static void ShowDetails(Task task) {
		_taskDetails.ShowTask(task);
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
			ShowError("An error occurred in the HTTP request.", true);
		}
	}

	private static void HttpRequestCompleted(
		long result, long responseCode, string[] headers, byte[] body, HttpRequest httpRequest, bool states
	) {
		httpRequest.QueueFree();
		var data = body.GetStringFromUtf8();
		if (result != (long)HttpRequest.Result.Success || responseCode != 200) {
			ShowError(
				"An error occurred in the HTTP request. " +
				$"Result code is {result}, response code is {responseCode}, data is {data}.",
				true
			);
			return;
		}

		if (string.IsNullOrEmpty(data)) {
			return;
		}
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
				ShowError($"JSON parsing failed: {json}", true);
			}
		}
		if (!states) {
			foreach (var task in Tasks.Values.Where(task => Tasks.ContainsKey(task.Parent))) {
				Tasks[task.Parent].Children.Add(task);
			}
			View.Render();
		}
	}
}