using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class TimeEntry {
	public DateTime Start;
	public DateTime End;
}

public class Task {
	public static int NextId { get; set; } = 1;
	[JsonIgnore] public bool Expanded;
	[JsonIgnore] public List<Task> Children = new();
	[JsonIgnore] public int Group;
	public int Id;
	[DefaultValue("")] public string Text = "";
	public DateTime Created;
	public DateTime Updated;
	public DateTime Completed;
	public int Parent;
	[DefaultValue("")] public string Description = "";
	public int Points;
	public int PointsDone;
	public bool Folder;
	public int RepeatingEvery;
	public int RepeatingInterval; // 0 - no repeating, 1 - repeating without date, 2 - day, 3 - week, 4 - month, 5 - year
	public bool RepeatingFromCompleted;
	public DateTime Date;
	public DateTime StartDate;
	public List<string> Tags;
	public int Priority;
	public double Order;
	public List<TimeEntry> TimeEntries = new();
	// ReSharper disable once UnusedMember.Global
	public static bool ShouldSerializeTimeEntries() {
		return false;
	}

	public static void Create(string text, int parent) {
		var datetime = CurrentTime();
		var task = new Task {
			Id = NextId,
			Text = text,
			Created = datetime,
			Updated = datetime,
			Parent = parent,
		};
		if (parent > 0) {
			App.Tasks[parent].Children.Add(task);
		}
		NextId++;
		task.Create();
	}

	public int CountChildren() {
		return Children.Sum(child => 1 + child.CountChildren());
	}

	public int CountPoints() {
		if (Children.Count < 1) {
			return (Points == 0 ? 1 : Points);
		}
		return Points + Children.Sum(child => child.CountPoints());
	}

	public int CountPointsDone() {
		var pointsDone = (Completed == DateTime.MinValue && PointsDone < Points ? PointsDone : Points);
		if (Children.Count < 1) {
			return (Completed != DateTime.MinValue && Points == 0 ? 1 : pointsDone);
		}
		return pointsDone + Children.Sum(child => child.CountPointsDone());
	}

	public TimeSpan CountRecorded() {
		var recorded = TimeSpan.Zero;
		foreach (var timeEntry in TimeEntries) {
			recorded += (timeEntry.End == DateTime.MinValue ? CurrentTime() : timeEntry.End) - timeEntry.Start;
		}

		return recorded;//Children.Aggregate(recorded, (total, child) => total + child.CountRecorded());
	}

	public void ToggleRecording() {
		if (IsRecording()) {
			StopRecording();
		} else {
			StartRecording();
		}
	}

	public bool IsRecording() {
		return TimeEntries.Any(timeEntry => timeEntry.End == DateTime.MinValue);
	}

	public void StartRecording() {
		App.Request(HttpClient.Method.Post, Id.ToString(), Endpoint.Recording);
		TimeEntries.Add(new TimeEntry {Start = CurrentTime()});
	}

	public void StopRecording() {
		App.Request(HttpClient.Method.Post, "", Endpoint.Recording);
		TimeEntries.Find(timeEntry => timeEntry.End == DateTime.MinValue).End = CurrentTime();
	}

	public void Complete() {
		Completed = (Completed == DateTime.MinValue ? CurrentTime() : DateTime.MinValue);
		Save();

		if (Completed == DateTime.MinValue || RepeatingInterval == 0) {
			return;
		}
		var newTask = Clone();
		newTask.Completed = DateTime.MinValue;
		newTask.StartDate = DateTime.MinValue;
		newTask.Date = DateTime.MinValue;

		if (StartDate != DateTime.MinValue && RepeatingInterval != 1) {
			newTask.StartDate = RepeatDate(RepeatingFromCompleted ? DateTime.Now.Date : StartDate);
		}
		if (Date != DateTime.MinValue && RepeatingInterval != 1) {
			newTask.Date = RepeatDate(RepeatingFromCompleted ? DateTime.Now.Date : Date);
		}

		newTask.Create();
	}

	public void Save() {
		Updated = CurrentTime();
		App.Tasks[Id] = this;
		App.View.Render();
		App.Request(HttpClient.Method.Post, Serialize());
	}

	public void Delete() {
		var tasksToDelete = GetChildrenIds(this);
		tasksToDelete.Add(Id);
		foreach (var taskId in tasksToDelete) {
			App.Tasks.Remove(taskId);
		}
		if (Parent > 0) {
			App.Tasks[Parent].Children.Remove(this);
		}
		App.View.Render();
		var idsAsString = string.Join("\n", tasksToDelete);
		App.Request(HttpClient.Method.Delete, idsAsString);
	}

	private void Create() {
		App.Tasks[Id] = this;
		App.View.Render();
		App.Request(HttpClient.Method.Put, Serialize());
	}

	private static Task Deserialize(string json) {
		return JsonConvert.DeserializeObject<Task>(json);
	}

	private string Serialize() {
		return JsonConvert.SerializeObject(this, new JsonSerializerSettings {
			DefaultValueHandling = DefaultValueHandling.Ignore,
		});
	}

	private Task Clone() {
		var task = Deserialize(Serialize());
		task.Id = NextId;
		NextId++;
		if (Parent > 0) {
			App.Tasks[Parent].Children.Add(task);
		}
		return task;
	}

	private static List<int> GetChildrenIds(Task parent) {
		var result = parent.Children.Select(task => task.Id).ToList();
		foreach (var child in parent.Children) {
			result.AddRange(GetChildrenIds(child));
		}
		return result;
	}

	private static DateTime CurrentTime() {
		return DateTime.Now.ToUniversalTime();
	}

	private DateTime RepeatDate(DateTime dateTime) {
		dateTime = dateTime.ToLocalTime();
		return (RepeatingInterval switch {
			2 => dateTime.AddDays(RepeatingEvery),
			3 => dateTime.AddDays(RepeatingEvery * 7),
			4 => dateTime.AddMonths(RepeatingEvery),
			5 => dateTime.AddYears(RepeatingEvery),
			_ => throw new ArgumentOutOfRangeException(),
		}).ToUniversalTime();
	}
}
